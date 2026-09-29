namespace EnterpriseDocumentIntelligence.SearchService.Domain;

public sealed record SearchQuery
{
    public string TenantId { get; init; }
    public string Query { get; init; }
    public int TopK { get; init; }

    public SearchQuery(string tenantId, string query, int topK = 10)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(query)) throw new ArgumentException("Query is required.", nameof(query));
        if (topK is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(topK));
        TenantId = tenantId;
        Query = query;
        TopK = topK;
    }
}

public sealed record SearchHit(
    string Id,
    Guid DocumentId,
    string Content,
    double Score,
    IReadOnlyCollection<string> Citations);
