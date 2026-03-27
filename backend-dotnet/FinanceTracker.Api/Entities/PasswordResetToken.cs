namespace FinanceTracker.Api.Entities;

public sealed class PasswordResetToken
{
    public long Id { get; set; }

    public string Token { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTimeOffset ExpiryTime { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
