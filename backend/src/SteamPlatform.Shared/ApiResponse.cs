namespace SteamPlatform.Shared;

public sealed record ApiResponse<T>(object Code, string Message, T? Data)
{
    public static ApiResponse<T> Success(T data) => new(0, "success", data);

    public static ApiResponse<T> Failure(int code, string message) => new(code, message, default);

    public static ApiResponse<T> Failure(string code, string message) => new(code, message, default);
}
