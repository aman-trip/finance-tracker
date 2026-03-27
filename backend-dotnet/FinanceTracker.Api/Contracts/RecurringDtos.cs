using System.ComponentModel.DataAnnotations;
using FinanceTracker.Api.Entities;
using FinanceTracker.Api.Infrastructure;

namespace FinanceTracker.Api.Contracts;

public sealed record RecurringTransactionRequest(
    [param: NotBlank(ErrorMessage = "must not be blank")]
    string? title,
    [param: Required(ErrorMessage = "must not be null")]
    TransactionType? type,
    [param: Required(ErrorMessage = "must not be null")]
    [param: MinValue("0.01", ErrorMessage = "must be greater than or equal to 0.01")]
    decimal? amount,
    Guid? categoryId,
    [param: Required(ErrorMessage = "must not be null")]
    Guid? accountId,
    [param: Required(ErrorMessage = "must not be null")]
    RecurringFrequency? frequency,
    [param: Required(ErrorMessage = "must not be null")]
    DateOnly? startDate,
    DateOnly? endDate,
    [param: Required(ErrorMessage = "must not be null")]
    DateOnly? nextRunDate,
    bool autoCreateTransaction
);

public sealed record RecurringTransactionResponse(
    Guid id,
    string title,
    TransactionType type,
    decimal amount,
    Guid? categoryId,
    string? categoryName,
    Guid accountId,
    string accountName,
    RecurringFrequency frequency,
    DateOnly startDate,
    DateOnly? endDate,
    DateOnly nextRunDate,
    bool autoCreateTransaction
);
