namespace EnterpriseDocumentIntelligence.SearchService.Domain;

public sealed record SearchQuery(
    Guid TenantId,
    Guid UserId,
    string Text,
    int TopK)
{
    public static SearchQuery Create(
        Guid tenantId,
        Guid userId,
        string text,
        int topK)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant is required.", nameof(tenantId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Query is required.", nameof(text));
        }

        if (topK is < 1 or > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(topK), "TopK must be between 1 and 50.");
        }

        return new SearchQuery(tenantId, userId, text.Trim(), topK);
    }
}