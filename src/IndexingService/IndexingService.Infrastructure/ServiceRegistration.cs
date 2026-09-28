using System.Collections.Concurrent;

using EnterpriseDocumentIntelligence.IndexingService.Application;
using EnterpriseDocumentIntelligence.IndexingService.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseDocumentIntelligence.IndexingService.Infrastructure;

public sealed class InMemoryIndexStore : IIndexStore
{
    private readonly ConcurrentDictionary<string, SearchIndexRecord> _items = new();

    public Task UpsertAsync(
        SearchIndexRecord record,
        CancellationToken cancellationToken = default)
    {
        _items[record.Id] = record;

        return Task.CompletedTask;
    }

    public Task<SearchIndexRecord?> GetAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _items.TryGetValue(id, out var record)
                ? record
                : null);
    }
}

public static class IndexingServiceInfrastructure
{
    public static IServiceCollection AddIndexingApplication(
        this IServiceCollection services)
    {
        services.AddSingleton<IIndexStore, InMemoryIndexStore>();
        services.AddScoped<IndexingApplication>();

        return services;
    }
}
