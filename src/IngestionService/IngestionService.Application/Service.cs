using EnterpriseDocumentIntelligence.IngestionService.Domain;
namespace EnterpriseDocumentIntelligence.IngestionService.Application;

public interface IIngestionJobStore { Task<IngestionJob?> GetAsync(Guid id,CancellationToken ct=default); Task SaveAsync(IngestionJob job,CancellationToken ct=default); }
public sealed record StartIngestionCommand(Guid JobId, Guid DocumentId, string TenantId);
public sealed class IngestionApplication(IIngestionJobStore store)
{
 public async Task<IngestionJob> StartAsync(StartIngestionCommand command,CancellationToken ct=default){ var existing=await store.GetAsync(command.JobId,ct); if(existing is not null) return existing; var job=new IngestionJob(command.JobId,command.DocumentId,command.TenantId); job.Start(); await store.SaveAsync(job,ct); return job; }
 public async Task CompleteAsync(Guid jobId,string location,CancellationToken ct=default){ var job=await store.GetAsync(jobId,ct)??throw new KeyNotFoundException($"Ingestion job {jobId} was not found."); job.Complete(location); await store.SaveAsync(job,ct); }
 public async Task FailAsync(Guid jobId,string error,CancellationToken ct=default){ var job=await store.GetAsync(jobId,ct)??throw new KeyNotFoundException($"Ingestion job {jobId} was not found."); job.Fail(error); await store.SaveAsync(job,ct); }
}