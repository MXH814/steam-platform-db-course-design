using System.Globalization;
using System.Security.Claims;
using SteamPlatform.Application.Common;

namespace SteamPlatform.Application.Auth;

public static class AuthClaimReader
{
    public static bool TryReadClaims(ClaimsPrincipal user, out AuthClaims? claims)
    {
        claims = null;
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var role = FindFirstValue(user, AuthTokenValidation.RoleClaim);
        var principalId = FindFirstValue(user, AuthTokenValidation.PrincipalIdClaim);
        var account = FindFirstValue(user, AuthTokenValidation.AccountClaim);
        var exp = FindFirstValue(user, AuthTokenValidation.ExpiresAtClaim) ?? FindFirstValue(user, "exp");
        if (InputGuards.IsBlank(role, principalId, account, exp) ||
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

    private static string? FindFirstValue(ClaimsPrincipal user, string claimType) =>
        user.FindFirst(claimType)?.Value;
}
