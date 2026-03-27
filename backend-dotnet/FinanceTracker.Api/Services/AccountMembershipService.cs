using FinanceTracker.Api.Contracts;
using FinanceTracker.Api.Data;
using FinanceTracker.Api.Entities;
using FinanceTracker.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Api.Services;

public sealed class AccountMembershipService(
    FinanceTrackerDbContext dbContext,
    CurrentUserService currentUserService,
    AccountService accountService)
{
    public async Task<AccountMemberResponse> InviteAsync(Guid accountId, AccountInviteRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.GetCurrentUserId();
        var account = await accountService.GetAccessibleAccountAsync(accountId, currentUserId, AccountAccessRequirement.Manage, cancellationToken);
        var role = ValidateRole(request.role);
        var normalizedEmail = NormalizeEmail(request.email);

        var invitedUser = await dbContext.Users
            .FirstOrDefaultAsync(user => user.Email.ToLower() == normalizedEmail, cancellationToken)
               ?? throw new NotFoundException("User not found");

        if (invitedUser.Id == account.UserId)
        {
            throw new BadRequestException("Account owner already has access");
        }

        var membership = await dbContext.AccountMemberships
            .FirstOrDefaultAsync(item => item.AccountId == account.Id && item.UserId == invitedUser.Id, cancellationToken);

        if (membership is null)
        {
            membership = new AccountMembership
            {
                AccountId = account.Id,
                UserId = invitedUser.Id,
                Role = role
            };

            dbContext.AccountMemberships.Add(membership);
        }
        else
        {
            membership.Role = role;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(invitedUser, membership.Role, owner: false);
    }

    public async Task<IReadOnlyList<AccountMemberResponse>> GetMembersAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.GetCurrentUserId();
        var account = await accountService.GetAccessibleAccountAsync(accountId, currentUserId, AccountAccessRequirement.View, cancellationToken);
        var owner = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == account.UserId, cancellationToken)
               ?? throw new NotFoundException("Account owner not found");

        var members = await dbContext.AccountMemberships
            .AsNoTracking()
            .Where(membership => membership.AccountId == account.Id && membership.UserId != account.UserId)
            .Join(
                dbContext.Users.AsNoTracking(),
                membership => membership.UserId,
                user => user.Id,
                (membership, user) => new
                {
                    user.Id,
                    user.Email,
                    user.DisplayName,
                    membership.Role
                })
            .ToListAsync(cancellationToken);

        return [
            ToResponse(owner, AccountMembershipRole.OWNER, owner: true),
            .. members
                .OrderBy(member => member.DisplayName)
                .Select(member => new AccountMemberResponse(member.Id, member.Email, member.DisplayName, member.Role, false))
        ];
    }

    public async Task<AccountMemberResponse> UpdateRoleAsync(
        Guid accountId,
        Guid memberUserId,
        AccountMemberUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.GetCurrentUserId();
        var account = await accountService.GetAccessibleAccountAsync(accountId, currentUserId, AccountAccessRequirement.Manage, cancellationToken);

        if (memberUserId == account.UserId)
        {
            throw new BadRequestException("Account owner role cannot be changed");
        }

        var role = ValidateRole(request.role);
        var membership = await dbContext.AccountMemberships
            .FirstOrDefaultAsync(item => item.AccountId == account.Id && item.UserId == memberUserId, cancellationToken)
               ?? throw new NotFoundException("Account member not found");

        var member = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == memberUserId, cancellationToken)
               ?? throw new NotFoundException("User not found");

        membership.Role = role;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(member, role, owner: false);
    }

    private static AccountMembershipRole ValidateRole(AccountMembershipRole? role)
    {
        if (role is null)
        {
            throw new BadRequestException("Role is required");
        }

        if (role == AccountMembershipRole.OWNER)
        {
            throw new BadRequestException("Owner role is reserved for account owners");
        }

        return role.Value;
    }

    private static string NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new BadRequestException("Email must not be blank");
        }

        return email.Trim().ToLowerInvariant();
    }

    private static AccountMemberResponse ToResponse(User user, AccountMembershipRole role, bool owner)
    {
        return new AccountMemberResponse(user.Id, user.Email, user.DisplayName, role, owner);
    }
}
