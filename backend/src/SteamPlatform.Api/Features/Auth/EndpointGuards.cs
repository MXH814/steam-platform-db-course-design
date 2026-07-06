namespace SteamPlatform.Api.Features.Auth;

public static class EndpointGuards
{
    public static bool IsBlank(params string?[] values) =>
        values.Any(static value => string.IsNullOrWhiteSpace(value));

    public static bool IsAdminRole(string role) =>
        role.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) ||
        role.Equals("SUPER_ADMIN", StringComparison.OrdinalIgnoreCase) ||
        role.Equals("RISK_ADMIN", StringComparison.OrdinalIgnoreCase) ||
        role.Equals("CUSTOMER_SERVICE", StringComparison.OrdinalIgnoreCase);

    public static IResult? DenyUnless(HttpRequest httpRequest, IAuthService auth, out AuthClaims? claims, params string[] roles)
    {
        claims = auth.ValidateToken(httpRequest.Headers.Authorization.ToString());
        if (claims is null)
        {
            return Results.Unauthorized();
        }

        var actualRole = claims.Role;
        return roles.Any(role => RoleMatches(actualRole, role))
            ? null
            : Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    private static bool RoleMatches(string actualRole, string requiredRole) =>
        requiredRole.Equals("ADMIN", StringComparison.OrdinalIgnoreCase)
            ? IsAdminRole(actualRole)
            : actualRole.Equals(requiredRole, StringComparison.OrdinalIgnoreCase);
}
