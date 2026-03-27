using FinanceTracker.Api.Data;
using FinanceTracker.Api.Entities;

namespace FinanceTracker.Api.Services;

public sealed class LedgerService(FinanceTrackerDbContext dbContext)
{
    public Transaction CreateTransaction(
        User user,
        Account account,
        Category? category,
        TransactionType type,
        decimal amount,
        DateOnly transactionDate,
        string? merchant,
        string? note,
        string? paymentMethod,
        Guid? transferGroupId)
    {
        ApplyEffect(account, type, amount);

        var transaction = new Transaction
        {
            User = user,
            UserId = user.Id,
            Account = account,
            AccountId = account.Id,
            Category = category,
            CategoryId = category?.Id,
            Type = type,
            Amount = amount,
            TransactionDate = transactionDate,
            Merchant = merchant,
            Note = note,
            PaymentMethod = paymentMethod,
            TransferGroupId = transferGroupId
        };

        dbContext.Transactions.Add(transaction);
        return transaction;
    }

    public void ReverseTransaction(Transaction transaction)
    {
        ReverseEffect(transaction.Account, transaction.Type, transaction.Amount);
    }

    public void ApplyEffect(Account account, TransactionType type, decimal amount)
    {
        account.CurrentBalance = type switch
        {
            TransactionType.INCOME or TransactionType.TRANSFER_IN => account.CurrentBalance + amount,
            TransactionType.EXPENSE or TransactionType.TRANSFER_OUT => account.CurrentBalance - amount,
            _ => account.CurrentBalance
        };
    }

    public void ReverseEffect(Account account, TransactionType type, decimal amount)
    {
        account.CurrentBalance = type switch
        {
            TransactionType.INCOME or TransactionType.TRANSFER_IN => account.CurrentBalance - amount,
            TransactionType.EXPENSE or TransactionType.TRANSFER_OUT => account.CurrentBalance + amount,
            _ => account.CurrentBalance
        };
    }
}
