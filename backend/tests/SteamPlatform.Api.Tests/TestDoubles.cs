using System.Data.Common;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SteamPlatform.Application.Auth;
using SteamPlatform.Infrastructure.Data;
using SteamPlatform.Application.CoreTransactions;

namespace SteamPlatform.Api.Tests;

public sealed class SteamPlatformApiFactory : WebApplicationFactory<Program>
{
    public const string SigningKey = "steam-platform-test-signing-key-000001";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            // Remove existing file-based configuration sources to avoid parsing appsettings files during tests
            configuration.Sources.Clear();
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:SigningKey"] = SigningKey
            });
        });
        builder.ConfigureServices(services =>
        {
            // Prevent tests from opening real database connections
            services.AddSingleton<IDbConnectionFactory, ThrowingConnectionFactory>();
            // Ensure tests use in-memory core transaction service when available
            services.AddSingleton<ICoreTransactionService, SteamPlatform.Infrastructure.CoreTransactions.InMemoryCoreTransactionService>();

            // Use a fixed signing key provider in tests for predictable tokens
            services.AddSingleton<IAuthSigningKeyProvider>(new FixedSigningKeyProvider());
        });
    }

    // Note: ConfigureHost override removed to keep WebApplicationFactory behavior standard.
}

internal sealed class FixedSigningKeyProvider(string signingKey = SteamPlatformApiFactory.SigningKey) : IAuthSigningKeyProvider
{
    public byte[] Key { get; } = Encoding.UTF8.GetBytes(signingKey);
}

internal sealed class ThrowingConnectionFactory : IDbConnectionFactory
{
    public DbConnection CreateConnection() => throw new InvalidOperationException("Tests must not open database connections.");
}
