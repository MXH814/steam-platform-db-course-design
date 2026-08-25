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

    [Fact]
    public void Phase1_constraint_counts_are_scoped_to_core_tables()
    {
        Assert.True(
            SqlFile.VerifyPhase1.Split("AND table_name IN", StringSplitOptions.None).Length >= 5,
            "Core table, primary key, foreign key, and unique constraint checks should each use an explicit core-table list.");
    }

    [Fact]
    public void Demo_reset_audit_migration_is_idempotent_and_creates_all_operational_tables()
    {
        var migration = SqlFile.DemoResetAuditMigration;

        Assert.Contains("user_tables", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("user_indexes", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DEMO_RESET_RUN", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DEMO_RESET_TABLE", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DEMO_RESET_EVENT", migration, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Social_realtime_migration_is_idempotent_and_creates_all_enhancement_tables()
    {
        var migration = SqlFile.SocialRealtimeMigration;

        Assert.Contains("ensure_table", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ensure_index", migration, StringComparison.OrdinalIgnoreCase);
        foreach (var table in new[]
                 {
                     "FRIEND_RELATION", "DIRECT_MESSAGE", "REVIEW_REACTION",
                     "WORKSHOP_ITEM", "WORKSHOP_SUBSCRIPTION", "USER_NOTIFICATION"
                 })
        {
            Assert.Contains(table, migration, StringComparison.OrdinalIgnoreCase);
        }
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
