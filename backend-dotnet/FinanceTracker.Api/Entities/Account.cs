namespace FinanceTracker.Api.Entities;

public sealed class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public AccountType Type { get; set; }

    public decimal OpeningBalance { get; set; }

    public decimal CurrentBalance { get; set; }

    public string? InstitutionName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
