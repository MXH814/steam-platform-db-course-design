namespace SteamPlatform.Application.Auth;

public static class AuthRoles
{
    public static bool IsAdminRole(string role) =>
        role.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) ||
        role.Equals("SUPER_ADMIN", StringComparison.OrdinalIgnoreCase) ||
        role.Equals("RISK_ADMIN", StringComparison.OrdinalIgnoreCase) ||
        role.Equals("CUSTOMER_SERVICE", StringComparison.OrdinalIgnoreCase);

    public static bool IsKnownRole(string? role) =>
        !string.IsNullOrWhiteSpace(role) && (
            role.Equals("PLAYER", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("DEVELOPER", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("SUPER_ADMIN", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("AUDITOR", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("RISK_ADMIN", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("CUSTOMER_SERVICE", StringComparison.OrdinalIgnoreCase));

    public static bool RoleMatches(string actualRole, string requiredRole) =>
        requiredRole.Equals("ADMIN", StringComparison.OrdinalIgnoreCase)
            ? IsAdminRole(actualRole)
            : actualRole.Equals(requiredRole, StringComparison.OrdinalIgnoreCase);
}
