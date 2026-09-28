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
    /// Attempts to persist an audit record.
    /// </summary>
    /// <returns><c>true</c> when the record was persisted; otherwise <c>false</c> when it already existed.</returns>
    Task<bool> AppendAsync(
        AuditRecord record,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit records belonging to a tenant, optionally filtered by resource and time.
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

        return await store.AppendAsync(
            record,
            cancellationToken);
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
            throw new ArgumentException(
                "Tenant id is required.",
                nameof(tenantId));
        }

        return store.QueryAsync(
            tenantId,
            resourceId,
            from,
            cancellationToken);
    }
}
