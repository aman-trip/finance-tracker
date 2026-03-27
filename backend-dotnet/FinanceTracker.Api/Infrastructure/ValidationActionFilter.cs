using FinanceTracker.Api.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FinanceTracker.Api.Infrastructure;

public sealed class ValidationActionFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
        {
            return;
        }

        var errors = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in context.ModelState)
        {
            if (entry.Value is null || entry.Value.Errors.Count == 0)
            {
                continue;
            }

            var key = NormalizeKey(entry.Key);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (!errors.ContainsKey(key))
            {
                errors[key] = entry.Value.Errors[0].ErrorMessage;
            }
        }

        var payload = new ApiErrorResponse(
            DateTimeOffset.UtcNow,
            StatusCodes.Status400BadRequest,
            "Bad Request",
            "Validation failed",
            errors
        );

        context.Result = new BadRequestObjectResult(payload);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    private static string NormalizeKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        var normalized = key.Replace("$.", string.Empty, StringComparison.Ordinal);
        var lastDot = normalized.LastIndexOf('.');
        if (lastDot >= 0 && lastDot < normalized.Length - 1)
        {
            normalized = normalized[(lastDot + 1)..];
        }

        if (normalized.EndsWith(']'))
        {
            var lastBracket = normalized.LastIndexOf('[');
            if (lastBracket >= 0)
            {
                normalized = normalized[..lastBracket];
            }
        }

        if (string.IsNullOrEmpty(normalized))
        {
            return normalized;
        }

        return char.ToLowerInvariant(normalized[0]) + normalized[1..];
    }
}
