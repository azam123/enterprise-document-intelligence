using EnterpriseDocumentIntelligence.IndexingService.Domain;

namespace EnterpriseDocumentIntelligence.IndexingService.Application;

public interface IIndexStore
{
    Task UpsertAsync(
        SearchIndexRecord record,
        CancellationToken cancellationToken = default);

    Task<SearchIndexRecord?> GetAsync(
        string id,
        CancellationToken cancellationToken = default);
}

public sealed class IndexingApplication(IIndexStore store)
{
    public Task UpsertAsync(
        SearchIndexRecord record,
        CancellationToken cancellationToken = default)
    {
        return store.UpsertAsync(record, cancellationToken);
    }

    public Task<SearchIndexRecord?> GetAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        return store.GetAsync(id, cancellationToken);
    }
}
