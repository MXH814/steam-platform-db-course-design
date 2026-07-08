using System.Data.Common;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SteamPlatform.Application.Auth;
using SteamPlatform.Infrastructure.Data;

namespace SteamPlatform.Api.Tests;

public sealed class SteamPlatformApiFactory : WebApplicationFactory<Program>
{
    public const string SigningKey = "steam-platform-test-signing-key-000001";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:SigningKey"] = SigningKey
            });
        });
    }
}

internal sealed class FixedSigningKeyProvider(string signingKey = SteamPlatformApiFactory.SigningKey) : IAuthSigningKeyProvider
{
    public byte[] Key { get; } = Encoding.UTF8.GetBytes(signingKey);
}

internal sealed class ThrowingConnectionFactory : IDbConnectionFactory
{
    public DbConnection CreateConnection() => throw new InvalidOperationException("Tests must not open database connections.");
}
