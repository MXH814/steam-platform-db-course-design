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

        return app;
    }
}