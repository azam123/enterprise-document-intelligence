using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using EnterpriseDocumentIntelligence.BuildingBlocks.Security;

public sealed record SearchRequest(
    string Query,
    int TopK = 10);

public sealed record SearchResult(
    string DocumentId,
    string Text,
    double Score,
    string Citation);

public sealed class SearchApplication(
    SearchClient client,
    AzureOpenAiEmbeddingClient embeddings,
    ICurrentUser user,
    ILogger<SearchApplication> log)
{
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchRequest request,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        EnsureTenant();

        var vector = await embeddings.EmbedAsync(
            request.Query,
            cancellationToken);

        var options = CreateSearchOptions(
            request.TopK,
            user.TenantId,
            user.UserId);

        var response = await client.SearchAsync<SearchDocument>(
            request.Query,
            options,
            cancellationToken);

        var results = new List<SearchResult>();

        await foreach (var result in response.Value.GetResultsAsync())
        {
            results.Add(
                new SearchResult(
                    result.Document.GetString("DocumentId") ?? string.Empty,
                    result.Document.GetString("Text") ?? string.Empty,
                    result.Score ?? 0d,
                    result.Document.GetString("Citation") ?? string.Empty));
        }

        log.LogInformation(
            "Hybrid retrieval Tenant={TenantId} Results={Count}",
            user.TenantId,
            results.Count);

        return results;
    }

    private static void ValidateRequest(SearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            throw new ArgumentException("Query is required.");
        }

        if (request.TopK is < 1 or > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(request.TopK));
        }
    }

    private void EnsureTenant()
    {
        if (user.TenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException();
        }
    }

    private static SearchOptions CreateSearchOptions(
        int topK,
        Guid tenantId,
        Guid userId)
    {
        var options = new SearchOptions
        {
            Size = topK,
            Filter = SearchFilterBuilder.Build(
                tenantId,
                userId)
        };

        options.Select.Add("DocumentId");
        options.Select.Add("Text");
        options.Select.Add("Citation");

        options.VectorSearch = new()
        {
            Queries =
            {
                new VectorizedQuery
                {
                    KNearestNeighborsCount = topK,
                    Fields = { "ContentVector" }
                }
            }
        };

        return options;
    }
}