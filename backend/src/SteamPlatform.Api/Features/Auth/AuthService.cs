using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using SteamPlatform.Api.Data;
using SteamPlatform.Api.Infrastructure;

namespace SteamPlatform.Api.Features.Auth;

public sealed class AuthService(
    IDbConnectionFactory connectionFactory,
    IAuthSigningKeyProvider signingKeyProvider,
    IPasswordHasher passwordHasher) : IAuthService
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    private readonly byte[] _key = CopySigningKey(signingKeyProvider ?? throw new ArgumentNullException(nameof(signingKeyProvider)));
    private readonly IPasswordHasher _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));

    public async Task<AuthResponse> RegisterPlayerAsync(RegisterPlayerRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (EndpointGuards.IsBlank(request.Account, request.Password, request.Nickname))
        {
            throw new ArgumentException("Account, Password and Nickname are required.");
        }

        var player = new
        {
            UserId = IdGenerator.NewId("P"),
            Account = request.Account.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Nickname = request.Nickname.Trim()
        };

        await using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            """
            insert into player
              (user_id, account, password_hash, nickname, credit_score, status, version, create_time, update_time)
            values
              (:UserId, :Account, :PasswordHash, :Nickname, 100, 'NORMAL', 0, SYSTIMESTAMP, SYSTIMESTAMP)
            """,
            player,
            cancellationToken: cancellationToken));

        var claims = new AuthClaims("PLAYER", player.UserId, player.Account, DateTimeOffset.UtcNow.AddHours(8));
        return new AuthResponse(CreateToken(claims), claims);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (EndpointGuards.IsBlank(request.Role, request.Account, request.Password))
        {
            throw new ArgumentException("Role, Account and Password are required.");
        }

        var role = request.Role.Trim().ToUpperInvariant();
        if (role is not ("PLAYER" or "DEVELOPER" or "ADMIN"))
        {
            throw new ArgumentException("Role must be PLAYER, DEVELOPER or ADMIN.");
        }

        await using var connection = _connectionFactory.CreateConnection();
        var account = request.Account.Trim();
        LoginRow? login = role switch
        {
            "PLAYER" => await connection.QueryFirstOrDefaultAsync<LoginRow>(new CommandDefinition(
                """
                select 'PLAYER' as role, user_id as principal_id, account, password_hash
                  from player
                 where account = :Account and status = 'NORMAL'
                """,
                new { Account = account },
                cancellationToken: cancellationToken)),

            "DEVELOPER" => await connection.QueryFirstOrDefaultAsync<LoginRow>(new CommandDefinition(
                """
                select 'DEVELOPER' as role, dev_id as principal_id, contact_email as account, tax_id as password_hash
                  from developer
                 where contact_email = :Account and status = 'APPROVED'
                """,
                new { Account = account },
                cancellationToken: cancellationToken)),

            "ADMIN" => await connection.QueryFirstOrDefaultAsync<LoginRow>(new CommandDefinition(
                """
                select upper(role) as role, admin_id as principal_id, account, password_hash
                  from admin_user
                 where account = :Account
                """,
                new { Account = account },
                cancellationToken: cancellationToken)),

            _ => throw new ArgumentException("Role must be PLAYER, DEVELOPER or ADMIN.")
        };

        if (login is null || !PasswordMatches(role, request.Password, login.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid role, account or password.");
        }

        var claims = new AuthClaims(login.Role, login.PrincipalId, login.Account, DateTimeOffset.UtcNow.AddHours(8));
        return new AuthResponse(CreateToken(claims), claims);
    }

    public string CreateToken(AuthClaims claims)
    {
        ArgumentNullException.ThrowIfNull(claims);
        if (!HasValidClaims(claims))
        {
            throw new ArgumentException("Claims must include a known role, principal id, account and future expiration.", nameof(claims));
        }

        var payload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(claims));
        return payload + "." + Sign(payload);
    }

    public AuthClaims? ValidateToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var normalized = token.Trim();
        if (normalized.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized["Bearer ".Length..].Trim();
        }

        var parts = normalized.Split('.', 2);
        if (parts.Length != 2 ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(Sign(parts[0])), Encoding.UTF8.GetBytes(parts[1])))
        {
            return null;
        }

        try
        {
            var claims = JsonSerializer.Deserialize<AuthClaims>(Base64UrlDecode(parts[0]));
            return claims is not null && HasValidClaims(claims) ? claims : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private bool PasswordMatches(string requestedRole, string password, string storedHash)
    {
        if (requestedRole.Equals("DEVELOPER", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(password, storedHash, StringComparison.Ordinal);
        }

        return _passwordHasher.Verify(password, storedHash, out _);
    }

    private string Sign(string payload)
    {
        using var hmac = new HMACSHA256(_key);
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    private static bool HasValidClaims(AuthClaims claims) =>
        IsKnownRole(claims.Role) &&
        !string.IsNullOrWhiteSpace(claims.PrincipalId) &&
        !string.IsNullOrWhiteSpace(claims.Account) &&
        claims.ExpiresAt > DateTimeOffset.UtcNow;

    private static bool IsKnownRole(string? role) =>
        !string.IsNullOrWhiteSpace(role) && (
            role.Equals("PLAYER", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("DEVELOPER", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("SUPER_ADMIN", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("AUDITOR", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("RISK_ADMIN", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("CUSTOMER_SERVICE", StringComparison.OrdinalIgnoreCase));

    private static byte[] CopySigningKey(IAuthSigningKeyProvider signingKeyProvider)
    {
        var key = signingKeyProvider.Key;
        return key is { Length: >= 32 }
            ? key.ToArray()
            : throw new InvalidOperationException("Auth signing key must be at least 32 bytes.");
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }

    private sealed class LoginRow
    {
        public string Role { get; set; } = "";
        public string PrincipalId { get; set; } = "";
        public string Account { get; set; } = "";
        public string PasswordHash { get; set; } = "";
    }
}
