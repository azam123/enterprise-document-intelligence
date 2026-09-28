using EnterpriseDocumentIntelligence.AuditService.Application;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;

namespace EnterpriseDocumentIntelligence.AuditService.Api;

/// <summary>
/// Compatibility adapter for callers that still resolve the processor from the API assembly.
/// The actual audit processing logic lives in the Application layer.
/// </summary>
public sealed class AuditEventProcessor(
    Application.AuditEventProcessor applicationProcessor)
{
    /// <summary>
    /// Delegates an audit message to the Application layer.
    /// </summary>
    public Task<bool> RecordAsync(
        AuditRequested request,
        CancellationToken cancellationToken = default)
    {
        return applicationProcessor.RecordAsync(
            request,
            cancellationToken);
    }
}
