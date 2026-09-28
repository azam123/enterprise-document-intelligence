using System.Text.Json;
using Azure.Messaging.ServiceBus;
using EnterpriseDocumentIntelligence.BuildingBlocks.Domain;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;

public sealed class AuditServiceBusFactory(IConfiguration configuration)
{
    private readonly ServiceBusClient client =
        !string.IsNullOrWhiteSpace(
            configuration["ServiceBus:ConnectionString"])
            ? new ServiceBusClient(
                configuration["ServiceBus:ConnectionString"]!)
            : new ServiceBusClient(
                configuration["ServiceBus:FullyQualifiedNamespace"]!,
                new Azure.Identity.DefaultAzureCredential());

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

public sealed class AuditWorker(
    AuditServiceBusFactory factory,
    DocumentDbContext db,
    ILogger<AuditWorker> log,
    AuditEventProcessor processor) : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken cancellationToken)
    {
        var processor = factory.Create();

        processor.ProcessMessageAsync += Handle;
        processor.ProcessErrorAsync += Error;

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

    private async Task Handle(ProcessMessageEventArgs args)
    {
        try
        {
            var auditEvent =
                JsonSerializer.Deserialize<AuditRequested>(
                    args.Message.Body.ToString(),
                    new JsonSerializerOptions(
                        JsonSerializerDefaults.Web))
                ?? throw new InvalidDataException(
                    "Invalid audit event.");

            await processor.RecordAsync(auditEvent, args.CancellationToken);

            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception exception)
        {
            log.LogError(
                exception,
                "Audit event processing failed");

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
            "Audit Service Bus error");

        return Task.CompletedTask;
    }
}