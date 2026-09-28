using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace EnterpriseDocumentIntelligence.BuildingBlocks.Security;

/// <summary>
/// Provides access to the authenticated request identity and tenant context.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Gets the current user's object identifier.
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Gets the current tenant identifier.
    /// </summary>
    Guid TenantId { get; }

    /// <summary>
    /// Gets a value indicating whether the current request is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Determines whether the current user has the supplied role.
    /// </summary>
    bool IsInRole(string role);

    /// <summary>
    /// Gets the current request trace/correlation identifier.
    /// </summary>
    string? CorrelationId { get; }
}

/// <summary>
/// Resolves the current user from the ASP.NET Core HTTP context.
/// </summary>
/// <param name="httpContextAccessor">Accessor for the current HTTP context.</param>
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal User =>
        httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();

    /// <inheritdoc />
    public bool IsAuthenticated =>
        User.Identity?.IsAuthenticated == true;

    /// <inheritdoc />
    public Guid UserId =>
        Guid.TryParse(
            User.FindFirstValue("oid") ??
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var userId)
            ? userId
            : Guid.Empty;

    /// <inheritdoc />
    public Guid TenantId =>
        Guid.TryParse(
            User.FindFirstValue("tid"),
            out var tenantId)
            ? tenantId
            : Guid.Empty;

    /// <inheritdoc />
    public bool IsInRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        return User.IsInRole(role);
    }

    /// <inheritdoc />
    public string? CorrelationId =>
        httpContextAccessor.HttpContext?.TraceIdentifier;
}

/// <summary>
/// Standard application roles used by the document intelligence platform.
/// </summary>
public static class Roles
{
    /// <summary>
    /// Allows read access to documents.
    /// </summary>
    public const string Reader = "Document.Reader";

    /// <summary>
    /// Allows document contribution operations.
    /// </summary>
    public const string Contributor = "Document.Contributor";

    /// <summary>
    /// Allows document administration operations.
    /// </summary>
    public const string Administrator = "Document.Administrator";
}