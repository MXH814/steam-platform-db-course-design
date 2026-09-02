using SteamPlatform.Api.Features.Auth;
using SteamPlatform.Application.Games;
using SteamPlatform.Shared;

namespace SteamPlatform.Api.Features.Games;

internal static class GameVisibilityGuard
{
    public static async Task<IResult?> DenyHiddenAsync(
        string gameId,
        IGameService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var game = await service.GetDetailAsync(gameId, cancellationToken);
        return CanView(httpContext, game) ? null : HiddenResult();
    }

    public static bool CanView(HttpContext httpContext, GameDetailResponse game)
    {
        if (string.Equals(game.Status, "ONLINE", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!EndpointGuards.TryReadClaims(httpContext.User, out var claims))
        {
            return false;
        }

        return EndpointGuards.IsAdminRole(claims!.Role) ||
               string.Equals(claims.Role, "DEVELOPER", StringComparison.OrdinalIgnoreCase) &&
               string.Equals(claims.PrincipalId, game.DeveloperId, StringComparison.OrdinalIgnoreCase);
    }

    public static IResult HiddenResult() =>
        Results.NotFound(ApiResponse<object?>.Failure(40401, "Game does not exist."));
}
