using System.Data.Common;

namespace SteamPlatform.Api.Data;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}
