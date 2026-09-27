using Azure.Messaging.ServiceBus;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;
using System.Text.Json;

public sealed class ServiceBusClientFactory(IConfiguration configuration)
{
    private readonly ServiceBusClient client =
        !string.IsNullOrWhiteSpace(configuration["ServiceBus:ConnectionString"])
            ? new ServiceBusClient(configuration["ServiceBus:ConnectionString"]!)
            : new ServiceBusClient(
                configuration["ServiceBus:FullyQualifiedNamespace"]!,
                new Azure.Identity.DefaultAzureCredential());

    public ServiceBusProcessor Create(
        string topic,
        string subscription)
    {
        return client.CreateProcessor(
            topic,
            subscription,
            new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = 8,
                PrefetchCount = 50,
                AutoCompleteMessages = false
            });
    }
}

public sealed class Worker(
    ServiceBusClientFactory factory,
    BlobIngestionService ingestion,
    IMessagePublisher publisher,
    ILogger<Worker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken cancellationToken)
    {
        var processor = factory.Create(
            Topics.DocumentEvents,
            "ingestion-sub");

        processor.ProcessMessageAsync += Handle;
        processor.ProcessErrorAsync += Error;

        await processor.StartProcessingAsync(cancellationToken);

        try
        {
            await Task.Delay(
                Timeout.Infinite,
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            await processor.StopProcessingAsync();
            await processor.DisposeAsync();
        }
    }

    private async Task Handle(ProcessMessageEventArgs args)
    {
        try
        {
            var message =
                JsonSerializer.Deserialize<DocumentUploaded>(
                    args.Message.Body.ToString(),
                    new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new InvalidDataException(
                    "Invalid DocumentUploaded event.");

            var fileName = Uri.UnescapeDataString(
                Path.GetFileName(
                    new Uri(message.BlobUri).AbsolutePath));

            var extractedUri = await ingestion.ExtractAndStoreAsync(
                message.BlobUri,
                message.TenantId,
                message.DocumentId,
                message.VersionId,
                message.ContentType,
                fileName,
                args.CancellationToken);

            await publisher.PublishAsync(
                Topics.IngestionEvents,
                new DocumentIngestionCompleted(
                    Guid.NewGuid(),
                    message.TenantId,
                    message.DocumentId,
                    message.VersionId,
                    extractedUri,
                    DateTimeOffset.UtcNow,
                    message.CorrelationId,
                    message.AllowedPrincipalIds),
                message.CorrelationId,
                args.CancellationToken);

            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception exception)
        {
            log.LogError(
                exception,
                "Ingestion failed for MessageId={MessageId}",
                args.Message.MessageId);

            if (args.Message.DeliveryCount >= 5)
            {
                await args.DeadLetterMessageAsync(
                    args.Message,
                    "max-delivery",
                    exception.Message);
            }
            else
            {
                await args.AbandonMessageAsync(args.Message);
            }
        }
    }

    private Task Error(ProcessErrorEventArgs args)
    {
        log.LogError(
            args.Exception,
            "Ingestion Service Bus error Entity={Entity}",
            args.EntityPath);

        return Task.CompletedTask;
    }
}