namespace EnterpriseDocumentIntelligence.IndexingService.Domain;

public sealed record SearchIndexRecord
{
    public string Id { get; init; }
    public Guid DocumentId { get; init; }
    public string TenantId { get; init; }
    public string Content { get; init; }
    public IReadOnlyList<float> Vector { get; init; }
    public IReadOnlyCollection<string> AllowedPrincipals { get; init; }

    public SearchIndexRecord(string id, Guid documentId, string tenantId, string content, IReadOnlyList<float> vector, IReadOnlyCollection<string> allowedPrincipals)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Index id is required.", nameof(id));
        if (documentId == Guid.Empty) throw new ArgumentException("Document id is required.", nameof(documentId));
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("Tenant is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("Content is required.", nameof(content));
        if (vector is null || vector.Count == 0) throw new ArgumentException("Vector is required.", nameof(vector));
        Id = id;
        DocumentId = documentId;
        TenantId = tenantId;
        Content = content;
        Vector = vector;
        AllowedPrincipals = allowedPrincipals;
    }
}
