using SteamPlatform.Application.Auth;
using SteamPlatform.Application.CoreTransactions;
using SteamPlatform.Infrastructure.CoreTransactions;
using SteamPlatform.Shared;

namespace SteamPlatform.Api.Tests;

public sealed class CoreTransactionServiceGuardTests
{
    private static readonly AuthClaims PlayerClaims = new("PLAYER", "P001", "alice", DateTimeOffset.UtcNow.AddMinutes(10));

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

    [Fact]
    public async Task Wallet_requires_player_claims_before_opening_database()
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());
        var adminClaims = new AuthClaims("ADMIN", "ADM001", "rootadmin", DateTimeOffset.UtcNow.AddMinutes(10));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetWalletAsync(adminClaims, CancellationToken.None));
    }
}
