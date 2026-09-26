using Azure.Storage.Blobs;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace EnterpriseDocumentIntelligence.IngestionService;

public interface IDocumentExtractor { Task<string> ExtractAsync(Stream content, string contentType, CancellationToken ct); }

public sealed class DocumentExtractor(HttpClient http, IConfiguration configuration) : IDocumentExtractor
{
    public async Task<string> ExtractAsync(Stream content, string contentType, CancellationToken ct)
    {
        if (contentType.Equals("text/plain", StringComparison.OrdinalIgnoreCase))
            return await new StreamReader(content, Encoding.UTF8, true, 1024, true).ReadToEndAsync(ct);
        if (contentType.Equals("application/vnd.openxmlformats-officedocument.wordprocessingml.document", StringComparison.OrdinalIgnoreCase))
            return ExtractDocx(content);
        if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) || contentType.Equals("application/msword", StringComparison.OrdinalIgnoreCase))
            return await ExtractWithDocumentIntelligenceAsync(content, ct);
        throw new NotSupportedException($"Unsupported content type '{contentType}'.");
    }

    private static string ExtractDocx(Stream content)
    {
        using var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
        var entry = archive.GetEntry("word/document.xml") ?? throw new InvalidDataException("DOCX document.xml is missing.");
        using var stream = entry.Open();
        var xml = XDocument.Load(stream);
        return string.Join(Environment.NewLine, xml.Descendants(XName.Get("t", "http://schemas.openxmlformats.org/wordprocessingml/2006/main")).Select(x => x.Value));
    }

    private async Task<string> ExtractWithDocumentIntelligenceAsync(Stream content, CancellationToken ct)
    {
        var endpoint = configuration["Ingestion:DocumentIntelligenceEndpoint"]?.TrimEnd('/') ?? throw new InvalidOperationException("Ingestion:DocumentIntelligenceEndpoint is required for PDF/DOC extraction.");
        var key = configuration["Ingestion:DocumentIntelligenceKey"] ?? throw new InvalidOperationException("Ingestion:DocumentIntelligenceKey is required for PDF/DOC extraction.");
        if (content.CanSeek) content.Position = 0;
        using var payload = new MemoryStream();
        await content.CopyToAsync(payload, ct);
        payload.Position = 0;

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/documentintelligence/documentModels/prebuilt-read:analyze?api-version=2024-11-30");
        request.Headers.Add("Ocp-Apim-Subscription-Key", key);
        request.Content = new StreamContent(payload);
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        string? operation = response.Headers.Location?.ToString();
        if (string.IsNullOrWhiteSpace(operation) && response.Headers.TryGetValues("Operation-Location", out var locations))
            operation = locations.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(operation)) throw new InvalidOperationException("Document Intelligence did not return an operation location.");

        for (var attempt = 0; attempt < 30; attempt++)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            using var poll = new HttpRequestMessage(HttpMethod.Get, operation);
            poll.Headers.Add("Ocp-Apim-Subscription-Key", key);
            using var pollResponse = await http.SendAsync(poll, ct);
            pollResponse.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await pollResponse.Content.ReadAsStringAsync(ct));
            var status = json.RootElement.GetProperty("status").GetString();
            if (string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase))
                return json.RootElement.GetProperty("analyzeResult").GetProperty("content").GetString() ?? string.Empty;
            if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Azure Document Intelligence analysis failed.");
        }
        throw new TimeoutException("Azure Document Intelligence analysis timed out.");
    }
}

public sealed class BlobIngestionService(BlobServiceClient blobs, IDocumentExtractor extractor)
{
    public async Task<Uri> ExtractAndStoreAsync(string blobUri, string contentType, Guid tenantId, Guid documentId, Guid versionId, CancellationToken ct)
    {
        var source = new BlobClient(new Uri(blobUri), new Azure.Identity.DefaultAzureCredential());
        await using var input = await source.OpenReadAsync(cancellationToken: ct);
        var text = await extractor.ExtractAsync(input, contentType, ct);
        var container = blobs.GetBlobContainerClient("extracted");
        await container.CreateIfNotExistsAsync(cancellationToken: ct);
        var target = container.GetBlobClient($"{tenantId}/{documentId}/{versionId}.txt");
        await using var output = new MemoryStream(Encoding.UTF8.GetBytes(text));
        await target.UploadAsync(output, overwrite: true, cancellationToken: ct);
        return target.Uri;
    }
}