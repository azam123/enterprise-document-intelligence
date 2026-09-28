using System.Linq;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;
using EnterpriseDocumentIntelligence.DocumentService.Application;

namespace EnterpriseDocumentIntelligence.DocumentService.Infrastructure;

public sealed class DocumentEventPublisher(
    IMessagePublisher publisher) : IDocumentEventPublisher
{
    public Task PublishUploadedAsync(
        EnterpriseDocumentIntelligence.BuildingBlocks.Domain.Document document,
        EnterpriseDocumentIntelligence.BuildingBlocks.Domain.DocumentVersion version,
        string correlationId,
        CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            Topics.DocumentEvents,
            new DocumentUploaded(
                Guid.NewGuid(),
                document.TenantId,
                document.Id,
                version.Id,
                version.BlobUri,
                document.ContentType,
                version.SizeBytes,
                DateTimeOffset.UtcNow,
                correlationId,
                document.Acls
                    .Where(acl => acl.Permission == "Read")
                    .Select(acl => acl.PrincipalId)
                    .Distinct()
                    .ToArray()),
            correlationId,
            cancellationToken);

    public Task PublishAclChangedAsync(
        EnterpriseDocumentIntelligence.BuildingBlocks.Domain.Document document,
        IReadOnlyList<Guid> allowedPrincipalIds,
        string correlationId,
        CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            Topics.DocumentEvents,
            new DocumentAclChanged(
                Guid.NewGuid(),
                document.TenantId,
                document.Id,
                allowedPrincipalIds,
                DateTimeOffset.UtcNow,
                correlationId),
            correlationId,
            cancellationToken);

    public Task PublishAuditAsync(
        Guid tenantId,
        Guid? actorId,
        string action,
        Guid? resourceId,
        string outcome,
        string correlationId,
        CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            Topics.AuditEvents,
            new AuditRequested(
                Guid.NewGuid(),
                tenantId,
                actorId,
                action,
                "Document",
                resourceId,
                outcome,
                correlationId,
                null),
            correlationId,
            cancellationToken);
}
