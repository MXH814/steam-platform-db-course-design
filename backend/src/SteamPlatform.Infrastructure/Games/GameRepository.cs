using Dapper;
using Oracle.ManagedDataAccess.Client;
using SteamPlatform.Application.Games;
using SteamPlatform.Infrastructure.Data;
using SteamPlatform.Shared;

namespace SteamPlatform.Infrastructure.Games;

public sealed class GameRepository(IDbConnectionFactory connectionFactory) : IGameRepository
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    public async Task<PagedResponse<GameListItemResponse>> ListAsync(GameListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        await using var connection = _connectionFactory.CreateConnection();
        var offset = (query.Page - 1) * query.PageSize;
        var parameters = new
        {
            Keyword = query.Keyword is null ? null : $"%{EscapeLike(query.Keyword.ToUpperInvariant())}%",
            query.Status,
            query.DeveloperId,
            query.MinPrice,
            query.MaxPrice,
            query.Reputation,
            Offset = offset,
            query.PageSize
        };

        const string filters = """
            where (:Status is null or g.status = :Status)
              and (:DeveloperId is null or g.dev_id = :DeveloperId)
              and (:Reputation is null or g.reputation = :Reputation)
              and (:Keyword is null or upper(g.game_name) like :Keyword escape '\')
              and (:MinPrice is null or (g.base_price * g.discount_rate) >= :MinPrice)
              and (:MaxPrice is null or (g.base_price * g.discount_rate) <= :MaxPrice)
            """;

        var total = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            $"""
            select count(*)
              from game g
              join developer d on d.dev_id = g.dev_id
            {filters}
            """,
            parameters,
            cancellationToken: cancellationToken));

        var games = await connection.QueryAsync<GameRow>(new CommandDefinition(
            $"""
            select
                   g.game_id as GameId,
                   g.game_name as GameName,
                   g.dev_id as DeveloperId,
                   d.company_name as DeveloperName,
                   g.base_price as BasePrice,
                   g.discount_rate as DiscountRate,
                   (g.base_price * g.discount_rate) as FinalPrice,
                   g.release_date as ReleaseDate,
                   g.reputation as Reputation,
                   g.status as Status
              from game g
              join developer d on d.dev_id = g.dev_id
            {filters}
             order by g.release_date desc, g.game_id desc
            offset :Offset rows fetch next :PageSize rows only
            """,
            parameters,
            cancellationToken: cancellationToken));

        return new PagedResponse<GameListItemResponse>(games.Select(game => game.ToListItem()).ToList(), query.Page, query.PageSize, total);
    }

    public async Task<GameDetailResponse?> GetDetailAsync(string gameId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        var game = await connection.QueryFirstOrDefaultAsync<GameRow>(new CommandDefinition(
            """
            select
                   g.game_id as GameId,
                   g.game_name as GameName,
                   g.dev_id as DeveloperId,
                   d.company_name as DeveloperName,
                   g.base_price as BasePrice,
                   g.discount_rate as DiscountRate,
                   (g.base_price * g.discount_rate) as FinalPrice,
                   g.release_date as ReleaseDate,
                   g.reputation as Reputation,
                   g.status as Status
              from game g
              join developer d on d.dev_id = g.dev_id
             where g.game_id = :GameId
            """,
            new { GameId = gameId },
            cancellationToken: cancellationToken));

        return game?.ToDetail();
    }

    public async Task<ReviewSummaryResponse> GetReviewSummaryAsync(string gameId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        var aggregate = await connection.QuerySingleAsync<ReviewSummaryAggregate>(new CommandDefinition(
            """
            with latest_versions as (
                select rv.review_id,
                       rv.is_recommend,
                       rv.content,
                       rv.create_time,
                       row_number() over (partition by rv.review_id order by rv.version_no desc) as rn
                  from review_version rv
            )
            select count(*) as ReviewCount,
                   coalesce(sum(case when lv.is_recommend = 1 then 1 else 0 end), 0) as RecommendCount
              from game_review gr
              left join latest_versions lv on lv.review_id = gr.review_id and lv.rn = 1
             where gr.game_id = :GameId
               and gr.status = 'VISIBLE'
            """,
            new { GameId = gameId },
            cancellationToken: cancellationToken));

        var latestContent = await connection.ExecuteScalarAsync<string?>(new CommandDefinition(
            """
            select content
              from (
                    select rv.content
                      from game_review gr
                      join review_version rv on rv.review_id = gr.review_id
                     where gr.game_id = :GameId
                       and gr.status = 'VISIBLE'
                     order by rv.create_time desc, rv.version_no desc
                   )
             where rownum <= 1
            """,
            new { GameId = gameId },
            cancellationToken: cancellationToken));

        var reviewCount = aggregate.ReviewCountAsInt();
        var recommendCount = aggregate.RecommendCountAsInt();
        var recommendRate = reviewCount == 0
            ? 0
            : decimal.Round(recommendCount * 100m / reviewCount, 2);

        return new ReviewSummaryResponse(reviewCount, recommendCount, recommendRate, latestContent);
    }

    public async Task<AchievementSummaryResponse> GetAchievementSummaryAsync(string gameId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        var aggregate = await connection.QuerySingleAsync<AchievementSummaryAggregate>(new CommandDefinition(
            """
            select count(*) as AchievementCount,
                   avg(global_rate) as AverageGlobalRate
              from achievement
             where game_id = :GameId
            """,
            new { GameId = gameId },
            cancellationToken: cancellationToken));

        var achievements = await connection.QueryAsync<AchievementSummaryItemRow>(new CommandDefinition(
            """
            select *
              from (
                    select ach_id as AchievementId,
                           ach_name as AchievementName,
                           description as Description,
                           global_rate as GlobalRate
                      from achievement
                     where game_id = :GameId
                     order by global_rate desc nulls last, ach_id
                   )
             where rownum <= 5
            """,
            new { GameId = gameId },
            cancellationToken: cancellationToken));

        return new AchievementSummaryResponse(
            aggregate.AchievementCountAsInt(),
            aggregate.AverageGlobalRate is null ? null : decimal.Round(aggregate.AverageGlobalRate.Value, 2),
            achievements.Select(achievement => achievement.ToResponse()).ToList());
    }

    public async Task<IReadOnlyList<GameContentPackageResponse>> GetContentPackagesAsync(string gameId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();

        var basePackage = await connection.QuerySingleAsync<GameContentPackageRow>(new CommandDefinition(
            """
            select g.game_id as PackageId,
                   g.game_id as GameId,
                   g.game_name as PackageName,
                   'BASE_GAME' as PackageType,
                   g.base_price as BasePrice,
                   g.discount_rate as DiscountRate,
                   (g.base_price * g.discount_rate) as FinalPrice,
                   cast(null as varchar2(255)) as ImageUrl,
                   'GAME' as SourceKind
              from game g
             where g.game_id = :GameId
            """,
            new { GameId = gameId },
            cancellationToken: cancellationToken));

        var itemPackages = await connection.QueryAsync<GameContentPackageRow>(new CommandDefinition(
            """
            select t.template_id as PackageId,
                   t.game_id as GameId,
                   t.item_name as PackageName,
                   case
                     when upper(t.item_name) like '%PACK%' or upper(t.item_name) like '%CHEST%' or upper(t.item_name) like '%CASE%' then 'ITEM_BUNDLE'
                     else 'COSMETIC_ITEM'
                   end as PackageType,
                   coalesce(min(case when o.order_type = 'SELL' and o.status = 'MATCHING' then o.target_price end), 0) as BasePrice,
                   1 as DiscountRate,
                   coalesce(min(case when o.order_type = 'SELL' and o.status = 'MATCHING' then o.target_price end), 0) as FinalPrice,
                   t.image_url as ImageUrl,
                   'ITEM_TEMPLATE' as SourceKind
              from item_template t
              left join market_order o on o.template_id = t.template_id
             where t.game_id = :GameId
             group by t.template_id, t.game_id, t.item_name, t.image_url
             order by PackageType desc, PackageName
            """,
            new { GameId = gameId },
            cancellationToken: cancellationToken));

        return new[] { basePackage.ToResponse() }
            .Concat(itemPackages.Select(row => row.ToResponse()))
            .ToList();
    }

    public async Task<GameItemSummaryResponse> GetItemSummaryAsync(string gameId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        var rows = (await connection.QueryAsync<GameItemSummaryEntryRow>(new CommandDefinition(
            """
            with inventory_counts as (
                select template_id, count(*) as inventory_item_count
                  from inventory_item
                 group by template_id
            ),
            order_counts as (
                select template_id,
                       sum(case when order_type = 'BUY' and status = 'MATCHING' then 1 else 0 end) as active_buy_order_count,
                       sum(case when order_type = 'SELL' and status = 'MATCHING' then 1 else 0 end) as active_sell_order_count,
                       max(case when order_type = 'BUY' and status = 'MATCHING' then target_price end) as highest_buy_price,
                       min(case when order_type = 'SELL' and status = 'MATCHING' then target_price end) as lowest_sell_price
                  from market_order
                 group by template_id
            ),
            trade_counts as (
                select template_id, count(*) as trade_count
                  from market_trade
                 group by template_id
            ),
            latest_trades as (
                select template_id,
                       trade_price,
                       row_number() over (partition by template_id order by trade_time desc, trade_id desc) as rn
                  from market_trade
            )
            select t.template_id as TemplateId,
                   t.item_name as ItemName,
                   t.rarity as Rarity,
                   t.image_url as ImageUrl,
                   coalesce(ic.inventory_item_count, 0) as InventoryItemCount,
                   coalesce(oc.active_buy_order_count, 0) as ActiveBuyOrderCount,
                   coalesce(oc.active_sell_order_count, 0) as ActiveSellOrderCount,
                   coalesce(tc.trade_count, 0) as TradeCount,
                   oc.highest_buy_price as HighestBuyPrice,
                   oc.lowest_sell_price as LowestSellPrice,
                   lt.trade_price as LastTradePrice
              from item_template t
              left join inventory_counts ic on ic.template_id = t.template_id
              left join order_counts oc on oc.template_id = t.template_id
              left join trade_counts tc on tc.template_id = t.template_id
              left join latest_trades lt on lt.template_id = t.template_id and lt.rn = 1
             where t.game_id = :GameId
             order by coalesce(tc.trade_count, 0) desc, t.rarity desc, t.item_name
            """,
            new { GameId = gameId },
            cancellationToken: cancellationToken))).ToList();

        return new GameItemSummaryResponse(
            gameId,
            rows.Count,
            rows.Sum(row => row.InventoryItemCountAsInt()),
            rows.Sum(row => row.ActiveBuyOrderCountAsInt()),
            rows.Sum(row => row.ActiveSellOrderCountAsInt()),
            rows.Sum(row => row.TradeCountAsInt()),
            MaxNullable(rows.Select(row => row.HighestBuyPrice)),
            MinNullable(rows.Select(row => row.LowestSellPrice)),
            rows.FirstOrDefault(row => row.LastTradePrice is not null)?.LastTradePrice,
            rows.Select(row => row.ToResponse()).ToList());
    }

    public async Task<bool> DeveloperExistsAsync(string developerId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "select count(*) from developer where dev_id = :DeveloperId",
            new { DeveloperId = developerId },
            cancellationToken: cancellationToken));

        return count > 0;
    }

    public async Task<GameDetailResponse> CreateAsync(string gameId, CreateGameRequest request, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            """
            insert into game
              (game_id, dev_id, game_name, base_price, discount_rate, release_date, reputation, status)
            values
              (:GameId, :DevId, :GameName, :BasePrice, :DiscountRate, :ReleaseDate, :Reputation, :Status)
            """,
            new
            {
                GameId = gameId,
                request.DevId,
                request.GameName,
                request.BasePrice,
                request.DiscountRate,
                request.ReleaseDate,
                request.Reputation,
                Status = "OFFLINE"
            },
            cancellationToken: cancellationToken));

        return await GetDetailAsync(gameId, cancellationToken)
            ?? throw new ResourceNotFoundException("Game does not exist.");
    }

    public async Task<bool> UpdateAsync(string gameId, string developerId, UpdateGameRequest request, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            """
            update game
               set game_name = :GameName,
                   base_price = :BasePrice,
                   discount_rate = :DiscountRate,
                   release_date = :ReleaseDate,
                   reputation = :Reputation
             where game_id = :GameId
               and dev_id = :DeveloperId
            """,
            new
            {
                GameId = gameId,
                DeveloperId = developerId,
                request.GameName,
                request.BasePrice,
                request.DiscountRate,
                request.ReleaseDate,
                request.Reputation
            },
            cancellationToken: cancellationToken));

        return affected > 0;
    }

    public async Task<bool> DeleteAsync(string gameId, string developerId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        int affected;
        try
        {
            affected = await connection.ExecuteAsync(new CommandDefinition(
                """
                delete from game
                 where game_id = :GameId
                   and dev_id = :DeveloperId
                   and status = 'OFFLINE'
                """,
                new { GameId = gameId, DeveloperId = developerId },
                cancellationToken: cancellationToken));
        }
        catch (OracleException exception) when (exception.Number == 2292)
        {
            throw new BusinessRuleException(
                "GAME_HAS_DEPENDENCIES",
                "Game cannot be deleted because orders, library records, CDKey batches, reviews, achievements, or item templates still reference it.");
        }

        return affected > 0;
    }

    public async Task<bool> SetStatusAsync(string gameId, string status, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            """
            update game
               set status = :Status
             where game_id = :GameId
            """,
            new { GameId = gameId, Status = status },
            cancellationToken: cancellationToken));

        return affected > 0;
    }

    private static string EscapeLike(string value) =>
        value.Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);

    private static decimal? MaxNullable(IEnumerable<decimal?> values)
    {
        var present = values.Where(value => value.HasValue).Select(value => value!.Value).ToList();
        return present.Count == 0 ? null : present.Max();
    }

    private static decimal? MinNullable(IEnumerable<decimal?> values)
    {
        var present = values.Where(value => value.HasValue).Select(value => value!.Value).ToList();
        return present.Count == 0 ? null : present.Min();
    }

    private sealed class ReviewSummaryAggregate
    {
        public decimal ReviewCount { get; init; }
        public decimal RecommendCount { get; init; }

        public int ReviewCountAsInt() => decimal.ToInt32(ReviewCount);

        public int RecommendCountAsInt() => decimal.ToInt32(RecommendCount);
    }

    private sealed class AchievementSummaryAggregate
    {
        public decimal AchievementCount { get; init; }
        public decimal? AverageGlobalRate { get; init; }

        public int AchievementCountAsInt() => decimal.ToInt32(AchievementCount);
    }

    private sealed class GameRow
    {
        public string GameId { get; init; } = "";
        public string GameName { get; init; } = "";
        public string DeveloperId { get; init; } = "";
        public string DeveloperName { get; init; } = "";
        public decimal BasePrice { get; init; }
        public decimal DiscountRate { get; init; }
        public decimal FinalPrice { get; init; }
        public DateTime ReleaseDate { get; init; }
        public string? Reputation { get; init; }
        public string Status { get; init; } = "";

        public GameListItemResponse ToListItem() => new(
            GameId,
            GameName,
            DeveloperId,
            DeveloperName,
            BasePrice,
            DiscountRate,
            FinalPrice,
            ReleaseDate,
            Reputation,
            Status);

        public GameDetailResponse ToDetail() => new(
            GameId,
            GameName,
            DeveloperId,
            DeveloperName,
            BasePrice,
            DiscountRate,
            FinalPrice,
            ReleaseDate,
            Reputation,
            Status);
    }

    private sealed class AchievementSummaryItemRow
    {
        public string AchievementId { get; init; } = "";
        public string AchievementName { get; init; } = "";
        public string? Description { get; init; }
        public decimal? GlobalRate { get; init; }

        public AchievementSummaryItemResponse ToResponse() => new(AchievementId, AchievementName, Description, GlobalRate);
    }

    private sealed class GameContentPackageRow
    {
        public string PackageId { get; init; } = "";
        public string GameId { get; init; } = "";
        public string PackageName { get; init; } = "";
        public string PackageType { get; init; } = "";
        public decimal BasePrice { get; init; }
        public decimal DiscountRate { get; init; }
        public decimal FinalPrice { get; init; }
        public string? ImageUrl { get; init; }
        public string SourceKind { get; init; } = "";

        public GameContentPackageResponse ToResponse() => new(
            PackageId,
            GameId,
            PackageName,
            PackageType,
            BasePrice,
            DiscountRate,
            FinalPrice,
            ImageUrl,
            SourceKind);
    }

    private sealed class GameItemSummaryEntryRow
    {
        public string TemplateId { get; init; } = "";
        public string ItemName { get; init; } = "";
        public string Rarity { get; init; } = "";
        public string? ImageUrl { get; init; }
        public decimal InventoryItemCount { get; init; }
        public decimal ActiveBuyOrderCount { get; init; }
        public decimal ActiveSellOrderCount { get; init; }
        public decimal TradeCount { get; init; }
        public decimal? HighestBuyPrice { get; init; }
        public decimal? LowestSellPrice { get; init; }
        public decimal? LastTradePrice { get; init; }

        public int InventoryItemCountAsInt() => decimal.ToInt32(InventoryItemCount);

        public int ActiveBuyOrderCountAsInt() => decimal.ToInt32(ActiveBuyOrderCount);

        public int ActiveSellOrderCountAsInt() => decimal.ToInt32(ActiveSellOrderCount);

        public int TradeCountAsInt() => decimal.ToInt32(TradeCount);

        public GameItemSummaryEntryResponse ToResponse() => new(
            TemplateId,
            ItemName,
            Rarity,
            ImageUrl,
            InventoryItemCountAsInt(),
            ActiveBuyOrderCountAsInt(),
            ActiveSellOrderCountAsInt(),
            TradeCountAsInt(),
            HighestBuyPrice,
            LowestSellPrice,
            LastTradePrice);
    }
}
