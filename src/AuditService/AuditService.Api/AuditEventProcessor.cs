using EnterpriseDocumentIntelligence.BuildingBlocks.Domain;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using Microsoft.EntityFrameworkCore;
public sealed class AuditEventProcessor(DocumentDbContext db)
{
    public async Task<bool> RecordAsync(AuditRequested request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.EventId == Guid.Empty) throw new ArgumentException("EventId is required.", nameof(request));
        if (request.TenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Action)) throw new ArgumentException("Action is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.ResourceType)) throw new ArgumentException("ResourceType is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Outcome)) throw new ArgumentException("Outcome is required.", nameof(request));
        if (await db.AuditEvents.AnyAsync(x => x.Id == request.EventId, cancellationToken)) return false;
        db.AuditEvents.Add(new AuditEvent(request.TenantId, request.ActorId, request.Action, request.ResourceType, request.ResourceId, request.Outcome, request.CorrelationId, request.MetadataJson, request.EventId));
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}