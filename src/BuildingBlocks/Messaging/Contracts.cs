namespace EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;

/// <summary>
/// Well-known Azure Service Bus topics used by the document intelligence pipeline.
/// </summary>
public static class Topics
{
    /// <summary>
    /// Topic carrying document lifecycle events.
    /// </summary>
    public const string DocumentEvents = "document-events";

    /// <summary>
    /// Topic carrying ingestion lifecycle events.
    /// </summary>
    public const string IngestionEvents = "ingestion-events";

    /// <summary>
    /// Topic carrying processing lifecycle events.
    /// </summary>
    public const string ProcessingEvents = "processing-events";

    /// <summary>
    /// Topic carrying embedding events.
    /// </summary>
    public const string EmbeddingEvents = "embedding-events";

    /// <summary>
    /// Topic carrying indexing lifecycle events.
    /// </summary>
    public const string IndexingEvents = "indexing-events";

    /// <summary>
    /// Topic carrying audit requests.
    /// </summary>
    public const string AuditEvents = "audit-events";
}

/// <summary>
/// Raised when a document version has been uploaded and is ready for ingestion.
/// </summary>
public sealed record DocumentUploaded(
    Guid EventId,
    Guid TenantId,
    Guid DocumentId,
    Guid VersionId,
    string BlobUri,
    string ContentType,
    long SizeBytes,
    DateTimeOffset OccurredAt,
    string CorrelationId,
    IReadOnlyList<Guid>? AllowedPrincipalIds = null);

/// <summary>
/// Raised after text extraction completes for a document version.
/// </summary>
public sealed record DocumentIngestionCompleted(
    Guid EventId,
    Guid TenantId,
    Guid DocumentId,
    Guid VersionId,
    string ExtractedTextUri,
    DateTimeOffset OccurredAt,
    string CorrelationId,
    IReadOnlyList<Guid>? AllowedPrincipalIds = null);

/// <summary>
/// Describes an individual text chunk produced by ProcessingService.
/// </summary>
public sealed record ChunkContract(
    Guid ChunkId,
    int Number,
    string Text,
    int TokenCount);

/// <summary>
/// Raised when document text has been split into searchable chunks.
/// </summary>
public sealed record DocumentProcessed(
    Guid EventId,
    Guid TenantId,
    Guid DocumentId,
    Guid VersionId,
    IReadOnlyList<ChunkContract> Chunks,
    DateTimeOffset OccurredAt,
    string CorrelationId,
    IReadOnlyList<Guid>? AllowedPrincipalIds = null);

/// <summary>
/// Contains a generated embedding for one processed chunk.
/// </summary>
public sealed record EmbeddedChunk(
    Guid ChunkId,
    int Number,
    string Text,
    int TokenCount,
    float[] Embedding,
    IReadOnlyList<Guid>? AllowedPrincipalIds = null);

/// <summary>
/// Raised when embeddings have been generated for a document version.
/// </summary>
public sealed record EmbeddingsCreated(
    Guid EventId,
    Guid TenantId,
    Guid DocumentId,
    Guid VersionId,
    IReadOnlyList<EmbeddedChunk> Chunks,
    DateTimeOffset OccurredAt,
    string CorrelationId);

/// <summary>
/// Raised when document chunks have been submitted to the search index.
/// </summary>
public sealed record IndexingCompleted(
    Guid EventId,
    Guid TenantId,
    Guid DocumentId,
    Guid VersionId,
    int IndexedChunks,
    DateTimeOffset OccurredAt,
    string CorrelationId);

/// <summary>
/// Raised when document access-control entries change.
/// </summary>
public sealed record DocumentAclChanged(
    Guid EventId,
    Guid TenantId,
    Guid DocumentId,
    IReadOnlyList<Guid> AllowedPrincipalIds,
    DateTimeOffset OccurredAt,
    string CorrelationId);

/// <summary>
/// Requests creation of an audit record.
/// </summary>
public sealed record AuditRequested(
    Guid EventId,
    Guid TenantId,
    Guid? ActorId,
    string Action,
    string ResourceType,
    Guid? ResourceId,
    string Outcome,
    string? CorrelationId,
    string? MetadataJson);