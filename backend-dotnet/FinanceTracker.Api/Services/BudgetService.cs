using FinanceTracker.Api.Contracts;
using FinanceTracker.Api.Data;
using FinanceTracker.Api.Entities;
using FinanceTracker.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Api.Services;

public sealed class BudgetService(
    FinanceTrackerDbContext dbContext,
    CategoryService categoryService,
    CurrentUserService currentUserService)
{
    public async Task<IReadOnlyList<BudgetResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var budgets = await dbContext.Budgets
            .AsNoTracking()
            .Include(budget => budget.Category)
            .Include(budget => budget.User)
            .Where(budget => budget.UserId == userId)
            .OrderByDescending(budget => budget.Year)
            .ThenByDescending(budget => budget.Month)
            .ToListAsync(cancellationToken);

        var responses = new List<BudgetResponse>(budgets.Count);
        foreach (var budget in budgets)
        {
            responses.Add(await ToResponseAsync(budget, cancellationToken));
        }

        return responses;
    }

    public async Task<BudgetResponse> CreateAsync(BudgetRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        await ValidateUniqueBudgetAsync(userId, request, null, cancellationToken);

        var budget = new Budget
        {
            UserId = userId
        };

        await MapAsync(budget, request, userId, cancellationToken);
        dbContext.Budgets.Add(budget);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(budget).Reference(item => item.Category).LoadAsync(cancellationToken);
        await dbContext.Entry(budget).Reference(item => item.User).LoadAsync(cancellationToken);
        return await ToResponseAsync(budget, cancellationToken);
    }

    public async Task<BudgetResponse> UpdateAsync(Guid id, BudgetRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var budget = await GetBudgetAsync(id, userId, cancellationToken);
        await ValidateUniqueBudgetAsync(userId, request, id, cancellationToken);
        await MapAsync(budget, request, userId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await dbContext.Entry(budget).Reference(item => item.Category).LoadAsync(cancellationToken);
        await dbContext.Entry(budget).Reference(item => item.User).LoadAsync(cancellationToken);
        return await ToResponseAsync(budget, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var budget = await GetBudgetAsync(id, currentUserService.GetCurrentUserId(), cancellationToken);
        dbContext.Budgets.Remove(budget);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Budget> GetBudgetAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Budgets
            .Include(budget => budget.Category)
            .Include(budget => budget.User)
            .FirstOrDefaultAsync(budget => budget.Id == id && budget.UserId == userId, cancellationToken)
               ?? throw new NotFoundException("Budget not found");
    }

    private async Task MapAsync(Budget budget, BudgetRequest request, Guid userId, CancellationToken cancellationToken)
    {
        var category = await categoryService.GetCategoryAsync(request.categoryId!.Value, userId, cancellationToken);
        budget.Category = category;
        budget.CategoryId = category.Id;
        budget.Month = request.month!.Value;
        budget.Year = request.year!.Value;
        budget.Amount = request.amount!.Value;
        budget.AlertThresholdPercent = request.alertThresholdPercent!.Value;
    }

    private async Task ValidateUniqueBudgetAsync(Guid userId, BudgetRequest request, Guid? existingBudgetId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Budgets.AnyAsync(
            budget => budget.UserId == userId
                      && budget.CategoryId == request.categoryId
                      && budget.Month == request.month
                      && budget.Year == request.year
                      && (existingBudgetId == null || budget.Id != existingBudgetId.Value),
            cancellationToken);

        if (exists)
        {
            throw new BadRequestException("Budget already exists for this category and month");
        }
    }

    private async Task<BudgetResponse> ToResponseAsync(Budget budget, CancellationToken cancellationToken)
    {
        var start = new DateOnly(budget.Year, budget.Month, 1);
        var end = new DateOnly(budget.Year, budget.Month, DateTime.DaysInMonth(budget.Year, budget.Month));

        var spent = await dbContext.Transactions
            .Where(transaction => transaction.UserId == budget.UserId
                                  && transaction.CategoryId == budget.CategoryId
                                  && transaction.Type == TransactionType.EXPENSE
                                  && transaction.TransactionDate >= start
                                  && transaction.TransactionDate <= end)
            .Select(transaction => (decimal?)transaction.Amount)
            .SumAsync(cancellationToken) ?? 0m;

        var utilization = budget.Amount == 0
            ? 0m
            : Math.Round(spent * 100m / budget.Amount, 2, MidpointRounding.AwayFromZero);

        var alertLevel = utilization >= 120m ? "120%"
            : utilization >= 100m ? "100%"
            : utilization >= 80m ? "80%"
            : "OK";

        return new BudgetResponse(
            budget.Id,
            budget.CategoryId,
            budget.Category.Name,
            budget.Month,
            budget.Year,
            budget.Amount,
            budget.AlertThresholdPercent,
            spent,
            utilization,
            alertLevel
        );
    }
}
