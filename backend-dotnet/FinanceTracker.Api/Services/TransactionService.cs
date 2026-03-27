using FinanceTracker.Api.Contracts;
using FinanceTracker.Api.Data;
using FinanceTracker.Api.Entities;
using FinanceTracker.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Api.Services;

public sealed class TransactionService(
    FinanceTrackerDbContext dbContext,
    CurrentUserService currentUserService,
    AccountService accountService,
    CategoryService categoryService,
    AccessControlLayer accessControlLayer,
    LedgerService ledgerService,
    RulesEngineService rulesEngineService)
{
    public async Task<PageResponse<TransactionResponse>> SearchAsync(
        string? search,
        Guid? categoryId,
        Guid? accountId,
        TransactionType? type,
        DateOnly? startDate,
        DateOnly? endDate,
        int page,
        int size,
        CancellationToken cancellationToken)
    {
        if (page < 0)
        {
            throw new BadRequestException("Page index must not be less than zero");
        }

        if (size < 1)
        {
            throw new BadRequestException("Page size must not be less than one");
        }

        var userId = currentUserService.GetCurrentUserId();
        var accessibleAccountIds = await accessControlLayer.GetAccessibleAccountIdsAsync(userId, cancellationToken);
        var query = dbContext.Transactions
            .AsNoTracking()
            .Include(transaction => transaction.Account)
            .Include(transaction => transaction.Category)
            .Where(transaction => accessibleAccountIds.Contains(transaction.AccountId));

        if (categoryId.HasValue)
        {
            query = query.Where(transaction => transaction.CategoryId == categoryId.Value);
        }

        if (accountId.HasValue)
        {
            query = query.Where(transaction => transaction.AccountId == accountId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(transaction => transaction.Type == type.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(transaction => transaction.TransactionDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(transaction => transaction.TransactionDate <= endDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var loweredSearch = search.ToLowerInvariant();
            query = query.Where(transaction =>
                (transaction.Merchant ?? string.Empty).ToLower().Contains(loweredSearch)
                || (transaction.Note ?? string.Empty).ToLower().Contains(loweredSearch));
        }

        query = query
            .OrderByDescending(transaction => transaction.TransactionDate)
            .ThenByDescending(transaction => transaction.CreatedAt);

        var totalElements = await query.LongCountAsync(cancellationToken);
        var transactions = await query
            .Skip(page * size)
            .Take(size)
            .ToListAsync(cancellationToken);
        var content = transactions.Select(ToResponse).ToList();

        var totalPages = totalElements == 0 ? 0 : (int)Math.Ceiling(totalElements / (double)size);
        var sort = new SortResponse(true, false, false);
        var pageable = new PageableResponse(sort, (long)page * size, page, size, true, false);

        return new PageResponse<TransactionResponse>(
            content,
            pageable,
            totalElements,
            totalPages,
            totalPages == 0 || page >= totalPages - 1,
            size,
            page,
            sort,
            content.Count,
            page == 0,
            content.Count == 0
        );
    }

    public async Task<TransactionResponse> CreateAsync(TransactionRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        ValidateManualTransactionType(request.type!.Value);

        var account = await accountService.GetAccessibleAccountAsync(
            request.accountId!.Value,
            userId,
            AccountAccessRequirement.Edit,
            cancellationToken);
        var user = await GetUserAsync(account.UserId, cancellationToken);
        var draft = await rulesEngineService.ApplyAsync(
            new TransactionRuleDraft(
                account.UserId,
                account.Id,
                request.type.Value,
                request.amount!.Value,
                request.transactionDate!.Value,
                request.merchant,
                request.note,
                request.paymentMethod,
                request.categoryId),
            cancellationToken);

        var category = draft.CategoryId.HasValue
            ? await categoryService.GetCategoryForAccountAsync(draft.CategoryId.Value, account.UserId, cancellationToken)
            : null;

        var created = ledgerService.CreateTransaction(
            user,
            account,
            category,
            draft.Type,
            draft.Amount,
            draft.TransactionDate,
            draft.Merchant,
            draft.Note,
            draft.PaymentMethod,
            null
        );

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(created);
    }

    public async Task<TransactionResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var transaction = await GetTransactionAsync(id, currentUserService.GetCurrentUserId(), AccountAccessRequirement.View, cancellationToken);
        return ToResponse(transaction);
    }

    public async Task<TransactionResponse> UpdateAsync(Guid id, TransactionRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        ValidateManualTransactionType(request.type!.Value);

        var existing = await GetTransactionAsync(id, userId, AccountAccessRequirement.Edit, cancellationToken);
        if (existing.Type is TransactionType.TRANSFER_IN or TransactionType.TRANSFER_OUT)
        {
            throw new BadRequestException("Transfer transactions cannot be edited from this endpoint");
        }

        ledgerService.ReverseTransaction(existing);

        var account = await accountService.GetAccessibleAccountAsync(
            request.accountId!.Value,
            userId,
            AccountAccessRequirement.Edit,
            cancellationToken);

        if (existing.UserId != account.UserId)
        {
            throw new BadRequestException("Transactions cannot be moved between accounts with different owners");
        }

        var draft = await rulesEngineService.ApplyAsync(
            new TransactionRuleDraft(
                account.UserId,
                account.Id,
                request.type.Value,
                request.amount!.Value,
                request.transactionDate!.Value,
                request.merchant,
                request.note,
                request.paymentMethod,
                request.categoryId),
            cancellationToken);

        var category = draft.CategoryId.HasValue
            ? await categoryService.GetCategoryForAccountAsync(draft.CategoryId.Value, account.UserId, cancellationToken)
            : null;

        existing.Account = account;
        existing.AccountId = account.Id;
        existing.Category = category;
        existing.CategoryId = category?.Id;
        existing.UserId = account.UserId;
        existing.Type = draft.Type;
        existing.Amount = draft.Amount;
        existing.TransactionDate = draft.TransactionDate;
        existing.Merchant = draft.Merchant;
        existing.Note = draft.Note;
        existing.PaymentMethod = draft.PaymentMethod;

        ledgerService.ApplyEffect(existing.Account, existing.Type, existing.Amount);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(existing);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var transaction = await GetTransactionAsync(id, currentUserService.GetCurrentUserId(), AccountAccessRequirement.Edit, cancellationToken);
        if (transaction.Type is TransactionType.TRANSFER_IN or TransactionType.TRANSFER_OUT)
        {
            throw new BadRequestException("Transfer transactions cannot be deleted from this endpoint");
        }

        ledgerService.ReverseTransaction(transaction);
        dbContext.Transactions.Remove(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Transaction> GetTransactionAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        return GetTransactionAsync(id, userId, AccountAccessRequirement.View, cancellationToken);
    }

    public async Task<Transaction> GetTransactionAsync(
        Guid id,
        Guid userId,
        AccountAccessRequirement requirement,
        CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Transactions
            .Include(transaction => transaction.Account)
            .Include(transaction => transaction.Category)
            .FirstOrDefaultAsync(transaction => transaction.Id == id, cancellationToken)
               ?? throw new NotFoundException("Transaction not found");

        await accessControlLayer.GetAccessibleAccountAsync(transaction.AccountId, userId, requirement, cancellationToken);
        return transaction;
    }

    public async Task<Transaction> CreateAutomatedTransactionAsync(
        User user,
        Account account,
        Category? category,
        TransactionType type,
        decimal amount,
        DateOnly date,
        string note,
        bool saveChanges,
        CancellationToken cancellationToken)
    {
        var draft = await rulesEngineService.ApplyAsync(
            new TransactionRuleDraft(
                account.UserId,
                account.Id,
                type,
                amount,
                date,
                null,
                note,
                "AUTO",
                category?.Id),
            cancellationToken);

        var resolvedCategory = draft.CategoryId.HasValue
            ? await categoryService.GetCategoryForAccountAsync(draft.CategoryId.Value, account.UserId, cancellationToken)
            : null;

        var transaction = ledgerService.CreateTransaction(
            user,
            account,
            resolvedCategory,
            draft.Type,
            draft.Amount,
            draft.TransactionDate,
            null,
            draft.Note,
            draft.PaymentMethod,
            null
        );

        if (saveChanges)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return transaction;
    }

    private static void ValidateManualTransactionType(TransactionType type)
    {
        if (type is TransactionType.TRANSFER_IN or TransactionType.TRANSFER_OUT)
        {
            throw new BadRequestException("Use the transfer endpoint for account transfers");
        }
    }

    private async Task<User> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
               ?? throw new NotFoundException("User not found");
    }

    private static TransactionResponse ToResponse(Transaction transaction)
    {
        return new TransactionResponse(
            transaction.Id,
            transaction.AccountId,
            transaction.Account.Name,
            transaction.CategoryId,
            transaction.Category?.Name,
            transaction.Type,
            transaction.Amount,
            transaction.TransactionDate,
            transaction.Merchant,
            transaction.Note,
            transaction.PaymentMethod,
            transaction.CreatedAt,
            transaction.UpdatedAt
        );
    }
}
