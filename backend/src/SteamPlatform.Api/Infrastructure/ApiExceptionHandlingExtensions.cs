using Microsoft.AspNetCore.Diagnostics;
using SteamPlatform.Shared;

namespace SteamPlatform.Api.Infrastructure;

public static class ApiExceptionHandlingExtensions
{
    public static void UseApiExceptionHandling(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                var (statusCode, response) = CreateApiResponse(exception);
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(response);
            });
        });
    }

    public static (int StatusCode, ApiResponse<object?> Response) CreateApiResponse(Exception? exception)
    {
        if (TryGetOracleErrorNumber(exception, out var oracleNumber))
        {
            if (oracleNumber == 1)
            {
                return (StatusCodes.Status409Conflict, ApiResponse<object?>.Failure(40903, "玩家已拥有该游戏或 CDKey 已被兑换。"));
            }

            return (StatusCodes.Status503ServiceUnavailable, ApiResponse<object?>.Failure(50301, $"数据库服务暂时不可用。错误代码: ORA-{oracleNumber}"));
        }

        return exception switch
        {
            ArgumentException argumentException => (StatusCodes.Status400BadRequest, ApiResponse<object?>.Failure(40001, argumentException.Message)),
            BusinessRuleException businessRuleException => (StatusCodes.Status409Conflict, ApiResponse<object?>.Failure(40900, businessRuleException.Message)),
            ResourceNotFoundException resourceNotFoundException => (StatusCodes.Status404NotFound, ApiResponse<object?>.Failure(40401, resourceNotFoundException.Message)),
            ForbiddenException forbiddenException => (StatusCodes.Status403Forbidden, ApiResponse<object?>.Failure(40301, forbiddenException.Message)),
            InvalidOperationException => (StatusCodes.Status500InternalServerError, ApiResponse<object?>.Failure(50001, "服务器配置错误。")),
            UnauthorizedAccessException unauthorizedAccessException => (StatusCodes.Status401Unauthorized, ApiResponse<object?>.Failure(40101, unauthorizedAccessException.Message)),
            _ => (StatusCodes.Status500InternalServerError, ApiResponse<object?>.Failure(50000, "服务器无法完成请求。"))
        };
    }

    private static bool TryGetOracleErrorNumber(Exception? exception, out int number)
    {
        number = 0;
        if (exception?.GetType().FullName != "Oracle.ManagedDataAccess.Client.OracleException")
        {
            return false;
        }

        var value = exception.GetType().GetProperty("Number")?.GetValue(exception);
        if (value is not int oracleNumber)
        {
            return false;
        }

        number = oracleNumber;
        return true;
    }
}
