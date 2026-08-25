using Microsoft.AspNetCore.Authentication.JwtBearer;
using SteamPlatform.Api.Features.Auth;
using SteamPlatform.Api.Features.Community;
using SteamPlatform.Api.Features.CoreTransactions;
using SteamPlatform.Api.Features.Engagement;
using SteamPlatform.Api.Features.Games;
using SteamPlatform.Api.Features.Inventory;
using SteamPlatform.Api.Features.Market;
using SteamPlatform.Api.Features.Notices;
using SteamPlatform.Api.Features.Social;
using SteamPlatform.Api.Infrastructure;
using SteamPlatform.Api.Realtime;
using SteamPlatform.Application.Auth;
using SteamPlatform.Application.Diagnostics;
using SteamPlatform.Application.Social;
using SteamPlatform.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new UtcDateTimeJsonConverter()));
builder.Services.AddSignalR();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IAuthSigningKeyProvider>((options, signingKeyProvider) =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = AuthTokenValidation.CreateParameters(signingKeyProvider.Key);
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrWhiteSpace(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs/social"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddSteamPlatformInfrastructure();
builder.Services.AddSingleton<ISocialRealtimeNotifier, SignalRSocialNotifier>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseApiExceptionHandling();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    service = "SteamPlatform.Api",
    status = "OK",
    time = DateTimeOffset.UtcNow
}));

app.MapGet("/api/health", () => Results.Ok(new
{
    service = "SteamPlatform.Api",
    status = "OK",
    time = DateTimeOffset.UtcNow
}));

app.MapGet("/health/database", async (IDatabaseHealthProbe probe, CancellationToken cancellationToken) =>
    Results.Ok(await probe.CheckAsync(cancellationToken)));

app.MapAuthEndpoints();
app.MapInventoryEndpoints();
app.MapGameEndpoints();
app.MapNoticeEndpoints();
app.MapCoreTransactionEndpoints();
app.MapCommunityEndpoints();
app.MapMarketEndpoints();
app.MapSocialEndpoints();
app.MapEngagementEndpoints();
app.MapHub<SocialHub>("/hubs/social").RequireAuthorization();

app.Run();

public partial class Program;
