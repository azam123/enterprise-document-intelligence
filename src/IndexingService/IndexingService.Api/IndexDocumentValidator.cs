public sealed class IndexDocumentValidator
{
    public void Validate(
        Guid tenantId,
        Guid documentId,
        Guid chunkId,
        int chunkNumber,
        string text,
        IReadOnlyCollection<Guid> allowedPrincipalIds,
        IReadOnlyList<float> vector)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (documentId == Guid.Empty) throw new ArgumentException("DocumentId is required.", nameof(documentId));
        if (chunkId == Guid.Empty) throw new ArgumentException("ChunkId is required.", nameof(chunkId));
        if (chunkNumber < 0) throw new ArgumentOutOfRangeException(nameof(chunkNumber));
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text is required.", nameof(text));
        ArgumentNullException.ThrowIfNull(allowedPrincipalIds);
        if (vector is null || vector.Count == 0) throw new ArgumentException("Vector is required.", nameof(vector));
        if (vector.Any(float.IsNaN) || vector.Any(float.IsInfinity)) throw new ArgumentException("Vector contains invalid values.", nameof(vector));
        if (allowedPrincipalIds.Any(x => x == Guid.Empty)) throw new ArgumentException("ACL principal IDs must be valid GUIDs.", nameof(allowedPrincipalIds));
    }
}