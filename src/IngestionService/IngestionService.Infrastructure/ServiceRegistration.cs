using System.Collections.Concurrent;
using EnterpriseDocumentIntelligence.IngestionService.Application;
using EnterpriseDocumentIntelligence.IngestionService.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseDocumentIntelligence.IngestionService.Infrastructure;

/// <summary>
/// Thread-safe in-memory job store used until durable ingestion-job persistence is introduced.
/// </summary>
public sealed class InMemoryIngestionJobStore : IIngestionJobStore
{
    private readonly ConcurrentDictionary<Guid, IngestionJob> jobs = new();

    /// <summary>
    /// Finds an ingestion job by identifier.
    /// </summary>
    public Task<IngestionJob?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        jobs.TryGetValue(
            id,
            out var job);

        return Task.FromResult(job);
    }

    /// <summary>
    /// Stores the current job instance.
    /// </summary>
    public Task SaveAsync(
        IngestionJob job,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        jobs[job.Id] = job;

        return Task.CompletedTask;
    }
}

/// <summary>
/// Registers IngestionService application and infrastructure dependencies.
/// </summary>
public static class IngestionServiceInfrastructure
{
    /// <summary>
    /// Registers the job store and application coordinator.
    /// </summary>
    public static IServiceCollection AddIngestionApplication(
        this IServiceCollection services)
    {
        services.AddSingleton<IIngestionJobStore, InMemoryIngestionJobStore>();
        services.AddSingleton<IngestionApplication>();

        return services;
    }
}
