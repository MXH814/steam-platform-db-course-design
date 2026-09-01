using System.Security.Cryptography;
using System.Text;
using SteamPlatform.Infrastructure.Auth;

namespace SteamPlatform.Api.Tests;

public sealed class PasswordHasherRegressionTests
{
    [Fact]
    public void Hash_generates_current_pbkdf2_format_with_unique_salts()
    {
        var hasher = new PasswordHasher();

        var first = hasher.Hash("correct-password");
        var second = hasher.Hash("correct-password");

        Assert.StartsWith("PBKDF2$SHA256$100000$", first, StringComparison.Ordinal);
        Assert.StartsWith("PBKDF2$SHA256$100000$", second, StringComparison.Ordinal);
        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Hash_rejects_missing_password(string? password)
    {
        var hasher = new PasswordHasher();

        var exception = Assert.Throws<ArgumentException>(() => hasher.Hash(password!));

        Assert.Equal("password", exception.ParamName);
    }

    [Fact]
    public void Verify_accepts_current_hash_and_rejects_wrong_password()
    {
        var hasher = new PasswordHasher();
        var storedHash = hasher.Hash("correct-password");

        Assert.True(hasher.Verify("correct-password", storedHash, out var needsRehash));
        Assert.False(needsRehash);
        Assert.False(hasher.Verify("wrong-password", storedHash, out needsRehash));
        Assert.False(needsRehash);
    }

    [Theory]
    [InlineData(null, "PBKDF2$SHA256$100000$c2FsdC1zYWx0$aGFzaC1oYXNoLWhhc2g=")]
    [InlineData("", "PBKDF2$SHA256$100000$c2FsdC1zYWx0$aGFzaC1oYXNoLWhhc2g=")]
    [InlineData("password", null)]
    [InlineData("password", "")]
    public void Verify_rejects_missing_inputs_without_throwing(string? password, string? storedHash)
    {
        var hasher = new PasswordHasher();

        Assert.False(hasher.Verify(password!, storedHash!, out var needsRehash));
        Assert.False(needsRehash);
    }

    [Fact]
    public void Verify_accepts_outdated_pbkdf2_hash_and_requests_rehash()
    {
        var hasher = new PasswordHasher();
        var storedHash = CreateVersionedHash("correct-password", 50_000);

        Assert.True(hasher.Verify("correct-password", storedHash, out var needsRehash));
        Assert.True(needsRehash);
        Assert.False(hasher.Verify("wrong-password", storedHash, out needsRehash));
        Assert.False(needsRehash);
    }

    [Theory]
    [InlineData("plain-password")]
    [InlineData("PBKDF2$SHA256")]
    [InlineData("PBKDF2$SHA256$ 100000 $c2FsdC1zYWx0$aGFzaC1oYXNoLWhhc2g=")]
    [InlineData("PBKDF2$SHA256$+100000$c2FsdC1zYWx0$aGFzaC1oYXNoLWhhc2g=")]
    [InlineData("PBKDF2$SHA256$0$c2FsdC1zYWx0$aGFzaC1oYXNoLWhhc2g=")]
    [InlineData("PBKDF2$SHA256$1000001$c2FsdC1zYWx0$aGFzaC1oYXNoLWhhc2g=")]
    [InlineData("PBKDF2$SHA256$100000$not-base64$aGFzaC1oYXNoLWhhc2g=")]
    [InlineData("PBKDF2$SHA256$100000$c2FsdC1zYWx0$not-base64")]
    [InlineData("PBKDF2$SHA256$100000$c2FsdA==$aGFzaA==")]
    public void Verify_rejects_malformed_or_unsafe_hashes(string storedHash)
    {
        var hasher = new PasswordHasher();

        Assert.False(hasher.Verify("password", storedHash, out var needsRehash));
        Assert.False(needsRehash);
    }

    private static string CreateVersionedHash(string password, int iterations)
    {
        var salt = Encoding.UTF8.GetBytes("legacy-salt-0001");
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            32);

        return $"PBKDF2$SHA256${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
}
