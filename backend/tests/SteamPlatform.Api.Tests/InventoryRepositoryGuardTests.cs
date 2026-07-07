namespace SteamPlatform.Api.Tests;

public sealed class InventoryRepositoryGuardTests
{
    [Fact]
    public void DropAsync_checks_player_library_ownership_before_selecting_item_template()
    {
        var source = ReadRepositorySource();
        var dropAsyncIndex = source.IndexOf("public async Task<InventoryItemResponse> DropAsync", StringComparison.Ordinal);
        Assert.True(dropAsyncIndex >= 0, "DropAsync source block should exist.");

        var dropAsyncSource = source[dropAsyncIndex..];

        var playerLibraryIndex = dropAsyncSource.IndexOf("from player_library", StringComparison.OrdinalIgnoreCase);
        var itemTemplateIndex = dropAsyncSource.IndexOf("from item_template", StringComparison.OrdinalIgnoreCase);
        var gameNotOwnedIndex = dropAsyncSource.IndexOf("GAME_NOT_OWNED", StringComparison.Ordinal);

        Assert.True(playerLibraryIndex >= 0, "DropAsync should check PLAYER_LIBRARY before dropping items.");
        Assert.True(gameNotOwnedIndex >= 0, "DropAsync should return the GAME_NOT_OWNED business error.");
        Assert.True(
            playerLibraryIndex < itemTemplateIndex,
            "DropAsync should verify ownership before randomly selecting an ITEM_TEMPLATE.");
    }

    private static string ReadRepositorySource()
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(
            root,
            "backend",
            "src",
            "SteamPlatform.Infrastructure",
            "Inventory",
            "InventoryRepository.cs"));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "README.md")) &&
                Directory.Exists(Path.Combine(directory.FullName, "backend")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be found.");
    }
}
