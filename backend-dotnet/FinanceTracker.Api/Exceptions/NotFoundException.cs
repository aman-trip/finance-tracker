namespace FinanceTracker.Api.Exceptions;

public sealed class NotFoundException(string message) : Exception(message);
