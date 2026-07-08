using System.Collections;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Data.Common;
using SteamPlatform.Application.Community;
using SteamPlatform.Infrastructure.Community;
using SteamPlatform.Infrastructure.Data;
using SteamPlatform.Shared;

namespace SteamPlatform.Api.Tests;

public sealed class CommunityRepositoryGuardTests
{
    [Fact]
    public async Task Create_review_rejects_unowned_game_before_writing_review()
    {
        var factory = new ScriptedScalarConnectionFactory(1, 0);
        var repository = new ReviewRepository(factory);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            repository.CreateAsync(
                "GAME_DST",
                "P002",
                new CreateReviewRequest(true, "I should own this before reviewing."),
                CancellationToken.None));

        Assert.Equal("GAME_NOT_OWNED", exception.Code);
        Assert.Contains(factory.CommandTexts, command => ContainsSql(command, "from player_library"));
        Assert.Contains(factory.CommandTexts, command => ContainsSql(command, "status = 'NORMAL'"));
        Assert.DoesNotContain(factory.CommandTexts, command => ContainsSql(command, "insert into game_review"));
        Assert.DoesNotContain(factory.CommandTexts, command => ContainsSql(command, "insert into review_version"));
    }

    [Fact]
    public async Task Unlock_achievement_rejects_unowned_game_before_writing_unlock()
    {
        var factory = new ScriptedScalarConnectionFactory("GAME_DST", 0);
        var repository = new AchievementRepository(factory);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            repository.UnlockAsync("P002", "ACH_DST_SURVIVE_001", CancellationToken.None));

        Assert.Equal("GAME_NOT_OWNED", exception.Code);
        Assert.Contains(factory.CommandTexts, command => ContainsSql(command, "from achievement"));
        Assert.Contains(factory.CommandTexts, command => ContainsSql(command, "from player_library"));
        Assert.Contains(factory.CommandTexts, command => ContainsSql(command, "status = 'NORMAL'"));
        Assert.DoesNotContain(factory.CommandTexts, command => ContainsSql(command, "from player_achievement"));
        Assert.DoesNotContain(factory.CommandTexts, command => ContainsSql(command, "insert into player_achievement"));
    }

    private static bool ContainsSql(string command, string expected) =>
        command.Contains(expected, StringComparison.OrdinalIgnoreCase);

    private sealed class ScriptedScalarConnectionFactory : IDbConnectionFactory
    {
        private readonly Queue<object?> _scalarResults;

        public ScriptedScalarConnectionFactory(params object?[] scalarResults)
        {
            _scalarResults = new Queue<object?>(scalarResults);
        }

        public List<string> CommandTexts { get; } = [];

        public DbConnection CreateConnection() => new ScriptedScalarConnection(_scalarResults, CommandTexts);
    }

    private sealed class ScriptedScalarConnection : DbConnection
    {
        private readonly Queue<object?> _scalarResults;
        private readonly List<string> _commandTexts;
        private ConnectionState _state = ConnectionState.Closed;

        public ScriptedScalarConnection(Queue<object?> scalarResults, List<string> commandTexts)
        {
            _scalarResults = scalarResults;
            _commandTexts = commandTexts;
        }

        [AllowNull]
        public override string ConnectionString { get; set; } = "";
        public override string Database => "Scripted";
        public override string DataSource => "Scripted";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => _state;

        public override void ChangeDatabase(string databaseName)
        {
        }

        public override void Close()
        {
            _state = ConnectionState.Closed;
        }

        public override void Open()
        {
            _state = ConnectionState.Open;
        }

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            Open();
            return Task.CompletedTask;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => new ScriptedTransaction(this);

        protected override DbCommand CreateDbCommand() => new ScriptedScalarCommand(this, _scalarResults, _commandTexts);
    }

    private sealed class ScriptedTransaction : DbTransaction
    {
        private readonly DbConnection _connection;

        public ScriptedTransaction(DbConnection connection)
        {
            _connection = connection;
        }

        public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
        protected override DbConnection DbConnection => _connection;

        public override void Commit()
        {
        }

        public override void Rollback()
        {
        }
    }

    private sealed class ScriptedScalarCommand : DbCommand
    {
        private readonly Queue<object?> _scalarResults;
        private readonly List<string> _commandTexts;
        private readonly FakeParameterCollection _parameters = new();

        public ScriptedScalarCommand(DbConnection connection, Queue<object?> scalarResults, List<string> commandTexts)
        {
            DbConnection = connection;
            _scalarResults = scalarResults;
            _commandTexts = commandTexts;
        }

        [AllowNull]
        public override string CommandText { get; set; } = "";
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection => _parameters;
        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel()
        {
        }

        public override int ExecuteNonQuery() =>
            throw new InvalidOperationException("The unowned-game guard should run before any write command.");

        public override object? ExecuteScalar()
        {
            _commandTexts.Add(CommandText);
            if (_scalarResults.Count == 0)
            {
                throw new InvalidOperationException($"No scripted scalar result exists for command: {CommandText}");
            }

            return _scalarResults.Dequeue();
        }

        public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken) => Task.FromResult(ExecuteScalar());

        public override void Prepare()
        {
        }

        protected override DbParameter CreateDbParameter() => new FakeParameter();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
            throw new InvalidOperationException("The unowned-game guard should run before any read past ownership validation.");
    }

    private sealed class FakeParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _parameters = [];

        public override int Count => _parameters.Count;
        public override object SyncRoot => this;

        public override int Add(object value)
        {
            _parameters.Add((DbParameter)value);
            return _parameters.Count - 1;
        }

        public override void AddRange(Array values)
        {
            foreach (var value in values)
            {
                Add(value!);
            }
        }

        public override void Clear() => _parameters.Clear();

        public override bool Contains(object value) => value is DbParameter parameter && _parameters.Contains(parameter);

        public override bool Contains(string value) => IndexOf(value) >= 0;

        public override void CopyTo(Array array, int index) => _parameters.ToArray().CopyTo(array, index);

        public override IEnumerator GetEnumerator() => _parameters.GetEnumerator();

        public override int IndexOf(object value) => value is DbParameter parameter ? _parameters.IndexOf(parameter) : -1;

        public override int IndexOf(string parameterName) =>
            _parameters.FindIndex(parameter => string.Equals(parameter.ParameterName, parameterName, StringComparison.OrdinalIgnoreCase));

        public override void Insert(int index, object value) => _parameters.Insert(index, (DbParameter)value);

        public override void Remove(object value)
        {
            if (value is DbParameter parameter)
            {
                _parameters.Remove(parameter);
            }
        }

        public override void RemoveAt(int index) => _parameters.RemoveAt(index);

        public override void RemoveAt(string parameterName)
        {
            var index = IndexOf(parameterName);
            if (index >= 0)
            {
                RemoveAt(index);
            }
        }

        protected override DbParameter GetParameter(int index) => _parameters[index];

        protected override DbParameter GetParameter(string parameterName) => _parameters[IndexOf(parameterName)];

        protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;

        protected override void SetParameter(string parameterName, DbParameter value)
        {
            var index = IndexOf(parameterName);
            if (index >= 0)
            {
                _parameters[index] = value;
                return;
            }

            _parameters.Add(value);
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