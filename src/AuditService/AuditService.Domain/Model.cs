namespace EnterpriseDocumentIntelligence.AuditService.Domain;

public sealed record AuditRecord
{
    public Guid EventId { get; init; }
    public Guid TenantId { get; init; }
    public Guid? ActorId { get; init; }
    public string Action { get; init; }
    public string ResourceType { get; init; }
    public Guid? ResourceId { get; init; }
    public string Outcome { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public string? CorrelationId { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; }

    public AuditRecord(Guid eventId, Guid tenantId, Guid? actorId, string action, string resourceType, Guid? resourceId, string outcome, DateTimeOffset occurredAt, string? correlationId, IReadOnlyDictionary<string, string> metadata)
    {
        if (eventId == Guid.Empty) throw new ArgumentException("Event id is required.", nameof(eventId));
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(action)) throw new ArgumentException("Action is required.", nameof(action));
        if (string.IsNullOrWhiteSpace(resourceType)) throw new ArgumentException("Resource type is required.", nameof(resourceType));
        if (string.IsNullOrWhiteSpace(outcome)) throw new ArgumentException("Outcome is required.", nameof(outcome));
        EventId = eventId;
        TenantId = tenantId;
        ActorId = actorId;
        Action = action;
        ResourceType = resourceType;
        ResourceId = resourceId;
        Outcome = outcome;
        OccurredAt = occurredAt;
        CorrelationId = correlationId;
        Metadata = metadata;
    }
}
