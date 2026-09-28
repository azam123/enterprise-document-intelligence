namespace EnterpriseDocumentIntelligence.SearchService.Domain;

public sealed record SearchQuery(
    string TenantId,
    string Query,
    int TopK = 10)
{
    public SearchQuery
    {
        if (string.IsNullOrWhiteSpace(TenantId))
        {
            throw new ArgumentException("Tenant is required.");
        }

        if (string.IsNullOrWhiteSpace(Query))
        {
            throw new ArgumentException("Query is required.");
        }

        if (TopK is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(TopK));
        }
    }
}

public sealed record SearchHit(
    string Id,
    Guid DocumentId,
    string Content,
    double Score,
    IReadOnlyCollection<string> Citations);
