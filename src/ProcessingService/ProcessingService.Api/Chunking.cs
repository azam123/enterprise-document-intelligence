using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;
using System.Text.RegularExpressions;

namespace EnterpriseDocumentIntelligence.ProcessingService;

public sealed class SemanticChunker
{
    private static readonly Regex SentenceBoundary = new(@"(?<=[.!?])\s+", RegexOptions.Compiled);
    public IReadOnlyList<ChunkContract> Chunk(string text, int maxTokens = 350, int overlapTokens = 40)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<ChunkContract>();
        if (maxTokens < 32) throw new ArgumentOutOfRangeException(nameof(maxTokens));
        if (overlapTokens < 0 || overlapTokens >= maxTokens) throw new ArgumentOutOfRangeException(nameof(overlapTokens));

        var sentences = SentenceBoundary.Split(text.Replace("\r\n", "\n").Trim())
            .Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
        var chunks = new List<ChunkContract>();
        var current = new List<string>();
        var currentTokens = 0;

        foreach (var sentence in sentences)
        {
            var tokens = EstimateTokens(sentence);
            if (current.Count > 0 && currentTokens + tokens > maxTokens)
            {
                chunks.Add(Create(chunks.Count, current));
                var overlap = TakeTail(current, overlapTokens);
                current = overlap.ToList();
                currentTokens = current.Sum(EstimateTokens);
            }
            current.Add(sentence);
            currentTokens += tokens;

            if (tokens > maxTokens)
            {
                var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                current.RemoveAt(current.Count - 1);
                foreach (var batch in Batch(words, maxTokens))
                {
                    chunks.Add(Create(chunks.Count, batch));
                }
                current.Clear();
                currentTokens = 0;
            }
        }

        if (current.Count > 0) chunks.Add(Create(chunks.Count, current));
        return chunks;
    }

    public static int EstimateTokens(string text) =>
        Math.Max(1, (int)Math.Ceiling(text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length * 1.3));

    private static IEnumerable<string> TakeTail(IReadOnlyList<string> values, int tokens)
    {
        var result = new List<string>();
        var total = 0;
        for (var i = values.Count - 1; i >= 0 && total < tokens; i--)
        {
            result.Insert(0, values[i]);
            total += EstimateTokens(values[i]);
        }
        return result;
    }

    private static IEnumerable<IReadOnlyList<string>> Batch(IReadOnlyList<string> words, int maxTokens)
    {
        var batch = new List<string>();
        var tokens = 0;
        foreach (var word in words)
        {
            if (batch.Count > 0 && tokens + EstimateTokens(word) > maxTokens)
            {
                yield return batch;
                batch = [];
                tokens = 0;
            }
            batch.Add(word);
            tokens += EstimateTokens(word);
        }
        if (batch.Count > 0) yield return batch;
    }

    private static ChunkContract Create(int number, IEnumerable<string> parts)
    {
        var text = string.Join(" ", parts);
        return new ChunkContract(Guid.NewGuid(), number, text, EstimateTokens(text));
    }
}