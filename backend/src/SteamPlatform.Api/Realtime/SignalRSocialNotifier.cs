using Microsoft.AspNetCore.SignalR;
using SteamPlatform.Application.Social;

namespace SteamPlatform.Api.Realtime;

public sealed class SignalRSocialNotifier(IHubContext<SocialHub> hubContext) : ISocialRealtimeNotifier
{
    public Task NotifyUserAsync(string userId, string eventName, object payload, CancellationToken cancellationToken) =>
        hubContext.Clients.Group(SocialHub.UserGroup(userId)).SendAsync(eventName, payload, cancellationToken);
}
