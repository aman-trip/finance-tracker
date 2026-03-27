using System.ComponentModel.DataAnnotations;
using FinanceTracker.Api.Entities;
using FinanceTracker.Api.Infrastructure;

namespace FinanceTracker.Api.Contracts;

public sealed record AccountRequest(
    [param: NotBlank(ErrorMessage = "must not be blank")]
    string? name,
    [param: Required(ErrorMessage = "must not be null")]
    AccountType? type,
    [param: Required(ErrorMessage = "must not be null")]
    [param: MinValue("0.0", ErrorMessage = "must be greater than or equal to 0.0")]
    decimal? openingBalance,
    string? institutionName
);

public sealed record AccountResponse(
    Guid id,
    string name,
    AccountType type,
    decimal openingBalance,
    decimal currentBalance,
    string? institutionName,
    DateTimeOffset createdAt
);

public sealed record TransferRequest(
    [param: Required(ErrorMessage = "must not be null")]
    Guid? sourceAccountId,
    [param: Required(ErrorMessage = "must not be null")]
    Guid? targetAccountId,
    [param: Required(ErrorMessage = "must not be null")]
    [param: MinValue("0.01", ErrorMessage = "must be greater than or equal to 0.01")]
    decimal? amount,
    [param: Required(ErrorMessage = "must not be null")]
    DateOnly? transactionDate,
    string? note
);
