using Dapper;
using Microsoft.Extensions.DependencyInjection;
using SteamPlatform.Application.Auth;
using SteamPlatform.Application.Community;
using SteamPlatform.Application.Diagnostics;
using SteamPlatform.Application.Notices;
using SteamPlatform.Infrastructure.Auth;
using SteamPlatform.Infrastructure.Community;
using SteamPlatform.Infrastructure.Data;
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
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IAchievementRepository, AchievementRepository>();
        services.AddScoped<IDatabaseHealthProbe, OracleDatabaseHealthProbe>();

        return services;
    }
}
