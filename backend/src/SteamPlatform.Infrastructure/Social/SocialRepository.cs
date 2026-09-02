using System.Data.Common;
using Dapper;
using SteamPlatform.Application.Social;
using SteamPlatform.Infrastructure.Data;
using SteamPlatform.Shared;

namespace SteamPlatform.Infrastructure.Social;

public sealed class SocialRepository(IDbConnectionFactory connectionFactory) : ISocialRepository
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    public async Task<IReadOnlyList<FriendListItem>> ListFriendsAsync(string userId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<FriendListRow>(new CommandDefinition(
            FriendListSql("(fr.user_low_id = :UserId or fr.user_high_id = :UserId)"),
            new { UserId = userId },
            cancellationToken: cancellationToken));
        return rows.Select(row => row.ToItem(userId)).ToList();
    }

    public async Task<IReadOnlyList<FriendGameActivityItem>> ListFriendsWhoPlayAsync(
        string userId,
        string gameId,
        CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<FriendGameActivityRow>(new CommandDefinition(
            """
            select p.user_id as UserId,
                   p.nickname as Nickname,
                   pl.play_minutes as PlayMinutes,
                   pl.last_play_time as LastPlayTime
              from friend_relation fr
              join player p
                on p.user_id = case
                     when fr.user_low_id = :UserId then fr.user_high_id
                     else fr.user_low_id
                   end
              join player_library pl
                on pl.user_id = p.user_id
               and pl.game_id = :GameId
               and pl.status = 'NORMAL'
             where fr.status = 'ACCEPTED'
               and (fr.user_low_id = :UserId or fr.user_high_id = :UserId)
             order by pl.play_minutes desc, p.nickname, p.user_id
            """,
            new { UserId = userId, GameId = gameId },
            cancellationToken: cancellationToken));

        return rows.Select(row => row.ToItem()).ToList();
    }

    public async Task<FriendMutationResult> RequestFriendAsync(string userId, string targetUserId, CancellationToken cancellationToken)
    {
        if (string.Equals(userId, targetUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException("CANNOT_FRIEND_SELF", "A player cannot send a friend request to themselves.");
        }

        var (lowId, highId) = OrderedPair(userId, targetUserId);
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var target = await connection.QueryFirstOrDefaultAsync<PlayerRow>(new CommandDefinition(
                "select user_id as UserId, nickname as Nickname from player where user_id = :TargetUserId and status = 'NORMAL'",
                new { TargetUserId = targetUserId }, transaction, cancellationToken: cancellationToken));
            if (target is null)
            {
                throw new ResourceNotFoundException("Target player does not exist.");
            }

            var existing = await connection.QueryFirstOrDefaultAsync<RelationRow>(new CommandDefinition(
                "select relation_id as RelationId, user_low_id as UserLowId, user_high_id as UserHighId, requested_by as RequestedBy, status as Status from friend_relation where user_low_id = :LowId and user_high_id = :HighId for update",
                new { LowId = lowId, HighId = highId }, transaction, cancellationToken: cancellationToken));

            string relationId;
            if (existing is null)
            {
                relationId = IdGenerator.NewId("FR");
                await connection.ExecuteAsync(new CommandDefinition(
                    "insert into friend_relation (relation_id, user_low_id, user_high_id, requested_by, status, created_at) values (:RelationId, :LowId, :HighId, :RequestedBy, 'PENDING', SYSTIMESTAMP)",
                    new { RelationId = relationId, LowId = lowId, HighId = highId, RequestedBy = userId }, transaction, cancellationToken: cancellationToken));
            }
            else if (existing.Status == "DECLINED")
            {
                relationId = existing.RelationId;
                await connection.ExecuteAsync(new CommandDefinition(
                    "update friend_relation set requested_by = :RequestedBy, status = 'PENDING', created_at = SYSTIMESTAMP, responded_at = null where relation_id = :RelationId",
                    new { RequestedBy = userId, RelationId = relationId }, transaction, cancellationToken: cancellationToken));
            }
            else
            {
                throw new BusinessRuleException("FRIEND_RELATION_EXISTS", $"A friend relation already exists with status {existing.Status}.");
            }

            var notification = await InsertNotificationAsync(
                connection, transaction, targetUserId, "FRIEND_REQUEST", "新的好友请求",
                "有玩家希望添加你为好友。", "/community", cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var friend = new FriendListItem(relationId, target.UserId, target.Nickname, "PENDING", false, null, null);
            return new FriendMutationResult(friend, targetUserId, notification);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<FriendMutationResult> AcceptFriendAsync(string userId, string relationId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var relation = await GetRelationForUpdateAsync(connection, transaction, relationId, cancellationToken)
                ?? throw new ResourceNotFoundException("Friend relation does not exist.");
            EnsureParty(relation, userId);
            if (relation.Status != "PENDING" || string.Equals(relation.RequestedBy, userId, StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException("FRIEND_REQUEST_NOT_ACCEPTABLE", "Only the recipient can accept a pending friend request.");
            }

            await connection.ExecuteAsync(new CommandDefinition(
                "update friend_relation set status = 'ACCEPTED', responded_at = SYSTIMESTAMP where relation_id = :RelationId",
                new { RelationId = relationId }, transaction, cancellationToken: cancellationToken));
            var notification = await InsertNotificationAsync(
                connection, transaction, relation.RequestedBy, "FRIEND_ACCEPTED", "好友请求已接受",
                "你的好友请求已被接受，现在可以开始聊天。", "/community", cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var friend = await GetFriendAsync(connection, relationId, userId, cancellationToken);
            return new FriendMutationResult(friend, relation.RequestedBy, notification);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<DirectMessageItem>> ListMessagesAsync(string userId, string friendUserId, int limit, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var relation = await FindAcceptedRelationAsync(connection, transaction, userId, friendUserId, false, cancellationToken)
            ?? throw new BusinessRuleException("NOT_FRIENDS", "Direct messages require an accepted friend relation.");

        await connection.ExecuteAsync(new CommandDefinition(
            "update direct_message set read_at = SYSTIMESTAMP where relation_id = :RelationId and sender_id <> :UserId and read_at is null",
            new { relation.RelationId, UserId = userId }, transaction, cancellationToken: cancellationToken));

        var rows = await connection.QueryAsync<DirectMessageItem>(new CommandDefinition(
            """
            select message_id as MessageId, relation_id as RelationId, sender_id as SenderId,
                   sender_nickname as SenderNickname, content as Content, sent_at as SentAt, read_at as ReadAt
              from (
                select dm.message_id, dm.relation_id, dm.sender_id, p.nickname as sender_nickname,
                       dm.content, dm.sent_at, dm.read_at
                  from direct_message dm
                  join player p on p.user_id = dm.sender_id
                 where dm.relation_id = :RelationId and dm.status = 'SENT'
                 order by dm.sent_at desc, dm.message_id desc
              )
             where rownum <= :Limit
             order by sent_at, message_id
            """,
            new { relation.RelationId, Limit = limit }, transaction, cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return rows.ToList();
    }

    public async Task<MessageDispatchResult> SendMessageAsync(string userId, string friendUserId, string content, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var relation = await FindAcceptedRelationAsync(connection, transaction, userId, friendUserId, true, cancellationToken)
                ?? throw new BusinessRuleException("NOT_FRIENDS", "Direct messages require an accepted friend relation.");
            var senderNickname = await connection.QuerySingleAsync<string>(new CommandDefinition(
                "select nickname from player where user_id = :UserId", new { UserId = userId }, transaction, cancellationToken: cancellationToken));
            var messageId = IdGenerator.NewId("MSG");
            await connection.ExecuteAsync(new CommandDefinition(
                "insert into direct_message (message_id, relation_id, sender_id, content, status, sent_at) values (:MessageId, :RelationId, :SenderId, :Content, 'SENT', SYSTIMESTAMP)",
                new { MessageId = messageId, relation.RelationId, SenderId = userId, Content = content }, transaction, cancellationToken: cancellationToken));
            var notification = await InsertNotificationAsync(
                connection, transaction, friendUserId, "DIRECT_MESSAGE", $"{senderNickname} 发来新消息",
                content.Length <= 120 ? content : content[..120], "/community", cancellationToken);
            var sentAt = await connection.QuerySingleAsync<DateTime>(new CommandDefinition(
                "select sent_at from direct_message where message_id = :MessageId", new { MessageId = messageId }, transaction, cancellationToken: cancellationToken));
            await transaction.CommitAsync(cancellationToken);

            var message = new DirectMessageItem(messageId, relation.RelationId, userId, senderNickname, content, sentAt, null);
            return new MessageDispatchResult(message, friendUserId, notification);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<ReviewInteractionItem>> ListReviewInteractionsAsync(string gameId, string? userId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<ReviewInteractionRow>(new CommandDefinition(
            ReviewInteractionSql("gr.game_id = :GameId and gr.status = 'VISIBLE'"),
            new { GameId = gameId, UserId = userId }, cancellationToken: cancellationToken));
        return rows.Select(row => row.ToItem()).ToList();
    }

    public async Task<ReviewInteractionResult> SetReviewInteractionAsync(string userId, string reviewId, ReviewInteractionRequest request, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var review = await connection.QueryFirstOrDefaultAsync<ReviewOwnerRow>(new CommandDefinition(
                "select review_id as ReviewId, user_id as OwnerUserId, game_id as GameId from game_review where review_id = :ReviewId and status = 'VISIBLE' for update",
                new { ReviewId = reviewId }, transaction, cancellationToken: cancellationToken))
                ?? throw new ResourceNotFoundException("Review does not exist.");

            if (request.VoteType is null && !request.IsStarred && !request.IsFunny && !request.IsAwarded)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    "delete from review_reaction where review_id = :ReviewId and user_id = :UserId",
                    new { ReviewId = reviewId, UserId = userId }, transaction, cancellationToken: cancellationToken));
            }
            else
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    """
                    merge into review_reaction target
                    using (select :ReviewId review_id, :UserId user_id from dual) source
                       on (target.review_id = source.review_id and target.user_id = source.user_id)
                    when matched then update set
                      target.vote_type = :VoteType,
                      target.is_starred = :IsStarred,
                      target.is_funny = :IsFunny,
                      target.is_awarded = :IsAwarded,
                      target.updated_at = SYSTIMESTAMP
                    when not matched then insert
                      (review_id, user_id, vote_type, is_starred, is_funny, is_awarded, updated_at)
                    values
                      (:ReviewId, :UserId, :VoteType, :IsStarred, :IsFunny, :IsAwarded, SYSTIMESTAMP)
                    """,
                    new
                    {
                        ReviewId = reviewId,
                        UserId = userId,
                        request.VoteType,
                        IsStarred = request.IsStarred ? 1 : 0,
                        IsFunny = request.IsFunny ? 1 : 0,
                        IsAwarded = request.IsAwarded ? 1 : 0
                    }, transaction, cancellationToken: cancellationToken));
            }

            await connection.ExecuteAsync(new CommandDefinition(
                "update game_review set thumbs_up = (select count(*) from review_reaction where review_id = :ReviewId and vote_type = 'UP') where review_id = :ReviewId",
                new { ReviewId = reviewId }, transaction, cancellationToken: cancellationToken));

            UserNotificationItem? notification = null;
            string? ownerToNotify = null;
            if (!string.Equals(review.OwnerUserId, userId, StringComparison.OrdinalIgnoreCase) &&
                (request.VoteType is not null || request.IsFunny || request.IsAwarded))
            {
                ownerToNotify = review.OwnerUserId;
                notification = await InsertNotificationAsync(
                    connection, transaction, review.OwnerUserId, "REVIEW_REACTION", "你的评测收到新互动",
                    "有玩家对你的评测进行了投票或社区互动。", $"/games/{review.GameId}/community", cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            var interaction = await GetReviewInteractionAsync(connection, review.GameId, reviewId, userId, cancellationToken);
            return new ReviewInteractionResult(interaction, ownerToNotify, notification);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<WorkshopItemView>> ListWorkshopItemsAsync(string gameId, string? userId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        return await ListWorkshopItemsInternalAsync(connection, gameId, userId, null, cancellationToken);
    }

    public async Task<WorkshopItemView> SetWorkshopSubscriptionAsync(string userId, string workshopItemId, bool isSubscribed, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var gameId = await connection.QueryFirstOrDefaultAsync<string?>(new CommandDefinition(
                "select game_id from workshop_item where workshop_item_id = :WorkshopItemId and status = 'PUBLISHED'",
                new { WorkshopItemId = workshopItemId }, transaction, cancellationToken: cancellationToken))
                ?? throw new ResourceNotFoundException("Workshop item does not exist.");

            if (isSubscribed)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    """
                    merge into workshop_subscription target
                    using (select :WorkshopItemId workshop_item_id, :UserId user_id from dual) source
                       on (target.workshop_item_id = source.workshop_item_id and target.user_id = source.user_id)
                    when not matched then insert (workshop_item_id, user_id, subscribed_at)
                    values (:WorkshopItemId, :UserId, SYSTIMESTAMP)
                    """,
                    new { WorkshopItemId = workshopItemId, UserId = userId }, transaction, cancellationToken: cancellationToken));
            }
            else
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    "delete from workshop_subscription where workshop_item_id = :WorkshopItemId and user_id = :UserId",
                    new { WorkshopItemId = workshopItemId, UserId = userId }, transaction, cancellationToken: cancellationToken));
            }

            await transaction.CommitAsync(cancellationToken);
            var rows = await ListWorkshopItemsInternalAsync(connection, gameId, userId, workshopItemId, cancellationToken);
            return rows.Single();
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<UserNotificationItem>> ListNotificationsAsync(string userId, int limit, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<NotificationRow>(new CommandDefinition(
            """
            select * from (
              select notification_id as NotificationId, notification_type as NotificationType,
                     title as Title, message as Message, target_url as TargetUrl,
                     is_read as IsReadNumber, created_at as CreatedAt, read_at as ReadAt
                from user_notification
               where user_id = :UserId
               order by created_at desc, notification_id desc
            ) where rownum <= :Limit
            """,
            new { UserId = userId, Limit = limit }, cancellationToken: cancellationToken));
        return rows.Select(row => row.ToItem()).ToList();
    }

    public async Task<UserNotificationItem> MarkNotificationReadAsync(string userId, string notificationId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            "update user_notification set is_read = 1, read_at = nvl(read_at, SYSTIMESTAMP) where notification_id = :NotificationId and user_id = :UserId",
            new { NotificationId = notificationId, UserId = userId }, transaction, cancellationToken: cancellationToken));
        if (affected == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ResourceNotFoundException("Notification does not exist.");
        }

        var row = await connection.QuerySingleAsync<NotificationRow>(new CommandDefinition(
            "select notification_id as NotificationId, notification_type as NotificationType, title as Title, message as Message, target_url as TargetUrl, is_read as IsReadNumber, created_at as CreatedAt, read_at as ReadAt from user_notification where notification_id = :NotificationId",
            new { NotificationId = notificationId }, transaction, cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return row.ToItem();
    }

    private static async Task<FriendListItem> GetFriendAsync(DbConnection connection, string relationId, string userId, CancellationToken cancellationToken)
    {
        var row = await connection.QuerySingleAsync<FriendListRow>(new CommandDefinition(
            FriendListSql("fr.relation_id = :RelationId and (fr.user_low_id = :UserId or fr.user_high_id = :UserId)"),
            new { RelationId = relationId, UserId = userId }, cancellationToken: cancellationToken));
        return row.ToItem(userId);
    }

    private static async Task<RelationRow?> GetRelationForUpdateAsync(DbConnection connection, DbTransaction transaction, string relationId, CancellationToken cancellationToken) =>
        await connection.QueryFirstOrDefaultAsync<RelationRow>(new CommandDefinition(
            "select relation_id as RelationId, user_low_id as UserLowId, user_high_id as UserHighId, requested_by as RequestedBy, status as Status from friend_relation where relation_id = :RelationId for update",
            new { RelationId = relationId }, transaction, cancellationToken: cancellationToken));

    private static async Task<RelationRow?> FindAcceptedRelationAsync(
        DbConnection connection,
        DbTransaction? transaction,
        string userId,
        string friendUserId,
        bool forUpdate,
        CancellationToken cancellationToken)
    {
        var (lowId, highId) = OrderedPair(userId, friendUserId);
        var suffix = forUpdate ? " for update" : string.Empty;
        return await connection.QueryFirstOrDefaultAsync<RelationRow>(new CommandDefinition(
            $"select relation_id as RelationId, user_low_id as UserLowId, user_high_id as UserHighId, requested_by as RequestedBy, status as Status from friend_relation where user_low_id = :LowId and user_high_id = :HighId and status = 'ACCEPTED'{suffix}",
            new { LowId = lowId, HighId = highId }, transaction, cancellationToken: cancellationToken));
    }

    private static async Task<ReviewInteractionItem> GetReviewInteractionAsync(DbConnection connection, string gameId, string reviewId, string userId, CancellationToken cancellationToken)
    {
        var row = await connection.QuerySingleAsync<ReviewInteractionRow>(new CommandDefinition(
            ReviewInteractionSql("gr.game_id = :GameId and gr.review_id = :ReviewId"),
            new { GameId = gameId, ReviewId = reviewId, UserId = userId }, cancellationToken: cancellationToken));
        return row.ToItem();
    }

    private static async Task<IReadOnlyList<WorkshopItemView>> ListWorkshopItemsInternalAsync(
        DbConnection connection,
        string gameId,
        string? userId,
        string? workshopItemId,
        CancellationToken cancellationToken)
    {
        var filter = workshopItemId is null ? string.Empty : " and wi.workshop_item_id = :WorkshopItemId";
        var rows = await connection.QueryAsync<WorkshopRow>(new CommandDefinition(
            $$"""
            select wi.workshop_item_id as WorkshopItemId, wi.game_id as GameId,
                   wi.creator_user_id as CreatorUserId, p.nickname as CreatorNickname,
                   wi.title as Title, wi.category as Category, wi.summary as Summary,
                   wi.details as Details, wi.image_url as ImageUrl,
                   nvl(subscription_totals.subscriber_count, 0) as SubscriberCount,
                   case when exists (
                     select 1
                       from workshop_subscription mine
                      where mine.workshop_item_id = wi.workshop_item_id
                        and mine.user_id = :UserId
                   ) then 1 else 0 end as IsSubscribedNumber,
                   wi.updated_at as UpdatedAt
              from workshop_item wi
              left join player p on p.user_id = wi.creator_user_id
              left join (
                select workshop_item_id, count(*) as subscriber_count
                  from workshop_subscription
                 group by workshop_item_id
              ) subscription_totals on subscription_totals.workshop_item_id = wi.workshop_item_id
             where wi.game_id = :GameId and wi.status = 'PUBLISHED'{{filter}}
             order by nvl(subscription_totals.subscriber_count, 0) desc, wi.updated_at desc
            """,
            new { GameId = gameId, UserId = userId, WorkshopItemId = workshopItemId }, cancellationToken: cancellationToken));
        return rows.Select(row => row.ToItem()).ToList();
    }

    private static async Task<UserNotificationItem> InsertNotificationAsync(
        DbConnection connection,
        DbTransaction transaction,
        string userId,
        string type,
        string title,
        string message,
        string? targetUrl,
        CancellationToken cancellationToken)
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

    private static string FriendListSql(string whereClause) =>
        $$"""
        with latest_message as (
          select relation_id, content, sent_at,
                 row_number() over (partition by relation_id order by sent_at desc, message_id desc) as rn
            from direct_message
           where status = 'SENT'
        )
        select fr.relation_id as RelationId,
               case when fr.user_low_id = :UserId then fr.user_high_id else fr.user_low_id end as FriendUserId,
               p.nickname as Nickname, fr.status as RelationStatus, fr.requested_by as RequestedBy,
               lm.content as LatestMessage, lm.sent_at as LatestMessageAt
          from friend_relation fr
          join player p on p.user_id = case when fr.user_low_id = :UserId then fr.user_high_id else fr.user_low_id end
          left join latest_message lm on lm.relation_id = fr.relation_id and lm.rn = 1
         where {{whereClause}}
         order by nvl(lm.sent_at, fr.created_at) desc
        """;

    private static string ReviewInteractionSql(string whereClause) =>
        $$"""
        select gr.review_id as ReviewId, mine.vote_type as VoteType,
               nvl(mine.is_starred, 0) as IsStarredNumber,
               nvl(mine.is_funny, 0) as IsFunnyNumber,
               nvl(mine.is_awarded, 0) as IsAwardedNumber,
               nvl(totals.up_votes, 0) as UpVotes,
               nvl(totals.down_votes, 0) as DownVotes,
               nvl(totals.funny_count, 0) as FunnyCount,
               nvl(totals.award_count, 0) as AwardCount
          from game_review gr
          left join review_reaction mine on mine.review_id = gr.review_id and mine.user_id = :UserId
          left join (
            select review_id,
                   sum(case when vote_type = 'UP' then 1 else 0 end) as up_votes,
                   sum(case when vote_type = 'DOWN' then 1 else 0 end) as down_votes,
                   sum(is_funny) as funny_count,
                   sum(is_awarded) as award_count
              from review_reaction
             group by review_id
          ) totals on totals.review_id = gr.review_id
         where {{whereClause}}
        """;

    private static void EnsureParty(RelationRow relation, string userId)
    {
        if (!string.Equals(relation.UserLowId, userId, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(relation.UserHighId, userId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("The current player is not part of this friend relation.");
        }
    }

    private static (string LowId, string HighId) OrderedPair(string left, string right) =>
        string.CompareOrdinal(left, right) < 0 ? (left, right) : (right, left);

    private sealed class RelationRow
    {
        public string RelationId { get; init; } = string.Empty;
        public string UserLowId { get; init; } = string.Empty;
        public string UserHighId { get; init; } = string.Empty;
        public string RequestedBy { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
    }

    private sealed class PlayerRow
    {
        public string UserId { get; init; } = string.Empty;
        public string Nickname { get; init; } = string.Empty;
    }

    private sealed class FriendListRow
    {
        public string RelationId { get; init; } = string.Empty;
        public string FriendUserId { get; init; } = string.Empty;
        public string Nickname { get; init; } = string.Empty;
        public string RelationStatus { get; init; } = string.Empty;
        public string RequestedBy { get; init; } = string.Empty;
        public string? LatestMessage { get; init; }
        public DateTime? LatestMessageAt { get; init; }

        public FriendListItem ToItem(string currentUserId) =>
            new(RelationId, FriendUserId, Nickname, RelationStatus,
                RelationStatus == "PENDING" && !string.Equals(RequestedBy, currentUserId, StringComparison.OrdinalIgnoreCase),
                LatestMessage, LatestMessageAt);
    }

    private sealed class FriendGameActivityRow
    {
        public string UserId { get; init; } = string.Empty;
        public string Nickname { get; init; } = string.Empty;
        public long PlayMinutes { get; init; }
        public DateTime? LastPlayTime { get; init; }

        public FriendGameActivityItem ToItem() =>
            new(UserId, Nickname, checked((int)PlayMinutes), LastPlayTime);
    }

    private sealed class ReviewOwnerRow
    {
        public string ReviewId { get; init; } = string.Empty;
        public string OwnerUserId { get; init; } = string.Empty;
        public string GameId { get; init; } = string.Empty;
    }

    private sealed class ReviewInteractionRow
    {
        public string ReviewId { get; init; } = string.Empty;
        public string? VoteType { get; init; }
        public int IsStarredNumber { get; init; }
        public int IsFunnyNumber { get; init; }
        public int IsAwardedNumber { get; init; }
        public int UpVotes { get; init; }
        public int DownVotes { get; init; }
        public int FunnyCount { get; init; }
        public int AwardCount { get; init; }

        public ReviewInteractionItem ToItem() =>
            new(ReviewId, VoteType, IsStarredNumber == 1, IsFunnyNumber == 1, IsAwardedNumber == 1, UpVotes, DownVotes, FunnyCount, AwardCount);
    }

    private sealed class WorkshopRow
    {
        public string WorkshopItemId { get; init; } = string.Empty;
        public string GameId { get; init; } = string.Empty;
        public string? CreatorUserId { get; init; }
        public string? CreatorNickname { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public string Summary { get; init; } = string.Empty;
        public string Details { get; init; } = string.Empty;
        public string? ImageUrl { get; init; }
        public int SubscriberCount { get; init; }
        public int IsSubscribedNumber { get; init; }
        public DateTime UpdatedAt { get; init; }

        public WorkshopItemView ToItem() =>
            new(WorkshopItemId, GameId, CreatorUserId, CreatorNickname, Title, Category, Summary, Details, ImageUrl, SubscriberCount, IsSubscribedNumber == 1, UpdatedAt);
    }

    private sealed class NotificationRow
    {
        public string NotificationId { get; init; } = string.Empty;
        public string NotificationType { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public string? TargetUrl { get; init; }
        public int IsReadNumber { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? ReadAt { get; init; }

        public UserNotificationItem ToItem() =>
            new(NotificationId, NotificationType, Title, Message, TargetUrl, IsReadNumber == 1, CreatedAt, ReadAt);
    }
}
