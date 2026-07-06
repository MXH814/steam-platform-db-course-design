using Oracle.ManagedDataAccess.Client;
using Xunit.Abstractions;

namespace SteamPlatform.Database.Tests;

public sealed class OracleSmokeTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Oracle_schema_smoke_check_when_connection_is_configured()
    {
        var connectionString = Environment.GetEnvironmentVariable("STEAM_ORACLE_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            output.WriteLine("STEAM_ORACLE_TEST_CONNECTION is not set; skipping optional Oracle smoke check.");
            return;
        }

        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();

        var ping = (string?)await ScalarAsync(connection, "select 'OK' from dual");
        Assert.Equal("OK", ping);

        var tableCount = Convert.ToInt32(await ScalarAsync(
            connection,
            """
            select count(*)
              from user_tables
             where table_name in (
               'PLAYER', 'DEVELOPER', 'ADMIN_USER', 'WALLET_ACCOUNT', 'SYS_NOTICE',
               'GAME', 'GAME_ORDER', 'ORDER_DETAIL', 'PAYMENT_TRANSACTION', 'ORDER_STATUS_LOG',
               'REFUND_TICKET', 'REFUND_DETAIL', 'REFUND_AUDIT_LOG', 'PLAYER_LIBRARY',
               'CDKEY_BATCH', 'CDKEY', 'CDKEY_REDEEM_LOG', 'GAME_REVIEW', 'REVIEW_VERSION',
               'ACHIEVEMENT', 'PLAYER_ACHIEVEMENT', 'ITEM_TEMPLATE', 'INVENTORY_ITEM',
               'MARKET_ORDER', 'MARKET_TRADE', 'ITEM_TRANSFER_LEDGER', 'WALLET_TRANSACTION'
             )
            """));
        Assert.Equal(27, tableCount);

        var noticePriority = Convert.ToInt32(await ScalarAsync(
            connection,
            """
            select count(*)
              from user_constraints
             where table_name = 'SYS_NOTICE'
               and constraint_name = 'CK_NOTICE_PRIORITY'
               and constraint_type = 'C'
               and status = 'ENABLED'
            """));
        Assert.Equal(1, noticePriority);
    }

    private static async Task<object?> ScalarAsync(OracleConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync();
    }
}
