using System.Security.Claims;
using SteamPlatform.Application.Auth;
using Microsoft.AspNetCore.Http;
using SteamPlatform.Api.Features.Auth;

namespace SteamPlatform.Api.Tests;

public sealed class EndpointGuardTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void IsBlank_detects_missing_values(string? value)
    {
        Assert.True(EndpointGuards.IsBlank("ok", value));
    }

    [Theory]
    [InlineData("ADMIN")]
    [InlineData("SUPER_ADMIN")]
    [InlineData("RISK_ADMIN")]
    [InlineData("CUSTOMER_SERVICE")]
    public void DenyUnless_treats_admin_aliases_as_admin(string role)
    {
        var context = new DefaultHttpContext { User = Principal(role) };

        var result = EndpointGuards.DenyUnless(context, out var claims, "ADMIN");

        Assert.Null(result);
        Assert.NotNull(claims);
        Assert.Equal(role, claims.Role);
    }

    [Fact]
    public void DenyUnless_forbids_non_matching_role()
    {
        var context = new DefaultHttpContext { User = Principal("PLAYER") };

        var result = EndpointGuards.DenyUnless(context, out _, "ADMIN");

        Assert.NotNull(result);
    }

    [Fact]
    public void DenyUnless_rejects_unauthenticated_principal()
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };

        var result = EndpointGuards.DenyUnless(context, out var claims, "PLAYER");

        Assert.NotNull(result);
        Assert.Null(claims);
    }

    private static ClaimsPrincipal Principal(string role)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(AuthTokenValidation.RoleClaim, role),
                new Claim(AuthTokenValidation.PrincipalIdClaim, "P001"),
                new Claim(AuthTokenValidation.AccountClaim, "account"),
                new Claim(AuthTokenValidation.ExpiresAtClaim, DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds().ToString())
            ],
            "TestAuth");

        return new ClaimsPrincipal(identity);
    }
}
