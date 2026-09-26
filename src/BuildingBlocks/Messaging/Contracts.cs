namespace EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;

public static class Topics
{
    public const string DocumentEvents = "document-events";
    public const string IngestionEvents = "ingestion-events";
    public const string ProcessingEvents = "processing-events";
    public const string EmbeddingEvents = "embedding-events";
    public const string IndexingEvents = "indexing-events";
    public const string AuditEvents = "audit-events";
}

public sealed record DocumentUploaded(
    Guid EventId, Guid TenantId, Guid DocumentId, Guid VersionId, string BlobUri,
    string ContentType, long SizeBytes, DateTimeOffset OccurredAt, string CorrelationId);

public sealed record DocumentIngestionCompleted(
    Guid EventId, Guid TenantId, Guid DocumentId, Guid VersionId, string ExtractedTextUri,
    DateTimeOffset OccurredAt, string CorrelationId);

public sealed record ChunkContract(Guid ChunkId, int Number, string Text, int TokenCount);

public sealed record DocumentProcessed(
    Guid EventId, Guid TenantId, Guid DocumentId, Guid VersionId,
    IReadOnlyList<ChunkContract> Chunks, DateTimeOffset OccurredAt, string CorrelationId,
    IReadOnlyList<Guid>? AllowedPrincipalIds = null);

public sealed record EmbeddedChunk(
    Guid ChunkId, int Number, string Text, int TokenCount, float[] Embedding);

public sealed record EmbeddingsCreated(
    Guid EventId, Guid TenantId, Guid DocumentId, Guid VersionId,
    IReadOnlyList<EmbeddedChunk> Chunks, DateTimeOffset OccurredAt, string CorrelationId,
    IReadOnlyList<Guid>? AllowedPrincipalIds = null);

public sealed record IndexingCompleted(
    Guid EventId, Guid TenantId, Guid DocumentId, Guid VersionId,
    int IndexedChunks, DateTimeOffset OccurredAt, string CorrelationId);

public sealed record AuditRequested(
    Guid EventId, Guid TenantId, Guid? ActorId, string Action, string ResourceType,
    Guid? ResourceId, string Outcome, string? CorrelationId, string? MetadataJson);
