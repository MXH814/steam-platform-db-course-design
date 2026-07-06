using System.Data.Common;
using Oracle.ManagedDataAccess.Client;

namespace SteamPlatform.Api.Data;

public sealed class OracleDbConnectionFactory(IConfiguration configuration) : IDbConnectionFactory
{
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public DbConnection CreateConnection()
    {
        var connectionString = _configuration.GetConnectionString("Oracle");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Oracle is not configured.");
        }

        return new OracleConnection(connectionString);
    }
}
