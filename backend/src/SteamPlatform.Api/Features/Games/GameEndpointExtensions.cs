using SteamPlatform.Api.Features.Auth;
using SteamPlatform.Application.Games;
using SteamPlatform.Shared;

namespace SteamPlatform.Api.Features.Games;

public static class GameEndpointExtensions
{
    public static IEndpointRouteBuilder MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var games = app.MapGroup("/api/games").WithTags("Games");

        games.MapGet("", async (
            string? keyword,
            string? status,
            string? developerId,
            decimal? minPrice,
            decimal? maxPrice,
            string? reputation,
            int? page,
            int? pageSize,
            IGameService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var normalizedStatus = status?.Trim().ToUpperInvariant();
            var normalizedDeveloperId = developerId?.Trim();
            if (normalizedStatus == "OFFLINE")
            {
                if (EndpointGuards.DenyUnless(httpContext, out var claims, "DEVELOPER", "ADMIN") is { } denied)
                {
                    return denied;
                }

                if (string.Equals(claims!.Role, "DEVELOPER", StringComparison.OrdinalIgnoreCase))
                {
                    if (normalizedDeveloperId is not null &&
                        !string.Equals(normalizedDeveloperId, claims.PrincipalId, StringComparison.OrdinalIgnoreCase))
                    {
                        return Results.Forbid();
                    }

                    normalizedDeveloperId = claims.PrincipalId;
                }
            }

            var query = new GameListQuery(
                keyword,
                normalizedStatus,
                normalizedDeveloperId,
                minPrice,
                maxPrice,
                reputation,
                page ?? 1,
                pageSize ?? 20);

            var result = await service.ListAsync(query, cancellationToken);
            return Results.Ok(ApiResponse<PagedResponse<GameListItemResponse>>.Success(result));
        });

        games.MapGet("{gameId}", async (
            string gameId,
            IGameService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetDetailAsync(gameId, cancellationToken);
            if (!GameVisibilityGuard.CanView(httpContext, result))
            {
                return GameVisibilityGuard.HiddenResult();
            }

            return Results.Ok(ApiResponse<GameDetailResponse>.Success(result));
        });

        games.MapGet("{gameId}/reviews/summary", async (
            string gameId,
            IGameService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (await GameVisibilityGuard.DenyHiddenAsync(gameId, service, httpContext, cancellationToken) is { } denied)
            {
                return denied;
            }

            var result = await service.GetReviewSummaryAsync(gameId, cancellationToken);
            return Results.Ok(ApiResponse<ReviewSummaryResponse>.Success(result));
        });

        games.MapGet("{gameId}/achievements/summary", async (
            string gameId,
            IGameService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (await GameVisibilityGuard.DenyHiddenAsync(gameId, service, httpContext, cancellationToken) is { } denied)
            {
                return denied;
            }

            var result = await service.GetAchievementSummaryAsync(gameId, cancellationToken);
            return Results.Ok(ApiResponse<AchievementSummaryResponse>.Success(result));
        });

        games.MapGet("{gameId}/content-packages", async (
            string gameId,
            IGameService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (await GameVisibilityGuard.DenyHiddenAsync(gameId, service, httpContext, cancellationToken) is { } denied)
            {
                return denied;
            }

            var result = await service.GetContentPackagesAsync(gameId, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyList<GameContentPackageResponse>>.Success(result));
        });

        games.MapGet("{gameId}/items/summary", async (
            string gameId,
            IGameService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (await GameVisibilityGuard.DenyHiddenAsync(gameId, service, httpContext, cancellationToken) is { } denied)
            {
                return denied;
            }

            var result = await service.GetItemSummaryAsync(gameId, cancellationToken);
            return Results.Ok(ApiResponse<GameItemSummaryResponse>.Success(result));
        });

        var developerGames = app.MapGroup("/api/developer/games").WithTags("Developer Games");

        developerGames.MapPost("", async (
            CreateGameRequest request,
            IGameService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "DEVELOPER") is { } denied)
            {
                return denied;
            }

            if (!string.Equals(claims!.PrincipalId, request.DevId, StringComparison.OrdinalIgnoreCase))
            {
                return Results.Forbid();
            }

            var result = await service.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/games/{result.GameId}", ApiResponse<GameDetailResponse>.Success(result));
        });

        developerGames.MapPut("{gameId}", async (
            string gameId,
            UpdateGameRequest request,
            IGameService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "DEVELOPER") is { } denied)
            {
                return denied;
            }

            var result = await service.UpdateAsync(gameId, claims!.PrincipalId, request, cancellationToken);
            return Results.Ok(ApiResponse<GameDetailResponse>.Success(result));
        });

        developerGames.MapDelete("{gameId}", async (
            string gameId,
            IGameService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "DEVELOPER") is { } denied)
            {
                return denied;
            }

            await service.DeleteAsync(gameId, claims!.PrincipalId, cancellationToken);
            return Results.NoContent();
        });

        var adminGames = app.MapGroup("/api/admin/games").WithTags("Admin Games");

        adminGames.MapPost("{gameId}/online", async (
            string gameId,
            IGameService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out _, "ADMIN") is { } denied)
            {
                return denied;
            }

            var result = await service.SetStatusAsync(gameId, "ONLINE", cancellationToken);
            return Results.Ok(ApiResponse<GameDetailResponse>.Success(result));
        });

        adminGames.MapPost("{gameId}/offline", async (
            string gameId,
            IGameService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out _, "ADMIN") is { } denied)
            {
                return denied;
            }

            var result = await service.SetStatusAsync(gameId, "OFFLINE", cancellationToken);
            return Results.Ok(ApiResponse<GameDetailResponse>.Success(result));
        });

        return app;
    }

}
