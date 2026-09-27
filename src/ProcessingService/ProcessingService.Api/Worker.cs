using Azure.Messaging.ServiceBus;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;
using System.Text.Json;

public sealed class ServiceBusClientFactory(IConfiguration configuration)
{
    private readonly ServiceBusClient client = !string.IsNullOrWhiteSpace(configuration["ServiceBus:ConnectionString"])
        ? new(configuration["ServiceBus:ConnectionString"]!)
        : new(configuration["ServiceBus:FullyQualifiedNamespace"]!, new Azure.Identity.DefaultAzureCredential());
    public ServiceBusProcessor Create(string topic, string subscription) => client.CreateProcessor(topic, subscription, new ServiceBusProcessorOptions { MaxConcurrentCalls = 8, PrefetchCount = 50, AutoCompleteMessages = false });
}
public sealed class Worker(ServiceBusClientFactory factory, ExtractedTextReader reader, SemanticChunker chunker, IMessagePublisher publisher, ILogger<Worker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var processor = factory.Create(Topics.IngestionEvents, "processing-sub");
        processor.ProcessMessageAsync += Handle; processor.ProcessErrorAsync += Error;
        await processor.StartProcessingAsync(ct);
        try { await Task.Delay(Timeout.Infinite, ct); } catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        finally { await processor.StopProcessingAsync(); await processor.DisposeAsync(); }
    }
    private async Task Handle(ProcessMessageEventArgs args)
    {
        try
        {
            var message = JsonSerializer.Deserialize<DocumentIngestionCompleted>(args.Message.Body.ToString(), new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? throw new InvalidDataException("Invalid ingestion event.");
            var text = await reader.ReadAsync(message.ExtractedTextUri, args.CancellationToken);
            var chunks = chunker.Chunk(text);
            if (chunks.Count == 0) throw new InvalidDataException("Extracted document contains no text.");
            await publisher.PublishAsync(Topics.ProcessingEvents, new DocumentProcessed(Guid.NewGuid(), message.TenantId, message.DocumentId, message.VersionId, chunks, DateTimeOffset.UtcNow, message.CorrelationId, message.AllowedPrincipalIds), message.CorrelationId, args.CancellationToken);
            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Processing failed for MessageId={MessageId}", args.Message.MessageId);
            if (args.Message.DeliveryCount >= 5) await args.DeadLetterMessageAsync(args.Message, "max-delivery", ex.Message); else await args.AbandonMessageAsync(args.Message);
        }
    }
    private Task Error(ProcessErrorEventArgs args) { log.LogError(args.Exception, "Processing Service Bus error Entity={Entity}", args.EntityPath); return Task.CompletedTask; }
}
