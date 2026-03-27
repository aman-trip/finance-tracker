using FinanceTracker.Api.Contracts;
using FinanceTracker.Api.Data;
using FinanceTracker.Api.Entities;
using FinanceTracker.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Api.Services;

public sealed class AccountService(
    FinanceTrackerDbContext dbContext,
    CurrentUserService currentUserService,
    AccessControlLayer accessControlLayer,
    LedgerService ledgerService)
{
    public async Task<IReadOnlyList<AccountResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var accessibleIds = await accessControlLayer.GetAccessibleAccountIdsAsync(currentUserService.GetCurrentUserId(), cancellationToken);
        var accounts = await dbContext.Accounts
            .AsNoTracking()
            .Where(account => accessibleIds.Contains(account.Id))
            .OrderByDescending(account => account.CreatedAt)
            .ToListAsync(cancellationToken);

        return accounts.Select(ToResponse).ToList();
    }

    public async Task<AccountResponse> CreateAsync(AccountRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();

        var account = new Account
        {
            UserId = userId,
            Name = request.name!,
            Type = request.type!.Value,
            OpeningBalance = request.openingBalance!.Value,
            CurrentBalance = request.openingBalance.Value,
            InstitutionName = request.institutionName
        };

        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(account);
    }

    public async Task<AccountResponse> UpdateAsync(Guid id, AccountRequest request, CancellationToken cancellationToken)
    {
        var account = await GetAccountAsync(id, currentUserService.GetCurrentUserId(), cancellationToken);
        account.Name = request.name!;
        account.Type = request.type!.Value;
        account.InstitutionName = request.institutionName;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(account);
    }

    public async Task TransferAsync(TransferRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        if (request.sourceAccountId == request.targetAccountId)
        {
            throw new BadRequestException("Source and target accounts must be different");
        }

        var source = await GetAccessibleAccountAsync(
            request.sourceAccountId!.Value,
            userId,
            AccountAccessRequirement.Edit,
            cancellationToken);
        var target = await GetAccessibleAccountAsync(
            request.targetAccountId!.Value,
            userId,
            AccountAccessRequirement.Edit,
            cancellationToken);

        if (source.UserId != target.UserId)
        {
            throw new BadRequestException("Transfers between accounts with different owners are not supported");
        }

        if (source.CurrentBalance < request.amount!.Value)
        {
            throw new BadRequestException("Insufficient balance for transfer");
        }

        var user = await GetUserAsync(source.UserId, cancellationToken);
        var transferGroupId = Guid.NewGuid();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        ledgerService.CreateTransaction(
            user,
            source,
            null,
            TransactionType.TRANSFER_OUT,
            request.amount.Value,
            request.transactionDate!.Value,
            target.Name,
            request.note is null ? $"Transfer to {target.Name}" : request.note,
            "TRANSFER",
            transferGroupId
        );

        ledgerService.CreateTransaction(
            user,
            target,
            null,
            TransactionType.TRANSFER_IN,
            request.amount.Value,
            request.transactionDate.Value,
            source.Name,
            request.note is null ? $"Transfer from {source.Name}" : request.note,
            "TRANSFER",
            transferGroupId
        );

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<Account> GetAccountAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Accounts
            .FirstOrDefaultAsync(account => account.Id == id && account.UserId == userId, cancellationToken)
               ?? throw new NotFoundException("Account not found");
    }

    public Task<Account> GetAccessibleAccountAsync(
        Guid id,
        Guid userId,
        AccountAccessRequirement requirement,
        CancellationToken cancellationToken)
    {
        return accessControlLayer.GetAccessibleAccountAsync(id, userId, requirement, cancellationToken);
    }

    private async Task<User> GetUserAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken)
               ?? throw new NotFoundException("User not found");
    }

    private static AccountResponse ToResponse(Account account)
    {
        return new AccountResponse(
            account.Id,
            account.Name,
            account.Type,
            account.OpeningBalance,
            account.CurrentBalance,
            account.InstitutionName,
            account.CreatedAt
        );
    }
}
