using Azure.Messaging.ServiceBus;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;
using System.Text.Json;

namespace EnterpriseDocumentIntelligence.EmbeddingService;

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
    IMessagePublisher publisher,
    AzureOpenAiEmbeddingClient embeddings,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var processor = factory.Create(Topics.ProcessingEvents, "embedding-sub");
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
            var message = JsonSerializer.Deserialize<DocumentProcessed>(
                args.Message.Body.ToString(), new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new InvalidDataException("Invalid DocumentProcessed event.");

            var output = new List<EmbeddedChunk>(message.Chunks.Count);
            foreach (var chunk in message.Chunks)
                output.Add(new EmbeddedChunk(chunk.ChunkId, chunk.Number, chunk.Text, chunk.TokenCount,
                    await embeddings.EmbedAsync(chunk.Text, args.CancellationToken)));

            await publisher.PublishAsync(Topics.EmbeddingEvents,
                new EmbeddingsCreated(Guid.NewGuid(), message.TenantId, message.DocumentId, message.VersionId,
                    output, DateTimeOffset.UtcNow, message.CorrelationId, message.AllowedPrincipalIds),
                message.CorrelationId, args.CancellationToken);

            await args.CompleteMessageAsync(args.Message);
            logger.LogInformation("Embedded {Count} chunks Document={DocumentId}", output.Count, message.DocumentId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Embedding failed MessageId={MessageId}", args.Message.MessageId);
            if (args.Message.DeliveryCount >= 5) await args.DeadLetterMessageAsync(args.Message, "max-delivery", ex.Message);
            else await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception, "Embedding Service Bus error Entity={Entity}", args.EntityPath);
        return Task.CompletedTask;
    }
}