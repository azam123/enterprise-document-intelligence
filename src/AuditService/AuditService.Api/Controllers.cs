using EnterpriseDocumentIntelligence.BuildingBlocks.Domain;
using EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;
using EnterpriseDocumentIntelligence.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseDocumentIntelligence.AuditService.Api;

public sealed record AuditRequest(string Action, string ResourceType, Guid? ResourceId, string Outcome, string? MetadataJson);

[ApiController]
[Route("api/v1/audit")]
[Authorize(Roles = Roles.Administrator)]
public sealed class AuditController(DocumentDbContext db, ICurrentUser user) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Record(AuditRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Action) || string.IsNullOrWhiteSpace(request.ResourceType))
            return BadRequest("Action and ResourceType are required.");
        if (user.TenantId == Guid.Empty) return Unauthorized();

        var entity = new AuditEvent(user.TenantId, user.UserId, request.Action, request.ResourceType,
            request.ResourceId, request.Outcome, user.CorrelationId, request.MetadataJson);
        db.AuditEvents.Add(entity);
        await db.SaveChangesAsync(ct);
        return Accepted(new { id = entity.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Get(DateTimeOffset? from, int take = 100, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 500);
        var query = db.AuditEvents.AsNoTracking().Where(x => x.TenantId == user.TenantId);
        if (from.HasValue) query = query.Where(x => x.CreatedAt >= from.Value);
        return Ok(await query.OrderByDescending(x => x.CreatedAt).Take(take).ToListAsync(ct));
    }
}