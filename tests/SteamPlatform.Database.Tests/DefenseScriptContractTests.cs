namespace SteamPlatform.Database.Tests;

public sealed class DefenseScriptContractTests
{
    [Fact]
    public void Defense_verification_covers_the_complete_schema_and_business_invariants()
    {
        var sql = SqlFile.VerifyDefense;

        Assert.Contains("expected application tables", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("v_count, 45", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("disabled relational constraints", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PLAYER.wallet_balance columns", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("order/detail amount mismatches", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("wallet/order frozen mismatches", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("trades missing transfer ledger", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("failed demo reset runs", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHENEVER SQLERROR EXIT FAILURE ROLLBACK", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("COMMIT;", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Explain_plan_script_covers_order_market_and_community_queries_and_rolls_back()
    {
        var sql = SqlFile.ExplainPlans;

        Assert.Contains("STEAM_DEF_ORDER", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("STEAM_DEF_MARKET", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("STEAM_DEF_DISCUSSION", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DBMS_XPLAN.DISPLAY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("Execution-plan inspection completed.", sql.Trim(), StringComparison.Ordinal);
        Assert.Contains("ROLLBACK;", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("COMMIT;", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Two_session_demo_uses_bounded_row_locking_and_never_changes_wallet_data()
    {
        Assert.Contains("FOR UPDATE", SqlFile.LockSessionA, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DBMS_SESSION.SLEEP(8)", SqlFile.LockSessionA, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ROLLBACK;", SqlFile.LockSessionA, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("FOR UPDATE WAIT 2", SqlFile.LockSessionB, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SQLCODE IN (-54, -30006)", SqlFile.LockSessionB, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ROLLBACK;", SqlFile.LockSessionB, StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain("UPDATE wallet_account", SqlFile.LockSessionA, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE wallet_account", SqlFile.LockSessionB, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("COMMIT;", SqlFile.LockSessionA, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("COMMIT;", SqlFile.LockSessionB, StringComparison.OrdinalIgnoreCase);
    }
}
