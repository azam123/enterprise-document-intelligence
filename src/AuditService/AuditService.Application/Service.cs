using EnterpriseDocumentIntelligence.AuditService.Domain;

namespace EnterpriseDocumentIntelligence.AuditService.Application;

/// <summary>
/// Provides persistence operations for audit records.
/// </summary>
public interface IAuditStore
{
    /// <summary>
    /// Determines whether an audit event has already been persisted.
    /// </summary>
    Task<bool> ExistsAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists an audit record.
    /// </summary>
    Task AppendAsync(
        AuditRecord record,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit records belonging to a tenant, optionally filtered by resource.
    /// </summary>
    Task<IReadOnlyList<AuditRecord>> QueryAsync(
        Guid tenantId,
        Guid? resourceId = null,
        DateTimeOffset? from = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Coordinates audit commands and queries while enforcing idempotent event recording.
/// </summary>
public sealed class AuditApplication(IAuditStore store)
{
    /// <summary>
    /// Records an audit event unless the event ID has already been processed.
    /// </summary>
    public async Task<bool> RecordAsync(
        AuditRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (await store.ExistsAsync(record.EventId, cancellationToken))
        {
            return false;
        }

        await store.AppendAsync(record, cancellationToken);

        return true;
    }

    /// <summary>
    /// Retrieves audit records for a tenant.
    /// </summary>
    public Task<IReadOnlyList<AuditRecord>> QueryAsync(
        Guid tenantId,
        Guid? resourceId = null,
        DateTimeOffset? from = null,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        return store.QueryAsync(
            tenantId,
            resourceId,
            from,
            cancellationToken);
    }
}
