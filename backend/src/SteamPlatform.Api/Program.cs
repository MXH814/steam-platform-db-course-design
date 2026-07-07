using Microsoft.AspNetCore.Authentication.JwtBearer;
using SteamPlatform.Api.Features.Auth;
using SteamPlatform.Api.Features.CoreTransactions;
using SteamPlatform.Api.Features.Inventory;
using SteamPlatform.Api.Features.Notices;
using SteamPlatform.Api.Infrastructure;
using SteamPlatform.Application.Auth;
using SteamPlatform.Application.Diagnostics;
using SteamPlatform.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IAuthSigningKeyProvider>((options, signingKeyProvider) =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = AuthTokenValidation.CreateParameters(signingKeyProvider.Key);
    });
builder.Services.AddAuthorization();
builder.Services.AddSteamPlatformInfrastructure();

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

app.MapGet("/health/database", async (IDatabaseHealthProbe probe, CancellationToken cancellationToken) =>
    Results.Ok(await probe.CheckAsync(cancellationToken)));

app.MapAuthEndpoints();
app.MapInventoryEndpoints();
app.MapNoticeEndpoints();
app.MapCoreTransactionEndpoints();

app.Run();

public partial class Program;
