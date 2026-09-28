using EnterpriseDocumentIntelligence.AuditService.Application;
using EnterpriseDocumentIntelligence.AuditService.Domain;
using EnterpriseDocumentIntelligence.BuildingBlocks.Messaging;
using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseDocumentIntelligence.AuditService.Api;

/// <summary>
/// Request payload used to create an audit record from an authenticated API caller.
/// </summary>
public sealed record AuditRequest(
    string Action,
    string ResourceType,
    Guid? ResourceId,
    string Outcome,
    string? MetadataJson);

/// <summary>
/// Provides secured audit recording and tenant-scoped audit queries.
/// </summary>
[ApiController]
[Route("api/v1/audit")]
[Authorize(Roles = Roles.Administrator)]
public sealed class AuditController(
    EnterpriseDocumentIntelligence.AuditService.Application.AuditEventProcessor auditEventProcessor,
    AuditApplication auditApplication,
    ICurrentUser user,
    ILogger<AuditController> logger) : ControllerBase
{
    /// <summary>
    /// Records an audit event for the authenticated tenant.
    /// </summary>
    /// <param name="request">The action, resource, outcome, and optional metadata to audit.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>The audit event identifier when accepted.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(AuditResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuditResponse>> RecordAsync(
        [FromBody] AuditRequest request,
        CancellationToken cancellationToken)
    {
        if (user.TenantId == Guid.Empty || user.UserId == Guid.Empty)
        {
            return Unauthorized();
        }

        var auditRequest = new AuditRequested(
            Guid.NewGuid(),
            user.TenantId,
            user.UserId,
            request.Action,
            request.ResourceType,
            request.ResourceId,
            request.Outcome,
            user.CorrelationId,
            request.MetadataJson);

        var recorded = await auditEventProcessor.RecordAsync(
            auditRequest,
            cancellationToken);

        logger.LogInformation(
            "Audit event {EventId} processed. Recorded={Recorded} TenantId={TenantId} Action={Action}",
            auditRequest.EventId,
            recorded,
            user.TenantId,
            auditRequest.Action);

        return Accepted(
            new AuditResponse(
                auditRequest.EventId,
                recorded));
    }

    /// <summary>
    /// Retrieves the authenticated tenant's audit records.
    /// </summary>
    /// <param name="resourceId">Optional resource identifier filter.</param>
    /// <param name="from">Optional lower bound for the event creation timestamp.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>Up to 500 most recent matching audit records.</returns>
    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<AuditRecord>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AuditRecord>>> GetAsync(
        [FromQuery] Guid? resourceId,
        [FromQuery] DateTimeOffset? from,
        CancellationToken cancellationToken)
    {
        if (user.TenantId == Guid.Empty)
        {
            return Unauthorized();
        }

        var records = await auditApplication.QueryAsync(
            user.TenantId,
            resourceId,
            from,
            cancellationToken);

        return Ok(records);
    }
}

/// <summary>
/// Response returned after an audit event has been processed.
/// </summary>
public sealed record AuditResponse(
    Guid EventId,
    bool Recorded);
