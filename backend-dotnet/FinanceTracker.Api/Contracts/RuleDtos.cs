using System.ComponentModel.DataAnnotations;
using FinanceTracker.Api.Infrastructure;

namespace FinanceTracker.Api.Contracts;

public sealed record RuleRequest(
    [param: NotBlank(ErrorMessage = "must not be blank")]
    string? name,
    [param: NotBlank(ErrorMessage = "must not be blank")]
    string? conditionJson,
    [param: NotBlank(ErrorMessage = "must not be blank")]
    string? actionJson,
    bool isActive
);

public sealed record RuleResponse(
    Guid id,
    string name,
    string conditionJson,
    string actionJson,
    bool isActive,
    DateTimeOffset createdAt,
    DateTimeOffset updatedAt
);
