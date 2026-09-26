using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using EnterpriseDocumentIntelligence.BuildingBlocks.Security;

namespace EnterpriseDocumentIntelligence.SearchService;

public sealed record SearchRequest(string Query, int TopK = 10);
public sealed record SearchResult(string DocumentId, string VersionId, string Text, double Score, string Citation);

public interface ISearchApplication
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(SearchRequest request, CancellationToken ct);
}

public sealed class SearchApplication(SearchClient client, AzureOpenAiEmbeddingClient embeddings, ICurrentUser user, ILogger<SearchApplication> logger) : ISearchApplication
{
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(SearchRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Query)) throw new ArgumentException("Query is required.");
        if (request.TopK is < 1 or > 50) throw new ArgumentException("TopK must be between 1 and 50.");
        if (user.TenantId == Guid.Empty) throw new UnauthorizedAccessException();

        var vector = await embeddings.EmbedAsync(request.Query, ct);
        var options = new SearchOptions
        {
            Size = request.TopK,
            Filter = BuildFilter(user.TenantId, user.UserId)
        };
        options.Select.Add("DocumentId");
        options.Select.Add("VersionId");
        options.Select.Add("Text");
        options.Select.Add("Citation");
        options.VectorSearch = new VectorSearchOptions();
        var vectorQuery = new VectorizedQuery(vector)
        {
            KNearestNeighborsCount = request.TopK
        };
        vectorQuery.Fields.Add("ContentVector");
        options.VectorSearch.Queries.Add(vectorQuery);

        var response = await client.SearchAsync<SearchDocument>(request.Query, options, ct);
        var results = new List<SearchResult>();
        await foreach (var result in response.Value.GetResultsAsync())
        {
            var document = result.Document;
            results.Add(new SearchResult(
                document.GetString("DocumentId") ?? string.Empty,
                document.GetString("VersionId") ?? string.Empty,
                document.GetString("Text") ?? string.Empty,
                result.Score ?? 0d,
                document.GetString("Citation") ?? string.Empty));
        }

        logger.LogInformation("Hybrid search Tenant={TenantId} User={UserId} Results={Count}", user.TenantId, user.UserId, results.Count);
        return results;
    }

    internal static string BuildFilter(Guid tenantId, Guid userId) =>
        $"TenantId eq '{tenantId}' and (not AllowedPrincipalIds/any() or AllowedPrincipalIds/any(p: p eq '{userId}'))";
}