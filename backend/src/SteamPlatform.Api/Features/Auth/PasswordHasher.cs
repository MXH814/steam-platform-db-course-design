using System.Security.Cryptography;

namespace SteamPlatform.Api.Features.Auth;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string storedHash, out bool needsRehash);
}

public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required.", nameof(password));
        }

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"PBKDF2$SHA256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string storedHash, out bool needsRehash)
    {
        needsRehash = false;
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var parts = storedHash.Split('$');
        if (parts.Length != 5 ||
            !parts[0].Equals("PBKDF2", StringComparison.OrdinalIgnoreCase) ||
            !parts[1].Equals("SHA256", StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(parts[2], out var iterations))
        {
            return CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(password),
                System.Text.Encoding.UTF8.GetBytes(storedHash));
        }

        var salt = Convert.FromBase64String(parts[3]);
        var expected = Convert.FromBase64String(parts[4]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        needsRehash = iterations < Iterations;
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
