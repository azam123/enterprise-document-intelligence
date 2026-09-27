namespace EnterpriseDocumentIntelligence.SearchService.Application;

public static class SearchFilterBuilder
{
    public static string Build(
        Guid tenantId,
        Guid userId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Tenant is required.",
                nameof(tenantId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User is required.",
                nameof(userId));
        }

        return
            $"TenantId eq '{Escape(tenantId.ToString())}' and " +
            $"(not AllowedPrincipalIds/any() or " +
            $"AllowedPrincipalIds/any(p: p eq '{Escape(userId.ToString())}'))";
    }

    private static string Escape(string value) =>
        value.Replace("'", "''", StringComparison.Ordinal);
}