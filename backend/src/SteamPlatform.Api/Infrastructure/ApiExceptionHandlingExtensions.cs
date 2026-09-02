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
        if (TryGetOracleErrorNumber(exception, out var oracleErrorNumber))
        {
            return CreateOracleApiResponse(oracleErrorNumber);
        }

        return exception switch
        {
            ArgumentException argumentException => (StatusCodes.Status400BadRequest, ApiResponse<object?>.Failure(40001, argumentException.Message)),
            BusinessRuleException businessRuleException => (StatusCodes.Status409Conflict, ApiResponse<object?>.Failure(40900, $"{businessRuleException.Code}: {businessRuleException.Message}")),
            ResourceNotFoundException resourceNotFoundException => (StatusCodes.Status404NotFound, ApiResponse<object?>.Failure(40401, resourceNotFoundException.Message)),
            ForbiddenException forbiddenException => (StatusCodes.Status403Forbidden, ApiResponse<object?>.Failure(40301, forbiddenException.Message)),
            InvalidOperationException => (StatusCodes.Status500InternalServerError, ApiResponse<object?>.Failure(50001, "服务器配置错误。")),
            UnauthorizedAccessException unauthorizedAccessException => (StatusCodes.Status401Unauthorized, ApiResponse<object?>.Failure(40101, unauthorizedAccessException.Message)),
            _ => (StatusCodes.Status500InternalServerError, ApiResponse<object?>.Failure(50000, "服务器无法完成请求。"))
        };
    }

    public static (int StatusCode, ApiResponse<object?> Response) CreateOracleApiResponse(int errorNumber) =>
        errorNumber switch
        {
            1 => (StatusCodes.Status409Conflict, ApiResponse<object?>.Failure(40901, "数据已存在或与现有记录冲突。")),
            54 or 60 => (StatusCodes.Status409Conflict, ApiResponse<object?>.Failure(40903, "数据正在被其他操作修改，请稍后重试。")),
            1400 or 12899 => (StatusCodes.Status400BadRequest, ApiResponse<object?>.Failure(40002, "提交的数据不符合字段长度或必填约束。")),
            2290 or 2291 or 2292 => (StatusCodes.Status409Conflict, ApiResponse<object?>.Failure(40902, "该操作与现有数据关系或业务约束冲突。")),
            1017 or 3113 or 3114 or 3135 or 12154 or 12514 or 12541 or 12545 =>
                (StatusCodes.Status503ServiceUnavailable, ApiResponse<object?>.Failure(50301, "数据库服务暂时不可用。")),
            _ => (StatusCodes.Status500InternalServerError, ApiResponse<object?>.Failure(50002, "数据库无法完成当前请求。"))
        };

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
