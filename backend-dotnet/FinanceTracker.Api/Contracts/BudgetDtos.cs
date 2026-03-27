using System.ComponentModel.DataAnnotations;
using FinanceTracker.Api.Infrastructure;

namespace FinanceTracker.Api.Contracts;

public sealed record BudgetRequest(
    [param: Required(ErrorMessage = "must not be null")]
    Guid? categoryId,
    [param: Required(ErrorMessage = "must not be null")]
    [param: MinValue("1", ErrorMessage = "must be greater than or equal to 1")]
    [param: MaxValue("12", ErrorMessage = "must be less than or equal to 12")]
    int? month,
    [param: Required(ErrorMessage = "must not be null")]
    [param: MinValue("2000", ErrorMessage = "must be greater than or equal to 2000")]
    [param: MaxValue("2100", ErrorMessage = "must be less than or equal to 2100")]
    int? year,
    [param: Required(ErrorMessage = "must not be null")]
    [param: MinValue("0.01", ErrorMessage = "must be greater than or equal to 0.01")]
    decimal? amount,
    [param: Required(ErrorMessage = "must not be null")]
    [param: MinValue("1", ErrorMessage = "must be greater than or equal to 1")]
    [param: MaxValue("200", ErrorMessage = "must be less than or equal to 200")]
    int? alertThresholdPercent
);

public sealed record BudgetResponse(
    Guid id,
    Guid categoryId,
    string categoryName,
    int month,
    int year,
    decimal amount,
    int alertThresholdPercent,
    decimal spentAmount,
    decimal utilizationPercent,
    string alertLevel
);
