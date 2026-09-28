public sealed record EmbeddingWorkItem(Guid ChunkId, int Number, string Text, int TokenCount);
public sealed record EmbeddingWorkResult(Guid ChunkId, int Number, string Text, int TokenCount, float[] Vector);
public sealed class EmbeddingBatchProcessor
{
    public async Task<IReadOnlyList<EmbeddingWorkResult>> ProcessAsync(
        IReadOnlyList<EmbeddingWorkItem> items,
        Func<string, CancellationToken, Task<float[]>> embed,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(embed);
        if (items.Count == 0) return [];
        var results = new List<EmbeddingWorkResult>(items.Count);
        foreach (var item in items)
        {
            if (item.ChunkId == Guid.Empty) throw new ArgumentException("ChunkId is required.", nameof(items));
            if (item.Number < 0) throw new ArgumentOutOfRangeException(nameof(items));
            if (string.IsNullOrWhiteSpace(item.Text)) throw new ArgumentException("Text is required.", nameof(items));
            if (item.TokenCount <= 0) throw new ArgumentOutOfRangeException(nameof(items));
            var vector = await embed(item.Text, cancellationToken);
            if (vector.Length == 0) throw new InvalidOperationException("Embedding provider returned an empty vector.");
            results.Add(new(item.ChunkId, item.Number, item.Text, item.TokenCount, vector));
        }
        return results;
    }
}