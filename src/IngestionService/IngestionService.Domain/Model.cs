namespace EnterpriseDocumentIntelligence.IngestionService.Domain;

/// <summary>
/// Represents the lifecycle state of a document ingestion job.
/// </summary>
public enum IngestionStatus
{
    /// <summary>Job has been created but processing has not started.</summary>
    Pending,

    /// <summary>Document extraction is in progress.</summary>
    Processing,

    /// <summary>Extraction completed and the resulting location was stored.</summary>
    Completed,

    /// <summary>Extraction failed and the error is recorded.</summary>
    Failed
}

/// <summary>
/// Tracks ingestion state for one document version.
/// </summary>
public sealed class IngestionJob
{
    /// <summary>
    /// Creates a new pending ingestion job.
    /// </summary>
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

    /// <summary>Gets the ingestion job identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the source document identifier.</summary>
    public Guid DocumentId { get; }

    /// <summary>Gets the tenant owning the document.</summary>
    public string TenantId { get; }

    /// <summary>Gets the current lifecycle status.</summary>
    public IngestionStatus Status { get; private set; }

    /// <summary>Gets the URI containing extracted text when processing succeeds.</summary>
    public string? ExtractedTextLocation { get; private set; }

    /// <summary>Gets the failure reason when processing fails.</summary>
    public string? Error { get; private set; }

    /// <summary>Gets the time at which the job was created.</summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>Gets the completion timestamp when processing succeeds.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// Transitions a pending job into processing.
    /// </summary>
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

    /// <summary>
    /// Completes a processing job with the extracted text location.
    /// </summary>
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

    /// <summary>
    /// Marks a job as failed and records a safe failure message.
    /// </summary>
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
