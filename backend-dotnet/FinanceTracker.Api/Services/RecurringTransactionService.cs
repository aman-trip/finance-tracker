using FinanceTracker.Api.Contracts;
using FinanceTracker.Api.Data;
using FinanceTracker.Api.Entities;
using FinanceTracker.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Api.Services;

public sealed class RecurringTransactionService(
    FinanceTrackerDbContext dbContext,
    CurrentUserService currentUserService,
    AccessControlLayer accessControlLayer,
    AccountService accountService,
    CategoryService categoryService,
    TransactionService transactionService)
{
    public async Task<IReadOnlyList<RecurringTransactionResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var accountIds = await accessControlLayer.GetAccessibleAccountIdsAsync(currentUserService.GetCurrentUserId(), cancellationToken);
        var recurringTransactions = await dbContext.RecurringTransactions
            .AsNoTracking()
            .Include(recurring => recurring.Account)
            .Include(recurring => recurring.Category)
            .Where(recurring => accountIds.Contains(recurring.AccountId))
            .OrderBy(recurring => recurring.NextRunDate)
            .ToListAsync(cancellationToken);

        return recurringTransactions.Select(ToResponse).ToList();
    }

    public async Task<RecurringTransactionResponse> CreateAsync(RecurringTransactionRequest request, CancellationToken cancellationToken)
    {
        ValidateType(request.type!.Value);
        ValidateDateRange(request);

        var currentUserId = currentUserService.GetCurrentUserId();
        var account = await accountService.GetAccessibleAccountAsync(
            request.accountId!.Value,
            currentUserId,
            AccountAccessRequirement.Edit,
            cancellationToken);
        var recurring = new RecurringTransaction
        {
            UserId = account.UserId
        };

        await MapAsync(recurring, request, account, cancellationToken);
        dbContext.RecurringTransactions.Add(recurring);
        await dbContext.SaveChangesAsync(cancellationToken);
        await LoadReferencesAsync(recurring, cancellationToken);
        return ToResponse(recurring);
    }

    public async Task<RecurringTransactionResponse> UpdateAsync(Guid id, RecurringTransactionRequest request, CancellationToken cancellationToken)
    {
        ValidateType(request.type!.Value);
        ValidateDateRange(request);

        var currentUserId = currentUserService.GetCurrentUserId();
        var recurring = await GetRecurringAsync(id, currentUserId, AccountAccessRequirement.Edit, cancellationToken);
        var account = await accountService.GetAccessibleAccountAsync(
            request.accountId!.Value,
            currentUserId,
            AccountAccessRequirement.Edit,
            cancellationToken);

        if (recurring.UserId != account.UserId)
        {
            throw new BadRequestException("Recurring transactions cannot be moved between accounts with different owners");
        }

        await MapAsync(recurring, request, account, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await LoadReferencesAsync(recurring, cancellationToken);
        return ToResponse(recurring);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var recurring = await GetRecurringAsync(id, currentUserService.GetCurrentUserId(), AccountAccessRequirement.Edit, cancellationToken);
        dbContext.RecurringTransactions.Remove(recurring);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessDueTransactionsAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var recurringTransactions = await dbContext.RecurringTransactions
            .Include(recurring => recurring.User)
            .Include(recurring => recurring.Account)
            .Include(recurring => recurring.Category)
            .Where(recurring => recurring.AutoCreateTransaction && recurring.NextRunDate <= today)
            .ToListAsync(cancellationToken);

        foreach (var recurring in recurringTransactions)
        {
            await ProcessRecurringTransactionAsync(recurring, cancellationToken);
        }
    }

    public async Task<RecurringTransaction> GetRecurringAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        return await GetRecurringAsync(id, userId, AccountAccessRequirement.View, cancellationToken);
    }

    public async Task<RecurringTransaction> GetRecurringAsync(
        Guid id,
        Guid userId,
        AccountAccessRequirement requirement,
        CancellationToken cancellationToken)
    {
        var recurring = await dbContext.RecurringTransactions
            .Include(recurring => recurring.Account)
            .Include(recurring => recurring.Category)
            .FirstOrDefaultAsync(recurring => recurring.Id == id, cancellationToken)
               ?? throw new NotFoundException("Recurring transaction not found");

        await accessControlLayer.GetAccessibleAccountAsync(recurring.AccountId, userId, requirement, cancellationToken);
        return recurring;
    }

    private async Task ProcessRecurringTransactionAsync(RecurringTransaction recurring, CancellationToken cancellationToken)
    {
        if (recurring.EndDate.HasValue && recurring.NextRunDate > recurring.EndDate.Value)
        {
            recurring.AutoCreateTransaction = false;
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await transactionService.CreateAutomatedTransactionAsync(
                recurring.User,
                recurring.Account,
                recurring.Category,
                recurring.Type,
                recurring.Amount,
                recurring.NextRunDate,
                $"Auto-created from recurring transaction: {recurring.Title}",
                saveChanges: false,
                cancellationToken
            );

            recurring.NextRunDate = NextDate(recurring.NextRunDate, recurring.Frequency);
            if (recurring.EndDate.HasValue && recurring.NextRunDate > recurring.EndDate.Value)
            {
                recurring.AutoCreateTransaction = false;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            throw;
        }
    }

    private async Task MapAsync(RecurringTransaction recurring, RecurringTransactionRequest request, Account account, CancellationToken cancellationToken)
    {
        var category = request.categoryId.HasValue
            ? await categoryService.GetCategoryForAccountAsync(request.categoryId.Value, account.UserId, cancellationToken)
            : null;

        recurring.UserId = account.UserId;
        recurring.Title = request.title!;
        recurring.Type = request.type!.Value;
        recurring.Amount = request.amount!.Value;
        recurring.Account = account;
        recurring.AccountId = account.Id;
        recurring.Category = category;
        recurring.CategoryId = category?.Id;
        recurring.Frequency = request.frequency!.Value;
        recurring.StartDate = request.startDate!.Value;
        recurring.EndDate = request.endDate;
        recurring.NextRunDate = request.nextRunDate!.Value;
        recurring.AutoCreateTransaction = request.autoCreateTransaction;
    }

    private async Task LoadReferencesAsync(RecurringTransaction recurring, CancellationToken cancellationToken)
    {
        await dbContext.Entry(recurring).Reference(item => item.Account).LoadAsync(cancellationToken);
        if (recurring.CategoryId.HasValue)
        {
            await dbContext.Entry(recurring).Reference(item => item.Category).LoadAsync(cancellationToken);
        }
    }

    private static RecurringTransactionResponse ToResponse(RecurringTransaction recurring)
    {
        return new RecurringTransactionResponse(
            recurring.Id,
            recurring.Title,
            recurring.Type,
            recurring.Amount,
            recurring.CategoryId,
            recurring.Category?.Name,
            recurring.AccountId,
            recurring.Account.Name,
            recurring.Frequency,
            recurring.StartDate,
            recurring.EndDate,
            recurring.NextRunDate,
            recurring.AutoCreateTransaction
        );
    }

    private static DateOnly NextDate(DateOnly current, RecurringFrequency frequency)
    {
        return frequency switch
        {
            RecurringFrequency.DAILY => current.AddDays(1),
            RecurringFrequency.WEEKLY => current.AddDays(7),
            RecurringFrequency.MONTHLY => current.AddMonths(1),
            RecurringFrequency.YEARLY => current.AddYears(1),
            _ => current
        };
    }

    private static void ValidateType(TransactionType type)
    {
        if (type is TransactionType.TRANSFER_IN or TransactionType.TRANSFER_OUT)
        {
            throw new BadRequestException("Recurring transfers are not supported");
        }
    }

    private static void ValidateDateRange(RecurringTransactionRequest request)
    {
        if (request.nextRunDate!.Value < request.startDate!.Value)
        {
            throw new BadRequestException("Next run date must be on or after start date");
        }

        if (request.endDate.HasValue && request.nextRunDate.Value > request.endDate.Value)
        {
            throw new BadRequestException("Next run date must be on or before end date");
        }
    }
}
