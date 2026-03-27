using System.ComponentModel.DataAnnotations;
using FinanceTracker.Api.Entities;
using FinanceTracker.Api.Infrastructure;

namespace FinanceTracker.Api.Contracts;

public sealed record AccountInviteRequest(
    [param: EmailAddress(ErrorMessage = "must be a well-formed email address")]
    [param: NotBlank(ErrorMessage = "must not be blank")]
    string? email,
    [param: Required(ErrorMessage = "must not be null")]
    AccountMembershipRole? role
);

public sealed record AccountMemberUpdateRequest(
    [param: Required(ErrorMessage = "must not be null")]
    AccountMembershipRole? role
);

public sealed record AccountMemberResponse(
    Guid userId,
    string email,
    string displayName,
    AccountMembershipRole role,
    bool owner
);
