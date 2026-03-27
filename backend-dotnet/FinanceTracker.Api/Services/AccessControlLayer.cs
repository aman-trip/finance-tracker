using FinanceTracker.Api.Data;
using FinanceTracker.Api.Entities;
using FinanceTracker.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Api.Services;

public sealed class AccessControlLayer(FinanceTrackerDbContext dbContext)
{
    public async Task<IReadOnlyList<AccountAccessSnapshot>> GetAccessibleAccountSnapshotsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var ownedAccounts = await dbContext.Accounts
            .AsNoTracking()
            .Where(account => account.UserId == userId)
            .Select(account => new AccountAccessSnapshot(account.Id, account.UserId, AccountMembershipRole.OWNER))
            .ToListAsync(cancellationToken);

        var sharedAccounts = await dbContext.AccountMemberships
            .AsNoTracking()
            .Where(membership => membership.UserId == userId)
            .Join(
                dbContext.Accounts.AsNoTracking(),
                membership => membership.AccountId,
                account => account.Id,
                (membership, account) => new AccountAccessSnapshot(account.Id, account.UserId, membership.Role))
            .ToListAsync(cancellationToken);

        return ownedAccounts
            .Concat(sharedAccounts)
            .DistinctBy(item => item.AccountId)
            .ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetAccessibleAccountIdsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var snapshots = await GetAccessibleAccountSnapshotsAsync(userId, cancellationToken);
        return snapshots.Select(item => item.AccountId).ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetAccessibleOwnerIdsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var snapshots = await GetAccessibleAccountSnapshotsAsync(userId, cancellationToken);
        return snapshots
            .Select(item => item.OwnerUserId)
            .Append(userId)
            .Distinct()
            .ToList();
    }

    public async Task<Account> GetAccessibleAccountAsync(
        Guid accountId,
        Guid userId,
        AccountAccessRequirement requirement,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.Accounts
            .FirstOrDefaultAsync(candidate => candidate.Id == accountId, cancellationToken)
               ?? throw new NotFoundException("Account not found");

        var role = await ResolveRoleAsync(account, userId, cancellationToken);
        if (role is null)
        {
            throw new NotFoundException("Account not found");
        }

        if (!HasAccess(role.Value, requirement))
        {
            throw new ForbiddenException("You do not have permission to access this account");
        }

        return account;
    }

    public async Task<AccountMembershipRole?> GetResolvedRoleAsync(Guid accountId, Guid userId, CancellationToken cancellationToken)
    {
        var account = await dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == accountId, cancellationToken);

        return account is null
            ? null
            : await ResolveRoleAsync(account, userId, cancellationToken);
    }

    private async Task<AccountMembershipRole?> ResolveRoleAsync(Account account, Guid userId, CancellationToken cancellationToken)
    {
        if (account.UserId == userId)
        {
            return AccountMembershipRole.OWNER;
        }

        return await dbContext.AccountMemberships
            .AsNoTracking()
            .Where(membership => membership.AccountId == account.Id && membership.UserId == userId)
            .Select(membership => (AccountMembershipRole?)membership.Role)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static bool HasAccess(AccountMembershipRole role, AccountAccessRequirement requirement)
    {
        return requirement switch
        {
            AccountAccessRequirement.View => true,
            AccountAccessRequirement.Edit => role is AccountMembershipRole.OWNER or AccountMembershipRole.EDITOR,
            AccountAccessRequirement.Manage => role == AccountMembershipRole.OWNER,
            _ => false
        };
    }
}

public sealed record AccountAccessSnapshot(
    Guid AccountId,
    Guid OwnerUserId,
    AccountMembershipRole Role
);

public enum AccountAccessRequirement
{
    View,
    Edit,
    Manage
}
