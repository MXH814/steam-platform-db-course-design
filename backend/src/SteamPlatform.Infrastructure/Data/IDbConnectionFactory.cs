using System.Data.Common;

namespace SteamPlatform.Infrastructure.Data;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}
