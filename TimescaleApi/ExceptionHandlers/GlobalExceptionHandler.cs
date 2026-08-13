using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TimescaleApi.Exceptions;

namespace TimescaleApi.ExceptionHandlers;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is CsvValidationException)
        {
            httpContext.Response.StatusCode =
                StatusCodes.Status400BadRequest;

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ошибка валидации CSV",
                Detail = exception.Message
            };

            await httpContext.Response.WriteAsJsonAsync(
                problemDetails,
                cancellationToken);

            return true;
        }

        _logger.LogError(
            exception,
            "Произошла необработанная ошибка.");

        httpContext.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        var internalError = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Внутренняя ошибка сервера",
            Detail = "Во время обработки запроса произошла ошибка."
        };

        await httpContext.Response.WriteAsJsonAsync(
            internalError,
            cancellationToken);

        return true;
    }
}