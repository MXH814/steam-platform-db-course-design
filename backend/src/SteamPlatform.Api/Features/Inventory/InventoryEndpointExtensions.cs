using SteamPlatform.Api.Features.Auth;
using SteamPlatform.Application.Common;
using SteamPlatform.Application.Inventory;

namespace SteamPlatform.Api.Features.Inventory;

public static class InventoryEndpointExtensions
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var templates = app.MapGroup("/api/item-templates").WithTags("Item Templates");

        templates.MapGet("", async (
            string? gameId,
            IInventoryRepository repository,
            CancellationToken cancellationToken) =>
            Results.Ok(await repository.ListTemplatesAsync(gameId, cancellationToken)));

        var inventory = app.MapGroup("/api/inventory").WithTags("Inventory");

        inventory.MapGet("", async (
            string? gameId,
            IInventoryRepository repository,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            return Results.Ok(await repository.ListAsync(claims!.PrincipalId, gameId, cancellationToken));
        });

        inventory.MapPost("drop", async (
            DropInventoryItemRequest request,
            IInventoryRepository repository,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(request.GameId))
            {
                return Results.BadRequest("GameId is required.");
            }

            var droppedItem = await repository.DropAsync(claims!.PrincipalId, request, cancellationToken);
            return Results.Created($"/api/inventory/{droppedItem.ItemId}", droppedItem);
        });

        var items = app.MapGroup("/api/items").WithTags("Items");

        items.MapGet("{itemId}/transfers", async (
            string itemId,
            IInventoryRepository repository,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (EndpointGuards.DenyUnless(httpContext, out var claims, "PLAYER") is { } denied)
            {
                return denied;
            }

            if (InputGuards.IsBlank(itemId))
            {
                return Results.BadRequest("ItemId is required.");
            }

            return Results.Ok(await repository.ListTransfersAsync(claims!.PrincipalId, itemId, cancellationToken));
        });

        return app;
    }
}
