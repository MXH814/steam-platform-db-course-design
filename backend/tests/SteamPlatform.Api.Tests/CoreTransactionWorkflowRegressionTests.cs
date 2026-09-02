using SteamPlatform.Application.Auth;
using SteamPlatform.Application.CoreTransactions;
using SteamPlatform.Infrastructure.CoreTransactions;
using SteamPlatform.Shared;

namespace SteamPlatform.Api.Tests;

public sealed class CoreTransactionWorkflowRegressionTests
{
    private static readonly AuthClaims Alice =
        new("PLAYER", "P001", "alice", DateTimeOffset.UtcNow.AddHours(1));

    private static readonly AuthClaims Bob =
        new("PLAYER", "P002", "bob", DateTimeOffset.UtcNow.AddHours(1));

    private static readonly AuthClaims Charlie =
        new("PLAYER", "P003", "charlie", DateTimeOffset.UtcNow.AddHours(1));

    private static readonly AuthClaims Admin =
        new("SUPER_ADMIN", "ADM001", "rootadmin", DateTimeOffset.UtcNow.AddHours(1));

    private static readonly AuthClaims Developer =
        new("DEVELOPER", "DEV_KLEI", "klei@example.com", DateTimeOffset.UtcNow.AddHours(1));

    [Fact]
    public async Task Cdkey_is_not_consumed_when_first_player_already_owns_game()
    {
        var service = new InMemoryCoreTransactionService();
        var batch = await CreateBatchAsync(service, quantity: 1);
        var cdkey = Assert.Single(batch.PlaintextKeys);

        var alreadyOwned = await service.RedeemCdkeyAsync(
            Alice,
            new RedeemCdkeyRequest(cdkey),
            CancellationToken.None);
        var redeemedByAnotherPlayer = await service.RedeemCdkeyAsync(
            Charlie,
            new RedeemCdkeyRequest(cdkey),
            CancellationToken.None);

        Assert.Equal("REDEEMED", alreadyOwned.Result);
        Assert.Contains("already owns", alreadyOwned.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("SUCCESS", redeemedByAnotherPlayer.Result);
        Assert.Equal("GAME_DST", redeemedByAnotherPlayer.GameId);
        Assert.False(string.IsNullOrWhiteSpace(redeemedByAnotherPlayer.LibraryId));
    }

    [Fact]
    public async Task Refund_approval_credits_authenticated_order_owner_wallet()
    {
        var service = new InMemoryCoreTransactionService();
        var before = await service.GetWalletAsync(Alice, CancellationToken.None);
        var refund = await service.CreateRefundAsync(
            Alice,
            new CreateRefundRequest("O_DST_001", "changed mind"),
            CancellationToken.None);

        var approved = await service.ApproveRefundAsync(
            Admin,
            refund.RefundId,
            new AuditRefundRequest("approved"),
            CancellationToken.None);
        var after = await service.GetWalletAsync(Alice, CancellationToken.None);

        Assert.Equal("APPROVED", approved.Status);
        Assert.Equal(before.AvailableBalance + refund.RefundAmount, after.AvailableBalance);
    }

    [Fact]
    public async Task Player_cannot_read_another_players_order()
    {
        var service = new InMemoryCoreTransactionService();

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.GetOrderAsync(Bob, "O_DST_001", CancellationToken.None));
    }

    [Fact]
    public async Task Long_batch_number_still_generates_unique_cdkeys()
    {
        var service = new InMemoryCoreTransactionService();

        var batch = await service.CreateCdkeyBatchAsync(
            Developer,
            new CreateCdkeyBatchRequest(
                "GAME_DST",
                new string('B', 80),
                DateTime.UtcNow.AddMinutes(-1),
                DateTime.UtcNow.AddDays(30),
                20),
            CancellationToken.None);

        Assert.Equal(20, batch.PlaintextKeys.Count);
        Assert.Equal(20, batch.PlaintextKeys.Distinct(StringComparer.Ordinal).Count());
        Assert.All(batch.PlaintextKeys, key => Assert.StartsWith("DST-", key, StringComparison.Ordinal));
    }

    private static Task<CdkeyBatchSummary> CreateBatchAsync(
        InMemoryCoreTransactionService service,
        int quantity) =>
        service.CreateCdkeyBatchAsync(
            Developer,
            new CreateCdkeyBatchRequest(
                "GAME_DST",
                "DST-REGRESSION",
                DateTime.UtcNow.AddMinutes(-1),
                DateTime.UtcNow.AddDays(30),
                quantity),
            CancellationToken.None);
}
