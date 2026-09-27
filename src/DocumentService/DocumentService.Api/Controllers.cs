using EnterpriseDocumentIntelligence.BuildingBlocks.Domain;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;
using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public sealed record CreateDocumentRequest(
    string Name,
    string ContentType,
    long SizeBytes);

public sealed record DocumentResponse(
    Guid Id,
    Guid TenantId,
    string Name,
    string ContentType,
    long SizeBytes,
    string Status,
    Guid CurrentVersionId);

public sealed record AclRequest(
    Guid PrincipalId,
    string PrincipalType = "User",
    string Permission = "Read");

public sealed class DocumentApp(
    DocumentDbContext db,
    ICurrentUser user,
    IMessagePublisher bus,
    ILogger<DocumentApp> log)
{
    public async Task<DocumentResponse> CreateAsync(
        CreateDocumentRequest request,
        CancellationToken cancellationToken)
    {
        EnsureTenant();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required.");
        }

        if (request.SizeBytes <= 0 || request.SizeBytes > 524_288_000)
        {
            throw new ArgumentException("Invalid document size.");
        }

        var document = new Document(
            user.TenantId,
            request.Name,
            request.ContentType,
            request.SizeBytes);

        var version = new DocumentVersion(
            document.Id,
            1,
            string.Empty,
            string.Empty,
            request.SizeBytes);

        document.Versions.Add(version);
        document.SetCurrentVersion(version.Id);

        db.Documents.Add(document);
        await db.SaveChangesAsync(cancellationToken);

        var correlationId = GetCorrelationId();

        await bus.PublishAsync(
            Topics.AuditEvents,
            new AuditRequested(
                Guid.NewGuid(),
                document.TenantId,
                user.UserId,
                "document.create",
                "Document",
                document.Id,
                "Succeeded",
                correlationId,
                null),
            correlationId,
            cancellationToken);

        log.LogInformation(
            "Created document {DocumentId} Tenant={TenantId}",
            document.Id,
            document.TenantId);

        return ToResponse(document);
    }

    public async Task<DocumentResponse?> GetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var document = await db.Documents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == id && x.TenantId == user.TenantId,
                cancellationToken);

        return document is null ? null : ToResponse(document);
    }

    public async Task AddAclAsync(
        Guid documentId,
        AclRequest request,
        CancellationToken cancellationToken)
    {
        var document = await GetDocumentWithAclAsync(
            documentId,
            cancellationToken);

        if (request.PrincipalId == Guid.Empty)
        {
            throw new ArgumentException("PrincipalId is required.");
        }

        var exists = document.Acls.Any(
            x => x.PrincipalId == request.PrincipalId &&
                 x.Permission == request.Permission);

        if (!exists)
        {
            document.Acls.Add(
                new DocumentAcl(
                    documentId,
                    request.PrincipalId,
                    request.PrincipalType,
                    request.Permission));
        }

        await db.SaveChangesAsync(cancellationToken);

        var principals = document.Acls
            .Where(x => x.Permission == "Read")
            .Select(x => x.PrincipalId)
            .Distinct()
            .ToArray();

        var correlationId = GetCorrelationId();

        await bus.PublishAsync(
            Topics.DocumentEvents,
            new DocumentAclChanged(
                Guid.NewGuid(),
                user.TenantId,
                documentId,
                principals,
                DateTimeOffset.UtcNow,
                correlationId),
            correlationId,
            cancellationToken);

        await bus.PublishAsync(
            Topics.AuditEvents,
            new AuditRequested(
                Guid.NewGuid(),
                user.TenantId,
                user.UserId,
                "document.acl.add",
                "Document",
                documentId,
                "Succeeded",
                correlationId,
                null),
            correlationId,
            cancellationToken);
    }

    public async Task RemoveAclAsync(
        Guid documentId,
        Guid principalId,
        string permission,
        CancellationToken cancellationToken)
    {
        var document = await GetDocumentWithAclAsync(
            documentId,
            cancellationToken);

        var acl = document.Acls.FirstOrDefault(
            x => x.PrincipalId == principalId &&
                 x.Permission == permission);

        if (acl is null)
        {
            return;
        }

        db.DocumentAcls.Remove(acl);
        await db.SaveChangesAsync(cancellationToken);

        var principals = document.Acls
            .Where(x => x.Permission == "Read" && x.Id != acl.Id)
            .Select(x => x.PrincipalId)
            .Distinct()
            .ToArray();

        var correlationId = GetCorrelationId();

        await bus.PublishAsync(
            Topics.DocumentEvents,
            new DocumentAclChanged(
                Guid.NewGuid(),
                user.TenantId,
                documentId,
                principals,
                DateTimeOffset.UtcNow,
                correlationId),
            correlationId,
            cancellationToken);
    }

    private async Task<Document> GetDocumentWithAclAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        return await db.Documents
            .Include(x => x.Acls)
            .SingleOrDefaultAsync(
                x => x.Id == documentId && x.TenantId == user.TenantId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Document not found.");
    }

    private void EnsureTenant()
    {
        if (user.TenantId == Guid.Empty)
        {
            throw new UnauthorizedAccessException();
        }
    }

    private string GetCorrelationId() =>
        user.CorrelationId ?? Guid.NewGuid().ToString("N");

    private static DocumentResponse ToResponse(Document document) =>
        new(
            document.Id,
            document.TenantId,
            document.Name,
            document.ContentType,
            document.SizeBytes,
            document.Status,
            document.CurrentVersionId);
}

[ApiController]
[Route("api/v1/documents")]
[Authorize]
public sealed class DocumentsController(DocumentApp app) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = Roles.Contributor + "," + Roles.Administrator)]
    public async Task<ActionResult<DocumentResponse>> Create(
        CreateDocumentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await app.CreateAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.Reader + "," + Roles.Contributor + "," + Roles.Administrator)]
    public async Task<ActionResult<DocumentResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        var document = await app.GetAsync(id, cancellationToken);

        return document is null
            ? NotFound()
            : Ok(document);
    }

    [HttpPost("{id:guid}/acl")]
    [Authorize(Roles = Roles.Administrator + "," + Roles.Contributor)]
    public async Task<IActionResult> AddAcl(
        Guid id,
        AclRequest request,
        CancellationToken cancellationToken)
    {
        await app.AddAclAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/acl/{principalId:guid}")]
    [Authorize(Roles = Roles.Administrator + "," + Roles.Contributor)]
    public async Task<IActionResult> RemoveAcl(
        Guid id,
        Guid principalId,
        [FromQuery] string permission = "Read",
        CancellationToken cancellationToken = default)
    {
        await app.RemoveAclAsync(
            id,
            principalId,
            permission,
            cancellationToken);

        return NoContent();
    }
}