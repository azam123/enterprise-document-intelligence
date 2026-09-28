using System.Text.Json;
using Azure.Messaging.ServiceBus;
using EnterpriseDocumentIntelligence.AuditService.Application;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;

namespace EnterpriseDocumentIntelligence.AuditService.Api;

/// <summary>
/// Creates the Service Bus processor used to consume audit events.
/// </summary>
public sealed class AuditServiceBusFactory(IConfiguration configuration)
{
    private readonly ServiceBusClient client =
        !string.IsNullOrWhiteSpace(configuration["ServiceBus:ConnectionString"])
            ? new ServiceBusClient(configuration["ServiceBus:ConnectionString"]!)
            : new ServiceBusClient(
                configuration["ServiceBus:FullyQualifiedNamespace"]!,
                new Azure.Identity.DefaultAzureCredential());

    /// <summary>
    /// Creates the configured audit subscription processor.
    /// </summary>
    public ServiceBusProcessor Create()
    {
        return client.CreateProcessor(
            Topics.AuditEvents,
            "audit-sub",
            new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = 4,
                PrefetchCount = 20,
                AutoCompleteMessages = false
            });
    }
}

/// <summary>
/// Consumes audit events from Service Bus and persists them through the Application layer.
/// </summary>
public sealed class AuditWorker(
    EnterpriseDocumentIntelligence.AuditService.Application.AuditEventProcessor auditEventProcessor,
    ILogger<AuditWorker> logger,
    AuditServiceBusFactory factory) : BackgroundService
{
    /// <summary>
    /// Starts the Service Bus processor and keeps it active until shutdown.
    /// </summary>
    protected override async Task ExecuteAsync(
        CancellationToken cancellationToken)
    {
        var processor = factory.Create();

        processor.ProcessMessageAsync += HandleMessageAsync;
        processor.ProcessErrorAsync += HandleErrorAsync;

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

    /// <summary>
    /// Deserializes, validates, persists, and completes one audit message.
    /// </summary>
    private async Task HandleMessageAsync(
        ProcessMessageEventArgs args)
    {
        try
        {
            var auditEvent =
                JsonSerializer.Deserialize<AuditRequested>(
                    args.Message.Body.ToString(),
                    new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new InvalidDataException(
                    "Invalid audit event.");

            await auditEventProcessor.RecordAsync(
                auditEvent,
                args.CancellationToken);

            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Audit event processing failed.");

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

    /// <summary>
    /// Logs Service Bus processor errors that occur outside message handling.
    /// </summary>
    private Task HandleErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(
            args.Exception,
            "Audit Service Bus error.");

        return Task.CompletedTask;
    }
}
