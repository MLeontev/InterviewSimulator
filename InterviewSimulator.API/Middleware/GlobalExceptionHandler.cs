using FluentValidation;
using Framework.Domain;
using Microsoft.AspNetCore.Diagnostics;

namespace InterviewSimulator.API.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Необработанное исключение: {Message}", exception.Message);
        
        var (statusCode, payload) = Map(exception);
        
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json; charset=utf-8";

        await httpContext.Response.WriteAsJsonAsync(payload, cancellationToken);
        return true;
    }

    private static (int StatusCode, object Payload) Map(Exception ex)
    {
        if (ex is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.ErrorMessage).Distinct().ToArray());

            return (StatusCodes.Status400BadRequest, Error.Validation(errors));
        }
        
        var error = new
        {
            code = "INTERNAL_SERVER_ERROR",
            description = "Произошла непредвиденная ошибка"
        };
        
        return (StatusCodes.Status500InternalServerError, error);
    }
}