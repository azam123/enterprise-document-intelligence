using Azure.Messaging.ServiceBus;
using EnterpriseDocumentIntelligence.BuildingBlocks.Domain;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;
using System.Text.Json;

namespace EnterpriseDocumentIntelligence.AuditService;

public sealed class ServiceBusClientFactory(IConfiguration configuration)
{
    private readonly ServiceBusClient client = !string.IsNullOrWhiteSpace(configuration["ServiceBus:ConnectionString"])
        ? new ServiceBusClient(configuration["ServiceBus:ConnectionString"]!)
        : new ServiceBusClient(configuration["ServiceBus:FullyQualifiedNamespace"]!, new Azure.Identity.DefaultAzureCredential());
    public ServiceBusProcessor Create(string topic, string subscription) =>
        client.CreateProcessor(topic, subscription, new ServiceBusProcessorOptions { MaxConcurrentCalls = 8, PrefetchCount = 50, AutoCompleteMessages = false });
}

public sealed class AuditWorker(ServiceBusClientFactory factory, DocumentDbContext db, ILogger<AuditWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var processor = factory.Create(Topics.AuditEvents, "audit-sub");
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
            var message = JsonSerializer.Deserialize<AuditRequested>(args.Message.Body.ToString(), new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new InvalidDataException("Invalid AuditRequested event.");
            if (!await db.AuditEvents.AnyAsync(x => x.Id == message.EventId, args.CancellationToken))
            {
                db.AuditEvents.Add(new AuditEvent(message.TenantId, message.ActorId, message.Action, message.ResourceType,
                    message.ResourceId, message.Outcome, message.CorrelationId, message.MetadataJson, message.EventId));
                await db.SaveChangesAsync(args.CancellationToken);
            }
            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Audit event processing failed");
            if (args.Message.DeliveryCount >= 5) await args.DeadLetterMessageAsync(args.Message, "max-delivery", ex.Message);
            else await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args) { logger.LogError(args.Exception, "Audit Service Bus error"); return Task.CompletedTask; }
}