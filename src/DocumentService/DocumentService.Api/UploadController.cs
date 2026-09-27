using System.Security.Cryptography;
using Azure.Storage.Blobs;
using EnterpriseDocumentIntelligence.BuildingBlocks.Domain;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;
using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/documents")]
[Authorize]
public sealed class DocumentUploadController(
    BlobServiceClient blobs,
    DocumentDbContext db,
    ICurrentUser user,
    IMessagePublisher bus,
    ILogger<DocumentUploadController> log) : ControllerBase
{
    private const long MaximumFileSize = 524_288_000;

    private static readonly string[] AllowedContentTypes =
    [
        "application/pdf",
        "text/plain",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/msword"
    ];

    [HttpPost("upload")]
    [RequestSizeLimit(MaximumFileSize)]
    [Authorize(Roles = Roles.Contributor + "," + Roles.Administrator)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("File is required.");
        }

        if (file.Length > MaximumFileSize)
        {
            return BadRequest("Maximum file size is 500 MB.");
        }

        if (!AllowedContentTypes.Contains(
                file.ContentType,
                StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest("Unsupported document type.");
        }

        var safeFileName = Path.GetFileName(file.FileName);

        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            return BadRequest("Invalid file name.");
        }

        if (!FileSignatureValidator.IsAllowed(file, safeFileName))
        {
            return BadRequest(
                "File content does not match the declared document type.");
        }

        var document = new Document(
            user.TenantId,
            safeFileName,
            file.ContentType,
            file.Length);

        var container = blobs.GetBlobContainerClient("documents");
        await container.CreateIfNotExistsAsync(
            cancellationToken: cancellationToken);

        var versionNumber = 1;
        var blob = container.GetBlobClient(
            $"{user.TenantId}/{document.Id}/{versionNumber}/" +
            Uri.EscapeDataString(safeFileName));

        await using var hashStream = file.OpenReadStream();
        var hash = await SHA256.HashDataAsync(
            hashStream,
            cancellationToken);

        await using var uploadStream = file.OpenReadStream();

        await blob.UploadAsync(
            uploadStream,
            overwrite: false,
            cancellationToken);

        var version = new DocumentVersion(
            document.Id,
            versionNumber,
            blob.Uri.ToString(),
            Convert.ToHexString(hash),
            file.Length);

        document.Versions.Add(version);
        document.SetCurrentVersion(version.Id);
        document.MarkProcessing();

        db.Documents.Add(document);
        await db.SaveChangesAsync(cancellationToken);

        var correlationId =
            user.CorrelationId ?? Guid.NewGuid().ToString("N");

        await bus.PublishAsync(
            Topics.DocumentEvents,
            new DocumentUploaded(
                Guid.NewGuid(),
                document.TenantId,
                document.Id,
                version.Id,
                blob.Uri.ToString(),
                document.ContentType,
                document.SizeBytes,
                DateTimeOffset.UtcNow,
                correlationId,
                []),
            correlationId,
            cancellationToken);

        await bus.PublishAsync(
            Topics.AuditEvents,
            new AuditRequested(
                Guid.NewGuid(),
                document.TenantId,
                user.UserId,
                "document.upload",
                "Document",
                document.Id,
                "Succeeded",
                correlationId,
                null),
            correlationId,
            cancellationToken);

        log.LogInformation(
            "Uploaded document {DocumentId} Blob={BlobUri}",
            document.Id,
            blob.Uri);

        return Accepted(
            new
            {
                documentId = document.Id,
                versionId = version.Id,
                status = document.Status
            });
    }
}