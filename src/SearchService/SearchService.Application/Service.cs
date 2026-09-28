using EnterpriseDocumentIntelligence.SearchService.Domain;

namespace EnterpriseDocumentIntelligence.SearchService.Application;

public interface ISearchProvider
{
    Task<IReadOnlyList<SearchHit>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken = default);
}

public sealed class SearchApplicationService(ISearchProvider provider)
{
    public Task<IReadOnlyList<SearchHit>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken = default)
    {
        return provider.SearchAsync(query, cancellationToken);
    }
}
