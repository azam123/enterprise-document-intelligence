namespace EnterpriseDocumentIntelligence.AuditService.Domain;

public sealed record AuditRecord(
    Guid EventId,
    string TenantId,
    string Action,
    string ResourceType,
    string ResourceId,
    string ActorId,
    DateTimeOffset OccurredAt,
    IReadOnlyDictionary<string, string> Metadata)
{
    public AuditRecord
    {
        if (EventId == Guid.Empty)
        {
            throw new ArgumentException("Event id is required.");
        }

        if (string.IsNullOrWhiteSpace(TenantId))
        {
            throw new ArgumentException("Tenant is required.");
        }

        if (string.IsNullOrWhiteSpace(Action))
        {
            throw new ArgumentException("Action is required.");
        }

        if (string.IsNullOrWhiteSpace(ResourceId))
        {
            throw new ArgumentException("Resource id is required.");
        }
    }
}
