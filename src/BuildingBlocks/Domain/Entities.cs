namespace EnterpriseDocumentIntelligence.BuildingBlocks.Domain;

/// <summary>
/// Base type for persisted domain entities.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Gets the unique identifier of the entity.
    /// </summary>
    public Guid Id { get; protected init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the UTC timestamp at which the entity was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the UTC timestamp at which the entity was last changed.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; protected set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Marks the entity as changed and updates its modification timestamp.
    /// </summary>
    protected void Touch()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Represents a document owned by a tenant.
/// </summary>
public sealed class Document : Entity
{
    private Document()
    {
    }

    /// <summary>
    /// Creates a new document.
    /// </summary>
    public Document(
        Guid tenantId,
        string name,
        string contentType,
        long size)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type is required.", nameof(contentType));
        }

        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be greater than zero.");
        }

        TenantId = tenantId;
        Name = name.Trim();
        ContentType = contentType.Trim();
        SizeBytes = size;
    }

    /// <summary>
    /// Gets the tenant that owns the document.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the document name.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets the MIME content type.
    /// </summary>
    public string ContentType { get; private set; } = null!;

    /// <summary>
    /// Gets the uploaded document size in bytes.
    /// </summary>
    public long SizeBytes { get; private set; }

    /// <summary>
    /// Gets the current document status.
    /// </summary>
    public string Status { get; private set; } = DocumentStatuses.Uploaded;

    /// <summary>
    /// Gets the identifier of the current document version.
    /// </summary>
    public Guid CurrentVersionId { get; private set; }

    /// <summary>
    /// Gets all versions belonging to the document.
    /// </summary>
    public List<DocumentVersion> Versions { get; private set; } = [];

    /// <summary>
    /// Gets all ACL entries belonging to the document.
    /// </summary>
    public List<DocumentAcl> Acls { get; private set; } = [];

    /// <summary>
    /// Makes the supplied version the current version.
    /// </summary>
    public void SetCurrentVersion(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Version is required.", nameof(id));
        }

        CurrentVersionId = id;
        Touch();
    }

    /// <summary>
    /// Marks the document as processing.
    /// </summary>
    public void MarkProcessing()
    {
        Status = DocumentStatuses.Processing;
        Touch();
    }

    /// <summary>
    /// Marks the document as ready.
    /// </summary>
    public void MarkReady()
    {
        Status = DocumentStatuses.Ready;
        Touch();
    }

    /// <summary>
    /// Marks the document as failed.
    /// </summary>
    public void MarkFailed()
    {
        Status = DocumentStatuses.Failed;
        Touch();
    }
}

/// <summary>
/// Common status values used by documents.
/// </summary>
public static class DocumentStatuses
{
    /// <summary>
    /// Document has been uploaded but processing has not completed.
    /// </summary>
    public const string Uploaded = "Uploaded";

    /// <summary>
    /// Document is currently being processed.
    /// </summary>
    public const string Processing = "Processing";

    /// <summary>
    /// Document processing completed successfully.
    /// </summary>
    public const string Ready = "Ready";

    /// <summary>
    /// Document processing failed.
    /// </summary>
    public const string Failed = "Failed";
}

/// <summary>
/// Represents a version of a document persisted in blob storage.
/// </summary>
public sealed class DocumentVersion : Entity
{
    private DocumentVersion()
    {
    }

    /// <summary>
    /// Creates a new document version.
    /// </summary>
    public DocumentVersion(
        Guid documentId,
        int versionNumber,
        string blobUri,
        string sha256,
        long sizeBytes)
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException("Document is required.", nameof(documentId));
        }

        if (versionNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(versionNumber));
        }

        if (string.IsNullOrWhiteSpace(blobUri))
        {
            throw new ArgumentException("Blob URI is required.", nameof(blobUri));
        }

        if (string.IsNullOrWhiteSpace(sha256))
        {
            throw new ArgumentException("SHA-256 hash is required.", nameof(sha256));
        }

        if (sizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        }

        DocumentId = documentId;
        VersionNumber = versionNumber;
        BlobUri = blobUri.Trim();
        Sha256 = sha256.Trim();
        SizeBytes = sizeBytes;
    }

    /// <summary>
    /// Gets the owning document identifier.
    /// </summary>
    public Guid DocumentId { get; private set; }

    /// <summary>
    /// Gets the sequential version number.
    /// </summary>
    public int VersionNumber { get; private set; }

    /// <summary>
    /// Gets the blob URI containing the version content.
    /// </summary>
    public string BlobUri { get; private set; } = null!;

    /// <summary>
    /// Gets the SHA-256 content hash.
    /// </summary>
    public string Sha256 { get; private set; } = null!;

    /// <summary>
    /// Gets the version size in bytes.
    /// </summary>
    public long SizeBytes { get; private set; }

    /// <summary>
    /// Gets the current processing status of this version.
    /// </summary>
    public string ProcessingStatus { get; private set; } = VersionProcessingStatuses.Pending;

    /// <summary>
    /// Gets the owning document navigation property.
    /// </summary>
    public Document Document { get; private set; } = null!;

    /// <summary>
    /// Updates the processing status.
    /// </summary>
    public void SetStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Status is required.", nameof(status));
        }

        ProcessingStatus = status.Trim();
        Touch();
    }
}

/// <summary>
/// Common processing statuses for document versions.
/// </summary>
public static class VersionProcessingStatuses
{
    /// <summary>
    /// Processing has not started.
    /// </summary>
    public const string Pending = "Pending";

    /// <summary>
    /// Processing is in progress.
    /// </summary>
    public const string Processing = "Processing";

    /// <summary>
    /// Processing completed successfully.
    /// </summary>
    public const string Completed = "Completed";

    /// <summary>
    /// Processing failed.
    /// </summary>
    public const string Failed = "Failed";
}

/// <summary>
/// Represents a searchable text chunk produced from a document version.
/// </summary>
public sealed class Chunk : Entity
{
    private Chunk()
    {
    }

    /// <summary>
    /// Creates a new text chunk.
    /// </summary>
    public Chunk(
        Guid documentVersionId,
        int chunkNumber,
        string text,
        int tokenCount)
    {
        if (documentVersionId == Guid.Empty)
        {
            throw new ArgumentException("Version is required.", nameof(documentVersionId));
        }

        if (chunkNumber < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkNumber));
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text is required.", nameof(text));
        }

        if (tokenCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tokenCount));
        }

        DocumentVersionId = documentVersionId;
        ChunkNumber = chunkNumber;
        Text = text.Trim();
        TokenCount = tokenCount;
    }

    /// <summary>
    /// Gets the parent document version identifier.
    /// </summary>
    public Guid DocumentVersionId { get; private set; }

    /// <summary>
    /// Gets the zero-based chunk number.
    /// </summary>
    public int ChunkNumber { get; private set; }

    /// <summary>
    /// Gets the normalized chunk text.
    /// </summary>
    public string Text { get; private set; } = null!;

    /// <summary>
    /// Gets the estimated token count.
    /// </summary>
    public int TokenCount { get; private set; }

    /// <summary>
    /// Gets the external vector identifier when one has been assigned.
    /// </summary>
    public string? VectorId { get; private set; }

    /// <summary>
    /// Associates an external vector identifier with the chunk.
    /// </summary>
    public void SetVectorId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Vector ID is required.", nameof(id));
        }

        VectorId = id.Trim();
        Touch();
    }
}

/// <summary>
/// Represents a document access-control entry.
/// </summary>
public sealed class DocumentAcl : Entity
{
    private DocumentAcl()
    {
    }

    /// <summary>
    /// Creates a document ACL entry.
    /// </summary>
    public DocumentAcl(
        Guid documentId,
        Guid principalId,
        string principalType,
        string permission)
    {
        if (documentId == Guid.Empty || principalId == Guid.Empty)
        {
            throw new ArgumentException("Document and principal are required.");
        }

        if (string.IsNullOrWhiteSpace(principalType))
        {
            throw new ArgumentException("ACL principal type is required.", nameof(principalType));
        }

        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new ArgumentException("ACL permission is required.", nameof(permission));
        }

        DocumentId = documentId;
        PrincipalId = principalId;
        PrincipalType = principalType.Trim();
        Permission = permission.Trim();
    }

    /// <summary>
    /// Gets the document identifier.
    /// </summary>
    public Guid DocumentId { get; private set; }

    /// <summary>
    /// Gets the identity or group identifier.
    /// </summary>
    public Guid PrincipalId { get; private set; }

    /// <summary>
    /// Gets the principal type.
    /// </summary>
    public string PrincipalType { get; private set; } = null!;

    /// <summary>
    /// Gets the permission assigned to the principal.
    /// </summary>
    public string Permission { get; private set; } = null!;
}

/// <summary>
/// Represents a processing job associated with a document version.
/// </summary>
public sealed class ProcessingJob : Entity
{
    private ProcessingJob()
    {
    }

    /// <summary>
    /// Creates a processing job.
    /// </summary>
    public ProcessingJob(Guid documentVersionId, string jobType)
    {
        if (documentVersionId == Guid.Empty)
        {
            throw new ArgumentException("Document version is required.", nameof(documentVersionId));
        }

        if (string.IsNullOrWhiteSpace(jobType))
        {
            throw new ArgumentException("Job type is required.", nameof(jobType));
        }

        DocumentVersionId = documentVersionId;
        JobType = jobType.Trim();
    }

    /// <summary>
    /// Gets the document version being processed.
    /// </summary>
    public Guid DocumentVersionId { get; private set; }

    /// <summary>
    /// Gets the job type.
    /// </summary>
    public string JobType { get; private set; } = null!;

    /// <summary>
    /// Gets the current job status.
    /// </summary>
    public string Status { get; private set; } = ProcessingJobStatuses.Queued;

    /// <summary>
    /// Gets the number of attempts made for the job.
    /// </summary>
    public int Attempts { get; private set; }

    /// <summary>
    /// Gets the most recent failure message.
    /// </summary>
    public string? Error { get; private set; }

    /// <summary>
    /// Marks the job as running and increments the attempt count.
    /// </summary>
    public void Start()
    {
        Status = ProcessingJobStatuses.Running;
        Attempts++;
        Error = null;
        Touch();
    }

    /// <summary>
    /// Marks the job as completed.
    /// </summary>
    public void Complete()
    {
        Status = ProcessingJobStatuses.Completed;
        Touch();
    }

    /// <summary>
    /// Marks the job as failed and records the error.
    /// </summary>
    public void Fail(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Error is required.", nameof(error));
        }

        Status = ProcessingJobStatuses.Failed;
        Error = error.Trim();
        Touch();
    }
}

/// <summary>
/// Common processing-job states.
/// </summary>
public static class ProcessingJobStatuses
{
    /// <summary>
    /// Job has been queued.
    /// </summary>
    public const string Queued = "Queued";

    /// <summary>
    /// Job is currently running.
    /// </summary>
    public const string Running = "Running";

    /// <summary>
    /// Job completed successfully.
    /// </summary>
    public const string Completed = "Completed";

    /// <summary>
    /// Job failed.
    /// </summary>
    public const string Failed = "Failed";
}

/// <summary>
/// Represents an auditable domain event persisted by AuditService.
/// </summary>
public sealed class AuditEvent : Entity
{
    private AuditEvent()
    {
    }

    /// <summary>
    /// Creates an audit event.
    /// </summary>
    public AuditEvent(
        Guid tenantId,
        Guid? actorId,
        string action,
        string resourceType,
        Guid? resourceId,
        string outcome,
        string? correlationId,
        string? metadataJson,
        Guid? eventId = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant is required.", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Audit action is required.", nameof(action));
        }

        if (string.IsNullOrWhiteSpace(resourceType))
        {
            throw new ArgumentException("Audit resource type is required.", nameof(resourceType));
        }

        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("Audit outcome is required.", nameof(outcome));
        }

        if (eventId.HasValue && eventId.Value == Guid.Empty)
        {
            throw new ArgumentException("Event ID cannot be empty.", nameof(eventId));
        }

        if (eventId.HasValue)
        {
            Id = eventId.Value;
        }

        TenantId = tenantId;
        ActorId = actorId;
        Action = action.Trim();
        ResourceType = resourceType.Trim();
        ResourceId = resourceId;
        Outcome = outcome.Trim();
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        MetadataJson = metadataJson;
    }

    /// <summary>
    /// Gets the tenant associated with the audit event.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the actor that initiated the event when available.
    /// </summary>
    public Guid? ActorId { get; private set; }

    /// <summary>
    /// Gets the audit action.
    /// </summary>
    public string Action { get; private set; } = null!;

    /// <summary>
    /// Gets the resource type affected by the action.
    /// </summary>
    public string ResourceType { get; private set; } = null!;

    /// <summary>
    /// Gets the resource identifier when available.
    /// </summary>
    public Guid? ResourceId { get; private set; }

    /// <summary>
    /// Gets the outcome of the action.
    /// </summary>
    public string Outcome { get; private set; } = null!;

    /// <summary>
    /// Gets the distributed correlation identifier.
    /// </summary>
    public string? CorrelationId { get; private set; }

    /// <summary>
    /// Gets optional JSON metadata associated with the audit event.
    /// </summary>
    public string? MetadataJson { get; private set; }
}