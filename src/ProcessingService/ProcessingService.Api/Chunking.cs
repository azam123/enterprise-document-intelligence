using Azure.Storage.Blobs;
using System.Text.RegularExpressions;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;

public sealed class SemanticChunker
{
    public IReadOnlyList<ChunkContract> Chunk(string text, int maxTokens = 500, int overlapTokens = 50)
    {
        if (maxTokens <= 0) throw new ArgumentOutOfRangeException(nameof(maxTokens));
        if (overlapTokens < 0 || overlapTokens >= maxTokens) throw new ArgumentOutOfRangeException(nameof(overlapTokens));
        if (string.IsNullOrWhiteSpace(text)) return [];
        var normalized = Regex.Replace(text.Replace("\r\n", "\n"), @"[ \t]+", " ").Trim();
        var units = Regex.Split(normalized, @"(?<=[.!?])\s+|\n{2,}")
            .Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
        var result = new List<ChunkContract>();
        var current = new List<string>();
        var currentTokens = 0;
        foreach (var unit in units)
        {
            var unitTokens = CountTokens(unit);
            if (unitTokens > maxTokens)
            {
                Flush(current, result);
                current.Clear(); currentTokens = 0;
                foreach (var sentence in SplitOversized(unit, maxTokens))
                    result.Add(new ChunkContract(Guid.NewGuid(), result.Count, sentence, CountTokens(sentence)));
                continue;
            }
            if (currentTokens + unitTokens > maxTokens && current.Count > 0)
            {
                Flush(current, result);
                var overlap = TakeTail(current, overlapTokens);
                current = overlap;
                currentTokens = current.Sum(CountTokens);
            }
            current.Add(unit); currentTokens += unitTokens;
        }
        Flush(current, result);
        return result;
    }

    private static void Flush(List<string> current, List<ChunkContract> result)
    {
        if (current.Count == 0) return;
        var text = string.Join(" ", current).Trim();
        result.Add(new ChunkContract(Guid.NewGuid(), result.Count, text, CountTokens(text)));
    }

    private static List<string> TakeTail(List<string> source, int target)
    {
        if (target == 0) return [];
        var output = new List<string>(); var total = 0;
        for (var i = source.Count - 1; i >= 0; i--)
        {
            var n = CountTokens(source[i]);
            if (total + n > target && output.Count > 0) break;
            output.Insert(0, source[i]); total += n;
            if (total >= target) break;
        }
        return output;
    }

    private static IEnumerable<string> SplitOversized(string value, int maxTokens)
    {
        var words = Regex.Split(value.Trim(), @"\s+").Where(x => x.Length > 0).ToArray();
        for (var i = 0; i < words.Length; i += maxTokens)
            yield return string.Join(" ", words.Skip(i).Take(maxTokens));
    }

    public static int CountTokens(string value) => Regex.Split(value.Trim(), @"\s+").Count(x => x.Length > 0);
}

public sealed class ExtractedTextReader(BlobServiceClient blobs)
{
    public async Task<string> ReadAsync(string uri, CancellationToken ct)
    {
        var parsed = new Uri(uri);
        var containerName = parsed.Segments.Skip(1).FirstOrDefault()?.Trim('/') ?? throw new InvalidDataException("Invalid blob URI.");
        var blobName = string.Join("", parsed.Segments.Skip(2));
        var blob = blobs.GetBlobContainerClient(containerName).GetBlobClient(Uri.UnescapeDataString(blobName));
        var response = await blob.DownloadContentAsync(ct);
        return response.Value.Content.ToString();
    }
}
