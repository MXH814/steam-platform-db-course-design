namespace SteamPlatform.Api.Tests;

public sealed class CoreTransactionRepositoryGuardTests
{
    [Fact]
    public void BuyGameAsync_rejects_idempotency_reuse_for_a_different_request()
    {
        var source = ReadSource("CoreTransactions", "CoreTransactionService.cs");
        var method = SliceFrom(source, "public async Task<OrderSummary> BuyGameAsync");

        Assert.Contains("EnsureSamePurchaseRequest(existing, gameId, paymentMethod)", method, StringComparison.Ordinal);
        Assert.Contains("IDEMPOTENCY_CONFLICT", source, StringComparison.Ordinal);
        Assert.Contains("existing.Details[0].GameId", source, StringComparison.Ordinal);
        Assert.Contains("existing.PaymentMethod", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateCdkeyBatchAsync_limits_developers_to_their_own_game()
    {
        var source = ReadSource("CoreTransactions", "CoreTransactionService.cs");
        var method = SliceFrom(source, "public async Task<CdkeyBatchSummary> CreateCdkeyBatchAsync");

        Assert.Contains("game.DeveloperId", method, StringComparison.Ordinal);
        Assert.Contains("operatorId", method, StringComparison.Ordinal);
        Assert.Contains("ForbiddenException", method, StringComparison.Ordinal);
        Assert.Contains("dev_id as DeveloperId", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Wallet_history_query_returns_business_statuses()
    {
        var source = ReadSource("CoreTransactions", "CoreTransactionService.cs");

        Assert.Contains("go.order_status order_status", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("go.payment_status payment_status", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rt.status refund_status", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Content_packages_are_not_inferred_from_inventory_templates()
    {
        var source = ReadSource("Games", "GameRepository.cs");
        var method = SliceFrom(source, "public async Task<IReadOnlyList<GameContentPackageResponse>> GetContentPackagesAsync");
        var nextMethod = method.IndexOf("public async Task<", 10, StringComparison.Ordinal);
        if (nextMethod >= 0)
        {
            method = method[..nextMethod];
        }

        Assert.DoesNotContain("item_template", method, StringComparison.OrdinalIgnoreCase);
    }

    private static string SliceFrom(string source, string marker)
    {
        var index = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(index >= 0, $"Expected source marker was not found: {marker}");
        return source[index..];
    }

    private static string ReadSource(string feature, string fileName)
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(
            root,
            "backend",
            "src",
            "SteamPlatform.Infrastructure",
            feature,
            fileName));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "README.md")) &&
                Directory.Exists(Path.Combine(directory.FullName, "backend")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be found.");
    }
}
