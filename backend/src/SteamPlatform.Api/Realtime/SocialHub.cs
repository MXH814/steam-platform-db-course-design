using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SteamPlatform.Application.Auth;

namespace SteamPlatform.Api.Realtime;

[Authorize]
public sealed class SocialHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (!AuthClaimReader.TryReadClaims(Context.User!, out var claims) ||
            !string.Equals(claims!.Role, "PLAYER", StringComparison.OrdinalIgnoreCase))
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(claims.PrincipalId));
        await base.OnConnectedAsync();
    }

    public static string UserGroup(string userId) => $"user:{userId}";
}
