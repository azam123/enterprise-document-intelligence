using EnterpriseDocumentIntelligence.IngestionService.Domain;

namespace EnterpriseDocumentIntelligence.IngestionService.Application;

/// <summary>
/// Provides persistence operations for ingestion jobs.
/// </summary>
public interface IIngestionJobStore
{
    /// <summary>
    /// Finds an ingestion job by identifier.
    /// </summary>
    Task<IngestionJob?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the current lifecycle state of an ingestion job.
    /// </summary>
    Task SaveAsync(
        IngestionJob job,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Identifies a document version that should be ingested.
/// </summary>
public sealed record StartIngestionCommand(
    Guid JobId,
    Guid DocumentId,
    string TenantId);

/// <summary>
/// Coordinates ingestion job lifecycle transitions.
/// </summary>
public sealed class IngestionApplication(
    IIngestionJobStore store)
{
    /// <summary>
    /// Starts a new job, or returns the existing job for an already delivered event.
    /// This provides message-level idempotency for the worker.
    /// </summary>
    public async Task<IngestionJob> StartAsync(
        StartIngestionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var existing = await store.GetAsync(
            command.JobId,
            cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var job = new IngestionJob(
            command.JobId,
            command.DocumentId,
            command.TenantId);

        job.Start();

        await store.SaveAsync(
            job,
            cancellationToken);

        return job;
    }

    /// <summary>
    /// Marks a job as successfully extracted.
    /// </summary>
    public async Task CompleteAsync(
        Guid jobId,
        string location,
        CancellationToken cancellationToken = default)
    {
        var job = await GetRequiredAsync(
            jobId,
            cancellationToken);

        job.Complete(location);

        await store.SaveAsync(
            job,
            cancellationToken);
    }

    /// <summary>
    /// Marks a job as failed and records the failure reason.
    /// </summary>
    public async Task FailAsync(
        Guid jobId,
        string error,
        CancellationToken cancellationToken = default)
    {
        var job = await GetRequiredAsync(
            jobId,
            cancellationToken);

        job.Fail(error);

        await store.SaveAsync(
            job,
            cancellationToken);
    }

    /// <summary>
    /// Loads a job and raises a clear error when it cannot be found.
    /// </summary>
    private async Task<IngestionJob> GetRequiredAsync(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        return await store.GetAsync(
                   jobId,
                   cancellationToken)
               ?? throw new KeyNotFoundException(
                   $"Ingestion job {jobId} was not found.");
    }
}
