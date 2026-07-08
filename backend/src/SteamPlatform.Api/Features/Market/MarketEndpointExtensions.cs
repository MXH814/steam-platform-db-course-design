using SteamPlatform.Api.Features.Auth;
using SteamPlatform.Application.Market;
using SteamPlatform.Shared;

namespace SteamPlatform.Api.Features.Market;

public static class MarketEndpointExtensions
{
    public static IEndpointRouteBuilder MapMarketEndpoints(this IEndpointRouteBuilder app)
    {
        var market = app.MapGroup("/api").WithTags("Market");

        market.MapGet("/market", async (
            string? gameId,
            IMarketRepository repository,
            CancellationToken cancellationToken) =>
        {
            var listings = await repository.GetListingsAsync(NormalizeOptional(gameId), cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyList<MarketListingDto>>.Success(listings));
        });

        market.MapGet("/market/orders", async (
            HttpContext httpContext,
            IMarketRepository repository,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            var orders = await repository.GetOrdersAsync(claims!.PrincipalId, cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyList<MarketOrderDto>>.Success(orders));
        });

        market.MapPost("/market/orders", async (
            CreateMarketOrderRequest request,
            HttpContext httpContext,
            IMarketRepository repository,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            var normalized = NormalizeCreateOrder(request);

            var order = normalized.OrderType switch
            {
                "BUY" => await repository.CreateBuyOrderAsync(claims!.PrincipalId, normalized, cancellationToken),
                "SELL" => await repository.CreateSellOrderAsync(claims!.PrincipalId, normalized, cancellationToken),
                _ => throw new ArgumentException("挂单类型只能是 BUY 或 SELL。")
            };

            return Results.Ok(ApiResponse<MarketOrderDto>.Success(order));
        });

        market.MapPost("/market/orders/{marketOrderId}/cancel", async (
            string marketOrderId,
            HttpContext httpContext,
            IMarketRepository repository,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (string.IsNullOrWhiteSpace(marketOrderId))
            {
                throw new ArgumentException("市场挂单编号不能为空。");
            }

            await repository.CancelOrderAsync(claims!.PrincipalId, marketOrderId.Trim(), cancellationToken);
            return Results.Ok(ApiResponse<object>.Success(new { marketOrderId }));
        });

        market.MapPost("/market/match", async (
            MatchMarketRequest request,
            HttpContext httpContext,
            IMarketRepository repository,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out _, "PLAYER") is { } denied)
            {
                return denied;
            }

            var normalized = new MatchMarketRequest(NormalizeOptional(request.TemplateId));
            var trade = await repository.MatchAsync(normalized, cancellationToken);
            return Results.Ok(ApiResponse<MarketTradeDto>.Success(trade));
        });

        market.MapGet("/market/trades", async (
            IMarketRepository repository,
            CancellationToken cancellationToken) =>
        {
            var trades = await repository.GetTradesAsync(cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyList<MarketTradeDto>>.Success(trades));
        });

        market.MapGet("/market/items/{itemId}/transfers", async (
            string itemId,
            IMarketRepository repository,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new ArgumentException("饰品编号不能为空。");
            }

            var transfers = await repository.GetItemTransfersAsync(itemId.Trim(), cancellationToken);
            return Results.Ok(ApiResponse<IReadOnlyList<ItemTransferDto>>.Success(transfers));
        });

        return app;
    }

    private static CreateMarketOrderRequest NormalizeCreateOrder(CreateMarketOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TemplateId))
        {
            throw new ArgumentException("饰品模板不能为空。");
        }

        if (request.TargetPrice <= 0)
        {
            throw new ArgumentException("挂单价格必须大于 0。");
        }

        return new CreateMarketOrderRequest(
            request.OrderType.Trim().ToUpperInvariant(),
            request.TemplateId.Trim().ToUpperInvariant(),
            NormalizeOptional(request.ItemId),
            request.TargetPrice);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }
}
