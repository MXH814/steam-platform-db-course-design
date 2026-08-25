namespace SteamPlatform.Api.Tests;

public sealed class MarketRepositoryGuardTests
{
    [Fact]
    public void Match_can_target_the_requesters_new_buy_order_and_rejects_self_trade()
    {
        var source = File.ReadAllText(FindMarketRepositorySource());

        Assert.Contains("b.user_id <> s.user_id", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("b.market_order_id = :BuyOrderId", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("b.user_id = :RequestedByUserId", source, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindMarketRepositorySource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "src",
                "SteamPlatform.Infrastructure",
                "Market",
                "MarketRepository.cs");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("MarketRepository.cs was not found from the test output directory.");
    }
}
