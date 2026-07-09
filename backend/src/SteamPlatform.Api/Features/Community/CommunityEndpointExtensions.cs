using SteamPlatform.Api.Features.Auth;
using SteamPlatform.Application.Common;
using SteamPlatform.Application.Community;

namespace SteamPlatform.Api.Features.Community;

public static class CommunityEndpointExtensions
{
    public static IEndpointRouteBuilder MapCommunityEndpoints(this IEndpointRouteBuilder app)
    {
        var community = app.MapGroup("/api").WithTags("Community");

        community.MapGet("/games/{gameId}/reviews", async (
            string gameId,
            int? limit,
            IReviewRepository repository,
            CancellationToken cancellationToken) =>
        {
            if (InputGuards.IsBlank(gameId))
            {
                return Results.BadRequest("GameId is required.");
            }

            return Results.Ok(await repository.ListByGameAsync(gameId, limit ?? 50, cancellationToken));
        });

        community.MapPost("/games/{gameId}/reviews", async (
            string gameId,
            CreateReviewRequest request,
            IReviewRepository repository,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(gameId, request.Content))
            {
                return Results.BadRequest("GameId and Content are required.");
            }

            var review = await repository.CreateAsync(gameId, claims!.PrincipalId, request, cancellationToken);
            return Results.Created($"/api/reviews/{review.ReviewId}", review);
        });

        community.MapPut("/reviews/{reviewId}", async (
            string reviewId,
            UpdateReviewRequest request,
            IReviewRepository repository,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(reviewId, request.Content))
            {
                return Results.BadRequest("ReviewId and Content are required.");
            }

            return Results.Ok(await repository.UpdateAsync(reviewId, claims!.PrincipalId, request, cancellationToken));
        });

        community.MapGet("/reviews/{reviewId}/versions", async (
            string reviewId,
            IReviewRepository repository,
            CancellationToken cancellationToken) =>
        {
            if (InputGuards.IsBlank(reviewId))
            {
                return Results.BadRequest("ReviewId is required.");
            }

            return Results.Ok(await repository.ListVersionsAsync(reviewId, cancellationToken));
        });

        community.MapPost("/admin/reviews/{reviewId}/hide", async (
            string reviewId,
            IReviewRepository repository,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out _, "ADMIN") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(reviewId))
            {
                return Results.BadRequest("ReviewId is required.");
            }

            return Results.Ok(await repository.SetStatusAsync(reviewId, "HIDDEN", cancellationToken));
        });

        community.MapPost("/admin/reviews/{reviewId}/show", async (
            string reviewId,
            IReviewRepository repository,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out _, "ADMIN") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(reviewId))
            {
                return Results.BadRequest("ReviewId is required.");
            }

            return Results.Ok(await repository.SetStatusAsync(reviewId, "VISIBLE", cancellationToken));
        });

        community.MapGet("/games/{gameId}/achievements", async (
            string gameId,
            IAchievementRepository repository,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (InputGuards.IsBlank(gameId))
            {
                return Results.BadRequest("GameId is required.");
            }

            var userId = TryGetPlayerId(httpContext);
            return Results.Ok(await repository.ListByGameAsync(gameId, userId, cancellationToken));
        });

        community.MapPost("/achievements/{achId}/unlock", async (
            string achId,
            IAchievementRepository repository,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(achId))
            {
                return Results.BadRequest("AchId is required.");
            }

            return Results.Ok(await repository.UnlockAsync(claims!.PrincipalId, achId, cancellationToken));
        });

        return app;
    }

    private static string? TryGetPlayerId(HttpContext httpContext)
    {
        if (!EndpointGuards.TryReadClaims(httpContext.User, out var claims) || claims is null)
        {
            return null;
        }

        return string.Equals(claims.Role, "PLAYER", StringComparison.OrdinalIgnoreCase)
            ? claims.PrincipalId
            : null;
    }
}
