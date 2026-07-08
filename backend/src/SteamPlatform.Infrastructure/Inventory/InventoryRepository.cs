using Dapper;
using SteamPlatform.Application.Inventory;
using SteamPlatform.Infrastructure.Data;
using SteamPlatform.Shared;

namespace SteamPlatform.Infrastructure.Inventory;

public sealed class InventoryRepository(IDbConnectionFactory connectionFactory) : IInventoryRepository
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    public async Task<IReadOnlyList<ItemTemplateResponse>> ListTemplatesAsync(
        string? gameId,
        CancellationToken cancellationToken)
    {
        var normalizedGameId = NormalizeOptional(gameId);

        await using var connection = _connectionFactory.CreateConnection();
        var templates = await connection.QueryAsync<ItemTemplateRow>(new CommandDefinition(
            """
            select template_id as TemplateId,
                   game_id as GameId,
                   item_name as ItemName,
                   rarity as Rarity,
                   image_url as ImageUrl
              from item_template
             where (:GameId is null or game_id = :GameId)
             order by game_id, rarity, item_name, template_id
            """,
            new { GameId = normalizedGameId },
            cancellationToken: cancellationToken));

        return templates.Select(template => template.ToResponse()).ToList();
    }

    public async Task<IReadOnlyList<InventoryItemResponse>> ListAsync(
        string userId,
        string? gameId,
        CancellationToken cancellationToken)
    {
        var normalizedUserId = NormalizeRequired(userId, nameof(userId));
        var normalizedGameId = NormalizeOptional(gameId);

        await using var connection = _connectionFactory.CreateConnection();
        var items = await connection.QueryAsync<InventoryItemRow>(new CommandDefinition(
            """
            select i.item_id as ItemId,
                   t.template_id as TemplateId,
                   t.game_id as GameId,
                   t.item_name as ItemName,
                   t.rarity as Rarity,
                   t.image_url as ImageUrl,
                   i.wear_rating as WearRating,
                   i.status as Status,
                   i.acquire_time as AcquireTime
              from inventory_item i
              join item_template t on t.template_id = i.template_id
             where i.user_id = :UserId
               and (:GameId is null or t.game_id = :GameId)
             order by i.acquire_time desc, i.item_id desc
            """,
            new { UserId = normalizedUserId, GameId = normalizedGameId },
            cancellationToken: cancellationToken));

        return items.Select(item => item.ToResponse()).ToList();
    }

    public async Task<InventoryItemResponse> DropAsync(
        string userId,
        DropInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedUserId = NormalizeRequired(userId, nameof(userId));
        var normalizedGameId = NormalizeRequired(request.GameId, nameof(request.GameId));

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var ownedGameCount = await connection.QuerySingleAsync<int>(new CommandDefinition(
            """
            select count(1)
              from player_library
             where user_id = :UserId
               and game_id = :GameId
               and status = 'NORMAL'
            """,
            new { UserId = normalizedUserId, GameId = normalizedGameId },
            transaction,
            cancellationToken: cancellationToken));

        if (ownedGameCount == 0)
        {
            throw new BusinessRuleException("GAME_NOT_OWNED", "Current player does not own this game.");
        }

        var template = await connection.QueryFirstOrDefaultAsync<ItemTemplateDropRow>(new CommandDefinition(
            """
            select *
              from (
                select template_id as TemplateId,
                       game_id as GameId,
                       item_name as ItemName,
                       rarity as Rarity,
                       image_url as ImageUrl
                  from item_template
                 where game_id = :GameId
                 order by dbms_random.value
              )
             where rownum = 1
            """,
            new { GameId = normalizedGameId },
            transaction,
            cancellationToken: cancellationToken));

        if (template is null)
        {
            throw new ResourceNotFoundException("No item template exists for this game.");
        }

        var itemId = IdGenerator.NewId("ITEM");
        var transferId = IdGenerator.NewId("ITL");
        var wearRating = Math.Round((decimal)Random.Shared.NextDouble(), 4);
        var acquireTime = DateTime.UtcNow;

        await connection.ExecuteAsync(new CommandDefinition(
            """
            insert into inventory_item
              (item_id, template_id, user_id, wear_rating, status, acquire_time, version)
            values
              (:ItemId, :TemplateId, :UserId, :WearRating, 'NORMAL', :AcquireTime, 0)
            """,
            new
            {
                ItemId = itemId,
                template.TemplateId,
                UserId = normalizedUserId,
                WearRating = wearRating,
                AcquireTime = acquireTime
            },
            transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            """
            insert into item_transfer_ledger
              (transfer_id, item_id, from_user_id, to_user_id, transfer_type, transfer_time)
            values
              (:TransferId, :ItemId, null, :ToUserId, 'DROP', :TransferTime)
            """,
            new
            {
                TransferId = transferId,
                ItemId = itemId,
                ToUserId = normalizedUserId,
                TransferTime = acquireTime
            },
            transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        return new InventoryItemResponse(
            itemId,
            template.TemplateId,
            template.GameId,
            template.ItemName,
            template.Rarity,
            template.ImageUrl,
            wearRating,
            "NORMAL",
            acquireTime);
    }

    public async Task<IReadOnlyList<ItemTransferResponse>> ListTransfersAsync(
        string userId,
        string itemId,
        CancellationToken cancellationToken)
    {
        var normalizedUserId = NormalizeRequired(userId, nameof(userId));
        var normalizedItemId = NormalizeRequired(itemId, nameof(itemId));

        await using var connection = _connectionFactory.CreateConnection();
        var transfers = await connection.QueryAsync<ItemTransferRow>(new CommandDefinition(
            """
            select l.transfer_id as TransferId,
                   l.item_id as ItemId,
                   l.from_user_id as FromUserId,
                   l.to_user_id as ToUserId,
                   l.transfer_type as TransferType,
                   l.transfer_time as TransferTime
              from item_transfer_ledger l
             where l.item_id = :ItemId
               and exists (
                 select 1
                   from inventory_item i
                  where i.item_id = l.item_id
                    and (
                      i.user_id = :UserId
                      or exists (
                        select 1
                          from item_transfer_ledger visible_l
                         where visible_l.item_id = l.item_id
                           and (visible_l.from_user_id = :UserId or visible_l.to_user_id = :UserId)
                      )
                    )
               )
             order by l.transfer_time asc, l.transfer_id asc
            """,
            new { UserId = normalizedUserId, ItemId = normalizedItemId },
            cancellationToken: cancellationToken));

        return transfers.Select(transfer => transfer.ToResponse()).ToList();
    }

    private static string NormalizeRequired(string? value, string fieldName)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? throw new ArgumentException($"{fieldName} is required.")
            : normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private sealed class ItemTemplateDropRow
    {
        public string TemplateId { get; init; } = "";

        public string GameId { get; init; } = "";

        public string ItemName { get; init; } = "";

        public string Rarity { get; init; } = "";

        public string? ImageUrl { get; init; }
    }

    private sealed class ItemTemplateRow
    {
        public string TemplateId { get; init; } = "";
        public string GameId { get; init; } = "";
        public string ItemName { get; init; } = "";
        public string Rarity { get; init; } = "";
        public string? ImageUrl { get; init; }

        public ItemTemplateResponse ToResponse() => new(TemplateId, GameId, ItemName, Rarity, ImageUrl);
    }

    private sealed class InventoryItemRow
    {
        public string ItemId { get; init; } = "";
        public string TemplateId { get; init; } = "";
        public string GameId { get; init; } = "";
        public string ItemName { get; init; } = "";
        public string Rarity { get; init; } = "";
        public string? ImageUrl { get; init; }
        public decimal? WearRating { get; init; }
        public string Status { get; init; } = "";
        public DateTime AcquireTime { get; init; }

        public InventoryItemResponse ToResponse() => new(
            ItemId,
            TemplateId,
            GameId,
            ItemName,
            Rarity,
            ImageUrl,
            WearRating,
            Status,
            AcquireTime);
    }

    private sealed class ItemTransferRow
    {
        public string TransferId { get; init; } = "";
        public string ItemId { get; init; } = "";
        public string? FromUserId { get; init; }
        public string ToUserId { get; init; } = "";
        public string TransferType { get; init; } = "";
        public DateTime TransferTime { get; init; }

        public ItemTransferResponse ToResponse() => new(TransferId, ItemId, FromUserId, ToUserId, TransferType, TransferTime);
    }
}
