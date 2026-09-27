using EnterpriseDocumentIntelligence.BuildingBlocks.Domain;
using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
using EnterpriseDocumentIntelligence.DocumentService.Domain;

namespace EnterpriseDocumentIntelligence.DocumentService.Application;

public sealed class DocumentServiceApplication(
    IDocumentRepository repository,
    IDocumentStorage storage,
    IDocumentEventPublisher events,
    ICurrentUser currentUser)
{
    public async Task<DocumentDto> CreateAsync(
        CreateDocumentCommand command,
        CancellationToken cancellationToken)
    {
        EnsureTenant();
        ValidateMetadata(command.Name, command.ContentType, command.SizeBytes);

        var document = new Document(
            currentUser.TenantId,
            new DocumentName(command.Name).Value,
            command.ContentType,
            command.SizeBytes);

        var version = new DocumentVersion(
            document.Id,
            1,
            string.Empty,
            string.Empty,
            command.SizeBytes);

        document.Versions.Add(version);
        document.SetCurrentVersion(version.Id);

        await repository.AddAsync(document, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        var correlationId = GetCorrelationId();
        await events.PublishAuditAsync(
            document.TenantId,
            currentUser.UserId,
            "document.create",
            document.Id,
            "Succeeded",
            correlationId,
            cancellationToken);

        return Map(document);
    }

    public async Task<UploadDocumentResult> UploadAsync(
        UploadDocumentCommand command,
        CancellationToken cancellationToken)
    {
        EnsureTenant();
        ValidateMetadata(command.FileName, command.ContentType, command.SizeBytes);

        var document = new Document(
            currentUser.TenantId,
            new DocumentName(command.FileName).Value,
            command.ContentType,
            command.SizeBytes);

        var stored = await storage.UploadAsync(
            currentUser.TenantId,
            document.Id,
            1,
            document.Name,
            command.Content,
            cancellationToken);

        var version = new DocumentVersion(
            document.Id,
            1,
            stored.BlobUri,
            stored.Sha256,
            stored.SizeBytes);

        document.Versions.Add(version);
        document.SetCurrentVersion(version.Id);
        document.MarkProcessing();

        await repository.AddAsync(document, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        var correlationId = GetCorrelationId();
        await events.PublishUploadedAsync(
            document,
            version,
            correlationId,
            cancellationToken);

        await events.PublishAuditAsync(
            document.TenantId,
            currentUser.UserId,
            "document.upload",
            document.Id,
            "Succeeded",
            correlationId,
            cancellationToken);

        return new UploadDocumentResult(
            document.Id,
            version.Id,
            document.Status);
    }

    public async Task<DocumentDto?> GetAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        EnsureTenant();
        var document = await repository.GetAsync(
            currentUser.TenantId,
            documentId,
            cancellationToken);

        return document is null ? null : Map(document);
    }

    public async Task AddAclAsync(
        AddAclCommand command,
        CancellationToken cancellationToken)
    {
        EnsureTenant();
        AclPolicy.EnsurePrincipal(command.PrincipalId);
        AclPolicy.EnsurePermission(command.Permission);

        var document = await repository.GetWithAclAsync(
            currentUser.TenantId,
            command.DocumentId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Document not found.");

        var exists = document.Acls.Any(
            acl => acl.PrincipalId == command.PrincipalId &&
                   string.Equals(acl.Permission, command.Permission, StringComparison.OrdinalIgnoreCase));

        if (!exists)
        {
            document.Acls.Add(
                new DocumentAcl(
                    document.Id,
                    command.PrincipalId,
                    command.PrincipalType,
                    command.Permission));

            await repository.SaveChangesAsync(cancellationToken);
        }

        await PublishAclChangedAsync(document, cancellationToken);
    }

    public async Task RemoveAclAsync(
        Guid documentId,
        Guid principalId,
        string permission,
        CancellationToken cancellationToken)
    {
        EnsureTenant();
        AclPolicy.EnsurePrincipal(principalId);
        AclPolicy.EnsurePermission(permission);

        var document = await repository.GetWithAclAsync(
            currentUser.TenantId,
            documentId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Document not found.");

        var acl = document.Acls.FirstOrDefault(
            item => item.PrincipalId == principalId &&
                    string.Equals(item.Permission, permission, StringComparison.OrdinalIgnoreCase));

        if (acl is null)
            return;

        await repository.RemoveAclAsync(acl, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        await PublishAclChangedAsync(document, cancellationToken);
    }

    private async Task PublishAclChangedAsync(
        Document document,
        CancellationToken cancellationToken)
    {
        var principals = document.Acls
            .Where(acl => string.Equals(acl.Permission, "Read", StringComparison.OrdinalIgnoreCase))
            .Select(acl => acl.PrincipalId)
            .Distinct()
            .ToArray();

        var correlationId = GetCorrelationId();

        await events.PublishAclChangedAsync(
            document,
            principals,
            correlationId,
            cancellationToken);

        await events.PublishAuditAsync(
            document.TenantId,
            currentUser.UserId,
            "document.acl.changed",
            document.Id,
            "Succeeded",
            correlationId,
            cancellationToken);
    }

    private void EnsureTenant()
    {
        if (!currentUser.IsAuthenticated || currentUser.TenantId == Guid.Empty)
            throw new UnauthorizedAccessException("Authenticated tenant context is required.");
    }

    private static void ValidateMetadata(
        string name,
        string contentType,
        long sizeBytes)
    {
        _ = new DocumentName(name);
        DocumentTypePolicy.EnsureAllowed(contentType, name);
        DocumentSizePolicy.EnsureValid(sizeBytes);
    }

    private string GetCorrelationId() =>
        currentUser.CorrelationId ?? Guid.NewGuid().ToString("N");

    private static DocumentDto Map(Document document) =>
        new(
            document.Id,
            document.TenantId,
            document.Name,
            document.ContentType,
            document.SizeBytes,
            document.Status,
            document.CurrentVersionId);
}
