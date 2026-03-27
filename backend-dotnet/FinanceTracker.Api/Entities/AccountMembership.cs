namespace FinanceTracker.Api.Entities;

public sealed class AccountMembership
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AccountId { get; set; }

    public Account Account { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public AccountMembershipRole Role { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
