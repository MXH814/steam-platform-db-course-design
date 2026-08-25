namespace SteamPlatform.Api.Tests;

public sealed class EngagementRepositoryGuardTests
{
    [Fact]
    public void Trade_offer_flow_locks_items_transfers_ownership_and_writes_ledger()
    {
        var source = File.ReadAllText(FindRepositorySource());

        Assert.Contains("for update", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("status = 'LOCKED'", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("insert into item_transfer_ledger", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("transfer_type, transfer_time", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("status = 'NORMAL'", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BeginTransactionAsync", source, StringComparison.Ordinal);
        Assert.Contains("RollbackAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Profile_and_feed_queries_enforce_visibility_through_oracle_relations()
    {
        var source = File.ReadAllText(FindRepositorySource());

        Assert.Contains("profile_visibility", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("friend_relation", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cp.visibility = 'PUBLIC'", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fr.status = 'ACCEPTED'", source, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositorySource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "src")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(directory!.FullName, "src", "SteamPlatform.Infrastructure", "Engagement", "EngagementRepository.cs");
    }
}
