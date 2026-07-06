using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Dapper;
using Microsoft.IdentityModel.Tokens;
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
    private readonly JwtSecurityTokenHandler _tokenHandler = new() { MapInboundClaims = false };

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

        if (role == "DEVELOPER")
        {
            throw new UnauthorizedAccessException("Developer password login is not available until developer credentials are added to the schema.");
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

        if (login is null || !_passwordHasher.Verify(request.Password, login.PasswordHash, out _))
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

        var normalized = new AuthClaims(
            claims.Role.Trim().ToUpperInvariant(),
            claims.PrincipalId.Trim(),
            claims.Account.Trim(),
            claims.ExpiresAt);
        var jwtClaims = new[]
        {
            new Claim(AuthTokenValidation.RoleClaim, normalized.Role),
            new Claim(AuthTokenValidation.PrincipalIdClaim, normalized.PrincipalId),
            new Claim(AuthTokenValidation.AccountClaim, normalized.Account),
            new Claim(AuthTokenValidation.ExpiresAtClaim, normalized.ExpiresAt.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Sub, normalized.PrincipalId),
            new Claim(JwtRegisteredClaimNames.UniqueName, normalized.Account),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            claims: jwtClaims,
            notBefore: DateTime.UtcNow,
            expires: normalized.ExpiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256));

        return _tokenHandler.WriteToken(token);
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

        try
        {
            var principal = _tokenHandler.ValidateToken(
                normalized,
                AuthTokenValidation.CreateParameters(_key),
                out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.Ordinal))
            {
                return null;
            }

            return EndpointGuards.TryReadClaims(principal, out var claims) ? claims : null;
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (SecurityTokenException)
        {
            return null;
        }
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

    private sealed class LoginRow
    {
        public string Role { get; set; } = "";
        public string PrincipalId { get; set; } = "";
        public string Account { get; set; } = "";
        public string PasswordHash { get; set; } = "";
    }
}
