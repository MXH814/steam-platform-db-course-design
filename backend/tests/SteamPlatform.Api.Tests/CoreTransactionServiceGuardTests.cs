using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using SteamPlatform.Application.Auth;
using SteamPlatform.Application.CoreTransactions;
using SteamPlatform.Infrastructure.Data;
using SteamPlatform.Infrastructure.CoreTransactions;
using SteamPlatform.Shared;

namespace SteamPlatform.Api.Tests;

public sealed class CoreTransactionServiceGuardTests
{
    private static readonly AuthClaims PlayerClaims = new("PLAYER", "P001", "alice", DateTimeOffset.UtcNow.AddMinutes(10));
    private static readonly AuthClaims AdminClaims = new("ADMIN", "ADM001", "rootadmin", DateTimeOffset.UtcNow.AddMinutes(10));
    private static readonly AuthClaims DeveloperClaims = new("DEVELOPER", "DEV001", "dev", DateTimeOffset.UtcNow.AddMinutes(10));

    [Theory]
    [InlineData("", "idem-1", "GameId is required.")]
    [InlineData("GAME_DST", "", "IdempotencyKey is required.")]
    public async Task BuyGame_rejects_invalid_request_before_opening_database(string gameId, string idempotencyKey, string expectedMessage)
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.BuyGameAsync(PlayerClaims, new CreateOrderRequest(gameId, idempotencyKey), CancellationToken.None));

        Assert.StartsWith(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ClaimFreeGame_rejects_non_cs2_before_opening_database()
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.ClaimFreeGameAsync(PlayerClaims, "GAME_DST", CancellationToken.None));

        Assert.Equal("GAME_NOT_FREE", exception.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ClaimFreeGame_rejects_blank_game_before_opening_database(string gameId)
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ClaimFreeGameAsync(PlayerClaims, gameId, CancellationToken.None));

        Assert.StartsWith("gameId is required.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddPlaytime_rejects_non_positive_minutes_before_opening_database()
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddPlaytimeAsync(PlayerClaims, "GAME_DST", new UpdatePlaytimeRequest(0), CancellationToken.None));

        Assert.StartsWith("MinutesToAdd must be greater than 0.", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("", "reason", "OrderId is required.")]
    [InlineData("O_DST_001", "", "Reason is required.")]
    public async Task CreateRefund_rejects_invalid_request_before_opening_database(string orderId, string reason, string expectedMessage)
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateRefundAsync(PlayerClaims, new CreateRefundRequest(orderId, reason), CancellationToken.None));

        Assert.StartsWith(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApproveRefund_requires_admin_claims_before_opening_database()
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ApproveRefundAsync(PlayerClaims, "R001", new AuditRefundRequest("ok"), CancellationToken.None));
    }

    [Fact]
    public async Task RejectRefund_rejects_blank_refund_id_before_opening_database()
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RejectRefundAsync(AdminClaims, "", new AuditRefundRequest("no"), CancellationToken.None));

        Assert.StartsWith("refundId is required.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateCdkeyBatch_requires_developer_or_admin_before_opening_database()
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateCdkeyBatchAsync(
                PlayerClaims,
                new CreateCdkeyBatchRequest("GAME_DST", "DST-DEMO", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 1),
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateCdkeyBatch_rejects_non_dst_before_opening_database()
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.CreateCdkeyBatchAsync(
                DeveloperClaims,
                new CreateCdkeyBatchRequest("GAME_CS2", "CS2-DEMO", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), 1),
                CancellationToken.None));

        Assert.Equal("CDKEY_GAME_UNSUPPORTED", exception.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task CreateCdkeyBatch_rejects_invalid_quantity_before_opening_database(int quantity)
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateCdkeyBatchAsync(
                DeveloperClaims,
                new CreateCdkeyBatchRequest("GAME_DST", "DST-DEMO", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), quantity),
                CancellationToken.None));

        Assert.StartsWith("Quantity must be between 1 and 100.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateCdkeyBatch_rejects_invalid_time_window_before_opening_database()
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());
        var now = DateTime.UtcNow;

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateCdkeyBatchAsync(
                DeveloperClaims,
                new CreateCdkeyBatchRequest("GAME_DST", "DST-DEMO", now, now, 1),
                CancellationToken.None));

        Assert.StartsWith("ExpireTime must be later than ValidFrom.", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task RedeemCdkey_rejects_blank_cdkey_before_opening_database(string cdkey)
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RedeemCdkeyAsync(PlayerClaims, new RedeemCdkeyRequest(cdkey), CancellationToken.None));

        Assert.StartsWith("Cdkey is required.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Wallet_requires_player_claims_before_opening_database()
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetWalletAsync(AdminClaims, CancellationToken.None));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100000)]
    [InlineData(1.001)]
    public async Task Recharge_rejects_invalid_amount_before_opening_database(decimal amount)
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.RechargeWalletAsync(PlayerClaims, new RechargeWalletRequest(amount, "recharge-idem"), CancellationToken.None));

        Assert.Equal("INVALID_AMOUNT", exception.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Recharge_requires_idempotency_key_before_opening_database(string idempotencyKey)
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.RechargeWalletAsync(PlayerClaims, new RechargeWalletRequest(10, idempotencyKey), CancellationToken.None));

        Assert.Equal("IDEMPOTENCY_KEY_REQUIRED", exception.Code);
    }

    [Fact]
    public async Task Recharge_rejects_long_idempotency_key_before_opening_database()
    {
        var service = new CoreTransactionService(new ThrowingConnectionFactory());
        var idempotencyKey = new string('x', 65);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.RechargeWalletAsync(PlayerClaims, new RechargeWalletRequest(10, idempotencyKey), CancellationToken.None));

        Assert.Equal("IDEMPOTENCY_CONFLICT", exception.Code);
    }

    [Fact]
    public async Task Recharge_replays_existing_same_amount_with_original_balance_snapshot()
    {
        var connection = new ScriptedReaderConnection(
            new Dictionary<string, object?>
            {
                ["WalletId"] = "W001",
                ["UserId"] = "P001",
                ["AvailableBalance"] = 25m,
                ["FrozenBalance"] = 5m,
                ["Version"] = 3L
            },
            new Dictionary<string, object?>
            {
                ["WalletId"] = "W001",
                ["TxnId"] = "WT_ORIGINAL",
                ["BizType"] = "RECHARGE",
                ["BizRefId"] = "WT_ORIGINAL",
                ["FundsDirection"] = "CREDIT",
                ["Amount"] = 100m,
                ["AvailBalBefore"] = 0m,
                ["AvailBalAfter"] = 100m,
                ["IdempotencyKey"] = "recharge-idem",
                ["CreateTime"] = DateTime.UtcNow
            });
        var service = new CoreTransactionService(new SingleConnectionFactory(connection));

        var result = await service.RechargeWalletAsync(PlayerClaims, new RechargeWalletRequest(100m, "recharge-idem"), CancellationToken.None);

        Assert.Equal("WT_ORIGINAL", result.TransactionId);
        Assert.Equal(100m, result.AvailableBalance);
        Assert.Equal(5m, result.FrozenBalance);
        Assert.Equal(105m, result.TotalBalance);
        Assert.Equal(0, connection.NonQueryCount);
        Assert.True(connection.Transaction?.Committed);
    }

    [Fact]
    public async Task Recharge_rejects_existing_same_key_with_different_amount()
    {
        var connection = new ScriptedReaderConnection(
            new Dictionary<string, object?>
            {
                ["WalletId"] = "W001",
                ["UserId"] = "P001",
                ["AvailableBalance"] = 100m,
                ["FrozenBalance"] = 0m,
                ["Version"] = 1L
            },
            new Dictionary<string, object?>
            {
                ["WalletId"] = "W001",
                ["TxnId"] = "WT_ORIGINAL",
                ["BizType"] = "RECHARGE",
                ["BizRefId"] = "WT_ORIGINAL",
                ["FundsDirection"] = "CREDIT",
                ["Amount"] = 100m,
                ["AvailBalBefore"] = 0m,
                ["AvailBalAfter"] = 100m,
                ["IdempotencyKey"] = "recharge-idem",
                ["CreateTime"] = DateTime.UtcNow
            });
        var service = new CoreTransactionService(new SingleConnectionFactory(connection));

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.RechargeWalletAsync(PlayerClaims, new RechargeWalletRequest(1m, "recharge-idem"), CancellationToken.None));

        Assert.Equal("IDEMPOTENCY_CONFLICT", exception.Code);
        Assert.Equal(0, connection.NonQueryCount);
        Assert.True(connection.Transaction?.RolledBack);
    }

    private sealed class SingleConnectionFactory(DbConnection connection) : IDbConnectionFactory
    {
        public DbConnection CreateConnection() => connection;
    }

    private sealed class ScriptedReaderConnection(params IReadOnlyDictionary<string, object?>[] rows) : DbConnection
    {
        private readonly Queue<IReadOnlyDictionary<string, object?>> _rows = new(rows);
        private ConnectionState _state = ConnectionState.Closed;

        public ScriptedReaderTransaction? Transaction { get; private set; }
        public int NonQueryCount { get; private set; }

        [AllowNull]
        public override string ConnectionString { get; set; } = "";
        public override string Database => "Fake";
        public override string DataSource => "Fake";
        public override string ServerVersion => "1";
        public override ConnectionState State => _state;

        public override void ChangeDatabase(string databaseName)
        {
        }

        public override void Close() => _state = ConnectionState.Closed;

        public override void Open() => _state = ConnectionState.Open;

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            _state = ConnectionState.Open;
            return Task.CompletedTask;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            Transaction = new ScriptedReaderTransaction(this, isolationLevel);
            return Transaction;
        }

        protected override DbCommand CreateDbCommand() => new ScriptedReaderCommand(this);

        public DbDataReader NextReader()
        {
            if (_rows.Count == 0)
            {
                return new SingleRowReader(null);
            }

            return new SingleRowReader(_rows.Dequeue());
        }

        public int ExecuteNonQuery()
        {
            NonQueryCount++;
            return 1;
        }
    }

    private sealed class ScriptedReaderTransaction(DbConnection connection, IsolationLevel isolationLevel) : DbTransaction
    {
        public bool Committed { get; private set; }
        public bool RolledBack { get; private set; }
        public override IsolationLevel IsolationLevel { get; } = isolationLevel;
        protected override DbConnection DbConnection { get; } = connection;

        public override void Commit() => Committed = true;

        public override Task CommitAsync(CancellationToken cancellationToken = default)
        {
            Committed = true;
            return Task.CompletedTask;
        }

        public override void Rollback() => RolledBack = true;

        public override Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            RolledBack = true;
            return Task.CompletedTask;
        }
    }

    private sealed class ScriptedReaderCommand(ScriptedReaderConnection connection) : DbCommand
    {
        private readonly FakeParameterCollection _parameters = new();

        [AllowNull]
        public override string CommandText { get; set; } = "";
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        [AllowNull]
        protected override DbConnection DbConnection { get; set; } = connection;
        protected override DbParameterCollection DbParameterCollection => _parameters;
        protected override DbTransaction? DbTransaction { get; set; }
        public override bool DesignTimeVisible { get; set; }

        public override void Cancel()
        {
        }

        public override int ExecuteNonQuery() => connection.ExecuteNonQuery();

        public override object? ExecuteScalar() => null;

        public override void Prepare()
        {
        }

        protected override DbParameter CreateDbParameter() => new FakeParameter();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => connection.NextReader();

        protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) =>
            Task.FromResult(connection.NextReader());

        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) =>
            Task.FromResult(ExecuteNonQuery());
    }

    private sealed class SingleRowReader(IReadOnlyDictionary<string, object?>? row) : DbDataReader
    {
        private readonly string[] _columns = row?.Keys.ToArray() ?? [];
        private bool _read;

        public override object this[int ordinal] => GetValue(ordinal);
        public override object this[string name] => row?[name] ?? DBNull.Value;
        public override int Depth => 0;
        public override int FieldCount => _columns.Length;
        public override bool HasRows => row is not null;
        public override bool IsClosed => false;
        public override int RecordsAffected => 0;

        public override bool GetBoolean(int ordinal) => (bool)GetValue(ordinal);
        public override byte GetByte(int ordinal) => (byte)GetValue(ordinal);
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => 0;
        public override char GetChar(int ordinal) => (char)GetValue(ordinal);
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => 0;
        public override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;
        public override DateTime GetDateTime(int ordinal) => (DateTime)GetValue(ordinal);
        public override decimal GetDecimal(int ordinal) => Convert.ToDecimal(GetValue(ordinal));
        public override double GetDouble(int ordinal) => Convert.ToDouble(GetValue(ordinal));
        public override Type GetFieldType(int ordinal) => GetValue(ordinal).GetType();
        public override float GetFloat(int ordinal) => Convert.ToSingle(GetValue(ordinal));
        public override Guid GetGuid(int ordinal) => (Guid)GetValue(ordinal);
        public override short GetInt16(int ordinal) => Convert.ToInt16(GetValue(ordinal));
        public override int GetInt32(int ordinal) => Convert.ToInt32(GetValue(ordinal));
        public override long GetInt64(int ordinal) => Convert.ToInt64(GetValue(ordinal));
        public override string GetName(int ordinal) => _columns[ordinal];
        public override int GetOrdinal(string name) => Array.IndexOf(_columns, name);
        public override string GetString(int ordinal) => (string)GetValue(ordinal);
        public override object GetValue(int ordinal) => row?[_columns[ordinal]] ?? DBNull.Value;

        public override int GetValues(object[] values)
        {
            var count = Math.Min(values.Length, FieldCount);
            for (var i = 0; i < count; i++)
            {
                values[i] = GetValue(i);
            }

            return count;
        }

        public override bool IsDBNull(int ordinal) => GetValue(ordinal) is DBNull;
        public override bool NextResult() => false;

        public override bool Read()
        {
            if (_read || row is null)
            {
                return false;
            }

            _read = true;
            return true;
        }

        public override IEnumerator GetEnumerator()
        {
            while (Read())
            {
                yield return this;
            }
        }
    }

    private sealed class FakeParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _items = [];

        public override int Count => _items.Count;
        public override object SyncRoot => this;

        public override int Add(object value)
        {
            _items.Add((DbParameter)value);
            return _items.Count - 1;
        }

        public override void AddRange(Array values)
        {
            foreach (var value in values)
            {
                Add(value!);
            }
        }

        public override void Clear() => _items.Clear();

        public override bool Contains(object value) => _items.Contains((DbParameter)value);

        public override bool Contains(string value) => _items.Any(parameter => parameter.ParameterName == value);

        public override void CopyTo(Array array, int index) => _items.ToArray().CopyTo(array, index);

        public override IEnumerator GetEnumerator() => _items.GetEnumerator();

        public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);

        public override int IndexOf(string parameterName) => _items.FindIndex(parameter => parameter.ParameterName == parameterName);

        public override void Insert(int index, object value) => _items.Insert(index, (DbParameter)value);

        public override void Remove(object value) => _items.Remove((DbParameter)value);

        public override void RemoveAt(int index) => _items.RemoveAt(index);

        public override void RemoveAt(string parameterName)
        {
            var index = IndexOf(parameterName);
            if (index >= 0)
            {
                RemoveAt(index);
            }
        }

        protected override DbParameter GetParameter(int index) => _items[index];

        protected override DbParameter GetParameter(string parameterName) => _items[IndexOf(parameterName)];

        protected override void SetParameter(int index, DbParameter value) => _items[index] = value;

        protected override void SetParameter(string parameterName, DbParameter value)
        {
            var index = IndexOf(parameterName);
            if (index >= 0)
            {
                _items[index] = value;
                return;
            }

            _items.Add(value);
        }
    }

    private sealed class FakeParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
        public override bool IsNullable { get; set; }
        [AllowNull]
        public override string ParameterName { get; set; } = "";
        [AllowNull]
        public override string SourceColumn { get; set; } = "";
        public override object? Value { get; set; }
        public override bool SourceColumnNullMapping { get; set; }
        public override int Size { get; set; }

        public override void ResetDbType()
        {
        }
    }
}
