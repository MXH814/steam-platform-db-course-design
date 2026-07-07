using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
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
                var problem = CreateProblem(exception);
                context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(problem);
            });
        });
    }

    public static ProblemDetails CreateProblem(Exception? exception)
    {
        if (TryGetOracleErrorNumber(exception, out var oracleNumber))
        {
            return NewProblem(StatusCodes.Status503ServiceUnavailable, "Oracle database error", $"Oracle error ORA-{oracleNumber}.");
        }

        return exception switch
        {
            ArgumentException argumentException => NewProblem(StatusCodes.Status400BadRequest, "Invalid request", argumentException.Message),
            ResourceNotFoundException resourceNotFoundException => NewProblem(StatusCodes.Status404NotFound, "Not found", resourceNotFoundException.Message),
            InvalidOperationException => NewProblem(StatusCodes.Status500InternalServerError, "Server configuration error", "The server is not configured correctly."),
            UnauthorizedAccessException unauthorizedAccessException => NewProblem(StatusCodes.Status401Unauthorized, "Unauthorized", unauthorizedAccessException.Message),
            _ => NewProblem(StatusCodes.Status500InternalServerError, "Unexpected server error", "The server could not complete the request.")
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

    private static ProblemDetails NewProblem(int status, string title, string detail) =>
        new()
        {
            Status = status,
            Title = title,
            Detail = detail
        };
}
