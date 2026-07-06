using System.Text.RegularExpressions;

namespace SteamPlatform.Database.Tests;

public sealed class SchemaContractTests
{
    private static readonly string[] ExpectedTables =
    [
        "PLAYER", "DEVELOPER", "ADMIN_USER", "WALLET_ACCOUNT", "SYS_NOTICE",
        "GAME", "GAME_ORDER", "ORDER_DETAIL", "PAYMENT_TRANSACTION", "ORDER_STATUS_LOG",
        "REFUND_TICKET", "REFUND_DETAIL", "REFUND_AUDIT_LOG", "PLAYER_LIBRARY",
        "CDKEY_BATCH", "CDKEY", "CDKEY_REDEEM_LOG", "GAME_REVIEW", "REVIEW_VERSION",
        "ACHIEVEMENT", "PLAYER_ACHIEVEMENT", "ITEM_TEMPLATE", "INVENTORY_ITEM",
        "MARKET_ORDER", "MARKET_TRADE", "ITEM_TRANSFER_LEDGER", "WALLET_TRANSACTION"
    ];

    [Fact]
    public void Schema_defines_expected_27_core_tables()
    {
        var tables = Regex.Matches(SqlFile.Schema, @"CREATE\s+TABLE\s+([A-Z_]+)\s*\(", RegexOptions.IgnoreCase)
            .Select(match => match.Groups[1].Value.ToUpperInvariant())
            .ToArray();

        Assert.Equal(27, tables.Length);
        Assert.Empty(ExpectedTables.Except(tables));
        Assert.Empty(tables.Except(ExpectedTables));
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
    public void Status_and_role_enums_match_documented_contract(string tableName, params string[] expectedValues)
    {
        var block = TableBlock(tableName);

        foreach (var value in expectedValues)
        {
            Assert.Contains($"'{value}'", block, StringComparison.OrdinalIgnoreCase);
        }
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
