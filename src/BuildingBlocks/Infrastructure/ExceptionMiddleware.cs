using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EnterpriseDocumentIntelligence.BuildingBlocks.Infrastructure;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try { await next(context); }
        catch (UnauthorizedAccessException ex) { logger.LogWarning(ex, "Unauthorized request"); await Write(context, 401, "Unauthorized", ex.Message); }
        catch (KeyNotFoundException ex) { logger.LogInformation(ex, "Resource not found"); await Write(context, 404, "Not Found", ex.Message); }
        catch (ArgumentException ex) { logger.LogInformation(ex, "Validation failure"); await Write(context, 400, "Validation Failed", ex.Message); }
        catch (InvalidOperationException ex) { logger.LogInformation(ex, "Invalid operation"); await Write(context, 409, "Conflict", ex.Message); }
        catch (Exception ex) { logger.LogError(ex, "Unhandled exception TraceId={TraceId}", context.TraceIdentifier); await Write(context, 500, "Unexpected server error", "An unexpected error occurred."); }
    }

    private static async Task Write(HttpContext context, int status, string title, string detail)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            type = $"https://httpstatuses.com/{status}",
            title,
            status,
            detail,
            traceId = context.TraceIdentifier
        }));
    }
}