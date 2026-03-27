using System.ComponentModel.DataAnnotations;
using FinanceTracker.Api.Entities;
using FinanceTracker.Api.Infrastructure;

namespace FinanceTracker.Api.Contracts;

public sealed record CategoryRequest(
    [param: NotBlank(ErrorMessage = "must not be blank")]
    string? name,
    [param: Required(ErrorMessage = "must not be null")]
    CategoryType? type,
    [param: NotBlank(ErrorMessage = "must not be blank")]
    string? color,
    [param: NotBlank(ErrorMessage = "must not be blank")]
    string? icon
);

public sealed record CategoryResponse(
    Guid id,
    string name,
    CategoryType type,
    string color,
    string icon,
    bool archived
);
