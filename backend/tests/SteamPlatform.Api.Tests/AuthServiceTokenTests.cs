using SteamPlatform.Api.Features.Auth;

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
        var claims = new AuthClaims("PLAYER", "P001", "alice", DateTimeOffset.UtcNow.AddMinutes(5));

        var token = service.CreateToken(claims);
        var validated = service.ValidateToken("Bearer " + token);

        Assert.NotNull(validated);
        Assert.Equal(claims.Role, validated.Role);
        Assert.Equal(claims.PrincipalId, validated.PrincipalId);
        Assert.Equal(claims.Account, validated.Account);
    }

    [Fact]
    public void ValidateToken_rejects_tampered_signature()
    {
        var service = CreateService();
        var token = service.CreateToken(new AuthClaims("PLAYER", "P001", "alice", DateTimeOffset.UtcNow.AddMinutes(5)));

        Assert.Null(service.ValidateToken(token + "x"));
    }

    private static AuthService CreateService() =>
        new(new ThrowingConnectionFactory(), new FixedSigningKeyProvider(), new PasswordHasher());
}
