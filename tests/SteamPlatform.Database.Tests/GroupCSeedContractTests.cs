using System.Text.RegularExpressions;

namespace SteamPlatform.Database.Tests;

public sealed class GroupCSeedContractTests
{
    [Fact]
    public void Seed_uses_final_cs2_and_dst_sample_games()
    {
        Assert.Contains("'GAME_CS2'", SqlFile.Data, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'Counter-Strike 2'", SqlFile.Data, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'GAME_DST'", SqlFile.Data, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Don''t Starve Together", SqlFile.Data, StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain("Team Fortress 2", SqlFile.Data, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Neon Drift", SqlFile.Data, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Archive Runner", SqlFile.Data, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Schema_allows_free_library_acquisition_for_cs2()
    {
        var libraryBlock = TableBlock("PLAYER_LIBRARY");

        Assert.Contains("'FREE'", libraryBlock, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Cs2_free_claim_is_zero_amount_and_does_not_create_wallet_debit()
    {
        Assert.Matches(
            @"INSERT\s+INTO\s+GAME_ORDER\s*\([^)]*\)\s*VALUES\s*\(\s*'O_CS2_FREE_001'\s*,\s*'P001'\s*,\s*0\.00\s*,\s*'BUY_GAME'\s*,\s*'COMPLETED'\s*,\s*'PAID'",
            SqlFile.Data);

        Assert.Matches(
            @"INSERT\s+INTO\s+ORDER_DETAIL\s*\([^)]*\)\s*VALUES\s*\(\s*'OD_CS2_FREE_001'\s*,\s*'O_CS2_FREE_001'\s*,\s*'GAME_CS2'\s*,\s*0\.00\s*,\s*0\.00\s*,\s*0\.00\s*,\s*0\.00\s*\)",
            SqlFile.Data);

        Assert.Matches(
            @"INSERT\s+INTO\s+PLAYER_LIBRARY\s*\([^)]*\)\s*VALUES\s*\(\s*'LIB_CS2_P001'\s*,\s*'P001'\s*,\s*'GAME_CS2'\s*,\s*'FREE'",
            SqlFile.Data);

        Assert.DoesNotContain("'O_CS2_FREE_001', 'DEBIT'", SqlFile.Data, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("'BUY_GAME', 'O_CS2_FREE_001'", SqlFile.Data, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Dst_paid_purchase_has_order_payment_wallet_transaction_and_library_asset()
    {
        Assert.Matches(
            @"INSERT\s+INTO\s+GAME_ORDER\s*\([^)]*\)\s*VALUES\s*\(\s*'O_DST_001'\s*,\s*'P001'\s*,\s*24\.00\s*,\s*'BUY_GAME'\s*,\s*'COMPLETED'\s*,\s*'PAID'",
            SqlFile.Data);

        Assert.Matches(
            @"INSERT\s+INTO\s+ORDER_DETAIL\s*\([^)]*\)\s*VALUES\s*\(\s*'OD_DST_001'\s*,\s*'O_DST_001'\s*,\s*'GAME_DST'\s*,\s*48\.00\s*,\s*24\.00\s*,\s*24\.00\s*,\s*0\.00\s*\)",
            SqlFile.Data);

        Assert.Matches(
            @"INSERT\s+INTO\s+PAYMENT_TRANSACTION\s*\([^)]*\)\s*VALUES\s*\(\s*'PAY_DST_001'\s*,\s*'O_DST_001'\s*,\s*'GW-DST-001'\s*,\s*24\.00\s*,\s*'SUCCESS'\s*,\s*'STEAM_WALLET'",
            SqlFile.Data);

        Assert.Matches(
            @"INSERT\s+INTO\s+WALLET_TRANSACTION\s*\([^)]*\)\s*VALUES\s*\(\s*'WT_DST_BUY_001'\s*,\s*'W001'\s*,\s*'BUY_GAME'\s*,\s*'O_DST_001'\s*,\s*'DEBIT'\s*,\s*24\.00\s*,\s*200\.00\s*,\s*176\.00\s*,\s*'idem-wallet-dst-buy-001'\s*,\s*'STEAM_WALLET'",
            SqlFile.Data);

        Assert.Matches(
            @"INSERT\s+INTO\s+PLAYER_LIBRARY\s*\([^)]*\)\s*VALUES\s*\(\s*'LIB_DST_P001'\s*,\s*'P001'\s*,\s*'GAME_DST'\s*,\s*'BUY'",
            SqlFile.Data);
    }

    [Fact]
    public void Seed_has_required_cs2_and_dst_item_template_coverage()
    {
        Assert.True(CountItemTemplates("GAME_CS2") >= 8, "CS2 should have at least 8 item templates.");
        Assert.True(CountItemTemplates("GAME_DST") >= 4, "DST should have at least 4 item templates.");
    }

    private static int CountItemTemplates(string gameId) =>
        Regex.Matches(
            SqlFile.Data,
            $@"INSERT\s+INTO\s+ITEM_TEMPLATE\s*\([^)]*\)\s*VALUES\s*\([^;]*'{Regex.Escape(gameId)}'",
            RegexOptions.IgnoreCase | RegexOptions.Singleline).Count;

    private static string TableBlock(string tableName)
    {
        var match = Regex.Match(
            SqlFile.Schema,
            $@"CREATE\s+TABLE\s+{Regex.Escape(tableName)}\s*\((.*?)\);\s*",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        return match.Success
            ? match.Groups[1].Value
            : throw new InvalidOperationException($"{tableName} table block was not found.");
    }
}
