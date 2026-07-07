namespace SteamPlatform.Application.Common;

public static class InputGuards
{
    public static bool IsBlank(params string?[] values) =>
        values.Any(static value => string.IsNullOrWhiteSpace(value));
}
