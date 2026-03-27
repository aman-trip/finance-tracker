namespace FinanceTracker.Api.Entities;

public enum AccountType
{
    BANK_ACCOUNT,
    CREDIT_CARD,
    CASH_WALLET,
    SAVINGS_ACCOUNT
}

public enum CategoryType
{
    INCOME,
    EXPENSE
}

public enum GoalStatus
{
    ACTIVE,
    COMPLETED,
    CANCELLED
}

public enum RecurringFrequency
{
    DAILY,
    WEEKLY,
    MONTHLY,
    YEARLY
}

public enum TransactionType
{
    INCOME,
    EXPENSE,
    TRANSFER_IN,
    TRANSFER_OUT
}

public enum AccountMembershipRole
{
    OWNER,
    EDITOR,
    VIEWER
}
