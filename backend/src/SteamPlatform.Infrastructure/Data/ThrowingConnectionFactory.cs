using System.Data.Common;

namespace SteamPlatform.Infrastructure.Data;

public sealed class ThrowingConnectionFactory : IDbConnectionFactory
{
    public DbConnection CreateConnection() => throw new InvalidOperationException("This connection factory must not be used in in-memory/test mode.");
}
