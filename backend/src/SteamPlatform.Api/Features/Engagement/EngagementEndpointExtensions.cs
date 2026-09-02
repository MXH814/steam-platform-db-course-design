using SteamPlatform.Api.Features.Auth;
using SteamPlatform.Api.Features.Games;
using SteamPlatform.Application.Common;
using SteamPlatform.Application.Engagement;
using SteamPlatform.Application.Games;

namespace SteamPlatform.Api.Features.Engagement;

public static class EngagementEndpointExtensions
{
    public static IEndpointRouteBuilder MapEngagementEndpoints(this IEndpointRouteBuilder app)
    {
        var engagement = app.MapGroup("/api").WithTags("Community engagement");

        engagement.MapGet("/profiles/{userId}", async (
            string userId, IEngagementService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (InputGuards.IsBlank(userId))
            {
                return Results.BadRequest("UserId is required.");
            }

            return Results.Ok(await service.GetProfileAsync(userId, TryGetPlayerId(context), cancellationToken));
        });

        engagement.MapGet("/profile", async (
            IEngagementService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(context, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            return Results.Ok(await service.GetProfileAsync(claims!.PrincipalId, claims.PrincipalId, cancellationToken));
        });

        engagement.MapPut("/profile", async (
            UpdatePlayerProfileRequest request, IEngagementService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(context, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            return Results.Ok(await service.UpdateProfileAsync(claims!.PrincipalId, request, cancellationToken));
        });

        engagement.MapPut("/profile/featured-badge", async (
            SetFeaturedBadgeRequest request, IEngagementService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(context, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            return Results.Ok(await service.SetFeaturedBadgeAsync(claims!.PrincipalId, request, cancellationToken));
        });

        engagement.MapGet("/players/search", async (
            string? query, int? limit, IEngagementService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(context, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            return Results.Ok(await service.SearchPlayersAsync(claims!.PrincipalId, query, limit ?? 12, cancellationToken));
        });

        engagement.MapGet("/trade-offers", async (
            string? status, IEngagementService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(context, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            return Results.Ok(await service.ListTradeOffersAsync(claims!.PrincipalId, status, cancellationToken));
        });

        engagement.MapGet("/players/{targetUserId}/tradeable-inventory", async (
            string targetUserId, IEngagementService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(context, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(targetUserId))
            {
                return Results.BadRequest("TargetUserId is required.");
            }

            return Results.Ok(await service.ListTradeableInventoryAsync(claims!.PrincipalId, targetUserId, cancellationToken));
        });

        engagement.MapPost("/trade-offers", async (
            CreateTradeOfferRequest request, IEngagementService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(context, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            return Results.Ok(await service.CreateTradeOfferAsync(claims!.PrincipalId, request, cancellationToken));
        });

        engagement.MapPost("/trade-offers/{offerId}/actions", async (
            string offerId, TradeOfferActionRequest request, IEngagementService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(context, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(offerId))
            {
                return Results.BadRequest("OfferId is required.");
            }

            return Results.Ok(await service.RespondTradeOfferAsync(claims!.PrincipalId, offerId, request, cancellationToken));
        });

        engagement.MapGet("/community/posts", async (
            string? gameId, int? limit, IGameService gameService, IEngagementService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (!string.IsNullOrWhiteSpace(gameId) &&
                await GameVisibilityGuard.DenyHiddenAsync(gameId, gameService, context, cancellationToken) is { } denied)
            {
                return denied;
            }

            return Results.Ok(await service.ListCommunityPostsAsync(TryGetPlayerId(context), gameId, limit ?? 30, cancellationToken));
        });

        engagement.MapPost("/community/posts", async (
            CreateCommunityPostRequest request, IEngagementService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(context, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            return Results.Ok(await service.CreateCommunityPostAsync(claims!.PrincipalId, request, cancellationToken));
        });

        engagement.MapPut("/community/posts/{postId}/reaction", async (
            string postId, SetPostReactionRequest request, IEngagementService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(context, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(postId))
            {
                return Results.BadRequest("PostId is required.");
            }

            return Results.Ok(await service.SetPostReactionAsync(claims!.PrincipalId, postId, request, cancellationToken));
        });

        engagement.MapGet("/games/{gameId}/discussions", async (
            string gameId, int? limit, IGameService gameService, IEngagementService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (InputGuards.IsBlank(gameId))
            {
                return Results.BadRequest("GameId is required.");
            }

            if (await GameVisibilityGuard.DenyHiddenAsync(gameId, gameService, context, cancellationToken) is { } denied)
            {
                return denied;
            }

            return Results.Ok(await service.ListDiscussionTopicsAsync(gameId, limit ?? 30, cancellationToken));
        });

        engagement.MapGet("/community/discussions/{topicId}", async (
            string topicId, IEngagementService service, CancellationToken cancellationToken) =>
        {
            if (InputGuards.IsBlank(topicId))
            {
                return Results.BadRequest("TopicId is required.");
            }

            return Results.Ok(await service.GetDiscussionTopicAsync(topicId, cancellationToken));
        });

        engagement.MapPost("/community/discussions", async (
            CreateDiscussionTopicRequest request, IEngagementService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(context, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            return Results.Ok(await service.CreateDiscussionTopicAsync(claims!.PrincipalId, request, cancellationToken));
        });

        engagement.MapPost("/community/discussions/{topicId}/replies", async (
            string topicId, CreateDiscussionReplyRequest request, IEngagementService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(context, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(topicId))
            {
                return Results.BadRequest("TopicId is required.");
            }

            return Results.Ok(await service.ReplyToDiscussionAsync(claims!.PrincipalId, topicId, request, cancellationToken));
        });

        return app;
    }

    private static string? TryGetPlayerId(HttpContext context) =>
        EndpointGuards.TryReadClaims(context.User, out var claims) &&
        string.Equals(claims?.Role, "PLAYER", StringComparison.OrdinalIgnoreCase)
            ? claims!.PrincipalId
            : null;
}
