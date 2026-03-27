namespace FinanceTracker.Api.Exceptions;

public sealed class BadRequestException(string message) : Exception(message);
