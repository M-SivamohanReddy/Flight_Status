using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FlightStatus.Api.Infrastructure;

/// <summary>
/// Catches every unhandled exception from any endpoint — the single reusable try-catch for the whole API.
/// Controllers do not need their own try-catch; they just let exceptions propagate here.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception   exception,
        CancellationToken ct)
    {
        logger.LogError(
            exception,
            "Unhandled {ExceptionType} on {Method} {Path} — {Message}",
            exception.GetType().Name,
            context.Request.Method,
            context.Request.Path,
            exception.Message);

        var statusCode = exception switch
        {
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            ArgumentException           => StatusCodes.Status400BadRequest,
            KeyNotFoundException        => StatusCodes.Status404NotFound,
            _                           => StatusCodes.Status500InternalServerError
        };

        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = statusCode,
                Title  = "An error occurred.",
                Detail = exception.Message
            }, ct);

        return true; // exception handled — do not re-throw
    }
}
