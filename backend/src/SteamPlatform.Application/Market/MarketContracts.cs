namespace SteamPlatform.Application.Market;

public interface IMarketRepository
{
    Task<IReadOnlyList<MarketListingDto>> GetListingsAsync(string? gameId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MarketOrderDto>> GetOrdersAsync(string? userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MarketTradeDto>> GetTradesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<MarketPricePointDto>> GetPriceHistoryAsync(string templateId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ItemTransferDto>> GetItemTransfersAsync(string itemId, CancellationToken cancellationToken);

    Task<MarketOrderDto> CreateBuyOrderAsync(string userId, CreateMarketOrderRequest request, CancellationToken cancellationToken);

    Task<MarketOrderDto> CreateSellOrderAsync(string userId, CreateMarketOrderRequest request, CancellationToken cancellationToken);

    Task CancelOrderAsync(string userId, string marketOrderId, CancellationToken cancellationToken);

    Task<MarketTradeDto> MatchAsync(MatchMarketRequest request, CancellationToken cancellationToken);
}

public sealed record CreateMarketOrderRequest(
    string OrderType,
    string TemplateId,
    string? ItemId,
    decimal TargetPrice);

public sealed record MatchMarketRequest(string? TemplateId);

public sealed class MarketListingDto
{
    public string TemplateId { get; init; } = "";
    public string GameId { get; init; } = "";
    public string ItemName { get; init; } = "";
    public string Rarity { get; init; } = "";
    public string? ImageUrl { get; init; }
    public decimal? HighestBuyPrice { get; init; }
    public decimal? LowestSellPrice { get; init; }
    public int ActiveBuyCount { get; init; }
    public int ActiveSellCount { get; init; }
}

public sealed class MarketOrderDto
{
    public string MarketOrderId { get; init; } = "";
    public string UserId { get; init; } = "";
    public string TemplateId { get; init; } = "";
    public string ItemName { get; init; } = "";
    public string OrderType { get; init; } = "";
    public string? ItemId { get; init; }
    public decimal TargetPrice { get; init; }
    public decimal FrozenAmount { get; init; }
    public string Status { get; init; } = "";
    public DateTime CreateTime { get; init; }
}

public sealed class MarketTradeDto
{
    public string TradeId { get; init; } = "";
    public string BuyOrderId { get; init; } = "";
    public string SellOrderId { get; init; } = "";
    public string TemplateId { get; init; } = "";
    public string ItemName { get; init; } = "";
    public string ItemId { get; init; } = "";
    public string BuyerId { get; init; } = "";
    public string SellerId { get; init; } = "";
    public decimal TradePrice { get; init; }
    public decimal PlatformFee { get; init; }
    public string Currency { get; init; } = "";
    public DateTime TradeTime { get; init; }
}

public sealed class MarketPricePointDto
{
    public DateTime TradeDate { get; init; }
    public int TradeCount { get; init; }
    public decimal MinPrice { get; init; }
    public decimal MaxPrice { get; init; }
    public decimal AveragePrice { get; init; }
    public decimal LastPrice { get; init; }
}

public sealed class ItemTransferDto
{
    public string TransferId { get; init; } = "";
    public string ItemId { get; init; } = "";
    public string ItemName { get; init; } = "";
    public string? FromUserId { get; init; }
    public string ToUserId { get; init; } = "";
    public string TransferType { get; init; } = "";
    public DateTime TransferTime { get; init; }
}
