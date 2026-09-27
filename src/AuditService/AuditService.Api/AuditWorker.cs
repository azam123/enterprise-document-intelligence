using Azure.Messaging.ServiceBus;
using EnterpriseDocumentIntelligence.BuildingBlocks.Domain;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public sealed class AuditServiceBusFactory(IConfiguration c)
{
 private readonly ServiceBusClient client=!string.IsNullOrWhiteSpace(c["ServiceBus:ConnectionString"])?new(c["ServiceBus:ConnectionString"]!):new(c["ServiceBus:FullyQualifiedNamespace"]!,new Azure.Identity.DefaultAzureCredential());
 public ServiceBusProcessor Create()=>client.CreateProcessor(Topics.AuditEvents,"audit-sub",new ServiceBusProcessorOptions{MaxConcurrentCalls=4,PrefetchCount=20,AutoCompleteMessages=false});
}
public sealed class AuditWorker(AuditServiceBusFactory factory,DocumentDbContext db,ILogger<AuditWorker> log):BackgroundService
{
 protected override async Task ExecuteAsync(CancellationToken ct){var p=factory.Create();p.ProcessMessageAsync+=Handle;p.ProcessErrorAsync+=Error;await p.StartProcessingAsync(ct);try{await Task.Delay(Timeout.Infinite,ct);}catch(OperationCanceledException) when(ct.IsCancellationRequested){}finally{await p.StopProcessingAsync();await p.DisposeAsync();}}
 private async Task Handle(ProcessMessageEventArgs a)
 {
  try{var e=JsonSerializer.Deserialize<AuditRequested>(a.Message.Body.ToString(),new JsonSerializerOptions(JsonSerializerDefaults.Web))??throw new InvalidDataException("Invalid audit event.");
   if(!await db.AuditEvents.AnyAsync(x=>x.Id==e.EventId,a.CancellationToken)){db.AuditEvents.Add(new AuditEvent(e.TenantId,e.ActorId,e.Action,e.ResourceType,e.ResourceId,e.Outcome,e.CorrelationId,e.MetadataJson,e.EventId));await db.SaveChangesAsync(a.CancellationToken);}
   await a.CompleteMessageAsync(a.Message);
  }catch(Exception ex){log.LogError(ex,"Audit event processing failed");if(a.Message.DeliveryCount>=5)await a.DeadLetterMessageAsync(a.Message,"max-delivery",ex.Message);else await a.AbandonMessageAsync(a.Message);}
 }
 private Task Error(ProcessErrorEventArgs e){log.LogError(e.Exception,"Audit Service Bus error");return Task.CompletedTask;}
}