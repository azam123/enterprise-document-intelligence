using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;
using System.Text.Json;

namespace EnterpriseDocumentIntelligence.ProcessingService;

public sealed class ServiceBusClientFactory(IConfiguration configuration)
{
    private readonly ServiceBusClient client = !string.IsNullOrWhiteSpace(configuration["ServiceBus:ConnectionString"])
        ? new ServiceBusClient(configuration["ServiceBus:ConnectionString"]!)
        : new ServiceBusClient(configuration["ServiceBus:FullyQualifiedNamespace"]!, new Azure.Identity.DefaultAzureCredential());

    public ServiceBusProcessor Create(string topic, string subscription) =>
        client.CreateProcessor(topic, subscription, new ServiceBusProcessorOptions { MaxConcurrentCalls = 8, PrefetchCount = 50, AutoCompleteMessages = false });
}

public sealed class Worker(
    ServiceBusClientFactory factory,
    BlobServiceClient blobs,
    SemanticChunker chunker,
    IMessagePublisher publisher,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var processor = factory.Create(Topics.IngestionEvents, "processing-sub");
        processor.ProcessMessageAsync += HandleAsync;
        processor.ProcessErrorAsync += OnErrorAsync;
        await processor.StartProcessingAsync(stoppingToken);
        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally { await processor.StopProcessingAsync(); await processor.DisposeAsync(); }
    }

    private async Task HandleAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var message = JsonSerializer.Deserialize<DocumentIngestionCompleted>(
                args.Message.Body.ToString(), new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new InvalidDataException("Invalid DocumentIngestionCompleted event.");

            var blob = new BlobClient(new Uri(message.ExtractedTextUri), new Azure.Identity.DefaultAzureCredential());
            var response = await blob.DownloadStreamingAsync(cancellationToken: args.CancellationToken);
            using var reader = new StreamReader(response.Value.Content);
            var text = await reader.ReadToEndAsync(args.CancellationToken);
            var chunks = chunker.Chunk(text);

            await publisher.PublishAsync(
                Topics.ProcessingEvents,
                new DocumentProcessed(Guid.NewGuid(), message.TenantId, message.DocumentId, message.VersionId,
                    chunks, DateTimeOffset.UtcNow, message.CorrelationId),
                message.CorrelationId,
                args.CancellationToken);

            await args.CompleteMessageAsync(args.Message);
            logger.LogInformation("Processed document {DocumentId} into {Count} chunks", message.DocumentId, chunks.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Processing failed MessageId={MessageId}", args.Message.MessageId);
            if (args.Message.DeliveryCount >= 5) await args.DeadLetterMessageAsync(args.Message, "max-delivery", ex.Message);
            else await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception, "Processing Service Bus error Entity={Entity}", args.EntityPath);
        return Task.CompletedTask;
    }
}