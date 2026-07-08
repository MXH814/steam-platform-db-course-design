namespace SteamPlatform.Shared;

public sealed class ForbiddenException(string message) : Exception(message);