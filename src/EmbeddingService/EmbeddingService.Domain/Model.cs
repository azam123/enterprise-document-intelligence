namespace EnterpriseDocumentIntelligence.EmbeddingService.Domain;

public sealed record EmbeddingVector
{
    public Guid DocumentId { get; init; }
    public int ChunkNumber { get; init; }
    public IReadOnlyList<float> Values { get; init; }
    public string Model { get; init; }

    public EmbeddingVector(Guid documentId, int chunkNumber, IReadOnlyList<float> values, string model)
    {
        if (documentId == Guid.Empty) throw new ArgumentException("Document id is required.", nameof(documentId));
        if (chunkNumber < 0) throw new ArgumentOutOfRangeException(nameof(chunkNumber));
        if (values is null || values.Count == 0) throw new ArgumentException("Embedding vector cannot be empty.", nameof(values));
        if (string.IsNullOrWhiteSpace(model)) throw new ArgumentException("Model is required.", nameof(model));
        DocumentId = documentId;
        ChunkNumber = chunkNumber;
        Values = values;
        Model = model;
    }
}
