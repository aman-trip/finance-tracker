using System.ComponentModel.DataAnnotations;
using FinanceTracker.Api.Entities;
using FinanceTracker.Api.Infrastructure;

namespace FinanceTracker.Api.Contracts;

public sealed record GoalRequest(
    [param: NotBlank(ErrorMessage = "must not be blank")]
    string? name,
    [param: Required(ErrorMessage = "must not be null")]
    [param: MinValue("0.01", ErrorMessage = "must be greater than or equal to 0.01")]
    decimal? targetAmount,
    DateOnly? targetDate,
    GoalStatus? status
);

public sealed record GoalContributionRequest(
    [param: Required(ErrorMessage = "must not be null")]
    Guid? accountId,
    [param: Required(ErrorMessage = "must not be null")]
    [param: MinValue("0.01", ErrorMessage = "must be greater than or equal to 0.01")]
    decimal? amount
);

public sealed record GoalResponse(
    Guid id,
    string name,
    decimal targetAmount,
    decimal currentAmount,
    DateOnly? targetDate,
    GoalStatus status,
    decimal progressPercent
);
