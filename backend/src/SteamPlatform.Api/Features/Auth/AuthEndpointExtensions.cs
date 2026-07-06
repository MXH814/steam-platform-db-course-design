namespace SteamPlatform.Api.Features.Auth;

public static class AuthEndpointExtensions
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Auth");

        auth.MapPost("/register", RegisterPlayerAsync);
        auth.MapPost("/register/player", RegisterPlayerAsync);

        auth.MapPost("/login", async (LoginRequest request, IAuthService authService, CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.IsBlank(request.Role, request.Account, request.Password))
            {
                return Results.BadRequest("Role, Account and Password are required.");
            }

            return Results.Ok(await authService.LoginAsync(request, cancellationToken));
        });

        auth.MapGet("/me", (HttpContext httpContext) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER", "DEVELOPER", "ADMIN", "AUDITOR") is { } denied)
            {
                return denied;
            }

            return Results.Ok(claims);
        });

        return app;
    }

    private static async Task<IResult> RegisterPlayerAsync(
        RegisterPlayerRequest request,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        if (EndpointGuards.IsBlank(request.Account, request.Password, request.Nickname))
        {
            return Results.BadRequest("Account, Password and Nickname are required.");
        }

        var response = await authService.RegisterPlayerAsync(request, cancellationToken);
        return Results.Created($"/api/players/{response.Claims.PrincipalId}", response);
    }
}
