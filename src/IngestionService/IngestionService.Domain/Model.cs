namespace EnterpriseDocumentIntelligence.IngestionService.Domain;

public enum IngestionStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public sealed class IngestionJob
{
    public IngestionJob(
        Guid id,
        Guid documentId,
        string tenantId)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Job id is required.",
                nameof(id));
        }

        if (documentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Document id is required.",
                nameof(documentId));
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException(
                "Tenant is required.",
                nameof(tenantId));
        }

        Id = id;
        DocumentId = documentId;
        TenantId = tenantId;
        Status = IngestionStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }

    public Guid DocumentId { get; }

    public string TenantId { get; }

    public IngestionStatus Status { get; private set; }

    public string? ExtractedTextLocation { get; private set; }

    public string? Error { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public void Start()
    {
        if (Status is not IngestionStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only pending jobs can start.");
        }

        Status = IngestionStatus.Processing;
        Error = null;
    }

    public void Complete(string location)
    {
        if (Status is not IngestionStatus.Processing)
        {
            throw new InvalidOperationException(
                "Job is not processing.");
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            throw new ArgumentException(
                "Extracted text location is required.",
                nameof(location));
        }

        ExtractedTextLocation = location;
        Status = IngestionStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Fail(string error)
    {
        if (Status is IngestionStatus.Completed)
        {
            throw new InvalidOperationException(
                "Completed job cannot fail.");
        }

        Error = string.IsNullOrWhiteSpace(error)
            ? "Unknown ingestion error"
            : error;

        Status = IngestionStatus.Failed;
    }
}
