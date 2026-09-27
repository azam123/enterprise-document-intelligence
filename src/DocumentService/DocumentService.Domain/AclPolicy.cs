namespace EnterpriseDocumentIntelligence.DocumentService.Domain;

public static class AclPolicy
{
    public static void EnsurePrincipal(Guid principalId)
    {
        if (principalId == Guid.Empty)
            throw new ArgumentException("PrincipalId is required.", nameof(principalId));
    }

    public static void EnsurePermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new ArgumentException("Permission is required.", nameof(permission));

        if (!string.Equals(permission, "Read", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(permission, "Write", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Permission must be Read or Write.", nameof(permission));
    }
}
