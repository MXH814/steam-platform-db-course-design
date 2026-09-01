using System.Text.RegularExpressions;

namespace SteamPlatform.Database.Tests;

public sealed class SchemaContractTests
{
    private static readonly string[] ExpectedTables =
    [
        "PLAYER", "DEVELOPER", "ADMIN_USER", "WALLET_ACCOUNT", "SYS_NOTICE",
        "GAME", "GAME_ORDER", "ORDER_DETAIL", "ORDER_STATUS_LOG", "PAYMENT_TRANSACTION",
        "REFUND_TICKET", "REFUND_DETAIL", "REFUND_AUDIT_LOG", "PLAYER_LIBRARY",
        "CDKEY_BATCH", "CDKEY", "CDKEY_REDEEM_LOG", "GAME_REVIEW", "REVIEW_VERSION",
        "ACHIEVEMENT", "PLAYER_ACHIEVEMENT", "ITEM_TEMPLATE", "INVENTORY_ITEM",
        "MARKET_ORDER", "MARKET_TRADE", "ITEM_TRANSFER_LEDGER", "WALLET_TRANSACTION"
    ];

    private static readonly string[] ExpectedOperationalTables =
    [
        "DEMO_RESET_RUN", "DEMO_RESET_TABLE", "DEMO_RESET_EVENT"
    ];

    private static readonly string[] ExpectedEnhancementTables =
    [
        "FRIEND_RELATION", "DIRECT_MESSAGE", "REVIEW_REACTION",
        "WORKSHOP_ITEM", "WORKSHOP_SUBSCRIPTION", "USER_NOTIFICATION",
        "PLAYER_PROFILE", "BADGE_CATALOG", "PLAYER_BADGE",
        "TRADE_OFFER", "TRADE_OFFER_ITEM", "COMMUNITY_POST",
        "COMMUNITY_POST_REACTION", "DISCUSSION_TOPIC", "DISCUSSION_REPLY"
    ];

    [Fact]
    public void Schema_defines_expected_tables()
    {
        var tables = Regex.Matches(SqlFile.Schema, @"CREATE\s+TABLE\s+([A-Z_]+)\s*\(", RegexOptions.IgnoreCase)
            .Select(match => match.Groups[1].Value.ToUpperInvariant())
            .ToArray();

        Assert.Equal(45, tables.Length);
        Assert.Empty(ExpectedTables.Except(tables));
        Assert.Empty(ExpectedEnhancementTables.Except(tables));
        Assert.Empty(ExpectedOperationalTables.Except(tables));
        Assert.Empty(tables.Except(ExpectedTables.Concat(ExpectedEnhancementTables).Concat(ExpectedOperationalTables)));
    }

    [Fact]
    public void Schema_defines_social_enhancement_tables_with_relational_constraints()
    {
        foreach (var table in ExpectedEnhancementTables)
        {
            Assert.Contains($"CREATE TABLE {table}", SqlFile.Schema, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("UK_FRIEND_PAIR", TableBlock("FRIEND_RELATION"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FK_MESSAGE_RELATION", TableBlock("DIRECT_MESSAGE"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PK_REVIEW_REACTION", TableBlock("REVIEW_REACTION"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PK_WORKSHOP_SUBSCRIPTION", TableBlock("WORKSHOP_SUBSCRIPTION"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CK_TRADE_OFFER_STATUS", TableBlock("TRADE_OFFER"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FK_OFFER_ITEM_INVENTORY", TableBlock("TRADE_OFFER_ITEM"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FK_DISCUSSION_REPLY_TOPIC", TableBlock("DISCUSSION_REPLY"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Schema_defines_demo_reset_audit_tables_without_changing_wallet_truth()
    {
        foreach (var table in ExpectedOperationalTables)
        {
            Assert.Contains($"CREATE TABLE {table}", SqlFile.Schema, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("wallet_balance", TableBlock("DEMO_RESET_RUN"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Expected_table_contract_is_unique_and_uses_oracle_identifiers()
    {
        var expectedTables = ExpectedTables
            .Concat(ExpectedEnhancementTables)
            .Concat(ExpectedOperationalTables)
            .ToArray();

        Assert.Equal(expectedTables.Length, expectedTables.Distinct(StringComparer.Ordinal).Count());
        Assert.All(expectedTables, table => Assert.Matches("^[A-Z][A-Z0-9_]*$", table));
    }

    [Fact]
    public void Schema_keeps_documented_table_order()
    {
        var tables = Regex.Matches(SqlFile.Schema, @"CREATE\s+TABLE\s+([A-Z_]+)\s*\(", RegexOptions.IgnoreCase)
            .Select(match => match.Groups[1].Value.ToUpperInvariant())
            .ToArray();
        var expectedTables = ExpectedTables
            .Concat(ExpectedEnhancementTables)
            .Concat(ExpectedOperationalTables)
            .ToArray();

        Assert.Equal(expectedTables, tables);
    }

    [Fact]
    public void Player_wallet_balance_column_is_not_reintroduced()
    {
        var playerBlock = TableBlock("PLAYER");

        Assert.DoesNotContain("wallet_balance", playerBlock, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("PLAYER", "CK_PLAYER_CREDIT", "credit_score BETWEEN 0 AND 100")]
    [InlineData("SYS_NOTICE", "CK_NOTICE_PRIORITY", "priority BETWEEN 0 AND 9")]
    [InlineData("WALLET_ACCOUNT", "CK_WALLET_AVAIL", "available_balance >= 0")]
    [InlineData("WALLET_ACCOUNT", "CK_WALLET_FROZEN", "frozen_balance >= 0")]
    public void Critical_check_constraints_remain_in_schema(string tableName, string constraintName, string expression)
    {
        var block = Normalize(TableBlock(tableName));

        Assert.Contains(constraintName, block, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Normalize(expression), block, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("ADMIN_USER", "SUPER_ADMIN", "AUDITOR", "RISK_ADMIN", "CUSTOMER_SERVICE")]
    [InlineData("SYS_NOTICE", "DRAFT", "PUBLISHED", "EXPIRED", "REVOKED")]
    [InlineData("DEVELOPER", "PENDING", "APPROVED", "REJECTED")]
    [InlineData("PAYMENT_TRANSACTION", "STEAM_WALLET", "WECHAT_PAY", "ALIPAY", "VISA", "MASTERCARD")]
    [InlineData("WALLET_TRANSACTION", "STEAM_WALLET", "WECHAT_PAY", "ALIPAY", "VISA", "MASTERCARD")]
    public void Status_and_role_enums_match_documented_contract(string tableName, params string[] expectedValues)
    {
        var block = TableBlock(tableName);

        foreach (var value in expectedValues)
        {
            Assert.Contains($"'{value}'", block, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Wallet_payment_method_migration_is_idempotent_and_backfills_legacy_rows()
    {
        var migration = SqlFile.WalletPaymentMethodMigration;

        Assert.Contains("user_tab_cols", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("user_constraints", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EXECUTE IMMEDIATE", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHERE payment_method IS NULL", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("raise_application_error", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CK_PAYMENT_METHOD", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CK_WALLET_TXN_PAYMENT_METHOD", migration, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Game_reputation_column_can_store_longest_documented_value()
    {
        var block = Normalize(TableBlock("GAME"));

        Assert.Contains("reputation VARCHAR2(30)", block, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'OVERWHELMINGLY_POSITIVE'", block, StringComparison.OrdinalIgnoreCase);
    }

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

    private static string Normalize(string value) =>
        Regex.Replace(value, @"\s+", " ").Trim();
}
