namespace FinanceTracker.Api.Entities;

public sealed class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid AccountId { get; set; }

    public Account Account { get; set; } = null!;

    public Guid? CategoryId { get; set; }

    public Category? Category { get; set; }

    public TransactionType Type { get; set; }

    public decimal Amount { get; set; }

    public DateOnly TransactionDate { get; set; }

    public string? Merchant { get; set; }

    public string? Note { get; set; }

    public string? PaymentMethod { get; set; }

    public Guid? TransferGroupId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
