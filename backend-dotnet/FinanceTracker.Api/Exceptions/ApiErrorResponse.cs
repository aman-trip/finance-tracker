namespace FinanceTracker.Api.Exceptions;

public sealed record ApiErrorResponse(
    DateTimeOffset timestamp,
    int status,
    string error,
    string message,
    IReadOnlyDictionary<string, string> validationErrors
);
