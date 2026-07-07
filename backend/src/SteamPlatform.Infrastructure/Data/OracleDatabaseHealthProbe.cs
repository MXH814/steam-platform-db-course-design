using Dapper;
using Microsoft.Extensions.Configuration;
using SteamPlatform.Application.Diagnostics;
using Oracle.ManagedDataAccess.Client;

namespace SteamPlatform.Infrastructure.Data;

public sealed class OracleDatabaseHealthProbe(IConfiguration configuration) : IDatabaseHealthProbe
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public async Task<DatabaseHealthResult> CheckAsync(CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("Oracle");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new DatabaseHealthResult(
                Status: "SKIPPED",
                Reason: "Configure ConnectionStrings:Oracle before checking the database.");
        }

        await using var connection = new OracleConnection(connectionString);
        var value = await connection.QuerySingleAsync<string>(new CommandDefinition(
            "select 'OK' from dual",
            cancellationToken: cancellationToken));
        return new DatabaseHealthResult(Status: value, Database: "Oracle");
    }
}
