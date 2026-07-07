namespace SteamPlatform.Shared;

public sealed class ResourceNotFoundException(string message) : Exception(message);
