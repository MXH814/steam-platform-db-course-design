using System.Security.Cryptography;

namespace SteamPlatform.Shared;

public static class IdGenerator
{
    public static string NewId(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException("Prefix is required.", nameof(prefix));
        }

        return prefix.Trim().ToUpperInvariant() + Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
    }
}
