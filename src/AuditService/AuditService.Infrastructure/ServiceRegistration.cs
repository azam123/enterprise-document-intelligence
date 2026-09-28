using System.Text.Json;
using EnterpriseDocumentIntelligence.AuditService.Application;
using EnterpriseDocumentIntelligence.AuditService.Domain;
using EnterpriseDocumentIntelligence.BuildingBlocks.Domain;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseDocumentIntelligence.AuditService.Infrastructure;

/// <summary>
/// Persists audit records in the shared SQL Server database.
/// </summary>
public sealed class SqlAuditStore(DocumentDbContext db) : IAuditStore
{
    /// <summary>
    /// Checks whether an audit event has already been persisted.
    /// </summary>
    public Task<bool> ExistsAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        return db.AuditEvents
            .AsNoTracking()
            .AnyAsync(
                auditEvent => auditEvent.Id == eventId,
                cancellationToken);
    }

    /// <summary>
    /// Inserts an audit record and safely ignores a duplicate event ID.
    /// </summary>
    public async Task<bool> AppendAsync(
        AuditRecord record,
        CancellationToken cancellationToken = default)
    {
        var auditEvent = new AuditEvent(
            record.TenantId,
            record.ActorId,
            record.Action,
            record.ResourceType,
            record.ResourceId,
            record.Outcome,
            record.CorrelationId,
            SerializeMetadata(record.Metadata),
            record.EventId);

        db.AuditEvents.Add(auditEvent);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException sqlException &&
                  (sqlException.Number == 2601 ||
                   sqlException.Number == 2627))
        {
            db.Entry(auditEvent).State = EntityState.Detached;

            return false;
        }
    }

    /// <summary>
    /// Retrieves audit records for a tenant with optional resource and time filtering.
    /// </summary>
    public async Task<IReadOnlyList<AuditRecord>> QueryAsync(
        Guid tenantId,
        Guid? resourceId = null,
        DateTimeOffset? from = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.AuditEvents
            .AsNoTracking()
            .Where(auditEvent => auditEvent.TenantId == tenantId);

        if (resourceId.HasValue)
        {
            query = query.Where(
                auditEvent => auditEvent.ResourceId == resourceId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(
                auditEvent => auditEvent.CreatedAt >= from.Value);
        }

        var events = await query
            .OrderByDescending(auditEvent => auditEvent.CreatedAt)
            .Take(500)
            .ToListAsync(cancellationToken);

        return events
            .Select(MapToDomain)
            .ToList();
    }

    /// <summary>
    /// Converts the persistence entity into the AuditService domain model.
    /// </summary>
    private static AuditRecord MapToDomain(AuditEvent auditEvent)
    {
        return new AuditRecord(
            auditEvent.Id,
            auditEvent.TenantId,
            auditEvent.ActorId,
            auditEvent.Action,
            auditEvent.ResourceType,
            auditEvent.ResourceId,
            auditEvent.Outcome,
            auditEvent.CreatedAt,
            auditEvent.CorrelationId,
            DeserializeMetadata(auditEvent.MetadataJson));
    }

    /// <summary>
    /// Serializes optional audit metadata into the existing persistence column.
    /// </summary>
    private static string? SerializeMetadata(
        IReadOnlyDictionary<string, string> metadata)
    {
        return metadata.Count == 0
            ? null
            : JsonSerializer.Serialize(metadata);
    }

    /// <summary>
    /// Deserializes persisted metadata while tolerating legacy or empty records.
    /// </summary>
    private static IReadOnlyDictionary<string, string> DeserializeMetadata(
        string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(
                       metadataJson)
                   ?? new Dictionary<string, string>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
    }
}

/// <summary>
/// Registers AuditService application and persistence dependencies.
/// </summary>
public static class AuditServiceInfrastructure
{
    /// <summary>
    /// Adds AuditService application services and the production SQL audit store.
    /// </summary>
    public static IServiceCollection AddAuditApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IAuditStore, SqlAuditStore>();
        services.AddScoped<AuditApplication>();
        services.AddScoped<AuditEventProcessor>();

        return services;
    }
}
