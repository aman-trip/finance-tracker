namespace FinanceTracker.Api.Entities;

public sealed class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public CategoryType Type { get; set; }

    public string Color { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public bool Archived { get; set; }
}
