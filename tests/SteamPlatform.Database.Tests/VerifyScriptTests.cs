namespace SteamPlatform.Database.Tests;

public sealed class VerifyScriptTests
{
    [Fact]
    public void Phase1_verify_script_fails_fast_on_sql_errors()
    {
        Assert.Contains("WHENEVER SQLERROR EXIT FAILURE ROLLBACK", SqlFile.VerifyPhase1, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Phase1_verify_script_checks_core_table_and_constraint_counts()
    {
        Assert.Contains("core table count", SqlFile.VerifyPhase1, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("primary key constraint count", SqlFile.VerifyPhase1, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("foreign key constraint count", SqlFile.VerifyPhase1, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("unique constraint count", SqlFile.VerifyPhase1, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("check constraint", SqlFile.VerifyPhase1, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("PLAYER")]
    [InlineData("ADMIN_USER")]
    [InlineData("SYS_NOTICE")]
    [InlineData("MARKET_TRADE")]
    public void Phase1_verify_script_covers_representative_core_tables(string tableName)
    {
        Assert.Contains($"'{tableName}'", SqlFile.VerifyPhase1, StringComparison.OrdinalIgnoreCase);
    }
}
