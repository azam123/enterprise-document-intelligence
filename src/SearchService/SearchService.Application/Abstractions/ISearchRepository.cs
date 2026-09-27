using EnterpriseDocumentIntelligence.SearchService.Domain;

namespace EnterpriseDocumentIntelligence.SearchService.Application.Abstractions;

public interface ISearchRepository
{
    Task<IReadOnlyList<SearchHit>> SearchAsync(
        SearchQuery query,
        ReadOnlyMemory<float> vector,
        CancellationToken cancellationToken);
}