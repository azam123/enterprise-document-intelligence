using EnterpriseDocumentIntelligence.AuditService.Domain;

namespace EnterpriseDocumentIntelligence.AuditService.Application;

public interface IAuditStore
{
    Task<bool> ExistsAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task AppendAsync(
        AuditRecord record,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditRecord>> QueryAsync(
        string tenantId,
        string? resourceId = null,
        CancellationToken cancellationToken = default);
}

public sealed class AuditApplication(IAuditStore store)
{
    public async Task<bool> RecordAsync(
        AuditRecord record,
        CancellationToken cancellationToken = default)
    {
        if (await store.ExistsAsync(record.EventId, cancellationToken))
        {
            return false;
        }

        await store.AppendAsync(record, cancellationToken);

        return true;
    }

    public Task<IReadOnlyList<AuditRecord>> QueryAsync(
        string tenantId,
        string? resourceId = null,
        CancellationToken cancellationToken = default)
    {
        return store.QueryAsync(
            tenantId,
            resourceId,
            cancellationToken);
    }
}
