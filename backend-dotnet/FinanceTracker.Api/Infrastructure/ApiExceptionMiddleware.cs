using FinanceTracker.Api.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;

namespace FinanceTracker.Api.Infrastructure;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled API exception");
            await WriteResponseAsync(context, exception);
        }
    }

    private static async Task WriteResponseAsync(HttpContext context, Exception exception)
    {
        var (status, message) = exception switch
        {
            NotFoundException notFound => (StatusCodes.Status404NotFound, notFound.Message),
            BadRequestException badRequest => (StatusCodes.Status400BadRequest, badRequest.Message),
            UnauthorizedException unauthorized => (StatusCodes.Status401Unauthorized, unauthorized.Message),
            ForbiddenException forbidden => (StatusCodes.Status403Forbidden, forbidden.Message),
            ArgumentException argumentException => (StatusCodes.Status400BadRequest, argumentException.Message),
            DbUpdateException => (StatusCodes.Status400BadRequest, "Request violates data constraints"),
            _ => (StatusCodes.Status500InternalServerError, exception.Message)
        };

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";

        var payload = new ApiErrorResponse(
            DateTimeOffset.UtcNow,
            status,
            ReasonPhrases.GetReasonPhrase(status),
            message,
            new Dictionary<string, string>()
        );

        await context.Response.WriteAsJsonAsync(payload);
    }
}
