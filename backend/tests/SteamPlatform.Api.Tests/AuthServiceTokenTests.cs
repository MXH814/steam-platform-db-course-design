using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using SteamPlatform.Application.Auth;
using SteamPlatform.Infrastructure.Auth;

namespace SteamPlatform.Api.Tests;

public sealed class AuthServiceTokenTests
{
    [Theory]
    [InlineData("alice", "PBKDF2$SHA256$100000$c2VlZC1hbGljZV9fX19fXw==$iTPCU6/lngHZz3zx/gYotoK0h7N0WJu8m0Vnre7/1NA=")]
    [InlineData("bob", "PBKDF2$SHA256$100000$c2VlZC1ib2JfX19fX19fXw==$2CvTcEyGV8IfmgB6hEZN+em2lyvIsaRLrQJ/5YgkipM=")]
    [InlineData("admin", "PBKDF2$SHA256$100000$c2VlZC1yb290YWRtaW5fXw==$yHE6M2jmsTpAplUmz5Vjp4o3zmV30sSQwdnx0jMVHpo=")]
    public void PasswordHasher_verifies_seed_hashes(string password, string storedHash)
    {
        var hasher = new PasswordHasher();

        Assert.True(hasher.Verify(password, storedHash, out var needsRehash));
        Assert.False(needsRehash);
    }

    [Theory]
    [InlineData("plain-password")]
    [InlineData("PBKDF2$SHA256$100000$not-base64$also-not-base64")]
    public void PasswordHasher_rejects_unrecognized_or_malformed_hashes(string storedHash)
    {
        var hasher = new PasswordHasher();

        Assert.False(hasher.Verify("plain-password", storedHash, out var needsRehash));
        Assert.False(needsRehash);
    }

    [Fact]
    public void CreateToken_rejects_invalid_claims()
    {
        var service = CreateService();

        var exception = Assert.Throws<ArgumentException>(() =>
            service.CreateToken(new AuthClaims("UNKNOWN", "P001", "alice", DateTimeOffset.UtcNow.AddMinutes(5))));

        Assert.Equal("claims", exception.ParamName);
    }

    [Fact]
    public void Token_round_trips_current_claims()
    {
        var service = CreateService();
        var claims = new AuthClaims("player", "P001", "alice", DateTimeOffset.UtcNow.AddMinutes(5));

        var token = service.CreateToken(claims);
        var validated = service.ValidateToken("Bearer " + token);

        Assert.Equal(3, token.Split('.').Length);
        Assert.NotNull(validated);
        Assert.Equal("PLAYER", validated.Role);
        Assert.Equal(claims.PrincipalId, validated.PrincipalId);
        Assert.Equal(claims.Account, validated.Account);
    }

    [Theory]
    [InlineData("PLAYER")]
    [InlineData("DEVELOPER")]
    [InlineData("ADMIN")]
    [InlineData("SUPER_ADMIN")]
    [InlineData("AUDITOR")]
    [InlineData("RISK_ADMIN")]
    [InlineData("CUSTOMER_SERVICE")]
    public void ValidateToken_accepts_known_roles(string role)
    {
        var service = CreateService();
        var token = service.CreateToken(new AuthClaims(role, "P001", "account", DateTimeOffset.UtcNow.AddMinutes(5)));

        var validated = service.ValidateToken(token);

        Assert.NotNull(validated);
        Assert.Equal(role, validated.Role);
    }

    [Fact]
    public void ValidateToken_rejects_tampered_signature()
    {
        var service = CreateService();
        var token = service.CreateToken(new AuthClaims("PLAYER", "P001", "alice", DateTimeOffset.UtcNow.AddMinutes(5)));

        Assert.Null(service.ValidateToken(token + "x"));
    }

    [Fact]
    public void ValidateToken_rejects_expired_tokens()
    {
        var service = CreateService();
        var token = CreateSignedToken(DateTimeOffset.UtcNow.AddMinutes(-5), SteamPlatformApiFactory.SigningKey);

        Assert.Null(service.ValidateToken(token));
    }

    [Fact]
    public void ValidateToken_rejects_tokens_signed_with_different_key()
    {
        var issuingService = CreateService("different-test-signing-key-0000000001");
        var validatingService = CreateService();
        var token = issuingService.CreateToken(new AuthClaims("PLAYER", "P001", "alice", DateTimeOffset.UtcNow.AddMinutes(5)));

        Assert.Null(validatingService.ValidateToken(token));
    }

    [Fact]
    public void Service_keeps_its_own_copy_of_signing_key()
    {
        var signingKeyProvider = new MutableSigningKeyProvider(SteamPlatformApiFactory.SigningKey);
        var service = new AuthService(new ThrowingConnectionFactory(), signingKeyProvider, new PasswordHasher());
        var token = service.CreateToken(new AuthClaims("PLAYER", "P001", "alice", DateTimeOffset.UtcNow.AddMinutes(5)));

        signingKeyProvider.Key[0] = (byte)'x';

        Assert.NotNull(service.ValidateToken(token));
    }

    [Fact]
    public async Task Developer_login_is_disabled_until_schema_has_password_credentials()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync(new LoginRequest("DEVELOPER", "dev@example.com", "TAX-DEMO-001"), CancellationToken.None));

        Assert.Contains("Developer password login is not available", exception.Message);
    }

    private static AuthService CreateService(string signingKey = SteamPlatformApiFactory.SigningKey) =>
        new(new ThrowingConnectionFactory(), new FixedSigningKeyProvider(signingKey), new PasswordHasher());

    private static string CreateSignedToken(DateTimeOffset expiresAt, string signingKey)
    {
        var claims = new[]
        {
            new Claim(AuthTokenValidation.RoleClaim, "PLAYER"),
            new Claim(AuthTokenValidation.PrincipalIdClaim, "P001"),
            new Claim(AuthTokenValidation.AccountClaim, "alice"),
            new Claim(AuthTokenValidation.ExpiresAtClaim, expiresAt.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };
        var token = new JwtSecurityToken(
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-10),
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class MutableSigningKeyProvider(string signingKey) : IAuthSigningKeyProvider
    {
        public byte[] Key { get; } = System.Text.Encoding.UTF8.GetBytes(signingKey);
    }
}
