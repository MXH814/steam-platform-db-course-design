using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SteamPlatform.Infrastructure.Auth;

namespace SteamPlatform.Api.Tests;

public sealed class AuthSigningKeyProviderTests
{
    [Fact]
    public void Configured_key_is_used_outside_development()
    {
        var provider = new AuthSigningKeyProvider(
            Configuration("configured-signing-key-000000000001"),
            Environment("Production"));

        Assert.Equal("configured-signing-key-000000000001", System.Text.Encoding.UTF8.GetString(provider.Key));
    }

    [Fact]
    public void Short_configured_key_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new AuthSigningKeyProvider(Configuration("too-short"), Environment("Production")));
    }

    [Fact]
    public void Development_without_config_uses_fallback_key()
    {
        var provider = new AuthSigningKeyProvider(Configuration(null), Environment("Development"));

        Assert.True(provider.Key.Length >= 32);
    }

    [Fact]
    public void Non_development_without_config_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new AuthSigningKeyProvider(Configuration(null), Environment("Production")));
    }

    [Fact]
    public void Key_getter_returns_defensive_copy()
    {
        var provider = new AuthSigningKeyProvider(
            Configuration("configured-signing-key-000000000001"),
            Environment("Production"));

        var first = provider.Key;
        first[0] = (byte)'x';

        Assert.Equal((byte)'c', provider.Key[0]);
    }

    private static IConfiguration Configuration(string? signingKey) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:SigningKey"] = signingKey
            })
            .Build();

    private static IHostEnvironment Environment(string environmentName) =>
        new StubHostEnvironment { EnvironmentName = environmentName };

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "";
        public string ApplicationName { get; set; } = "SteamPlatform.Api.Tests";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
