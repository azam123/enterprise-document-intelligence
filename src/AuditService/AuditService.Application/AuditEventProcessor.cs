using System.Text.Json;
using EnterpriseDocumentIntelligence.AuditService.Domain;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;

namespace EnterpriseDocumentIntelligence.AuditService.Application;

/// <summary>
/// Converts audit messages from the messaging contract into application audit records.
/// </summary>
public sealed class AuditEventProcessor(AuditApplication auditApplication)
{
    /// <summary>
    /// Records an incoming audit message idempotently.
    /// </summary>
    public Task<bool> RecordAsync(
        AuditRequested request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var record = new AuditRecord(
            request.EventId,
            request.TenantId,
            request.ActorId,
            request.Action,
            request.ResourceType,
            request.ResourceId,
            request.Outcome,
            DateTimeOffset.UtcNow,
            request.CorrelationId,
            ParseMetadata(request.MetadataJson));

        return auditApplication.RecordAsync(
            record,
            cancellationToken);
    }

    /// <summary>
    /// Parses optional JSON metadata without allowing malformed metadata to bypass audit validation.
    /// </summary>
    private static IReadOnlyDictionary<string, string> ParseMetadata(
        string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return new Dictionary<string, string>();
        }

        using var document = JsonDocument.Parse(metadataJson);

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException(
                "Audit metadata must be a JSON object.",
                nameof(metadataJson));
        }

        return document.RootElement.EnumerateObject()
            .ToDictionary(
                property => property.Name,
                property => property.Value.ToString());
    }
}
