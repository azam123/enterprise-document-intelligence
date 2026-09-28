namespace EnterpriseDocumentIntelligence.AuditService.Domain;

/// <summary>
/// Represents an immutable audit record captured for an enterprise action.
/// </summary>
public sealed record AuditRecord(
    Guid EventId,
    Guid TenantId,
    Guid? ActorId,
    string Action,
    string ResourceType,
    Guid? ResourceId,
    string Outcome,
    DateTimeOffset OccurredAt,
    string? CorrelationId,
    IReadOnlyDictionary<string, string> Metadata)
{
    /// <summary>
    /// Validates the invariants required for a persisted audit record.
    /// </summary>
    public AuditRecord
    {
        if (EventId == Guid.Empty)
        {
            throw new ArgumentException("Event id is required.", nameof(EventId));
        }

        if (TenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(TenantId));
        }

        if (string.IsNullOrWhiteSpace(Action))
        {
            throw new ArgumentException("Action is required.", nameof(Action));
        }

        if (string.IsNullOrWhiteSpace(ResourceType))
        {
            throw new ArgumentException("Resource type is required.", nameof(ResourceType));
        }

        if (string.IsNullOrWhiteSpace(Outcome))
        {
            throw new ArgumentException("Outcome is required.", nameof(Outcome));
        }
    }
}
