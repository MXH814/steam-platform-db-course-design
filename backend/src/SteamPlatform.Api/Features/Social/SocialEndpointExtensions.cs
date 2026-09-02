using SteamPlatform.Api.Features.Auth;
using SteamPlatform.Api.Features.Games;
using SteamPlatform.Application.Common;
using SteamPlatform.Application.Games;
using SteamPlatform.Application.Social;

namespace SteamPlatform.Api.Features.Social;

public static class SocialEndpointExtensions
{
    public static IEndpointRouteBuilder MapSocialEndpoints(this IEndpointRouteBuilder app)
    {
        var social = app.MapGroup("/api").WithTags("Social");

        social.MapGet("/friends", async (ISocialService service, HttpContext context, CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(context, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            return Results.Ok(await service.ListFriendsAsync(claims!.PrincipalId, cancellationToken));
        });

        social.MapGet("/games/{gameId}/friends-who-play", async (
            string gameId,
            IGameService gameService,
            ISocialService service,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(context, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(gameId))
            {
                return Results.BadRequest("GameId is required.");
            }

            if (await GameVisibilityGuard.DenyHiddenAsync(gameId, gameService, context, cancellationToken) is { } hiddenGame)
            {
                return hiddenGame;
            }

            return Results.Ok(await service.ListFriendsWhoPlayAsync(claims!.PrincipalId, gameId, cancellationToken));
        });

        social.MapPost("/friends/{targetUserId}", async (
            string targetUserId,
            ISocialService service,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(context, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(targetUserId))
            {
                return Results.BadRequest("TargetUserId is required.");
            }

            return Results.Ok(await service.RequestFriendAsync(claims!.PrincipalId, targetUserId, cancellationToken));
        });

        social.MapPost("/friends/requests/{relationId}/accept", async (
            string relationId,
            ISocialService service,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(context, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(relationId))
            {
                return Results.BadRequest("RelationId is required.");
            }

            return Results.Ok(await service.AcceptFriendAsync(claims!.PrincipalId, relationId, cancellationToken));
        });

        social.MapGet("/friends/{friendUserId}/messages", async (
            string friendUserId,
            int? limit,
            ISocialService service,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(context, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(friendUserId))
            {
                return Results.BadRequest("FriendUserId is required.");
            }

            return Results.Ok(await service.ListMessagesAsync(claims!.PrincipalId, friendUserId, limit ?? 50, cancellationToken));
        });

        social.MapPost("/friends/{friendUserId}/messages", async (
            string friendUserId,
            SendDirectMessageRequest request,
            ISocialService service,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(context, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(friendUserId, request.Content))
            {
                return Results.BadRequest("FriendUserId and Content are required.");
            }

            return Results.Ok(await service.SendMessageAsync(claims!.PrincipalId, friendUserId, request, cancellationToken));
        });

        social.MapGet("/games/{gameId}/review-interactions", async (
            string gameId,
            IGameService gameService,
            ISocialService service,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (InputGuards.IsBlank(gameId))
            {
                return Results.BadRequest("GameId is required.");
            }

            if (await GameVisibilityGuard.DenyHiddenAsync(gameId, gameService, context, cancellationToken) is { } denied)
            {
                return denied;
            }

            return Results.Ok(await service.ListReviewInteractionsAsync(gameId, TryGetPlayerId(context), cancellationToken));
        });

        social.MapPut("/reviews/{reviewId}/interaction", async (
            string reviewId,
            ReviewInteractionRequest request,
            ISocialService service,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(context, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(reviewId))
            {
                return Results.BadRequest("ReviewId is required.");
            }

            return Results.Ok(await service.SetReviewInteractionAsync(claims!.PrincipalId, reviewId, request, cancellationToken));
        });

        social.MapGet("/games/{gameId}/workshop", async (
            string gameId,
            IGameService gameService,
            ISocialService service,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (InputGuards.IsBlank(gameId))
            {
                return Results.BadRequest("GameId is required.");
            }

            if (await GameVisibilityGuard.DenyHiddenAsync(gameId, gameService, context, cancellationToken) is { } denied)
            {
                return denied;
            }

            return Results.Ok(await service.ListWorkshopItemsAsync(gameId, TryGetPlayerId(context), cancellationToken));
        });

        social.MapPut("/workshop/{workshopItemId}/subscription", async (
            string workshopItemId,
            WorkshopSubscriptionRequest request,
            ISocialService service,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(context, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(workshopItemId))
            {
                return Results.BadRequest("WorkshopItemId is required.");
            }

            return Results.Ok(await service.SetWorkshopSubscriptionAsync(claims!.PrincipalId, workshopItemId, request, cancellationToken));
        });

        social.MapGet("/notifications", async (
            int? limit,
            ISocialService service,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(context, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            return Results.Ok(await service.ListNotificationsAsync(claims!.PrincipalId, limit ?? 50, cancellationToken));
        });

        social.MapPut("/notifications/{notificationId}/read", async (
            string notificationId,
            ISocialService service,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(context, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(notificationId))
            {
                return Results.BadRequest("NotificationId is required.");
            }

            return Results.Ok(await service.MarkNotificationReadAsync(claims!.PrincipalId, notificationId, cancellationToken));
        });

        return app;
    }

    private static string? TryGetPlayerId(HttpContext context) =>
        EndpointGuards.TryReadClaims(context.User, out var claims) &&
        string.Equals(claims?.Role, "PLAYER", StringComparison.OrdinalIgnoreCase)
            ? claims!.PrincipalId
            : null;
}
