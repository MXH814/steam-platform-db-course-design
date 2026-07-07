using SteamPlatform.Api.Features.Auth;
using SteamPlatform.Application.Common;
using SteamPlatform.Application.CoreTransactions;

namespace SteamPlatform.Api.Features.CoreTransactions;

public static class CoreTransactionEndpointExtensions
{
    public static IEndpointRouteBuilder MapCoreTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var wallet = app.MapGroup("/api/wallet").WithTags("Wallet");

        wallet.MapGet("", async (
            ICoreTransactionService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            return Results.Ok(await service.GetWalletAsync(claims!, cancellationToken));
        });

        wallet.MapGet("/transactions", async (
            int? limit,
            ICoreTransactionService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            return Results.Ok(await service.ListWalletTransactionsAsync(claims!, limit ?? 50, cancellationToken));
        });

        var orders = app.MapGroup("/api/orders").WithTags("Orders");

        orders.MapPost("", async (
            CreateOrderRequest request,
            ICoreTransactionService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(request.GameId, request.IdempotencyKey))
            {
                return Results.BadRequest("GameId and IdempotencyKey are required.");
            }

            return Results.Created("/api/orders", await service.BuyGameAsync(claims!, request, cancellationToken));
        });

        orders.MapGet("", async (
            ICoreTransactionService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            return Results.Ok(await service.ListOrdersAsync(claims!, cancellationToken));
        });

        orders.MapGet("{orderId}", async (
            string orderId,
            ICoreTransactionService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(orderId))
            {
                return Results.BadRequest("OrderId is required.");
            }

            return Results.Ok(await service.GetOrderAsync(claims!, orderId, cancellationToken));
        });

        app.MapPost("/api/games/{gameId}/free-claim", async (
            string gameId,
            ICoreTransactionService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(gameId))
            {
                return Results.BadRequest("GameId is required.");
            }

            return Results.Created("/api/orders", await service.ClaimFreeGameAsync(claims!, gameId, cancellationToken));
        }).WithTags("Games");

        var library = app.MapGroup("/api/library").WithTags("Library");

        library.MapGet("", async (
            ICoreTransactionService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            return Results.Ok(await service.ListLibraryAsync(claims!, cancellationToken));
        });

        library.MapPost("{gameId}/playtime", async (
            string gameId,
            UpdatePlaytimeRequest request,
            ICoreTransactionService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(gameId))
            {
                return Results.BadRequest("GameId is required.");
            }

            if (request.MinutesToAdd <= 0)
            {
                return Results.BadRequest("MinutesToAdd must be greater than 0.");
            }

            return Results.Ok(await service.AddPlaytimeAsync(claims!, gameId, request, cancellationToken));
        });

        return app;
    }
}
