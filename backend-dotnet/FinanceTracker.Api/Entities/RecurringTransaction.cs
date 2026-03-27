namespace FinanceTracker.Api.Entities;

public sealed class RecurringTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public TransactionType Type { get; set; }

    public decimal Amount { get; set; }

    public Guid? CategoryId { get; set; }

    public Category? Category { get; set; }

    public Guid AccountId { get; set; }

    public Account Account { get; set; } = null!;

    public RecurringFrequency Frequency { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public DateOnly NextRunDate { get; set; }

    public bool AutoCreateTransaction { get; set; }
}
