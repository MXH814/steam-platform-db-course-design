using Dapper;
using Microsoft.Extensions.DependencyInjection;
using SteamPlatform.Application.Auth;
using SteamPlatform.Application.Community;
using SteamPlatform.Application.CoreTransactions;
using SteamPlatform.Application.Diagnostics;
using SteamPlatform.Application.Games;
using SteamPlatform.Application.Inventory;
using SteamPlatform.Application.Notices;
using SteamPlatform.Infrastructure.Auth;
using SteamPlatform.Infrastructure.Community;
using SteamPlatform.Infrastructure.CoreTransactions;
using SteamPlatform.Infrastructure.Data;
using SteamPlatform.Infrastructure.Games;
using SteamPlatform.Infrastructure.Inventory;
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
<<<<<<< HEAD

        // Default (deployable) registrations: Oracle-backed implementations.
        services.AddSingleton<IDbConnectionFactory, OracleDbConnectionFactory>();
        services.AddScoped<ICoreTransactionService, CoreTransactions.CoreTransactionService>();
=======
        services.AddScoped<ICoreTransactionService, CoreTransactionService>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
>>>>>>> 94c69591a6157d35e14fb13bc8b9bad43b5137db
        services.AddScoped<INoticeRepository, NoticeRepository>();
        services.AddScoped<IGameService, GameService>();
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IAchievementRepository, AchievementRepository>();
        services.AddScoped<IDatabaseHealthProbe, OracleDatabaseHealthProbe>();

        return services;
    }
}
