using Dapper;
using Microsoft.Extensions.DependencyInjection;
using SteamPlatform.Application.Auth;
using SteamPlatform.Application.CoreTransactions;
using SteamPlatform.Application.Diagnostics;
using SteamPlatform.Application.Notices;
using SteamPlatform.Infrastructure.Auth;
using SteamPlatform.Infrastructure.CoreTransactions;
using SteamPlatform.Infrastructure.Data;
using SteamPlatform.Infrastructure.Notices;

namespace SteamPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSteamPlatformInfrastructure(this IServiceCollection services)
    {
        // Keep Dapper mapping consistent across the project
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        // Register core infrastructure services. Production and development default to Oracle-backed implementations.
        services.AddSingleton<IAuthSigningKeyProvider, AuthSigningKeyProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();

        // Default (deployable) registrations: Oracle-backed implementations.
        services.AddSingleton<IDbConnectionFactory, OracleDbConnectionFactory>();
        services.AddScoped<ICoreTransactionService, CoreTransactions.CoreTransactionService>();
        services.AddScoped<INoticeRepository, NoticeRepository>();
        services.AddScoped<IDatabaseHealthProbe, OracleDatabaseHealthProbe>();

        return services;
    }
}
