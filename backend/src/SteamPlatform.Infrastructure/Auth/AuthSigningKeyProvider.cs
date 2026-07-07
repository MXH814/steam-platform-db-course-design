using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SteamPlatform.Application.Auth;

namespace SteamPlatform.Infrastructure.Auth;

public sealed class AuthSigningKeyProvider(IConfiguration configuration, IHostEnvironment environment) : IAuthSigningKeyProvider
{
    private static readonly byte[] DevelopmentFallbackKey = Encoding.UTF8.GetBytes("steam-platform-dev-signing-key-000001");
    private readonly byte[] _key = ResolveKey(configuration, environment);

    public byte[] Key => _key.ToArray();

    private static byte[] ResolveKey(IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var configured = configuration["Auth:SigningKey"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var key = Encoding.UTF8.GetBytes(configured);
            return key.Length >= 32
                ? key
                : throw new InvalidOperationException("Auth signing key must be at least 32 bytes.");
        }

        if (environment.IsDevelopment())
        {
            return DevelopmentFallbackKey;
        }

        throw new InvalidOperationException("Auth:SigningKey must be configured outside Development.");
    }
}
