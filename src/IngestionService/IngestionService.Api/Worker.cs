using Azure.Messaging.ServiceBus;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;
using System.Text.Json;

namespace EnterpriseDocumentIntelligence.IngestionService;

public sealed class ServiceBusClientFactory(IConfiguration configuration)
{
    private readonly ServiceBusClient client = !string.IsNullOrWhiteSpace(configuration["ServiceBus:ConnectionString"])
        ? new ServiceBusClient(configuration["ServiceBus:ConnectionString"]!)
        : new ServiceBusClient(configuration["ServiceBus:FullyQualifiedNamespace"]!, new Azure.Identity.DefaultAzureCredential());

    public ServiceBusProcessor Create(string topic, string subscription) =>
        client.CreateProcessor(topic, subscription, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 8,
            PrefetchCount = 50,
            AutoCompleteMessages = false
        });
}

public sealed class Worker(
    ServiceBusClientFactory factory,
    BlobIngestionService ingestion,
    IMessagePublisher publisher,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var processor = factory.Create(Topics.DocumentEvents, "ingestion-sub");
        processor.ProcessMessageAsync += HandleAsync;
        processor.ProcessErrorAsync += OnErrorAsync;
        await processor.StartProcessingAsync(stoppingToken);

        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally
        {
            await processor.StopProcessingAsync();
            await processor.DisposeAsync();
        }
    }

    private async Task HandleAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var message = JsonSerializer.Deserialize<DocumentUploaded>(
                args.Message.Body.ToString(), new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new InvalidDataException("Invalid DocumentUploaded event.");

            var extractedUri = await ingestion.ExtractAndStoreAsync(
                message.BlobUri, message.ContentType, message.TenantId, message.DocumentId, message.VersionId, args.CancellationToken);

            await publisher.PublishAsync(
                Topics.IngestionEvents,
                new DocumentIngestionCompleted(Guid.NewGuid(), message.TenantId, message.DocumentId, message.VersionId,
                    extractedUri.ToString(), DateTimeOffset.UtcNow, message.CorrelationId),
                message.CorrelationId,
                args.CancellationToken);

            await args.CompleteMessageAsync(args.Message);
            logger.LogInformation("Ingested document {DocumentId}", message.DocumentId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ingestion failed MessageId={MessageId}", args.Message.MessageId);
            if (args.Message.DeliveryCount >= 5)
                await args.DeadLetterMessageAsync(args.Message, "max-delivery", ex.Message);
            else
                await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception, "Ingestion Service Bus error Entity={Entity}", args.EntityPath);
        return Task.CompletedTask;
    }
}