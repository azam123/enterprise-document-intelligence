using EnterpriseDocumentIntelligence.SearchService.Application.Abstractions;
using EnterpriseDocumentIntelligence.SearchService.Domain;

namespace EnterpriseDocumentIntelligence.SearchService.Application;

public sealed class SearchService(
    IQueryEmbeddingService embeddingService,
    ISearchRepository searchRepository)
{
    public async Task<IReadOnlyList<SearchHit>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        var vector = await embeddingService.CreateEmbeddingAsync(
            query.Text,
            cancellationToken);

        return await searchRepository.SearchAsync(
            query,
            vector,
            cancellationToken);
    }
}