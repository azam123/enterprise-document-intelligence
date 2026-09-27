using Azure.Messaging.ServiceBus;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;
using System.Text.Json;

public sealed class ServiceBusClientFactory(IConfiguration configuration)
{
    private readonly ServiceBusClient client=!string.IsNullOrWhiteSpace(configuration["ServiceBus:ConnectionString"])?new(configuration["ServiceBus:ConnectionString"]!):new(configuration["ServiceBus:FullyQualifiedNamespace"]!,new Azure.Identity.DefaultAzureCredential());
    public ServiceBusProcessor Create(string topic,string subscription)=>client.CreateProcessor(topic,subscription,new ServiceBusProcessorOptions{MaxConcurrentCalls=4,PrefetchCount=20,AutoCompleteMessages=false});
}
public sealed class Worker(ServiceBusClientFactory factory,IMessagePublisher publisher,AzureOpenAiEmbeddingClient embeddings,ILogger<Worker> log):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var p=factory.Create(Topics.ProcessingEvents,"embedding-sub");p.ProcessMessageAsync+=Handle;p.ProcessErrorAsync+=Error;await p.StartProcessingAsync(ct);
        try{await Task.Delay(Timeout.Infinite,ct);}catch(OperationCanceledException) when(ct.IsCancellationRequested){}finally{await p.StopProcessingAsync();await p.DisposeAsync();}
    }
    private async Task Handle(ProcessMessageEventArgs a)
    {
        try{
            var m=JsonSerializer.Deserialize<DocumentProcessed>(a.Message.Body.ToString(),new JsonSerializerOptions(JsonSerializerDefaults.Web))??throw new InvalidDataException("Invalid processed event.");
            var output=new List<EmbeddedChunk>(m.Chunks.Count);
            foreach(var c in m.Chunks) output.Add(new(c.ChunkId,c.Number,c.Text,c.TokenCount,await embeddings.EmbedAsync(c.Text,a.CancellationToken),m.AllowedPrincipalIds));
            await publisher.PublishAsync(Topics.EmbeddingEvents,new EmbeddingsCreated(Guid.NewGuid(),m.TenantId,m.DocumentId,m.VersionId,output,DateTimeOffset.UtcNow,m.CorrelationId),m.CorrelationId,a.CancellationToken);
            await a.CompleteMessageAsync(a.Message);
        }catch(Exception ex){log.LogError(ex,"Embedding failed");if(a.Message.DeliveryCount>=5)await a.DeadLetterMessageAsync(a.Message,"max-delivery",ex.Message);else await a.AbandonMessageAsync(a.Message);}
    }
    private Task Error(ProcessErrorEventArgs e){log.LogError(e.Exception,"Embedding Service Bus error");return Task.CompletedTask;}
}
