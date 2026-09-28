using EnterpriseDocumentIntelligence.EmbeddingService.Domain;

namespace EnterpriseDocumentIntelligence.EmbeddingService.Application;

public interface IEmbeddingStore
{
    Task SaveAsync(
        EmbeddingVector vector,
        CancellationToken cancellationToken = default);

    Task<EmbeddingVector?> GetAsync(
        Guid documentId,
        int chunkNumber,
        CancellationToken cancellationToken = default);
}

public interface IEmbeddingGenerator
{
    Task<IReadOnlyList<float>> GenerateAsync(
        string text,
        CancellationToken cancellationToken = default);
}

public sealed class EmbeddingApplication(
    IEmbeddingGenerator generator,
    IEmbeddingStore store)
{
    public async Task<EmbeddingVector> GenerateAsync(
        Guid documentId,
        int chunkNumber,
        string text,
        string model,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text is required.", nameof(text));
        }

        var values = await generator.GenerateAsync(text, cancellationToken);

        var vector = new EmbeddingVector(
            documentId,
            chunkNumber,
            values,
            model);

        await store.SaveAsync(vector, cancellationToken);

        return vector;
    }
}
