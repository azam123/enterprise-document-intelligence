using Azure.Storage.Blobs;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace EnterpriseDocumentIntelligence.IngestionService;

public interface IDocumentExtractor
{
    Task<string> ExtractAsync(Stream content, string contentType, CancellationToken ct);
}

public sealed class DocumentExtractor : IDocumentExtractor
{
    public async Task<string> ExtractAsync(Stream content, string contentType, CancellationToken ct)
    {
        if (contentType.Equals("text/plain", StringComparison.OrdinalIgnoreCase))
            return await new StreamReader(content, Encoding.UTF8, leaveOpen: true).ReadToEndAsync(ct);

        if (contentType.Equals("application/vnd.openxmlformats-officedocument.wordprocessingml.document", StringComparison.OrdinalIgnoreCase))
            return ExtractDocx(content);

        throw new NotSupportedException("PDF/DOC extraction requires Azure Document Intelligence configuration.");
    }

    private static string ExtractDocx(Stream content)
    {
        using var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
        var entry = archive.GetEntry("word/document.xml") ?? throw new InvalidDataException("DOCX document.xml is missing.");
        using var stream = entry.Open();
        var xml = XDocument.Load(stream);
        return string.Join(Environment.NewLine,
            xml.Descendants(XName.Get("t", "http://schemas.openxmlformats.org/wordprocessingml/2006/main")).Select(x => x.Value));
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