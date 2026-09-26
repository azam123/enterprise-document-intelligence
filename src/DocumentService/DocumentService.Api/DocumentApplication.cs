using Azure.Storage.Blobs;
using EnterpriseDocumentIntelligence.BuildingBlocks.Domain;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;
using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace EnterpriseDocumentIntelligence.DocumentService;

public sealed record DocumentResponse(Guid Id, Guid TenantId, string Name, string ContentType, long SizeBytes, string Status, Guid CurrentVersionId);
public sealed record DocumentListItem(Guid Id, string Name, string ContentType, long SizeBytes, string Status, Guid CurrentVersionId, DateTimeOffset CreatedAt);

public sealed class DocumentApplication(
    DocumentDbContext db,
    BlobServiceClient blobs,
    ICurrentUser user,
    IMessagePublisher publisher,
    ILogger<DocumentApplication> logger)
{
    private const long MaxSize = 500L * 1024 * 1024;
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf", "text/plain", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/msword"
    };

    public async Task<DocumentResponse> UploadAsync(IFormFile file, CancellationToken ct)
    {
        if (!user.IsAuthenticated || user.TenantId == Guid.Empty) throw new UnauthorizedAccessException();
        if (file is null || file.Length == 0) throw new ArgumentException("File is required.");
        if (file.Length > MaxSize) throw new ArgumentException("Maximum file size is 500 MB.");
        if (!AllowedTypes.Contains(file.ContentType)) throw new ArgumentException("Unsupported document type.");

        var container = blobs.GetBlobContainerClient("documents");
        await container.CreateIfNotExistsAsync(cancellationToken: ct);

        var document = new Document(user.TenantId, file.FileName, file.ContentType, file.Length);
        var blob = container.GetBlobClient($"{user.TenantId}/{document.Id}/1/{Uri.EscapeDataString(file.FileName)}");

        await using (var hashInput = file.OpenReadStream())
        {
            var hash = await SHA256.HashDataAsync(hashInput, ct);
            await using var uploadInput = file.OpenReadStream();
            await blob.UploadAsync(uploadInput, overwrite: false, ct);
            var version = new DocumentVersion(document.Id, 1, blob.Uri.ToString(), Convert.ToHexString(hash), file.Length);
            document.Versions.Add(version);
            document.SetCurrentVersion(version.Id);
        }

        document.MarkProcessing();
        db.Documents.Add(document);
        await db.SaveChangesAsync(ct);

        var correlation = user.CorrelationId ?? Guid.NewGuid().ToString("N");
        await publisher.PublishAsync(Topics.DocumentEvents,
            new DocumentUploaded(Guid.NewGuid(), document.TenantId, document.Id, document.CurrentVersionId,
                blob.Uri.ToString(), document.ContentType, document.SizeBytes, DateTimeOffset.UtcNow, correlation),
            correlation, ct);

        logger.LogInformation("Document uploaded {DocumentId} Tenant={TenantId}", document.Id, document.TenantId);
        return ToResponse(document);
    }

    public async Task<DocumentResponse?> GetAsync(Guid id, CancellationToken ct)
    {
        var document = await db.Documents.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.TenantId == user.TenantId, ct);
        return document is null ? null : ToResponse(document);
    }

    public async Task<IReadOnlyList<DocumentListItem>> ListAsync(CancellationToken ct) =>
        await db.Documents.AsNoTracking().Where(x => x.TenantId == user.TenantId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new DocumentListItem(x.Id, x.Name, x.ContentType, x.SizeBytes, x.Status, x.CurrentVersionId, x.CreatedAt))
            .ToListAsync(ct);

    private static DocumentResponse ToResponse(Document x) =>
        new(x.Id, x.TenantId, x.Name, x.ContentType, x.SizeBytes, x.Status, x.CurrentVersionId);
}