using Dapper;
using Microsoft.Extensions.DependencyInjection;
using SteamPlatform.Application.Auth;
using SteamPlatform.Application.Diagnostics;
using SteamPlatform.Application.Games;
using SteamPlatform.Application.Notices;
using SteamPlatform.Infrastructure.Auth;
using SteamPlatform.Infrastructure.Data;
using SteamPlatform.Infrastructure.Games;
using SteamPlatform.Infrastructure.Notices;

namespace SteamPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSteamPlatformInfrastructure(this IServiceCollection services)
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        services.AddSingleton<IDbConnectionFactory, OracleDbConnectionFactory>();
        services.AddSingleton<IAuthSigningKeyProvider, AuthSigningKeyProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<INoticeRepository, NoticeRepository>();
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IDatabaseHealthProbe, OracleDatabaseHealthProbe>();

        return services;
    }
}
