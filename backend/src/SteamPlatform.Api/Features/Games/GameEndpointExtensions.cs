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
            CancellationToken cancellationToken) =>
        {
            var query = new GameListQuery(
                keyword,
                status,
                developerId,
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
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetDetailAsync(gameId, cancellationToken);
            return Results.Ok(ApiResponse<GameDetailResponse>.Success(result));
        });

        games.MapGet("{gameId}/reviews/summary", async (
            string gameId,
            IGameService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetReviewSummaryAsync(gameId, cancellationToken);
            return Results.Ok(ApiResponse<ReviewSummaryResponse>.Success(result));
        });

        games.MapGet("{gameId}/achievements/summary", async (
            string gameId,
            IGameService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetAchievementSummaryAsync(gameId, cancellationToken);
            return Results.Ok(ApiResponse<AchievementSummaryResponse>.Success(result));
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
            if (EndpointGuards.DenyUnless(httpContext, out _, "DEVELOPER") is { } denied)
            {
                return denied;
            }

            var result = await service.UpdateAsync(gameId, request, cancellationToken);
            return Results.Ok(ApiResponse<GameDetailResponse>.Success(result));
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
