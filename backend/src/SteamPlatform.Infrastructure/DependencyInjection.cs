using Dapper;
using Microsoft.Extensions.Configuration;
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
    public static IServiceCollection AddSteamPlatformInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        var useInMemory = configuration.GetValue<bool>("UseInMemoryDatabase");

        services.AddSingleton<IAuthSigningKeyProvider, AuthSigningKeyProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();

        if (useInMemory)
        {
            // In-memory implementations for local development and tests
            services.AddSingleton<IDbConnectionFactory, ThrowingConnectionFactory>();
            services.AddSingleton<ICoreTransactionService, CoreTransactions.InMemoryCoreTransactionService>();
            services.AddScoped<INoticeRepository, NoticeRepository>();
            services.AddScoped<IDatabaseHealthProbe, OracleDatabaseHealthProbe>();
        }
        else
        {
            services.AddSingleton<IDbConnectionFactory, OracleDbConnectionFactory>();
            services.AddScoped<ICoreTransactionService, CoreTransactions.CoreTransactionService>();
            services.AddScoped<INoticeRepository, NoticeRepository>();
            services.AddScoped<IDatabaseHealthProbe, OracleDatabaseHealthProbe>();
        }

        return services;
    }
}
