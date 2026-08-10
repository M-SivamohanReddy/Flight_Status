using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightStatus.Api.Infrastructure;

/// <summary>
/// Unified middleware: global error handling, request/response logging, endpoint existence, JWT structural validation.
/// Must be placed after UseRouting() so GetEndpoint() is populated.
/// </summary>
public sealed class RequestPipelineMiddleware(ILogger<RequestPipelineMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var correlationId = context.TraceIdentifier;
        var sw            = Stopwatch.StartNew();

        // Propagate correlation ID back to the caller for distributed tracing
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.TryAdd("X-Correlation-Id", correlationId);
            return Task.CompletedTask;
        });

        LogRequest(context, correlationId);

        // ── 1. Endpoint existence ───────────────────────────────────────────────
        var endpoint = context.GetEndpoint();
        if (endpoint is null)
        {
            sw.Stop();
            logger.LogWarning(
                "[RES] {CorrelationId} 404 in {Ms}ms — no route matched {Method} {Path}",
                correlationId, sw.ElapsedMilliseconds,
                context.Request.Method, context.Request.Path);

            await WriteErrorAsync(context, StatusCodes.Status404NotFound,
                "Not Found",
                $"No route matched '{context.Request.Method} {context.Request.Path}'.");
            return;
        }

        // ── 2. JWT structural validation for protected endpoints ────────────────
        // Only checks presence + format; full crypto validation stays in UseAuthentication.
        var requiresAuth = endpoint.Metadata.GetMetadata<IAuthorizeData>() is not null;
        if (requiresAuth)
        {
            var (valid, error) = ValidateJwtFormat(context);
            if (!valid)
            {
                sw.Stop();
                logger.LogWarning(
                    "[RES] {CorrelationId} 401 in {Ms}ms — {Error} | {Method} {Path}",
                    correlationId, sw.ElapsedMilliseconds, error,
                    context.Request.Method, context.Request.Path);

                await WriteErrorAsync(context, StatusCodes.Status401Unauthorized,
                    "Unauthorized", error!);
                return;
            }
        }

        // ── 3. Execute the rest of the pipeline (catch unhandled exceptions) ───
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            sw.Stop();
            var statusCode = exception switch
            {
                UnauthorizedAccessException => StatusCodes.Status403Forbidden,
                ArgumentException           => StatusCodes.Status400BadRequest,
                KeyNotFoundException        => StatusCodes.Status404NotFound,
                OperationCanceledException  => StatusCodes.Status499ClientClosedRequest,
                _                           => StatusCodes.Status500InternalServerError
            };

            logger.LogError(
                exception,
                "[ERR] {CorrelationId} {ExceptionType} on {Method} {Path} in {Ms}ms — {Message}",
                correlationId, exception.GetType().Name,
                context.Request.Method, context.Request.Path,
                sw.ElapsedMilliseconds, exception.Message);

            await WriteErrorAsync(context, statusCode, GetTitle(statusCode), exception.Message);
            return;
        }

        // ── 4. Response logging ─────────────────────────────────────────────────
        sw.Stop();
        LogResponse(context, correlationId, sw.ElapsedMilliseconds);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private void LogRequest(HttpContext context, string correlationId)
    {
        logger.LogInformation(
            "[REQ] {CorrelationId} {Method} {Path}{Query} | IP: {Ip} | User-Agent: {UA}",
            correlationId,
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString,
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            context.Request.Headers.UserAgent.ToString());
    }

    private void LogResponse(HttpContext context, string correlationId, long elapsedMs)
    {
        var status = context.Response.StatusCode;
        var path   = context.Request.Path;
        var method = context.Request.Method;

        if (status >= 500)
            logger.LogError(
                "[RES] {CorrelationId} {Method} {Path} → {Status} in {Ms}ms",
                correlationId, method, path, status, elapsedMs);
        else if (status >= 400)
            logger.LogWarning(
                "[RES] {CorrelationId} {Method} {Path} → {Status} in {Ms}ms",
                correlationId, method, path, status, elapsedMs);
        else
            logger.LogInformation(
                "[RES] {CorrelationId} {Method} {Path} → {Status} in {Ms}ms",
                correlationId, method, path, status, elapsedMs);
    }

    private static (bool Valid, string? Error) ValidateJwtFormat(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authHeader))
            return (false, "Authorization header is missing.");

        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return (false, "Authorization header must use the Bearer scheme.");

        var token = authHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(token))
            return (false, "Bearer token is empty.");

        // JWT structure: header.payload.signature (exactly 3 base64url segments)
        if (token.Split('.').Length != 3)
            return (false, "JWT token is malformed — expected three dot-separated segments.");

        return (true, null);
    }

    private static Task WriteErrorAsync(HttpContext context, int status, string title, string detail)
    {
        context.Response.StatusCode  = status;
        context.Response.ContentType = "application/problem+json";

        return context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status   = status,
            Title    = title,
            Detail   = detail,
            Instance = context.Request.Path
        });
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest          => "Bad Request",
        StatusCodes.Status403Forbidden           => "Forbidden",
        StatusCodes.Status404NotFound            => "Not Found",
        StatusCodes.Status499ClientClosedRequest => "Client Closed Request",
        _                                        => "Internal Server Error"
    };
}
