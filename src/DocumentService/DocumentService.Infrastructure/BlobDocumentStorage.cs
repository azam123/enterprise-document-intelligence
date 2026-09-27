using System.Security.Cryptography;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EnterpriseDocumentIntelligence.DocumentService.Application;

namespace EnterpriseDocumentIntelligence.DocumentService.Infrastructure;

public sealed class BlobDocumentStorage(BlobServiceClient blobs) : IDocumentStorage
{
    private const string ContainerName = "documents";

    public async Task<StoredDocument> UploadAsync(
        Guid tenantId,
        Guid documentId,
        int versionNumber,
        string fileName,
        Stream content,
        CancellationToken cancellationToken)
    {
        if (!content.CanRead)
            throw new ArgumentException("Content stream must be readable.", nameof(content));

        var container = blobs.GetBlobContainerClient(ContainerName);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobName =
            $"{tenantId}/{documentId}/{versionNumber}/{Uri.EscapeDataString(fileName)}";
        var blob = container.GetBlobClient(blobName);

        await using var buffered = new MemoryStream();
        await content.CopyToAsync(buffered, cancellationToken);
        var bytes = buffered.ToArray();

        var hash = Convert.ToHexString(SHA256.HashData(bytes));

        await using var uploadStream = new MemoryStream(bytes, writable: false);
        await blob.UploadAsync(
            uploadStream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "application/octet-stream"
                },
                Conditions = null
            },
            cancellationToken);

        return new StoredDocument(
            blob.Uri.ToString(),
            hash,
            bytes.LongLength);
    }
}
