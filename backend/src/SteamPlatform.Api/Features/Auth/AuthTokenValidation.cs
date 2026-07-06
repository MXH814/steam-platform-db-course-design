using Microsoft.IdentityModel.Tokens;

namespace SteamPlatform.Api.Features.Auth;

public static class AuthTokenValidation
{
    public const string RoleClaim = "role";
    public const string PrincipalIdClaim = "principal_id";
    public const string AccountClaim = "account";
    public const string ExpiresAtClaim = "expires_at";

    public static TokenValidationParameters CreateParameters(byte[] signingKey) =>
        new()
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(CopySigningKey(signingKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = RoleClaim,
            NameClaimType = AccountClaim
        };

    private static byte[] CopySigningKey(byte[] signingKey) =>
        signingKey is { Length: >= 32 }
            ? signingKey.ToArray()
            : throw new InvalidOperationException("Auth signing key must be at least 32 bytes.");
}
