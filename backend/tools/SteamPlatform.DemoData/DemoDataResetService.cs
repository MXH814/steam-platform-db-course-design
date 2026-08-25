using System.Data;
using System.Security.Cryptography;
using System.Text;
using Oracle.ManagedDataAccess.Client;

namespace SteamPlatform.DemoData;

public sealed class DemoDataResetService(string connectionString, DemoDataManifest manifest, string repositoryRoot)
{
    private const string ResetConfirmation = "RESET-DEMO-DATA";
    private const string RestoreConfirmation = "RESTORE-DEMO-DATA";

    public static string RequiredResetConfirmation => ResetConfirmation;
    public static string RequiredRestoreConfirmation => RestoreConfirmation;

    public async Task<string> ResetAsync(string actor, string? confirmation, CancellationToken cancellationToken = default)
    {
        RequireConfirmation(confirmation, ResetConfirmation);
        var baselinePath = manifest.ResolveBaselinePath(repositoryRoot);
        var baseline = File.ReadAllText(baselinePath);
        var statements = SqlScriptParser.ParseBaseline(baseline);
        var baselineHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(baseline)));
        var runId = CreateRunId();

        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureAuditSchemaAsync(connection, cancellationToken);

        try
        {
            await CreateSnapshotAsync(connection, runId, actor, baselineHash, cancellationToken);
            await SetStatusAsync(connection, runId, "RESETTING", "RESET_STARTED", "Transactional demo reset started.", null, false, cancellationToken);

            await using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
            try
            {
                foreach (var table in manifest.DeletionOrder)
                {
                    await ExecuteAsync(connection, transaction, $"DELETE FROM {table}", cancellationToken);
                }

                foreach (var statement in statements)
                {
                    await ExecuteAsync(connection, transaction, statement, cancellationToken);
                }

                await ValidateMinimumRowsAsync(connection, transaction, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            await SetStatusAsync(connection, runId, "RESET_COMPLETED", "RESET_COMPLETED", "Baseline committed and minimum row counts verified.", null, true, cancellationToken);
            return runId;
        }
        catch (Exception exception)
        {
            await TryRecordFailureAsync(connection, runId, "RESET_FAILED", exception, cancellationToken);
            throw;
        }
    }

    public async Task RestoreAsync(string runId, string actor, string? confirmation, CancellationToken cancellationToken = default)
    {
        RequireConfirmation(confirmation, RestoreConfirmation);
        ValidateRunId(runId);

        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureAuditSchemaAsync(connection, cancellationToken);
        var mappings = await LoadMappingsAsync(connection, runId, cancellationToken);
        if (mappings.Count == 0)
        {
            throw new InvalidOperationException($"No snapshot tables were found for run {runId}.");
        }

        try
        {
            await SetStatusAsync(connection, runId, "RESTORING", "RESTORE_STARTED", $"Restore requested by {actor}.", null, false, cancellationToken);
            await using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
            try
            {
                foreach (var mapping in mappings.OrderByDescending(item => item.Order))
                {
                    await ExecuteAsync(connection, transaction, $"DELETE FROM {mapping.SourceTable}", cancellationToken);
                }

                foreach (var mapping in mappings.OrderBy(item => item.Order))
                {
                    await ExecuteAsync(connection, transaction, $"INSERT INTO {mapping.SourceTable} SELECT * FROM {mapping.BackupTable}", cancellationToken);
                    var restoredCount = await CountAsync(connection, transaction, mapping.SourceTable, cancellationToken);
                    if (restoredCount != mapping.RowCount)
                    {
                        throw new InvalidOperationException($"Restore count mismatch for {mapping.SourceTable}: expected {mapping.RowCount}, got {restoredCount}.");
                    }
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            await SetStatusAsync(connection, runId, "RESTORED", "RESTORE_COMPLETED", "Snapshot restored and row counts verified.", null, true, cancellationToken);
        }
        catch (Exception exception)
        {
            await TryRecordFailureAsync(connection, runId, "RESTORE_FAILED", exception, cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<ResetRunSummary>> ListRunsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureAuditSchemaAsync(connection, cancellationToken);
        const string sql = "SELECT run_id, status, initiated_by, started_at, completed_at FROM demo_reset_run ORDER BY started_at DESC FETCH FIRST 20 ROWS ONLY";
        await using var command = CreateCommand(connection, null, sql);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<ResetRunSummary>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new ResetRunSummary(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetDateTime(3),
                reader.IsDBNull(4) ? null : reader.GetDateTime(4)));
        }

        return result;
    }

    private async Task CreateSnapshotAsync(OracleConnection connection, string runId, string actor, string baselineHash, CancellationToken cancellationToken)
    {
        await AuditAsync(connection, async transaction =>
        {
            const string insertRun = "INSERT INTO demo_reset_run (run_id, status, initiated_by, baseline_sha256) VALUES (:run_id, 'SNAPSHOTTING', :actor, :hash)";
            await ExecuteAsync(connection, transaction, insertRun, cancellationToken,
                Parameter("run_id", runId), Parameter("actor", actor), Parameter("hash", baselineHash));
            await InsertEventAsync(connection, transaction, runId, "SNAPSHOT_STARTED", "Creating same-schema logical snapshot tables.", cancellationToken);
        }, cancellationToken);

        for (var index = 0; index < manifest.InsertionOrder.Count; index++)
        {
            var sourceTable = manifest.InsertionOrder[index];
            var backupTable = $"DRB_{runId}_{index + 1:00}";
            await ExecuteAsync(connection, null, $"CREATE TABLE {backupTable} NOLOGGING AS SELECT * FROM {sourceTable}", cancellationToken);
            var rowCount = await CountAsync(connection, null, backupTable, cancellationToken);

            await AuditAsync(connection, async transaction =>
            {
                const string insertMapping = "INSERT INTO demo_reset_table (run_id, table_order, source_table, backup_table, row_count) VALUES (:run_id, :table_order, :source_table, :backup_table, :row_count)";
                await ExecuteAsync(connection, transaction, insertMapping, cancellationToken,
                    Parameter("run_id", runId),
                    Parameter("table_order", index + 1, OracleDbType.Int32),
                    Parameter("source_table", sourceTable),
                    Parameter("backup_table", backupTable),
                    Parameter("row_count", rowCount, OracleDbType.Int64));
            }, cancellationToken);
        }

        await SetStatusAsync(connection, runId, "SNAPSHOT_READY", "SNAPSHOT_READY", $"Snapshot contains {manifest.InsertionOrder.Count} business tables.", null, false, cancellationToken);
    }

    private async Task ValidateMinimumRowsAsync(OracleConnection connection, OracleTransaction transaction, CancellationToken cancellationToken)
    {
        foreach (var (table, minimum) in manifest.MinimumRows)
        {
            var actual = await CountAsync(connection, transaction, table, cancellationToken);
            if (actual < minimum)
            {
                throw new InvalidOperationException($"Baseline validation failed for {table}: expected at least {minimum}, got {actual}.");
            }
        }
    }

    private static async Task<IReadOnlyList<TableSnapshot>> LoadMappingsAsync(OracleConnection connection, string runId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT table_order, source_table, backup_table, row_count FROM demo_reset_table WHERE run_id = :run_id ORDER BY table_order";
        await using var command = CreateCommand(connection, null, sql, Parameter("run_id", runId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<TableSnapshot>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new TableSnapshot(
                Convert.ToInt32(reader.GetValue(0)),
                reader.GetString(1),
                reader.GetString(2),
                Convert.ToInt64(reader.GetValue(3))));
        }

        return result;
    }

    private static async Task EnsureAuditSchemaAsync(OracleConnection connection, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(*) FROM user_tables WHERE table_name IN ('DEMO_RESET_RUN', 'DEMO_RESET_TABLE', 'DEMO_RESET_EVENT')";
        await using var command = CreateCommand(connection, null, sql);
        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (count != 3)
        {
            throw new InvalidOperationException("Demo reset audit tables are missing. Apply database/migrations/20260825_demo_reset_audit.sql first.");
        }
    }

    private static async Task SetStatusAsync(
        OracleConnection connection,
        string runId,
        string status,
        string eventType,
        string message,
        string? error,
        bool complete,
        CancellationToken cancellationToken)
    {
        await AuditAsync(connection, async transaction =>
        {
            var sql = complete
                ? "UPDATE demo_reset_run SET status = :status, completed_at = SYSTIMESTAMP, error_message = :error WHERE run_id = :run_id"
                : "UPDATE demo_reset_run SET status = :status, error_message = :error WHERE run_id = :run_id";
            await ExecuteAsync(connection, transaction, sql, cancellationToken,
                Parameter("status", status), Parameter("error", error), Parameter("run_id", runId));
            await InsertEventAsync(connection, transaction, runId, eventType, message, cancellationToken);
        }, cancellationToken);
    }

    private static async Task TryRecordFailureAsync(OracleConnection connection, string runId, string status, Exception exception, CancellationToken cancellationToken)
    {
        try
        {
            var error = exception.Message.Length <= 1000 ? exception.Message : exception.Message[..1000];
            await SetStatusAsync(connection, runId, status, status, error, error, true, cancellationToken);
        }
        catch
        {
            // Preserve the original reset/restore exception if audit recording also fails.
        }
    }

    private static async Task InsertEventAsync(OracleConnection connection, OracleTransaction transaction, string runId, string eventType, string message, CancellationToken cancellationToken)
    {
        const string sql = "INSERT INTO demo_reset_event (event_id, run_id, event_type, message) VALUES (:event_id, :run_id, :event_type, :message)";
        await ExecuteAsync(connection, transaction, sql, cancellationToken,
            Parameter("event_id", Guid.NewGuid().ToString("N")),
            Parameter("run_id", runId),
            Parameter("event_type", eventType),
            Parameter("message", message));
    }

    private static async Task AuditAsync(OracleConnection connection, Func<OracleTransaction, Task> action, CancellationToken cancellationToken)
    {
        await using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            await action(transaction);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<long> CountAsync(OracleConnection connection, OracleTransaction? transaction, string table, CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(connection, transaction, $"SELECT COUNT(*) FROM {table}");
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<int> ExecuteAsync(
        OracleConnection connection,
        OracleTransaction? transaction,
        string sql,
        CancellationToken cancellationToken,
        params OracleParameter[] parameters)
    {
        await using var command = CreateCommand(connection, transaction, sql, parameters);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static OracleCommand CreateCommand(OracleConnection connection, OracleTransaction? transaction, string sql, params OracleParameter[] parameters)
    {
        var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = sql;
        command.Transaction = transaction;
        command.Parameters.AddRange(parameters);
        return command;
    }

    private static OracleParameter Parameter(string name, object? value, OracleDbType type = OracleDbType.Varchar2) =>
        new(name, type) { Value = value ?? DBNull.Value };

    private static void RequireConfirmation(string? actual, string expected)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Operation refused. Pass --confirm {expected} after reviewing the plan.");
        }
    }

    private static string CreateRunId() =>
        $"{DateTime.UtcNow:yyyyMMddHHmmss}{RandomNumberGenerator.GetInt32(0, 65536):X4}";

    private static void ValidateRunId(string runId)
    {
        if (runId.Length is < 14 or > 20 || runId.Any(character => !char.IsAsciiLetterOrDigit(character)))
        {
            throw new ArgumentException("Invalid run id.", nameof(runId));
        }
    }

    private sealed record TableSnapshot(int Order, string SourceTable, string BackupTable, long RowCount);
}

public sealed record ResetRunSummary(string RunId, string Status, string InitiatedBy, DateTime StartedAt, DateTime? CompletedAt);
