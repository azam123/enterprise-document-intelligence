using Azure.Messaging.ServiceBus;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;
using System.Text.Json;

namespace EnterpriseDocumentIntelligence.IndexingService;

public sealed class ServiceBusClientFactory(IConfiguration configuration)
{
    private readonly ServiceBusClient client = !string.IsNullOrWhiteSpace(configuration["ServiceBus:ConnectionString"])
        ? new ServiceBusClient(configuration["ServiceBus:ConnectionString"]!)
        : new ServiceBusClient(configuration["ServiceBus:FullyQualifiedNamespace"]!, new Azure.Identity.DefaultAzureCredential());

    public ServiceBusProcessor Create(string topic, string subscription) =>
        client.CreateProcessor(topic, subscription, new ServiceBusProcessorOptions { MaxConcurrentCalls = 8, PrefetchCount = 50, AutoCompleteMessages = false });
}

public sealed class Worker(ServiceBusClientFactory factory, SearchClient index, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var processor = factory.Create(Topics.EmbeddingEvents, "indexing-sub");
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
            var message = JsonSerializer.Deserialize<EmbeddingsCreated>(
                args.Message.Body.ToString(), new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new InvalidDataException("Invalid EmbeddingsCreated event.");

            var batch = IndexDocumentsBatch<SearchDocument>();
            foreach (var chunk in message.Chunks)
                batch.Actions.Add(IndexDocumentsAction.Upload(
                    IndexDocumentFactory.Create(message.TenantId, message.DocumentId, message.VersionId, chunk, message.AllowedPrincipalIds)));

            if (batch.Actions.Count > 0)
                await index.IndexDocumentsAsync(batch, cancellationToken: args.CancellationToken);

            await args.CompleteMessageAsync(args.Message);
            logger.LogInformation("Indexed {Count} chunks Document={DocumentId}", message.Chunks.Count, message.DocumentId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Indexing failed MessageId={MessageId}", args.Message.MessageId);
            if (args.Message.DeliveryCount >= 5) await args.DeadLetterMessageAsync(args.Message, "max-delivery", ex.Message);
            else await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception, "Indexing Service Bus error Entity={Entity}", args.EntityPath);
        return Task.CompletedTask;
    }
}