using FinanceTracker.Api.Contracts;
using FinanceTracker.Api.Data;
using FinanceTracker.Api.Entities;
using FinanceTracker.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Api.Services;

public sealed class CategoryService(
    FinanceTrackerDbContext dbContext,
    CurrentUserService currentUserService,
    AccessControlLayer accessControlLayer,
    ILogger<CategoryService> logger)
{
    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var ownerIds = await accessControlLayer.GetAccessibleOwnerIdsAsync(userId, cancellationToken);

        var categories = await dbContext.Categories
            .AsNoTracking()
            .Where(category => ownerIds.Contains(category.UserId) && !category.Archived)
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);

        return categories.Select(ToResponse).ToList();
    }

    public async Task<CategoryResponse> CreateAsync(CategoryRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var name = NormalizeName(request.name);

        await EnsureUniqueNameAsync(userId, request.type!.Value, name, null, cancellationToken);

        var category = new Category
        {
            UserId = userId,
            Name = name,
            Type = request.type!.Value,
            Color = request.color!,
            Icon = request.icon!,
            Archived = false
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(category);
    }

    public async Task<CategoryResponse> UpdateAsync(Guid id, CategoryRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var category = await GetCategoryAsync(id, userId, cancellationToken);
        var name = NormalizeName(request.name);

        await EnsureUniqueNameAsync(userId, request.type!.Value, name, id, cancellationToken);

        category.Name = name;
        category.Type = request.type!.Value;
        category.Color = request.color!;
        category.Icon = request.icon!;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(category);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await GetCategoryAsync(id, currentUserService.GetCurrentUserId(), cancellationToken);
        category.Archived = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateDefaultCategoriesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var trackedCategoryIds = dbContext.ChangeTracker.Entries<Category>()
            .Where(entry => entry.Entity.Id != Guid.Empty)
            .Select(entry => entry.Entity.Id)
            .Distinct()
            .ToList();

        if (trackedCategoryIds.Count > 0)
        {
            logger.LogInformation(
                "Default category creation starting with {TrackedCount} tracked categories for user {UserId}: {CategoryIds}",
                trackedCategoryIds.Count,
                userId,
                string.Join(", ", trackedCategoryIds));
        }

        var hasCategories = await dbContext.Categories
            .AsNoTracking()
            .AnyAsync(
            category => category.UserId == userId && !category.Archived,
            cancellationToken);

        if (hasCategories)
        {
            logger.LogInformation("Skipping default category creation because categories already exist for user {UserId}", userId);
            return;
        }

        var categories = DefaultExpenseCategories()
            .Select(item => BuildDefaultCategory(userId, item.name, CategoryType.EXPENSE, item.color, item.icon))
            .Concat(DefaultIncomeCategories()
                .Select(item => BuildDefaultCategory(userId, item.name, CategoryType.INCOME, item.color, item.icon)))
            .ToList();

        logger.LogInformation(
            "Creating {CategoryCount} default categories for user {UserId}: {Categories}",
            categories.Count,
            userId,
            string.Join(", ", categories.Select(category => $"{category.Id}:{category.Name}:{category.Type}")));

        dbContext.Categories.AddRange(categories);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Category> GetCategoryAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Categories
            .FirstOrDefaultAsync(category => category.Id == id && category.UserId == userId && !category.Archived, cancellationToken)
               ?? throw new NotFoundException("Category not found");
    }

    public async Task<Category> GetCategoryForAccountAsync(Guid id, Guid accountOwnerUserId, CancellationToken cancellationToken)
    {
        return await dbContext.Categories
            .FirstOrDefaultAsync(
                category => category.Id == id
                            && category.UserId == accountOwnerUserId
                            && !category.Archived,
                cancellationToken)
               ?? throw new NotFoundException("Category not found");
    }

    private static Category BuildDefaultCategory(Guid userId, string name, CategoryType type, string color, string icon)
    {
        return new Category
        {
            UserId = userId,
            Name = name,
            Type = type,
            Color = color,
            Icon = icon,
            Archived = false
        };
    }

    private async Task EnsureUniqueNameAsync(
        Guid userId,
        CategoryType type,
        string normalizedName,
        Guid? existingCategoryId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Categories
            .AsNoTracking()
            .AnyAsync(
                category => category.UserId == userId
                            && category.Type == type
                            && category.Name.ToLower() == normalizedName.ToLower()
                            && (existingCategoryId == null || category.Id != existingCategoryId.Value),
                cancellationToken);

        if (exists)
        {
            throw new BadRequestException("Category already exists");
        }
    }

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BadRequestException("Category name must not be blank");
        }

        return name.Trim();
    }

    private static IReadOnlyList<(string name, string color, string icon)> DefaultExpenseCategories()
    {
        return
        [
            ("Food", "#EF4444", "utensils"),
            ("Rent", "#F97316", "house"),
            ("Utilities", "#EAB308", "bolt"),
            ("Transport", "#14B8A6", "car"),
            ("Entertainment", "#8B5CF6", "film"),
            ("Shopping", "#EC4899", "shopping-bag"),
            ("Health", "#10B981", "heart-pulse"),
            ("Education", "#3B82F6", "graduation-cap"),
            ("Travel", "#06B6D4", "plane"),
            ("Subscriptions", "#6366F1", "repeat"),
            ("Misc", "#6B7280", "circle")
        ];
    }

    private static IReadOnlyList<(string name, string color, string icon)> DefaultIncomeCategories()
    {
        return
        [
            ("Salary", "#16A34A", "briefcase"),
            ("Freelance", "#0EA5E9", "laptop"),
            ("Bonus", "#84CC16", "sparkles"),
            ("Investment", "#22C55E", "chart-line"),
            ("Gift", "#F59E0B", "gift"),
            ("Refund", "#06B6D4", "rotate-ccw"),
            ("Other", "#64748B", "plus-circle")
        ];
    }

    private static CategoryResponse ToResponse(Category category)
    {
        return new CategoryResponse(
            category.Id,
            category.Name,
            category.Type,
            category.Color,
            category.Icon,
            category.Archived
        );
    }
}
