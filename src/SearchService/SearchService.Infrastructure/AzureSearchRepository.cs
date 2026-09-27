using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using EnterpriseDocumentIntelligence.SearchService.Application.Abstractions;
using EnterpriseDocumentIntelligence.SearchService.Domain;

namespace EnterpriseDocumentIntelligence.SearchService.Infrastructure;

public sealed class AzureSearchRepository(SearchClient searchClient) : ISearchRepository
{
    public async Task<IReadOnlyList<SearchHit>> SearchAsync(
        SearchQuery query,
        ReadOnlyMemory<float> vector,
        CancellationToken cancellationToken)
    {
        var options = new SearchOptions
        {
            Size = query.TopK,
            Filter = BuildFilter(query.TenantId, query.UserId)
        };

        options.Select.Add("DocumentId");
        options.Select.Add("Text");
        options.Select.Add("Citation");

        options.VectorSearch.Queries.Add(
            new VectorizedQuery(vector)
            {
                KNearestNeighborsCount = query.TopK
            });

        options.VectorSearch.Queries[0].Fields.Add("ContentVector");

        var response = await searchClient.SearchAsync<SearchDocument>(
            query.Text,
            options,
            cancellationToken);

        var hits = new List<SearchHit>();

        await foreach (var result in response.Value.GetResultsAsync())
        {
            hits.Add(
                new SearchHit(
                    result.Document.GetString("DocumentId") ?? string.Empty,
                    result.Document.GetString("Text") ?? string.Empty,
                    result.Score ?? 0d,
                    result.Document.GetString("Citation") ?? string.Empty));
        }

        return hits;
    }

    private static string BuildFilter(Guid tenantId, Guid userId)
    {
        var tenant = Escape(tenantId.ToString());
        var user = Escape(userId.ToString());

        return
            $"TenantId eq '{tenant}' and " +
            $"(not AllowedPrincipalIds/any() or " +
            $"AllowedPrincipalIds/any(p: p eq '{user}'))";
    }

    private static string Escape(string value) =>
        value.Replace("'", "''", StringComparison.Ordinal);
}