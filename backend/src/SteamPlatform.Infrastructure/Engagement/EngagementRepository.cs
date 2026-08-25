using System.Data.Common;
using Dapper;
using SteamPlatform.Application.Engagement;
using SteamPlatform.Application.Social;
using SteamPlatform.Infrastructure.Data;
using SteamPlatform.Shared;

namespace SteamPlatform.Infrastructure.Engagement;

public sealed class EngagementRepository(IDbConnectionFactory connectionFactory) : IEngagementRepository
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    public async Task<PlayerProfileView> GetProfileAsync(string userId, string? viewerUserId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        var profile = await GetProfileRowAsync(connection, null, userId, cancellationToken)
            ?? throw new ResourceNotFoundException("Player profile does not exist.");
        await EnsureProfileVisibleAsync(connection, null, profile, viewerUserId, cancellationToken);
        return await BuildProfileViewAsync(connection, null, profile, viewerUserId, cancellationToken);
    }

    public async Task<PlayerProfileView> UpdateProfileAsync(string userId, UpdatePlayerProfileRequest request, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        if (request.ShowcaseGameId is not null)
        {
            var gameExists = await connection.QuerySingleAsync<int>(new CommandDefinition(
                "select count(1) from game where game_id = :GameId and status = 'ONLINE'",
                new { GameId = request.ShowcaseGameId }, transaction, cancellationToken: cancellationToken));
            if (gameExists == 0)
            {
                throw new ResourceNotFoundException("Showcase game does not exist.");
            }
        }

        var playerExists = await connection.QuerySingleAsync<int>(new CommandDefinition(
            "select count(1) from player where user_id = :UserId and status = 'NORMAL'",
            new { UserId = userId }, transaction, cancellationToken: cancellationToken));
        if (playerExists == 0)
        {
            throw new ResourceNotFoundException("Player does not exist.");
        }

        await connection.ExecuteAsync(new CommandDefinition(
            """
            merge into player_profile target
            using (select :UserId user_id from dual) source
               on (target.user_id = source.user_id)
            when matched then update set
              target.headline = :Headline,
              target.bio = :Bio,
              target.avatar_key = :AvatarKey,
              target.background_key = :BackgroundKey,
              target.theme_key = :ThemeKey,
              target.showcase_game_id = :ShowcaseGameId,
              target.profile_visibility = :ProfileVisibility,
              target.updated_at = SYSTIMESTAMP
            when not matched then insert
              (user_id, headline, bio, avatar_key, background_key, theme_key, showcase_game_id, profile_visibility, updated_at)
            values
              (:UserId, :Headline, :Bio, :AvatarKey, :BackgroundKey, :ThemeKey, :ShowcaseGameId, :ProfileVisibility, SYSTIMESTAMP)
            """,
            new
            {
                UserId = userId,
                request.Headline,
                request.Bio,
                request.AvatarKey,
                request.BackgroundKey,
                request.ThemeKey,
                request.ShowcaseGameId,
                request.ProfileVisibility
            }, transaction, cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return await GetProfileAsync(userId, userId, cancellationToken);
    }

    public async Task<PlayerProfileView> SetFeaturedBadgeAsync(string userId, string badgeId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var owned = await connection.QuerySingleAsync<int>(new CommandDefinition(
            "select count(1) from player_badge where user_id = :UserId and badge_id = :BadgeId",
            new { UserId = userId, BadgeId = badgeId }, transaction, cancellationToken: cancellationToken));
        if (owned == 0)
        {
            throw new BusinessRuleException("BADGE_NOT_OWNED", "The selected badge is not owned by this player.");
        }

        await connection.ExecuteAsync(new CommandDefinition(
            "update player_badge set is_featured = case when badge_id = :BadgeId then 1 else 0 end where user_id = :UserId",
            new { UserId = userId, BadgeId = badgeId }, transaction, cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return await GetProfileAsync(userId, userId, cancellationToken);
    }

    public async Task<IReadOnlyList<PlayerSearchItem>> SearchPlayersAsync(string userId, string query, int limit, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<PlayerSearchRow>(new CommandDefinition(
            """
            select * from (
              select p.user_id as UserId, p.nickname as Nickname,
                     nvl(pp.avatar_key, 'AVATAR_BLUE') as AvatarKey, pp.headline as Headline,
                     fr.relation_id as RelationId, fr.status as RelationStatus,
                     case when fr.status = 'PENDING' and fr.requested_by <> :UserId then 1 else 0 end as IsIncomingNumber
                from player p
                left join player_profile pp on pp.user_id = p.user_id
                left join friend_relation fr
                  on fr.user_low_id = case when p.user_id < :UserId then p.user_id else :UserId end
                 and fr.user_high_id = case when p.user_id < :UserId then :UserId else p.user_id end
               where p.user_id <> :UserId
                 and p.status = 'NORMAL'
                 and (:QueryText is null or lower(p.nickname) like :QueryPattern or lower(p.account) like :QueryPattern)
               order by case when fr.status = 'ACCEPTED' then 0 when fr.status = 'PENDING' then 1 else 2 end,
                        lower(p.nickname), p.user_id
            ) where rownum <= :Limit
            """,
            new
            {
                UserId = userId,
                QueryText = string.IsNullOrEmpty(query) ? null : query,
                QueryPattern = $"%{query.ToLowerInvariant()}%",
                Limit = limit
            }, cancellationToken: cancellationToken));
        return rows.Select(row => row.ToItem()).ToList();
    }

    public async Task<IReadOnlyList<TradeOfferView>> ListTradeOffersAsync(string userId, string? status, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        var offers = (await connection.QueryAsync<TradeOfferRow>(new CommandDefinition(
            """
            select o.offer_id as OfferId, o.sender_id as SenderId, sender.nickname as SenderNickname,
                   o.recipient_id as RecipientId, recipient.nickname as RecipientNickname,
                   o.message as Message, o.status as Status, o.created_at as CreatedAt,
                   o.responded_at as RespondedAt, o.version as Version
              from trade_offer o
              join player sender on sender.user_id = o.sender_id
              join player recipient on recipient.user_id = o.recipient_id
             where (o.sender_id = :UserId or o.recipient_id = :UserId)
               and (:Status is null or o.status = :Status)
             order by case when o.status = 'PENDING' then 0 else 1 end, o.created_at desc, o.offer_id desc
            """,
            new { UserId = userId, Status = status }, cancellationToken: cancellationToken))).ToList();

        var result = new List<TradeOfferView>(offers.Count);
        foreach (var offer in offers)
        {
            result.Add(await BuildTradeOfferAsync(connection, null, offer, userId, cancellationToken));
        }

        return result;
    }

    public async Task<IReadOnlyList<TradeableInventoryItemView>> ListTradeableInventoryAsync(string userId, string targetUserId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        if (!string.Equals(userId, targetUserId, StringComparison.OrdinalIgnoreCase) &&
            !await AreAcceptedFriendsAsync(connection, null, userId, targetUserId, cancellationToken))
        {
            throw new BusinessRuleException("NOT_FRIENDS", "Only accepted friends can inspect tradeable inventory.");
        }

        var rows = await connection.QueryAsync<TradeableInventoryRow>(new CommandDefinition(
            """
            select i.item_id as ItemId, i.template_id as TemplateId, t.game_id as GameId,
                   t.item_name as ItemName, t.rarity as Rarity, t.image_url as ImageUrl,
                   i.wear_rating as WearRating
              from inventory_item i
              join item_template t on t.template_id = i.template_id
             where i.user_id = :TargetUserId and i.status = 'NORMAL'
             order by t.game_id, t.rarity, t.item_name, i.item_id
            """,
            new { TargetUserId = targetUserId }, cancellationToken: cancellationToken));
        return rows.Select(row => row.ToItem()).ToList();
    }

    public async Task<TradeOfferMutationResult> CreateTradeOfferAsync(string userId, CreateTradeOfferRequest request, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            if (!await AreAcceptedFriendsAsync(connection, transaction, userId, request.RecipientId, cancellationToken))
            {
                throw new BusinessRuleException("NOT_FRIENDS", "Trade offers require an accepted friend relation.");
            }

            var roles = request.OfferedItemIds.Select(id => (ItemId: id, ExpectedOwner: userId, Role: "OFFERED"))
                .Concat(request.RequestedItemIds.Select(id => (ItemId: id, ExpectedOwner: request.RecipientId, Role: "REQUESTED")))
                .OrderBy(item => item.ItemId, StringComparer.Ordinal)
                .ToList();
            foreach (var item in roles)
            {
                var locked = await GetInventoryForUpdateAsync(connection, transaction, item.ItemId, cancellationToken)
                    ?? throw new ResourceNotFoundException($"Inventory item {item.ItemId} does not exist.");
                if (!string.Equals(locked.UserId, item.ExpectedOwner, StringComparison.OrdinalIgnoreCase) || locked.Status != "NORMAL")
                {
                    throw new BusinessRuleException("TRADE_ITEM_UNAVAILABLE", $"Inventory item {item.ItemId} is not available to trade.");
                }
            }

            var offerId = IdGenerator.NewId("TO");
            await connection.ExecuteAsync(new CommandDefinition(
                "insert into trade_offer (offer_id, sender_id, recipient_id, message, status, created_at, version) values (:OfferId, :SenderId, :RecipientId, :Message, 'PENDING', SYSTIMESTAMP, 0)",
                new { OfferId = offerId, SenderId = userId, request.RecipientId, request.Message }, transaction, cancellationToken: cancellationToken));
            foreach (var item in roles)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    "insert into trade_offer_item (offer_id, item_id, item_role, owner_id_at_create) values (:OfferId, :ItemId, :ItemRole, :OwnerIdAtCreate)",
                    new { OfferId = offerId, item.ItemId, ItemRole = item.Role, OwnerIdAtCreate = item.ExpectedOwner }, transaction, cancellationToken: cancellationToken));
                await connection.ExecuteAsync(new CommandDefinition(
                    "update inventory_item set status = 'LOCKED', version = version + 1 where item_id = :ItemId",
                    new { item.ItemId }, transaction, cancellationToken: cancellationToken));
            }

            var notification = await InsertNotificationAsync(
                connection, transaction, request.RecipientId, "TRADE_OFFER", "收到新的交易报价",
                "好友向你发送了物品交换报价，报价中的物品已暂时锁定。", "/trade-offers", cancellationToken);
            var offerRow = await GetTradeOfferRowAsync(connection, transaction, offerId, true, cancellationToken)
                ?? throw new InvalidOperationException("Created trade offer could not be read back.");
            var view = await BuildTradeOfferAsync(connection, transaction, offerRow, userId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new TradeOfferMutationResult(view, request.RecipientId, notification);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<TradeOfferMutationResult> RespondTradeOfferAsync(string userId, string offerId, string action, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var offer = await GetTradeOfferRowAsync(connection, transaction, offerId, true, cancellationToken)
                ?? throw new ResourceNotFoundException("Trade offer does not exist.");
            if (offer.Status != "PENDING")
            {
                throw new BusinessRuleException("TRADE_OFFER_NOT_PENDING", "Only a pending trade offer can be changed.");
            }

            var recipientAction = action is "ACCEPT" or "DECLINE";
            if (recipientAction && !string.Equals(userId, offer.RecipientId, StringComparison.OrdinalIgnoreCase))
            {
                throw new ForbiddenException("Only the recipient can accept or decline this trade offer.");
            }

            if (action == "CANCEL" && !string.Equals(userId, offer.SenderId, StringComparison.OrdinalIgnoreCase))
            {
                throw new ForbiddenException("Only the sender can cancel this trade offer.");
            }

            var items = (await connection.QueryAsync<TradeOfferItemRow>(new CommandDefinition(
                TradeOfferItemsSql + " order by oi.item_id for update of i.status, i.user_id",
                new { OfferId = offerId }, transaction, cancellationToken: cancellationToken))).ToList();
            if (items.Count == 0)
            {
                throw new BusinessRuleException("EMPTY_TRADE_OFFER", "The trade offer contains no inventory items.");
            }

            if (action == "ACCEPT")
            {
                foreach (var item in items)
                {
                    if (item.Status != "LOCKED" || !string.Equals(item.CurrentOwnerId, item.OwnerIdAtCreate, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new BusinessRuleException("TRADE_ITEM_CHANGED", "An item in this trade offer is no longer available.");
                    }

                    var newOwner = item.ItemRole == "OFFERED" ? offer.RecipientId : offer.SenderId;
                    await connection.ExecuteAsync(new CommandDefinition(
                        "update inventory_item set user_id = :NewOwner, status = 'NORMAL', version = version + 1 where item_id = :ItemId",
                        new { NewOwner = newOwner, item.ItemId }, transaction, cancellationToken: cancellationToken));
                    await connection.ExecuteAsync(new CommandDefinition(
                        "insert into item_transfer_ledger (transfer_id, item_id, from_user_id, to_user_id, transfer_type, transfer_time) values (:TransferId, :ItemId, :FromUserId, :ToUserId, 'TRADE', SYSTIMESTAMP)",
                        new { TransferId = IdGenerator.NewId("ITL"), item.ItemId, FromUserId = item.OwnerIdAtCreate, ToUserId = newOwner }, transaction, cancellationToken: cancellationToken));
                }
            }
            else
            {
                foreach (var item in items)
                {
                    if (item.Status == "LOCKED" && string.Equals(item.CurrentOwnerId, item.OwnerIdAtCreate, StringComparison.OrdinalIgnoreCase))
                    {
                        await connection.ExecuteAsync(new CommandDefinition(
                            "update inventory_item set status = 'NORMAL', version = version + 1 where item_id = :ItemId",
                            new { item.ItemId }, transaction, cancellationToken: cancellationToken));
                    }
                }
            }

            var nextStatus = action switch
            {
                "ACCEPT" => "ACCEPTED",
                "DECLINE" => "DECLINED",
                _ => "CANCELED"
            };
            await connection.ExecuteAsync(new CommandDefinition(
                "update trade_offer set status = :Status, responded_at = SYSTIMESTAMP, version = version + 1 where offer_id = :OfferId",
                new { Status = nextStatus, OfferId = offerId }, transaction, cancellationToken: cancellationToken));
            var notifyUserId = string.Equals(userId, offer.SenderId, StringComparison.OrdinalIgnoreCase) ? offer.RecipientId : offer.SenderId;
            var notification = await InsertNotificationAsync(
                connection, transaction, notifyUserId, "TRADE_OFFER", $"交易报价已{TradeStatusLabel(nextStatus)}",
                nextStatus == "ACCEPTED" ? "物品交换已完成，并已写入不可变转移账本。" : "交易报价已结束，锁定物品已经释放。",
                "/trade-offers", cancellationToken);
            var updated = await GetTradeOfferRowAsync(connection, transaction, offerId, false, cancellationToken)
                ?? throw new InvalidOperationException("Updated trade offer could not be read back.");
            var view = await BuildTradeOfferAsync(connection, transaction, updated, userId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new TradeOfferMutationResult(view, notifyUserId, notification);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<CommunityPostView>> ListCommunityPostsAsync(string? viewerUserId, string? gameId, int limit, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        var rows = await QueryCommunityPostsAsync(connection, null, viewerUserId, gameId, null, limit, cancellationToken);
        return rows.Select(row => row.ToItem()).ToList();
    }

    public async Task<CommunityPostView> CreateCommunityPostAsync(string userId, CreateCommunityPostRequest request, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        if (request.GameId is not null)
        {
            var gameExists = await connection.QuerySingleAsync<int>(new CommandDefinition(
                "select count(1) from game where game_id = :GameId and status = 'ONLINE'",
                new { request.GameId }, transaction, cancellationToken: cancellationToken));
            if (gameExists == 0)
            {
                throw new ResourceNotFoundException("Referenced game does not exist.");
            }
        }

        var postId = IdGenerator.NewId("POST");
        await connection.ExecuteAsync(new CommandDefinition(
            """
            insert into community_post
              (post_id, author_id, game_id, post_type, content, media_url, visibility, status, created_at, updated_at)
            values
              (:PostId, :AuthorId, :GameId, :PostType, :Content, :MediaUrl, :Visibility, 'VISIBLE', SYSTIMESTAMP, SYSTIMESTAMP)
            """,
            new { PostId = postId, AuthorId = userId, request.GameId, request.PostType, request.Content, request.MediaUrl, request.Visibility },
            transaction, cancellationToken: cancellationToken));
        var row = (await QueryCommunityPostsAsync(connection, transaction, userId, null, postId, 1, cancellationToken)).Single();
        await transaction.CommitAsync(cancellationToken);
        return row.ToItem();
    }

    public async Task<CommunityPostView> SetPostReactionAsync(string userId, string postId, string? reactionType, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var visible = (await QueryCommunityPostsAsync(connection, transaction, userId, null, postId, 1, cancellationToken)).SingleOrDefault()
            ?? throw new ResourceNotFoundException("Community post does not exist or is not visible.");
        if (reactionType is null)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "delete from community_post_reaction where post_id = :PostId and user_id = :UserId",
                new { PostId = postId, UserId = userId }, transaction, cancellationToken: cancellationToken));
        }
        else
        {
            await connection.ExecuteAsync(new CommandDefinition(
                """
                merge into community_post_reaction target
                using (select :PostId post_id, :UserId user_id from dual) source
                   on (target.post_id = source.post_id and target.user_id = source.user_id)
                when matched then update set target.reaction_type = :ReactionType, target.created_at = SYSTIMESTAMP
                when not matched then insert (post_id, user_id, reaction_type, created_at)
                  values (:PostId, :UserId, :ReactionType, SYSTIMESTAMP)
                """,
                new { PostId = postId, UserId = userId, ReactionType = reactionType }, transaction, cancellationToken: cancellationToken));
        }

        var updated = (await QueryCommunityPostsAsync(connection, transaction, userId, null, visible.PostId, 1, cancellationToken)).Single();
        await transaction.CommitAsync(cancellationToken);
        return updated.ToItem();
    }

    public async Task<IReadOnlyList<DiscussionTopicView>> ListDiscussionTopicsAsync(string gameId, int limit, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        var rows = await QueryDiscussionTopicsAsync(connection, null, gameId, null, limit, cancellationToken);
        return rows.Select(row => row.ToView([])).ToList();
    }

    public async Task<DiscussionTopicView> GetDiscussionTopicAsync(string topicId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        return await BuildDiscussionTopicAsync(connection, null, topicId, cancellationToken);
    }

    public async Task<DiscussionTopicView> CreateDiscussionTopicAsync(string userId, CreateDiscussionTopicRequest request, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var gameExists = await connection.QuerySingleAsync<int>(new CommandDefinition(
            "select count(1) from game where game_id = :GameId and status = 'ONLINE'",
            new { request.GameId }, transaction, cancellationToken: cancellationToken));
        if (gameExists == 0)
        {
            throw new ResourceNotFoundException("Discussion game does not exist.");
        }

        var topicId = IdGenerator.NewId("TOPIC");
        await connection.ExecuteAsync(new CommandDefinition(
            "insert into discussion_topic (topic_id, game_id, author_id, title, body, status, created_at, updated_at) values (:TopicId, :GameId, :AuthorId, :Title, :Body, 'OPEN', SYSTIMESTAMP, SYSTIMESTAMP)",
            new { TopicId = topicId, request.GameId, AuthorId = userId, request.Title, request.Body }, transaction, cancellationToken: cancellationToken));
        var topic = await BuildDiscussionTopicAsync(connection, transaction, topicId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return topic;
    }

    public async Task<DiscussionReplyMutationResult> ReplyToDiscussionAsync(string userId, string topicId, string body, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var topic = await connection.QueryFirstOrDefaultAsync<TopicOwnerRow>(new CommandDefinition(
                "select topic_id as TopicId, author_id as AuthorId, status as Status from discussion_topic where topic_id = :TopicId for update",
                new { TopicId = topicId }, transaction, cancellationToken: cancellationToken))
                ?? throw new ResourceNotFoundException("Discussion topic does not exist.");
            if (topic.Status != "OPEN")
            {
                throw new BusinessRuleException("DISCUSSION_NOT_OPEN", "Replies can only be added to an open topic.");
            }

            await connection.ExecuteAsync(new CommandDefinition(
                "insert into discussion_reply (reply_id, topic_id, author_id, body, status, created_at, updated_at) values (:ReplyId, :TopicId, :AuthorId, :Body, 'VISIBLE', SYSTIMESTAMP, SYSTIMESTAMP)",
                new { ReplyId = IdGenerator.NewId("REPLY"), TopicId = topicId, AuthorId = userId, Body = body }, transaction, cancellationToken: cancellationToken));
            await connection.ExecuteAsync(new CommandDefinition(
                "update discussion_topic set updated_at = SYSTIMESTAMP where topic_id = :TopicId",
                new { TopicId = topicId }, transaction, cancellationToken: cancellationToken));

            UserNotificationItem? notification = null;
            string? notifyUserId = null;
            if (!string.Equals(topic.AuthorId, userId, StringComparison.OrdinalIgnoreCase))
            {
                notifyUserId = topic.AuthorId;
                notification = await InsertNotificationAsync(
                    connection, transaction, notifyUserId, "COMMUNITY_REPLY", "你的讨论收到新回复",
                    "有玩家回复了你发布的社区讨论。", $"/community/discussions/{topicId}", cancellationToken);
            }

            var view = await BuildDiscussionTopicAsync(connection, transaction, topicId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new DiscussionReplyMutationResult(view, notifyUserId, notification);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<ProfileRow?> GetProfileRowAsync(DbConnection connection, DbTransaction? transaction, string userId, CancellationToken cancellationToken) =>
        await connection.QueryFirstOrDefaultAsync<ProfileRow>(new CommandDefinition(
            """
            select p.user_id as UserId, p.nickname as Nickname,
                   pp.headline as Headline, pp.bio as Bio,
                   nvl(pp.avatar_key, 'AVATAR_BLUE') as AvatarKey,
                   nvl(pp.background_key, 'BACKGROUND_CS2') as BackgroundKey,
                   nvl(pp.theme_key, 'STEAM_BLUE') as ThemeKey,
                   pp.showcase_game_id as ShowcaseGameId, g.game_name as ShowcaseGameName,
                   nvl(pp.profile_visibility, 'PUBLIC') as ProfileVisibility,
                   nvl(pp.updated_at, p.update_time) as UpdatedAt
              from player p
              left join player_profile pp on pp.user_id = p.user_id
              left join game g on g.game_id = pp.showcase_game_id
             where p.user_id = :UserId and p.status = 'NORMAL'
            """,
            new { UserId = userId }, transaction, cancellationToken: cancellationToken));

    private static async Task EnsureProfileVisibleAsync(DbConnection connection, DbTransaction? transaction, ProfileRow profile, string? viewerUserId, CancellationToken cancellationToken)
    {
        if (string.Equals(profile.UserId, viewerUserId, StringComparison.OrdinalIgnoreCase) || profile.ProfileVisibility == "PUBLIC")
        {
            return;
        }

        if (profile.ProfileVisibility == "PRIVATE" || viewerUserId is null ||
            !await AreAcceptedFriendsAsync(connection, transaction, profile.UserId, viewerUserId, cancellationToken))
        {
            throw new ForbiddenException("This profile is not visible to the current viewer.");
        }
    }

    private static async Task<PlayerProfileView> BuildProfileViewAsync(DbConnection connection, DbTransaction? transaction, ProfileRow profile, string? viewerUserId, CancellationToken cancellationToken)
    {
        var friendCount = await connection.QuerySingleAsync<int>(new CommandDefinition(
            "select count(1) from friend_relation where status = 'ACCEPTED' and (user_low_id = :UserId or user_high_id = :UserId)",
            new { profile.UserId }, transaction, cancellationToken: cancellationToken));
        var badges = (await connection.QueryAsync<BadgeRow>(new CommandDefinition(
            """
            select bc.badge_id as BadgeId, bc.badge_name as BadgeName, bc.description as Description,
                   bc.icon_key as IconKey, bc.xp_value as XpValue, bc.rarity as Rarity,
                   pb.earned_at as EarnedAt, pb.is_featured as IsFeaturedNumber
              from player_badge pb
              join badge_catalog bc on bc.badge_id = pb.badge_id
             where pb.user_id = :UserId
             order by pb.is_featured desc, bc.xp_value desc, pb.earned_at desc
            """,
            new { profile.UserId }, transaction, cancellationToken: cancellationToken))).Select(row => row.ToItem()).ToList();
        return profile.ToView(friendCount, badges.Sum(badge => badge.XpValue), badges,
            string.Equals(profile.UserId, viewerUserId, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<bool> AreAcceptedFriendsAsync(DbConnection connection, DbTransaction? transaction, string left, string right, CancellationToken cancellationToken)
    {
        var (lowId, highId) = OrderedPair(left, right);
        return await connection.QuerySingleAsync<int>(new CommandDefinition(
            "select count(1) from friend_relation where user_low_id = :LowId and user_high_id = :HighId and status = 'ACCEPTED'",
            new { LowId = lowId, HighId = highId }, transaction, cancellationToken: cancellationToken)) > 0;
    }

    private static async Task<InventoryLockRow?> GetInventoryForUpdateAsync(DbConnection connection, DbTransaction transaction, string itemId, CancellationToken cancellationToken) =>
        await connection.QueryFirstOrDefaultAsync<InventoryLockRow>(new CommandDefinition(
            "select item_id as ItemId, user_id as UserId, status as Status from inventory_item where item_id = :ItemId for update",
            new { ItemId = itemId }, transaction, cancellationToken: cancellationToken));

    private static async Task<TradeOfferRow?> GetTradeOfferRowAsync(DbConnection connection, DbTransaction? transaction, string offerId, bool forUpdate, CancellationToken cancellationToken) =>
        await connection.QueryFirstOrDefaultAsync<TradeOfferRow>(new CommandDefinition(
            """
            select o.offer_id as OfferId, o.sender_id as SenderId, sender.nickname as SenderNickname,
                   o.recipient_id as RecipientId, recipient.nickname as RecipientNickname,
                   o.message as Message, o.status as Status, o.created_at as CreatedAt,
                   o.responded_at as RespondedAt, o.version as Version
              from trade_offer o
              join player sender on sender.user_id = o.sender_id
              join player recipient on recipient.user_id = o.recipient_id
             where o.offer_id = :OfferId
            """ + (forUpdate ? " for update of o.status" : string.Empty),
            new { OfferId = offerId }, transaction, cancellationToken: cancellationToken));

    private static async Task<TradeOfferView> BuildTradeOfferAsync(DbConnection connection, DbTransaction? transaction, TradeOfferRow offer, string userId, CancellationToken cancellationToken)
    {
        var items = (await connection.QueryAsync<TradeOfferItemRow>(new CommandDefinition(
            TradeOfferItemsSql + " order by case when oi.item_role = 'OFFERED' then 0 else 1 end, t.item_name, oi.item_id",
            new { offer.OfferId }, transaction, cancellationToken: cancellationToken))).ToList();
        var offered = items.Where(item => item.ItemRole == "OFFERED").Select(item => item.ToItem()).ToList();
        var requested = items.Where(item => item.ItemRole == "REQUESTED").Select(item => item.ToItem()).ToList();
        return offer.ToView(
            offered,
            requested,
            offer.Status == "PENDING" && string.Equals(userId, offer.RecipientId, StringComparison.OrdinalIgnoreCase),
            offer.Status == "PENDING" && string.Equals(userId, offer.RecipientId, StringComparison.OrdinalIgnoreCase),
            offer.Status == "PENDING" && string.Equals(userId, offer.SenderId, StringComparison.OrdinalIgnoreCase));
    }

    private static Task<IEnumerable<CommunityPostRow>> QueryCommunityPostsAsync(
        DbConnection connection, DbTransaction? transaction, string? viewerUserId, string? gameId, string? postId, int limit, CancellationToken cancellationToken) =>
        connection.QueryAsync<CommunityPostRow>(new CommandDefinition(
            """
            select * from (
              select cp.post_id as PostId, cp.author_id as AuthorId, p.nickname as AuthorNickname,
                     nvl(pp.avatar_key, 'AVATAR_BLUE') as AvatarKey,
                     cp.game_id as GameId, g.game_name as GameName, cp.post_type as PostType,
                     cp.content as Content, cp.media_url as MediaUrl, cp.visibility as Visibility,
                     cp.created_at as CreatedAt, nvl(rt.like_count, 0) as LikeCount,
                     nvl(rt.award_count, 0) as AwardCount, mine.reaction_type as MyReaction
                from community_post cp
                join player p on p.user_id = cp.author_id
                left join player_profile pp on pp.user_id = cp.author_id
                left join game g on g.game_id = cp.game_id
                left join (
                  select post_id,
                         sum(case when reaction_type = 'LIKE' then 1 else 0 end) as like_count,
                         sum(case when reaction_type = 'AWARD' then 1 else 0 end) as award_count
                    from community_post_reaction group by post_id
                ) rt on rt.post_id = cp.post_id
                left join community_post_reaction mine on mine.post_id = cp.post_id and mine.user_id = :ViewerUserId
               where cp.status = 'VISIBLE'
                 and (:GameId is null or cp.game_id = :GameId)
                 and (:PostId is null or cp.post_id = :PostId)
                 and (
                   cp.visibility = 'PUBLIC' or cp.author_id = :ViewerUserId or
                   (:ViewerUserId is not null and exists (
                     select 1 from friend_relation fr
                      where fr.status = 'ACCEPTED'
                        and fr.user_low_id = case when cp.author_id < :ViewerUserId then cp.author_id else :ViewerUserId end
                        and fr.user_high_id = case when cp.author_id < :ViewerUserId then :ViewerUserId else cp.author_id end
                   ))
                 )
               order by cp.created_at desc, cp.post_id desc
            ) where rownum <= :Limit
            """,
            new { ViewerUserId = viewerUserId, GameId = gameId, PostId = postId, Limit = limit },
            transaction, cancellationToken: cancellationToken));

    private static Task<IEnumerable<DiscussionTopicRow>> QueryDiscussionTopicsAsync(
        DbConnection connection, DbTransaction? transaction, string? gameId, string? topicId, int limit, CancellationToken cancellationToken) =>
        connection.QueryAsync<DiscussionTopicRow>(new CommandDefinition(
            """
            select * from (
              select dt.topic_id as TopicId, dt.game_id as GameId, g.game_name as GameName,
                     dt.author_id as AuthorId, p.nickname as AuthorNickname,
                     nvl(pp.avatar_key, 'AVATAR_BLUE') as AvatarKey,
                     dt.title as Title, dt.body as Body, dt.status as Status,
                     dt.created_at as CreatedAt, dt.updated_at as UpdatedAt,
                     nvl(rc.reply_count, 0) as ReplyCount
                from discussion_topic dt
                join game g on g.game_id = dt.game_id
                join player p on p.user_id = dt.author_id
                left join player_profile pp on pp.user_id = dt.author_id
                left join (
                  select topic_id, count(*) as reply_count
                    from discussion_reply where status = 'VISIBLE' group by topic_id
                ) rc on rc.topic_id = dt.topic_id
               where dt.status <> 'HIDDEN'
                 and (:GameId is null or dt.game_id = :GameId)
                 and (:TopicId is null or dt.topic_id = :TopicId)
               order by dt.updated_at desc, dt.topic_id desc
            ) where rownum <= :Limit
            """,
            new { GameId = gameId, TopicId = topicId, Limit = limit }, transaction, cancellationToken: cancellationToken));

    private static async Task<DiscussionTopicView> BuildDiscussionTopicAsync(DbConnection connection, DbTransaction? transaction, string topicId, CancellationToken cancellationToken)
    {
        var topic = (await QueryDiscussionTopicsAsync(connection, transaction, null, topicId, 1, cancellationToken)).SingleOrDefault()
            ?? throw new ResourceNotFoundException("Discussion topic does not exist.");
        var replies = (await connection.QueryAsync<DiscussionReplyRow>(new CommandDefinition(
            """
            select dr.reply_id as ReplyId, dr.author_id as AuthorId, p.nickname as AuthorNickname,
                   nvl(pp.avatar_key, 'AVATAR_BLUE') as AvatarKey,
                   dr.body as Body, dr.created_at as CreatedAt
              from discussion_reply dr
              join player p on p.user_id = dr.author_id
              left join player_profile pp on pp.user_id = dr.author_id
             where dr.topic_id = :TopicId and dr.status = 'VISIBLE'
             order by dr.created_at, dr.reply_id
            """,
            new { TopicId = topicId }, transaction, cancellationToken: cancellationToken))).Select(row => row.ToItem()).ToList();
        return topic.ToView(replies);
    }

    private static async Task<UserNotificationItem> InsertNotificationAsync(
        DbConnection connection, DbTransaction transaction, string userId, string type, string title,
        string message, string? targetUrl, CancellationToken cancellationToken)
    {
        var notificationId = IdGenerator.NewId("NTF");
        await connection.ExecuteAsync(new CommandDefinition(
            "insert into user_notification (notification_id, user_id, notification_type, title, message, target_url, is_read, created_at) values (:NotificationId, :UserId, :Type, :Title, :Message, :TargetUrl, 0, SYSTIMESTAMP)",
            new { NotificationId = notificationId, UserId = userId, Type = type, Title = title, Message = message, TargetUrl = targetUrl }, transaction, cancellationToken: cancellationToken));
        var createdAt = await connection.QuerySingleAsync<DateTime>(new CommandDefinition(
            "select created_at from user_notification where notification_id = :NotificationId",
            new { NotificationId = notificationId }, transaction, cancellationToken: cancellationToken));
        return new UserNotificationItem(notificationId, type, title, message, targetUrl, false, createdAt, null);
    }

    private static (string LowId, string HighId) OrderedPair(string left, string right) =>
        string.CompareOrdinal(left, right) < 0 ? (left, right) : (right, left);

    private static string TradeStatusLabel(string status) => status switch
    {
        "ACCEPTED" => "接受",
        "DECLINED" => "拒绝",
        _ => "撤销"
    };

    private const string TradeOfferItemsSql =
        """
        select oi.item_id as ItemId, i.template_id as TemplateId, t.game_id as GameId,
               t.item_name as ItemName, t.rarity as Rarity, t.image_url as ImageUrl,
               i.wear_rating as WearRating, oi.item_role as ItemRole,
               oi.owner_id_at_create as OwnerIdAtCreate, i.user_id as CurrentOwnerId,
               i.status as Status
          from trade_offer_item oi
          join inventory_item i on i.item_id = oi.item_id
          join item_template t on t.template_id = i.template_id
         where oi.offer_id = :OfferId
        """;

    private sealed class ProfileRow
    {
        public string UserId { get; init; } = string.Empty;
        public string Nickname { get; init; } = string.Empty;
        public string? Headline { get; init; }
        public string? Bio { get; init; }
        public string AvatarKey { get; init; } = string.Empty;
        public string BackgroundKey { get; init; } = string.Empty;
        public string ThemeKey { get; init; } = string.Empty;
        public string? ShowcaseGameId { get; init; }
        public string? ShowcaseGameName { get; init; }
        public string ProfileVisibility { get; init; } = string.Empty;
        public DateTime UpdatedAt { get; init; }

        public PlayerProfileView ToView(int friendCount, int totalXp, IReadOnlyList<ProfileBadgeView> badges, bool isOwnProfile) =>
            new(UserId, Nickname, Headline, Bio, AvatarKey, BackgroundKey, ThemeKey, ShowcaseGameId,
                ShowcaseGameName, ProfileVisibility, friendCount, totalXp, badges, UpdatedAt, isOwnProfile);
    }

    private sealed class BadgeRow
    {
        public string BadgeId { get; init; } = string.Empty;
        public string BadgeName { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string IconKey { get; init; } = string.Empty;
        public int XpValue { get; init; }
        public string Rarity { get; init; } = string.Empty;
        public DateTime EarnedAt { get; init; }
        public int IsFeaturedNumber { get; init; }
        public ProfileBadgeView ToItem() => new(BadgeId, BadgeName, Description, IconKey, XpValue, Rarity, EarnedAt, IsFeaturedNumber == 1);
    }

    private sealed class PlayerSearchRow
    {
        public string UserId { get; init; } = string.Empty;
        public string Nickname { get; init; } = string.Empty;
        public string AvatarKey { get; init; } = string.Empty;
        public string? Headline { get; init; }
        public string? RelationId { get; init; }
        public string? RelationStatus { get; init; }
        public int IsIncomingNumber { get; init; }
        public PlayerSearchItem ToItem() => new(UserId, Nickname, AvatarKey, Headline, RelationId, RelationStatus, IsIncomingNumber == 1);
    }

    private sealed class InventoryLockRow
    {
        public string ItemId { get; init; } = string.Empty;
        public string UserId { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
    }

    private sealed class TradeOfferRow
    {
        public string OfferId { get; init; } = string.Empty;
        public string SenderId { get; init; } = string.Empty;
        public string SenderNickname { get; init; } = string.Empty;
        public string RecipientId { get; init; } = string.Empty;
        public string RecipientNickname { get; init; } = string.Empty;
        public string? Message { get; init; }
        public string Status { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public DateTime? RespondedAt { get; init; }
        public long Version { get; init; }
        public TradeOfferView ToView(IReadOnlyList<TradeOfferItemView> offered, IReadOnlyList<TradeOfferItemView> requested, bool canAccept, bool canDecline, bool canCancel) =>
            new(OfferId, SenderId, SenderNickname, RecipientId, RecipientNickname, Message, Status, CreatedAt,
                RespondedAt, Version, offered, requested, canAccept, canDecline, canCancel);
    }

    private sealed class TradeOfferItemRow
    {
        public string ItemId { get; init; } = string.Empty;
        public string TemplateId { get; init; } = string.Empty;
        public string GameId { get; init; } = string.Empty;
        public string ItemName { get; init; } = string.Empty;
        public string Rarity { get; init; } = string.Empty;
        public string? ImageUrl { get; init; }
        public decimal? WearRating { get; init; }
        public string ItemRole { get; init; } = string.Empty;
        public string OwnerIdAtCreate { get; init; } = string.Empty;
        public string CurrentOwnerId { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public TradeOfferItemView ToItem() => new(ItemId, TemplateId, GameId, ItemName, Rarity, ImageUrl, WearRating, ItemRole, OwnerIdAtCreate);
    }

    private sealed class TradeableInventoryRow
    {
        public string ItemId { get; init; } = string.Empty;
        public string TemplateId { get; init; } = string.Empty;
        public string GameId { get; init; } = string.Empty;
        public string ItemName { get; init; } = string.Empty;
        public string Rarity { get; init; } = string.Empty;
        public string? ImageUrl { get; init; }
        public decimal? WearRating { get; init; }
        public TradeableInventoryItemView ToItem() => new(ItemId, TemplateId, GameId, ItemName, Rarity, ImageUrl, WearRating);
    }

    private sealed class CommunityPostRow
    {
        public string PostId { get; init; } = string.Empty;
        public string AuthorId { get; init; } = string.Empty;
        public string AuthorNickname { get; init; } = string.Empty;
        public string AvatarKey { get; init; } = string.Empty;
        public string? GameId { get; init; }
        public string? GameName { get; init; }
        public string PostType { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public string? MediaUrl { get; init; }
        public string Visibility { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public int LikeCount { get; init; }
        public int AwardCount { get; init; }
        public string? MyReaction { get; init; }
        public CommunityPostView ToItem() => new(PostId, AuthorId, AuthorNickname, AvatarKey, GameId, GameName, PostType, Content, MediaUrl, Visibility, CreatedAt, LikeCount, AwardCount, MyReaction);
    }

    private sealed class DiscussionTopicRow
    {
        public string TopicId { get; init; } = string.Empty;
        public string GameId { get; init; } = string.Empty;
        public string GameName { get; init; } = string.Empty;
        public string AuthorId { get; init; } = string.Empty;
        public string AuthorNickname { get; init; } = string.Empty;
        public string AvatarKey { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Body { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public int ReplyCount { get; init; }
        public DiscussionTopicView ToView(IReadOnlyList<DiscussionReplyView> replies) =>
            new(TopicId, GameId, GameName, AuthorId, AuthorNickname, AvatarKey, Title, Body, Status, CreatedAt, UpdatedAt, ReplyCount, replies);
    }

    private sealed class DiscussionReplyRow
    {
        public string ReplyId { get; init; } = string.Empty;
        public string AuthorId { get; init; } = string.Empty;
        public string AuthorNickname { get; init; } = string.Empty;
        public string AvatarKey { get; init; } = string.Empty;
        public string Body { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public DiscussionReplyView ToItem() => new(ReplyId, AuthorId, AuthorNickname, AvatarKey, Body, CreatedAt);
    }

    private sealed class TopicOwnerRow
    {
        public string TopicId { get; init; } = string.Empty;
        public string AuthorId { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
    }
}
