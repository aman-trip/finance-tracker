using FinanceTracker.Api.Contracts;
using FinanceTracker.Api.Data;
using FinanceTracker.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Api.Services;

public sealed class InsightsService(
    FinanceTrackerDbContext dbContext,
    CurrentUserService currentUserService,
    AccessControlLayer accessControlLayer,
    ReportService reportService)
{
    public async Task<HealthScoreResponse> GetHealthScoreAsync(CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var accountIds = await accessControlLayer.GetAccessibleAccountIdsAsync(userId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentMonthStart = new DateOnly(today.Year, today.Month, 1);
        var ninetyDaysAgo = today.AddDays(-89);

        var transactions = await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => accountIds.Contains(transaction.AccountId)
                                  && transaction.TransactionDate >= ninetyDaysAgo
                                  && transaction.TransactionDate <= today)
            .ToListAsync(cancellationToken);

        var currentBalance = await dbContext.Accounts
            .AsNoTracking()
            .Where(account => accountIds.Contains(account.Id))
            .Select(account => (decimal?)account.CurrentBalance)
            .SumAsync(cancellationToken) ?? 0m;

        var breakdown = new List<HealthScoreBreakdownItem>(4);
        var suggestions = new List<string>();

        var savingsRateScore = BuildSavingsRateScore(transactions, suggestions);
        breakdown.Add(savingsRateScore);

        var expenseStabilityScore = BuildExpenseStabilityScore(transactions, today, suggestions);
        breakdown.Add(expenseStabilityScore);

        var budgetScore = await BuildBudgetAdherenceScoreAsync(userId, currentMonthStart, today, cancellationToken, suggestions);
        breakdown.Add(budgetScore);

        var cashBufferScore = BuildCashBufferScore(transactions, currentBalance, suggestions);
        breakdown.Add(cashBufferScore);

        var score = (int)Math.Round(breakdown.Sum(item => item.score) * 100m / breakdown.Sum(item => item.maxScore), MidpointRounding.AwayFromZero);
        if (suggestions.Count == 0)
        {
            suggestions.Add("Your cashflow profile is healthy. Keep budgets and recurring transactions updated for more accurate recommendations.");
        }

        return new HealthScoreResponse(score, breakdown, suggestions.Distinct().Take(5).ToList());
    }

    public async Task<InsightsOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var healthScore = await GetHealthScoreAsync(cancellationToken);
        var highlights = await reportService.InsightsAsync(cancellationToken);
        return new InsightsOverviewResponse(healthScore, highlights);
    }

    private static HealthScoreBreakdownItem BuildSavingsRateScore(
        IReadOnlyList<Transaction> transactions,
        List<string> suggestions)
    {
        var income = transactions
            .Where(transaction => transaction.Type == TransactionType.INCOME)
            .Sum(transaction => transaction.Amount);
        var expense = transactions
            .Where(transaction => transaction.Type == TransactionType.EXPENSE)
            .Sum(transaction => transaction.Amount);

        var score = 10;
        var detail = "No income history yet.";

        if (income > 0m)
        {
            var savingsRate = Math.Max(0m, (income - expense) * 100m / income);
            score = savingsRate switch
            {
                >= 25m => 25,
                >= 15m => 20,
                >= 5m => 14,
                > 0m => 10,
                _ => 4
            };
            detail = $"Savings rate over the last 90 days is {savingsRate:0.0}%.";

            if (savingsRate < 10m)
            {
                suggestions.Add("Aim to keep at least 10% of income unspent to improve resilience.");
            }
        }

        return new HealthScoreBreakdownItem("Savings Rate", score, 25, detail);
    }

    private static HealthScoreBreakdownItem BuildExpenseStabilityScore(
        IReadOnlyList<Transaction> transactions,
        DateOnly today,
        List<string> suggestions)
    {
        var monthlyExpenseTotals = Enumerable.Range(0, 3)
            .Select(offset =>
            {
                var month = today.AddMonths(-offset);
                var monthStart = new DateOnly(month.Year, month.Month, 1);
                var monthEnd = new DateOnly(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
                return transactions
                    .Where(transaction => transaction.Type == TransactionType.EXPENSE
                                          && transaction.TransactionDate >= monthStart
                                          && transaction.TransactionDate <= monthEnd)
                    .Sum(transaction => transaction.Amount);
            })
            .ToList();

        if (monthlyExpenseTotals.All(total => total == 0m))
        {
            return new HealthScoreBreakdownItem("Expense Stability", 10, 20, "Need more expense history to measure volatility.");
        }

        var average = monthlyExpenseTotals.Average();
        var variance = monthlyExpenseTotals.Average(total => Math.Pow((double)(total - average), 2));
        var standardDeviation = Math.Sqrt(variance);
        var coefficientOfVariation = average == 0m ? 0d : standardDeviation / (double)average;

        var score = coefficientOfVariation switch
        {
            <= 0.15d => 20,
            <= 0.30d => 16,
            <= 0.45d => 12,
            <= 0.60d => 8,
            _ => 4
        };

        if (score <= 8)
        {
            suggestions.Add("Expense swings are high. Review discretionary categories and recurring bills for stability.");
        }

        return new HealthScoreBreakdownItem(
            "Expense Stability",
            score,
            20,
            $"3-month expense volatility coefficient is {coefficientOfVariation:0.00}.");
    }

    private async Task<HealthScoreBreakdownItem> BuildBudgetAdherenceScoreAsync(
        Guid userId,
        DateOnly currentMonthStart,
        DateOnly today,
        CancellationToken cancellationToken,
        List<string> suggestions)
    {
        var budgets = await dbContext.Budgets
            .AsNoTracking()
            .Where(budget => budget.UserId == userId
                             && budget.Month == today.Month
                             && budget.Year == today.Year)
            .ToListAsync(cancellationToken);

        if (budgets.Count == 0)
        {
            suggestions.Add("Set monthly budgets to unlock stronger health-score recommendations.");
            return new HealthScoreBreakdownItem("Budget Adherence", 9, 20, "No active budgets set for this month.");
        }

        var utilizationScores = new List<int>(budgets.Count);
        foreach (var budget in budgets)
        {
            var spent = await dbContext.Transactions
                .AsNoTracking()
                .Where(transaction => transaction.UserId == userId
                                      && transaction.CategoryId == budget.CategoryId
                                      && transaction.Type == TransactionType.EXPENSE
                                      && transaction.TransactionDate >= currentMonthStart
                                      && transaction.TransactionDate <= today)
                .Select(transaction => (decimal?)transaction.Amount)
                .SumAsync(cancellationToken) ?? 0m;

            var utilization = budget.Amount <= 0m ? 0m : spent * 100m / budget.Amount;
            utilizationScores.Add(utilization switch
            {
                <= 80m => 20,
                <= 100m => 16,
                <= 120m => 10,
                _ => 4
            });
        }

        var averageScore = (int)Math.Round(utilizationScores.Average(), MidpointRounding.AwayFromZero);
        if (averageScore < 14)
        {
            suggestions.Add("Budget adherence is slipping. Tighten categories that are trending over plan.");
        }

        return new HealthScoreBreakdownItem(
            "Budget Adherence",
            averageScore,
            20,
            $"Average budget adherence score across {budgets.Count} budget(s) is {averageScore}/20.");
    }

    private static HealthScoreBreakdownItem BuildCashBufferScore(
        IReadOnlyList<Transaction> transactions,
        decimal currentBalance,
        List<string> suggestions)
    {
        var monthlyExpenseEstimate = transactions
            .Where(transaction => transaction.Type == TransactionType.EXPENSE)
            .Sum(transaction => transaction.Amount) / 3m;

        if (monthlyExpenseEstimate <= 0m)
        {
            return new HealthScoreBreakdownItem("Cash Buffer", 15, 35, "Not enough expense history to estimate cash runway.");
        }

        var monthsCovered = currentBalance / monthlyExpenseEstimate;
        var score = monthsCovered switch
        {
            >= 6m => 35,
            >= 3m => 28,
            >= 1.5m => 20,
            >= 1m => 14,
            _ => 8
        };

        if (monthsCovered < 2m)
        {
            suggestions.Add("Build at least two months of expense runway to reduce short-term risk.");
        }

        return new HealthScoreBreakdownItem(
            "Cash Buffer",
            score,
            35,
            $"Current balances cover approximately {monthsCovered:0.0} month(s) of recent expenses.");
    }
}
