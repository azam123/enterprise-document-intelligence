using System.Text.Json;
using Azure.Messaging.ServiceBus;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;
using EnterpriseDocumentIntelligence.IngestionService.Application;

namespace EnterpriseDocumentIntelligence.IngestionService.Api;

/// <summary>
/// Creates Service Bus processors for ingestion subscriptions.
/// </summary>
public sealed class ServiceBusClientFactory(IConfiguration configuration)
{
    private readonly ServiceBusClient client =
        !string.IsNullOrWhiteSpace(configuration["ServiceBus:ConnectionString"])
            ? new ServiceBusClient(
                configuration["ServiceBus:ConnectionString"]!)
            : new ServiceBusClient(
                configuration["ServiceBus:FullyQualifiedNamespace"]!,
                new Azure.Identity.DefaultAzureCredential());

    /// <summary>
    /// Creates a processor with the ingestion concurrency settings.
    /// </summary>
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

/// <summary>
/// Consumes document-upload events, extracts text, and publishes ingestion results.
/// </summary>
public sealed class Worker(
    ServiceBusClientFactory factory,
    BlobIngestionService ingestion,
    IngestionApplication application,
    IMessagePublisher publisher,
    ILogger<Worker> logger) : BackgroundService
{
    /// <summary>
    /// Starts the document-event processor and keeps it alive until shutdown.
    /// </summary>
    protected override async Task ExecuteAsync(
        CancellationToken cancellationToken)
    {
        var processor = factory.Create(
            Topics.DocumentEvents,
            "ingestion-sub");

        processor.ProcessMessageAsync += HandleAsync;
        processor.ProcessErrorAsync += HandleErrorAsync;

        await processor.StartProcessingAsync(
            cancellationToken);

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

    /// <summary>
    /// Processes one document-upload event and records its ingestion lifecycle.
    /// </summary>
    private async Task HandleAsync(
        ProcessMessageEventArgs args)
    {
        DocumentUploaded? message = null;

        try
        {
            message = JsonSerializer.Deserialize<DocumentUploaded>(
                args.Message.Body.ToString(),
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            if (message is null)
            {
                throw new InvalidDataException(
                    "Invalid DocumentUploaded event.");
            }

            var job = await application.StartAsync(
                new StartIngestionCommand(
                    message.EventId,
                    message.DocumentId,
                    message.TenantId.ToString()),
                args.CancellationToken);

            if (job.Status is EnterpriseDocumentIntelligence.IngestionService.Domain.IngestionStatus.Completed)
            {
                await args.CompleteMessageAsync(args.Message);
                return;
            }

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

            await application.CompleteAsync(
                message.EventId,
                extractedUri,
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

            await args.CompleteMessageAsync(
                args.Message);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Ingestion failed for MessageId={MessageId}",
                args.Message.MessageId);

            if (message is not null)
            {
                try
                {
                    await application.FailAsync(
                        message.EventId,
                        exception.Message,
                        args.CancellationToken);
                }
                catch (Exception stateException)
                {
                    logger.LogError(
                        stateException,
                        "Failed to persist ingestion failure state for EventId={EventId}",
                        message.EventId);
                }
            }

            if (args.Message.DeliveryCount >= 5)
            {
                await args.DeadLetterMessageAsync(
                    args.Message,
                    "max-delivery",
                    exception.Message);
            }
            else
            {
                await args.AbandonMessageAsync(
                    args.Message);
            }
        }
    }

    /// <summary>
    /// Logs Service Bus errors that occur outside message processing.
    /// </summary>
    private Task HandleErrorAsync(
        ProcessErrorEventArgs args)
    {
        logger.LogError(
            args.Exception,
            "Ingestion Service Bus error Entity={Entity}",
            args.EntityPath);

        return Task.CompletedTask;
    }
}
