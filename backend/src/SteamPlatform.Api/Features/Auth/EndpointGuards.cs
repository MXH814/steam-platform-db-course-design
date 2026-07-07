using System.Security.Claims;
using SteamPlatform.Application.Auth;
using SteamPlatform.Application.Common;

namespace SteamPlatform.Api.Features.Auth;

public static class EndpointGuards
{
    public static bool IsBlank(params string?[] values) =>
        InputGuards.IsBlank(values);

    public static bool IsAdminRole(string role) =>
        AuthRoles.IsAdminRole(role);

    public static IResult? DenyUnless(HttpContext httpContext, out AuthClaims? claims, params string[] roles)
    {
        if (!TryReadClaims(httpContext.User, out claims))
        {
            return Results.Unauthorized();
        }

        var currentClaims = claims!;
        var actualRole = currentClaims.Role;
        return roles.Any(role => AuthRoles.RoleMatches(actualRole, role))
            ? null
            : Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    public static bool TryReadClaims(ClaimsPrincipal user, out AuthClaims? claims) =>
        AuthClaimReader.TryReadClaims(user, out claims);
}
