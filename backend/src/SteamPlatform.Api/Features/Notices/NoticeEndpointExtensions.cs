using SteamPlatform.Api.Features.Auth;
using SteamPlatform.Application.Common;
using SteamPlatform.Application.Notices;

namespace SteamPlatform.Api.Features.Notices;

public static class NoticeEndpointExtensions
{
    public static IEndpointRouteBuilder MapNoticeEndpoints(this IEndpointRouteBuilder app)
    {
        var notices = app.MapGroup("/api/notices").WithTags("Notices");

        notices.MapGet("", async (int? limit, INoticeRepository repository, CancellationToken cancellationToken) =>
            Results.Ok(await repository.ListPublishedAsync(limit ?? 50, cancellationToken)));

        notices.MapPost("", async (
            CreateNoticeRequest request,
            INoticeRepository repository,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "ADMIN", "DEVELOPER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(request.Title, request.Content))
            {
                return Results.BadRequest("Title and Content are required.");
            }

            var normalized = request with
            {
                PublisherType = EndpointGuards.IsAdminRole(claims!.Role) ? "ADMIN" : "DEVELOPER",
                PublisherId = claims.PrincipalId
            };

            var notice = await repository.CreateAsync(normalized, cancellationToken);
            return Results.Created($"/api/notices/{notice.NoticeId}", notice);
        });

        var admin = app.MapGroup("/api/admin/notices").WithTags("Admin Notices");

        admin.MapPost("", async (
            CreateNoticeRequest request,
            INoticeRepository repository,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "ADMIN") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(request.Title, request.Content))
            {
                return Results.BadRequest("Title and Content are required.");
            }

            var normalized = request with
            {
                PublisherType = "ADMIN",
                PublisherId = claims!.PrincipalId
            };

            var notice = await repository.CreateAsync(normalized, cancellationToken);
            return Results.Created($"/api/notices/{notice.NoticeId}", notice);
        });

        admin.MapPut("{noticeId}", async (
            string noticeId,
            UpdateNoticeRequest request,
            INoticeRepository repository,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out _, "ADMIN") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(noticeId, request.Title, request.Content, request.Status))
            {
                return Results.BadRequest("NoticeId, Title, Content and Status are required.");
            }

            return Results.Ok(await repository.UpdateAsync(noticeId, request, cancellationToken));
        });

        return app;
    }
}
