using SteamPlatform.Api.Features.Auth;
using SteamPlatform.Application.Common;
using SteamPlatform.Application.CoreTransactions;
using SteamPlatform.Shared;

namespace SteamPlatform.Api.Features.CoreTransactions;

public static class CoreTransactionEndpointExtensions
{
    private const int WalletIdempotencyKeyRequiredCode = 40001;
    private const int WalletNotFoundCode = 40401;
    private const int WalletHistoryNotFoundCode = 40402;
    private const int WalletInvalidAmountCode = 40901;
    private const int WalletIdempotencyConflictCode = 40902;

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

            try
            {
                return Results.Ok(ApiResponse<WalletSummary>.Success(await service.GetWalletAsync(claims!, cancellationToken)));
            }
            catch (ResourceNotFoundException exception)
            {
                return IsWalletMissing(exception) ? WalletNotFound(exception) : WalletHistoryNotFound(exception);
            }
        });

        wallet.MapPost("/recharge", async (
            RechargeWalletRequest request,
            ICoreTransactionService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(request.IdempotencyKey))
            {
                return Results.BadRequest(ApiResponse<object>.Failure(WalletIdempotencyKeyRequiredCode, "IDEMPOTENCY_KEY_REQUIRED: IdempotencyKey is required."));
            }

            try
            {
                var result = await service.RechargeWalletAsync(claims!, request, cancellationToken);
                return Results.Ok(ApiResponse<RechargeWalletResult>.Success(result));
            }
            catch (BusinessRuleException exception)
            {
                return WalletBusinessFailure(exception);
            }
            catch (ResourceNotFoundException exception)
            {
                return IsWalletMissing(exception) ? WalletNotFound(exception) : WalletHistoryNotFound(exception);
            }
        });

        wallet.MapGet("/transactions", async (
            int? page,
            int? pageSize,
            ICoreTransactionService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            try
            {
                var result = await service.ListWalletTransactionsAsync(claims!, page ?? 1, pageSize ?? 20, cancellationToken);
                return Results.Ok(ApiResponse<PagedResponse<WalletTransactionEntry>>.Success(result));
            }
            catch (ResourceNotFoundException exception)
            {
                return WalletNotFound(exception);
            }
        });

        wallet.MapGet("/history", async (
            int? page,
            int? pageSize,
            ICoreTransactionService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            try
            {
                var result = await service.ListWalletHistoryAsync(claims!, page ?? 1, pageSize ?? 20, cancellationToken);
                return Results.Ok(ApiResponse<PagedResponse<WalletHistoryEntry>>.Success(result));
            }
            catch (ResourceNotFoundException exception)
            {
                return WalletNotFound(exception);
            }
        });

        wallet.MapGet("/history/{historyId}", async (
            string historyId,
            ICoreTransactionService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(historyId))
            {
                return Results.BadRequest(ApiResponse<object>.Failure(40002, "HISTORY_ID_REQUIRED: HistoryId is required."));
            }

            try
            {
                var result = await service.GetWalletHistoryEntryAsync(claims!, historyId, cancellationToken);
                return Results.Ok(ApiResponse<WalletHistoryEntry>.Success(result));
            }
            catch (ResourceNotFoundException exception)
            {
                return IsWalletMissing(exception) ? WalletNotFound(exception) : WalletHistoryNotFound(exception);
            }
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

        var refunds = app.MapGroup("/api/refunds").WithTags("Refunds");

        refunds.MapPost("", async (
            CreateRefundRequest request,
            ICoreTransactionService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(request.OrderId, request.Reason))
            {
                return Results.BadRequest("OrderId and Reason are required.");
            }

            return Results.Created("/api/refunds", await service.CreateRefundAsync(claims!, request, cancellationToken));
        });

        refunds.MapGet("", async (
            ICoreTransactionService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            return Results.Ok(await service.ListRefundsAsync(claims!, cancellationToken));
        });

        var adminRefunds = app.MapGroup("/api/admin/refunds").WithTags("Admin Refunds");

        adminRefunds.MapGet("", async (
            ICoreTransactionService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "ADMIN") is { } denied)
            {
                return denied;
            }

            return Results.Ok(await service.ListAllRefundsAsync(claims!, cancellationToken));
        });

        adminRefunds.MapPost("{refundId}/approve", async (
            string refundId,
            AuditRefundRequest request,
            ICoreTransactionService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "ADMIN") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(refundId))
            {
                return Results.BadRequest("RefundId is required.");
            }

            return Results.Ok(await service.ApproveRefundAsync(claims!, refundId, request, cancellationToken));
        });

        adminRefunds.MapPost("{refundId}/reject", async (
            string refundId,
            AuditRefundRequest request,
            ICoreTransactionService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "ADMIN") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(refundId))
            {
                return Results.BadRequest("RefundId is required.");
            }

            return Results.Ok(await service.RejectRefundAsync(claims!, refundId, request, cancellationToken));
        });

        app.MapPost("/api/developer/cdkey-batches", async (
            CreateCdkeyBatchRequest request,
            ICoreTransactionService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "ADMIN", "DEVELOPER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(request.GameId, request.BatchNo))
            {
                return Results.BadRequest("GameId and BatchNo are required.");
            }

            if (request.Quantity <= 0)
            {
                return Results.BadRequest("Quantity must be greater than 0.");
            }

            return Results.Created("/api/developer/cdkey-batches", await service.CreateCdkeyBatchAsync(claims!, request, cancellationToken));
        }).WithTags("Developer CDKeys");

        app.MapPost("/api/cdkeys/redeem", async (
            RedeemCdkeyRequest request,
            ICoreTransactionService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(request.Cdkey))
            {
                return Results.BadRequest("Cdkey is required.");
            }

            return Results.Ok(await service.RedeemCdkeyAsync(claims!, request, cancellationToken));
        }).WithTags("CDKeys");

        return app;
    }

    private static IResult WalletBusinessFailure(BusinessRuleException exception) =>
        Results.Conflict(ApiResponse<object>.Failure(WalletBusinessCode(exception.Code), $"{exception.Code}: {exception.Message}"));

    private static IResult WalletNotFound(ResourceNotFoundException exception) =>
        Results.NotFound(ApiResponse<object>.Failure(WalletNotFoundCode, $"WALLET_NOT_FOUND: {exception.Message}"));

    private static IResult WalletHistoryNotFound(ResourceNotFoundException exception) =>
        Results.NotFound(ApiResponse<object>.Failure(WalletHistoryNotFoundCode, $"WALLET_HISTORY_NOT_FOUND: {exception.Message}"));

    private static bool IsWalletMissing(ResourceNotFoundException exception) =>
        exception.Message.Contains("Wallet does not exist", StringComparison.OrdinalIgnoreCase);

    private static int WalletBusinessCode(string code) =>
        code.Equals("INVALID_AMOUNT", StringComparison.OrdinalIgnoreCase)
            ? WalletInvalidAmountCode
            : WalletIdempotencyConflictCode;
}
