using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace EnterpriseDocumentIntelligence.BuildingBlocks.Security;

public interface ICurrentUser
{
    Guid UserId { get; }
    Guid TenantId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    string? CorrelationId { get; }
}

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal User =>
        httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();

    public bool IsAuthenticated =>
        User.Identity?.IsAuthenticated == true;

    public Guid UserId =>
        Guid.TryParse(
            User.FindFirstValue("oid") ??
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var userId)
            ? userId
            : Guid.Empty;

    public Guid TenantId =>
        Guid.TryParse(User.FindFirstValue("tid"), out var tenantId)
            ? tenantId
            : Guid.Empty;

    public bool IsInRole(string role) =>
        User.IsInRole(role);

    public string? CorrelationId =>
        httpContextAccessor.HttpContext?.TraceIdentifier;
}

public static class Roles
{
    public const string Reader = "Document.Reader";
    public const string Contributor = "Document.Contributor";
    public const string Administrator = "Document.Administrator";
}