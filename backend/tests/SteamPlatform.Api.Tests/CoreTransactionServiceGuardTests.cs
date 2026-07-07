using SteamPlatform.Application.Auth;
using SteamPlatform.Application.CoreTransactions;
using SteamPlatform.Infrastructure.CoreTransactions;
using SteamPlatform.Shared;

namespace SteamPlatform.Api.Tests;

public sealed class CoreTransactionServiceGuardTests
{
    private static readonly AuthClaims PlayerClaims = new("PLAYER", "P001", "alice", DateTimeOffset.UtcNow.AddMinutes(10));
    private static readonly AuthClaims AdminClaims = new("ADMIN", "ADM001", "rootadmin", DateTimeOffset.UtcNow.AddMinutes(10));
    private static readonly AuthClaims DeveloperClaims = new("DEVELOPER", "DEV001", "dev", DateTimeOffset.UtcNow.AddMinutes(10));

    [Theory]
    [InlineData("", "idem-1", "GameId is required.")]
    [InlineData("GAME_DST", "", "IdempotencyKey is required.")]
    public async Task BuyGame_rejects_invalid_request_before_opening_database(string gameId, string idempotencyKey, string expectedMessage)
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.BuyGameAsync(PlayerClaims, new CreateOrderRequest(gameId, idempotencyKey), CancellationToken.None));

        Assert.StartsWith(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ClaimFreeGame_rejects_non_cs2_before_opening_database()
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.ClaimFreeGameAsync(PlayerClaims, "GAME_DST", CancellationToken.None));

        Assert.Equal("GAME_NOT_FREE", exception.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ClaimFreeGame_rejects_blank_game_before_opening_database(string gameId)
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ClaimFreeGameAsync(PlayerClaims, gameId, CancellationToken.None));

        Assert.StartsWith("gameId is required.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddPlaytime_rejects_non_positive_minutes_before_opening_database()
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddPlaytimeAsync(PlayerClaims, "GAME_DST", new UpdatePlaytimeRequest(0), CancellationToken.None));

        Assert.StartsWith("MinutesToAdd must be greater than 0.", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("", "reason", "OrderId is required.")]
    [InlineData("O_DST_001", "", "Reason is required.")]
    public async Task CreateRefund_rejects_invalid_request_before_opening_database(string orderId, string reason, string expectedMessage)
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateRefundAsync(PlayerClaims, new CreateRefundRequest(orderId, reason), CancellationToken.None));

        Assert.StartsWith(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApproveRefund_requires_admin_claims_before_opening_database()
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ApproveRefundAsync(PlayerClaims, "R001", new AuditRefundRequest("ok"), CancellationToken.None));
    }

    [Fact]
    public async Task RejectRefund_rejects_blank_refund_id_before_opening_database()
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RejectRefundAsync(AdminClaims, "", new AuditRefundRequest("no"), CancellationToken.None));

        Assert.StartsWith("refundId is required.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateCdkeyBatch_requires_developer_or_admin_before_opening_database()
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateCdkeyBatchAsync(
                PlayerClaims,
                new CreateCdkeyBatchRequest("GAME_DST", "DST-DEMO", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 1),
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateCdkeyBatch_rejects_non_dst_before_opening_database()
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateCdkeyBatchAsync(
                DeveloperClaims,
                new CreateCdkeyBatchRequest("GAME_CS2", "CS2-DEMO", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 1),
                CancellationToken.None));

        Assert.Equal("CDKEY_GAME_UNSUPPORTED", exception.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task CreateCdkeyBatch_rejects_invalid_quantity_before_opening_database(int quantity)
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateCdkeyBatchAsync(
                DeveloperClaims,
                new CreateCdkeyBatchRequest("GAME_DST", "DST-DEMO", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), quantity),
                CancellationToken.None));

        Assert.StartsWith("Quantity must be between 1 and 100.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateCdkeyBatch_rejects_invalid_time_window_before_opening_database()
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());
        var now = DateTime.UtcNow;

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateCdkeyBatchAsync(
                DeveloperClaims,
                new CreateCdkeyBatchRequest("GAME_DST", "DST-DEMO", now, now, 1),
                CancellationToken.None));

        Assert.StartsWith("ExpireTime must be later than ValidFrom.", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task RedeemCdkey_rejects_blank_cdkey_before_opening_database(string cdkey)
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RedeemCdkeyAsync(PlayerClaims, new RedeemCdkeyRequest(cdkey), CancellationToken.None));

        Assert.StartsWith("Cdkey is required.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Wallet_requires_player_claims_before_opening_database()
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetWalletAsync(AdminClaims, CancellationToken.None));
    }
}
