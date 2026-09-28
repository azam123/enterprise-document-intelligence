using System.Collections.Concurrent;

using EnterpriseDocumentIntelligence.AuditService.Application;
using EnterpriseDocumentIntelligence.AuditService.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseDocumentIntelligence.AuditService.Infrastructure;

public sealed class InMemoryAuditStore : IAuditStore
{
    private readonly ConcurrentDictionary<Guid, AuditRecord> _items = new();

    public Task<bool> ExistsAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_items.ContainsKey(eventId));
    }

    public Task AppendAsync(
        AuditRecord record,
        CancellationToken cancellationToken = default)
    {
        _items.TryAdd(record.EventId, record);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AuditRecord>> QueryAsync(
        string tenantId,
        string? resourceId = null,
        CancellationToken cancellationToken = default)
    {
        var records = _items.Values
            .Where(record =>
                record.TenantId == tenantId &&
                (resourceId == null || record.ResourceId == resourceId))
            .OrderByDescending(record => record.OccurredAt)
            .ToList();

        return Task.FromResult(
            (IReadOnlyList<AuditRecord>)records);
    }
}

public static class AuditServiceInfrastructure
{
    public static IServiceCollection AddAuditApplication(
        this IServiceCollection services)
    {
        services.AddSingleton<IAuditStore, InMemoryAuditStore>();
        services.AddScoped<AuditApplication>();

        return services;
    }
}
