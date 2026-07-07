namespace SteamPlatform.Application.Auth;

public sealed record RegisterPlayerRequest(string Account, string Password, string Nickname);

public sealed record LoginRequest(string Role, string Account, string Password);

public sealed record AuthClaims(string Role, string PrincipalId, string Account, DateTimeOffset ExpiresAt);

public sealed record AuthResponse(string Token, AuthClaims Claims);

public interface IAuthService
{
    Task<AuthResponse> RegisterPlayerAsync(RegisterPlayerRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    string CreateToken(AuthClaims claims);
    AuthClaims? ValidateToken(string? token);
}

public interface IAuthSigningKeyProvider
{
    byte[] Key { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string storedHash, out bool needsRehash);
}
