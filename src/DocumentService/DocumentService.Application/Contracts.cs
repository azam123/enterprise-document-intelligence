using EnterpriseDocumentIntelligence.BuildingBlocks.Domain;

namespace EnterpriseDocumentIntelligence.DocumentService.Application;

public sealed record CreateDocumentCommand(
    string Name,
    string ContentType,
    long SizeBytes);

public sealed record UploadDocumentCommand(
    string FileName,
    string ContentType,
    Stream Content,
    long SizeBytes);

public sealed record AddAclCommand(
    Guid DocumentId,
    Guid PrincipalId,
    string PrincipalType,
    string Permission);

public sealed record DocumentDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string ContentType,
    long SizeBytes,
    string Status,
    Guid CurrentVersionId);

public sealed record UploadDocumentResult(
    Guid DocumentId,
    Guid VersionId,
    string Status);

public interface IDocumentRepository
{
    Task AddAsync(Document document, CancellationToken cancellationToken);
    Task<Document?> GetAsync(Guid tenantId, Guid documentId, CancellationToken cancellationToken);
    Task<Document?> GetWithAclAsync(Guid tenantId, Guid documentId, CancellationToken cancellationToken);
    Task AddAclAsync(DocumentAcl acl, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task RemoveAclAsync(DocumentAcl acl, CancellationToken cancellationToken);
}

public interface IDocumentStorage
{
    Task<StoredDocument> UploadAsync(
        Guid tenantId,
        Guid documentId,
        int versionNumber,
        string fileName,
        Stream content,
        CancellationToken cancellationToken);
}

public sealed record StoredDocument(
    string BlobUri,
    string Sha256,
    long SizeBytes);

public interface IDocumentEventPublisher
{
    Task PublishUploadedAsync(
        Document document,
        DocumentVersion version,
        string correlationId,
        CancellationToken cancellationToken);

    Task PublishAclChangedAsync(
        Document document,
        IReadOnlyList<Guid> allowedPrincipalIds,
        string correlationId,
        CancellationToken cancellationToken);

    Task PublishAuditAsync(
        Guid tenantId,
        Guid? actorId,
        string action,
        Guid? resourceId,
        string outcome,
        string correlationId,
        CancellationToken cancellationToken);
}
