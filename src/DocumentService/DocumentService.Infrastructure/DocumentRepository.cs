using EnterpriseDocumentIntelligence.BuildingBlocks.Domain;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using EnterpriseDocumentIntelligence.DocumentService.Application;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseDocumentIntelligence.DocumentService.Infrastructure;

public sealed class DocumentRepository(DocumentDbContext db) : IDocumentRepository
{
    public async Task AddAsync(Document document, CancellationToken cancellationToken) =>
        await db.Documents.AddAsync(document, cancellationToken);

    public Task<Document?> GetAsync(
        Guid tenantId,
        Guid documentId,
        CancellationToken cancellationToken) =>
        db.Documents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                document => document.Id == documentId &&
                             document.TenantId == tenantId,
                cancellationToken);

    public Task<Document?> GetWithAclAsync(
        Guid tenantId,
        Guid documentId,
        CancellationToken cancellationToken) =>
        db.Documents
            .Include(document => document.Acls)
            .SingleOrDefaultAsync(
                document => document.Id == documentId &&
                             document.TenantId == tenantId,
                cancellationToken);

    public Task AddAclAsync(
        DocumentAcl acl,
        CancellationToken cancellationToken)
    {
        db.DocumentAcls.Add(acl);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task RemoveAclAsync(
        DocumentAcl acl,
        CancellationToken cancellationToken)
    {
        db.DocumentAcls.Remove(acl);
        await Task.CompletedTask;
    }
}
