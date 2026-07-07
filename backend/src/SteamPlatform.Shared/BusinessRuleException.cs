namespace SteamPlatform.Shared;

public sealed class BusinessRuleException(string code, string message) : Exception(message)
{
    public string Code { get; } = string.IsNullOrWhiteSpace(code)
        ? throw new ArgumentException("Code is required.", nameof(code))
        : code.Trim().ToUpperInvariant();
}
