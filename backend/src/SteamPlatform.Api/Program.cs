using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Oracle.ManagedDataAccess.Client;
using SteamPlatform.Api.Data;
using SteamPlatform.Api.Features.Auth;
using SteamPlatform.Api.Features.Notices;
using SteamPlatform.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IDbConnectionFactory, OracleDbConnectionFactory>();
builder.Services.AddSingleton<IAuthSigningKeyProvider, AuthSigningKeyProvider>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IAuthSigningKeyProvider>((options, signingKeyProvider) =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = AuthTokenValidation.CreateParameters(signingKeyProvider.Key);
    });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<INoticeRepository, NoticeRepository>();

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

app.MapGet("/health/database", async (IConfiguration configuration) =>
{
    var connectionString = configuration.GetConnectionString("Oracle");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Ok(new
        {
            status = "SKIPPED",
            reason = "Configure ConnectionStrings:Oracle before checking the database."
        });
    }

    await using var connection = new OracleConnection(connectionString);
    var value = await connection.QuerySingleAsync<string>("select 'OK' from dual");
    return Results.Ok(new { database = "Oracle", status = value });
});

app.MapAuthEndpoints();
app.MapNoticeEndpoints();

app.Run();

public partial class Program;
