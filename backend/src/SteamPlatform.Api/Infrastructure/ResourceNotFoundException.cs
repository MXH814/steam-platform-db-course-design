namespace SteamPlatform.Api.Infrastructure;

public sealed class ResourceNotFoundException(string message) : Exception(message);
