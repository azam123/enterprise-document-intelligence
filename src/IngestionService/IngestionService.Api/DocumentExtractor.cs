using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

public interface IDocumentExtractor
{
    Task<string> ExtractAsync(Stream content, string contentType, string fileName, CancellationToken cancellationToken);
}

public sealed class DocumentExtractor(HttpClient httpClient, IConfiguration configuration) : IDocumentExtractor
{
    private static readonly HashSet<string> TextTypes = new(StringComparer.OrdinalIgnoreCase) { "text/plain", "text/markdown" };

    public async Task<string> ExtractAsync(Stream content, string contentType, string fileName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (TextTypes.Contains(contentType) || Path.GetExtension(fileName).Equals(".txt", StringComparison.OrdinalIgnoreCase))
            return await new StreamReader(content, Encoding.UTF8, true, 4096, true).ReadToEndAsync(cancellationToken);
        if (contentType.Equals("application/vnd.openxmlformats-officedocument.wordprocessingml.document", StringComparison.OrdinalIgnoreCase) ||
            Path.GetExtension(fileName).Equals(".docx", StringComparison.OrdinalIgnoreCase))
            return ExtractDocx(content);
        if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) ||
            contentType.Equals("application/msword", StringComparison.OrdinalIgnoreCase) ||
            Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase) ||
            Path.GetExtension(fileName).Equals(".doc", StringComparison.OrdinalIgnoreCase))
            return await ExtractWithDocumentIntelligenceAsync(content, cancellationToken);
        throw new NotSupportedException($"Unsupported document type '{contentType}'.");
    }

    private static string ExtractDocx(Stream content)
    {
        using var archive = new ZipArchive(content, ZipArchiveMode.Read, true);
        var entry = archive.GetEntry("word/document.xml") ?? throw new InvalidDataException("DOCX does not contain word/document.xml.");
        using var stream = entry.Open();
        var xml = XDocument.Load(stream);
        var paragraphs = xml.Descendants(XName.Get("p", "http://schemas.openxmlformats.org/wordprocessingml/2006/main"))
            .Select(p => string.Concat(p.Descendants(XName.Get("t", "http://schemas.openxmlformats.org/wordprocessingml/2006/main")).Select(t => t.Value)))
            .Where(x => !string.IsNullOrWhiteSpace(x));
        return string.Join(Environment.NewLine, paragraphs);
    }

    private async Task<string> ExtractWithDocumentIntelligenceAsync(Stream content, CancellationToken cancellationToken)
    {
        var endpoint = configuration["Ingestion:DocumentIntelligenceEndpoint"]?.TrimEnd('/') ?? throw new InvalidOperationException("Ingestion:DocumentIntelligenceEndpoint is required.");
        var key = configuration["Ingestion:DocumentIntelligenceKey"] ?? throw new InvalidOperationException("Ingestion:DocumentIntelligenceKey is required.");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/documentintelligence/documentModels/prebuilt-read:analyze?api-version=2024-11-30");
        request.Headers.Add("Ocp-Apim-Subscription-Key", key);
        request.Content = new StreamContent(content);
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        if (!response.Headers.TryGetValues("Operation-Location", out var values)) throw new InvalidOperationException("Document Intelligence did not return Operation-Location.");
        var operationUri = values.Single();
        for (var attempt = 0; attempt < 30; attempt++)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            using var poll = new HttpRequestMessage(HttpMethod.Get, operationUri);
            poll.Headers.Add("Ocp-Apim-Subscription-Key", key);
            using var result = await httpClient.SendAsync(poll, cancellationToken);
            result.EnsureSuccessStatusCode();
            await using var stream = await result.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var status = json.RootElement.GetProperty("status").GetString();
            if (string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase))
                return json.RootElement.GetProperty("analyzeResult").GetProperty("content").GetString() ?? string.Empty;
            if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Document Intelligence extraction failed.");
        }
        throw new TimeoutException("Document Intelligence extraction timed out.");
    }
}

public sealed class BlobIngestionService(BlobServiceClient blobs, IDocumentExtractor extractor)
{
    public async Task<string> ExtractAndStoreAsync(string blobUri, Guid tenantId, Guid documentId, Guid versionId, string contentType, string fileName, CancellationToken ct)
    {
        var source = new Azure.Storage.Blobs.BlobClient(new Uri(blobUri), new Azure.Identity.DefaultAzureCredential());
        await using var input = await source.OpenReadAsync(cancellationToken: ct);
        var text = await extractor.ExtractAsync(input, contentType, fileName, ct);
        var container = blobs.GetBlobContainerClient("extracted");
        await container.CreateIfNotExistsAsync(cancellationToken: ct);
        var target = container.GetBlobClient($"{tenantId}/{documentId}/{versionId}.txt");
        await using var output = new MemoryStream(Encoding.UTF8.GetBytes(text));
        await target.UploadAsync(output, overwrite: true, cancellationToken: ct);
        return target.Uri.ToString();
    }
}