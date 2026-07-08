namespace SteamPlatform.Application.Inventory;

public sealed record InventoryItemResponse(
    string ItemId,
    string TemplateId,
    string GameId,
    string ItemName,
    string Rarity,
    string? ImageUrl,
    decimal? WearRating,
    string Status,
    DateTime AcquireTime);

public sealed record ItemTemplateResponse(
    string TemplateId,
    string GameId,
    string ItemName,
    string Rarity,
    string? ImageUrl);

public sealed record ItemTransferResponse(
    string TransferId,
    string ItemId,
    string? FromUserId,
    string ToUserId,
    string TransferType,
    DateTime TransferTime);

public sealed record DropInventoryItemRequest(string GameId);

public interface IInventoryRepository
{
    Task<IReadOnlyList<ItemTemplateResponse>> ListTemplatesAsync(
        string? gameId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<InventoryItemResponse>> ListAsync(
        string userId,
        string? gameId,
        CancellationToken cancellationToken);

    Task<InventoryItemResponse> DropAsync(
        string userId,
        DropInventoryItemRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ItemTransferResponse>> ListTransfersAsync(
        string userId,
        string itemId,
        CancellationToken cancellationToken);
}
