namespace FinanceTracker.Api.Entities;

public sealed class Goal
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public decimal TargetAmount { get; set; }

    public decimal CurrentAmount { get; set; }

    public DateOnly? TargetDate { get; set; }

    public GoalStatus Status { get; set; }
}
