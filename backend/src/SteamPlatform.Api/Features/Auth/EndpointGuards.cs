using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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

    public static IResult? DenyUnless(HttpContext httpContext, out AuthClaims? claims, params string[] roles)
    {
        if (!TryReadClaims(httpContext.User, out claims))
        {
            return Results.Unauthorized();
        }

        var currentClaims = claims!;
        var actualRole = currentClaims.Role;
        return roles.Any(role => RoleMatches(actualRole, role))
            ? null
            : Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    public static bool TryReadClaims(ClaimsPrincipal user, out AuthClaims? claims)
    {
        claims = null;
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var role = user.FindFirstValue(AuthTokenValidation.RoleClaim);
        var principalId = user.FindFirstValue(AuthTokenValidation.PrincipalIdClaim);
        var account = user.FindFirstValue(AuthTokenValidation.AccountClaim);
        var exp = user.FindFirstValue(AuthTokenValidation.ExpiresAtClaim) ?? user.FindFirstValue(JwtRegisteredClaimNames.Exp);
        if (IsBlank(role, principalId, account, exp) ||
            !long.TryParse(exp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var expSeconds))
        {
            return false;
        }

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            return false;
        }

        claims = new AuthClaims(role!, principalId!, account!, expiresAt);
        return true;
    }

    private static bool RoleMatches(string actualRole, string requiredRole) =>
        requiredRole.Equals("ADMIN", StringComparison.OrdinalIgnoreCase)
            ? IsAdminRole(actualRole)
            : actualRole.Equals(requiredRole, StringComparison.OrdinalIgnoreCase);
}
