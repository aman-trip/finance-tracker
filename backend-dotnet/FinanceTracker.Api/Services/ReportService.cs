using FinanceTracker.Api.Contracts;
using FinanceTracker.Api.Data;
using FinanceTracker.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Api.Services;

public sealed class ReportService(
    FinanceTrackerDbContext dbContext,
    CurrentUserService currentUserService,
    AccessControlLayer accessControlLayer)
{
    public async Task<CategorySpendResponse> CategorySpendAsync(DateOnly? startDate, DateOnly? endDate, CancellationToken cancellationToken)
    {
        var range = NormalizeRange(startDate, endDate);
        var accountIds = await GetAccessibleAccountIdsAsync(cancellationToken);

        var transactions = await dbContext.Transactions
            .AsNoTracking()
            .Include(transaction => transaction.Category)
            .Where(transaction => accountIds.Contains(transaction.AccountId)
                                  && transaction.TransactionDate >= range.start
                                  && transaction.TransactionDate <= range.end)
            .ToListAsync(cancellationToken);

        var grouped = new Dictionary<string, decimal>();
        foreach (var transaction in transactions.Where(transaction => transaction.Type == TransactionType.EXPENSE))
        {
            var categoryName = transaction.Category?.Name ?? "Uncategorized";
            grouped[categoryName] = grouped.TryGetValue(categoryName, out var current)
                ? current + transaction.Amount
                : transaction.Amount;
        }

        var items = grouped
            .OrderByDescending(item => item.Value)
            .Select(item => new CategorySpendItem(item.Key, item.Value))
            .ToList();

        return new CategorySpendResponse(items, grouped.Values.Sum());
    }

    public async Task<IReadOnlyList<IncomeExpenseTrendItem>> IncomeExpenseTrendAsync(DateOnly? startDate, DateOnly? endDate, CancellationToken cancellationToken)
    {
        var range = NormalizeRange(startDate, endDate);
        var accountIds = await GetAccessibleAccountIdsAsync(cancellationToken);

        var transactions = await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => accountIds.Contains(transaction.AccountId)
                                  && transaction.TransactionDate >= range.start
                                  && transaction.TransactionDate <= range.end)
            .OrderBy(transaction => transaction.TransactionDate)
            .ToListAsync(cancellationToken);

        var grouped = new Dictionary<DateOnly, (decimal income, decimal expense)>();
        foreach (var transaction in transactions)
        {
            if (transaction.Type is not (TransactionType.INCOME or TransactionType.EXPENSE))
            {
                continue;
            }

            (decimal income, decimal expense) current = grouped.TryGetValue(transaction.TransactionDate, out var value)
                ? value
                : (income: 0m, expense: 0m);
            grouped[transaction.TransactionDate] = transaction.Type == TransactionType.INCOME
                ? (current.income + transaction.Amount, current.expense)
                : (current.income, current.expense + transaction.Amount);
        }

        return grouped
            .OrderBy(item => item.Key)
            .Select(item => new IncomeExpenseTrendItem(item.Key, item.Value.income, item.Value.expense))
            .ToList();
    }

    public async Task<IReadOnlyList<AccountBalanceTrendItem>> AccountBalanceTrendAsync(DateOnly? startDate, DateOnly? endDate, CancellationToken cancellationToken)
    {
        var range = NormalizeRange(startDate, endDate);
        var accountIds = await GetAccessibleAccountIdsAsync(cancellationToken);

        var accounts = await dbContext.Accounts
            .AsNoTracking()
            .Where(account => accountIds.Contains(account.Id))
            .OrderByDescending(account => account.CreatedAt)
            .ToListAsync(cancellationToken);

        var items = new List<AccountBalanceTrendItem>(accounts.Count);
        foreach (var account in accounts)
        {
            var running = account.OpeningBalance;
            var balanceAtRangeStart = running;
            var points = new List<Point>();
            var transactions = await dbContext.Transactions
                .AsNoTracking()
                .Where(transaction => transaction.AccountId == account.Id
                                      && transaction.TransactionDate <= range.end)
                .OrderBy(transaction => transaction.TransactionDate)
                .ToListAsync(cancellationToken);

            foreach (var transaction in transactions)
            {
                running += SignedAmount(transaction);

                if (transaction.TransactionDate < range.start)
                {
                    balanceAtRangeStart = running;
                    continue;
                }

                if (transaction.TransactionDate <= range.end)
                {
                    points.Add(new Point(transaction.TransactionDate, running));
                }
            }

            if (points.Count == 0)
            {
                points.Add(new Point(range.start, balanceAtRangeStart));
            }

            items.Add(new AccountBalanceTrendItem(account.Name, points));
        }

        return items;
    }

    public async Task<TrendsResponse> TrendsAsync(DateOnly? startDate, DateOnly? endDate, CancellationToken cancellationToken)
    {
        var range = NormalizeRange(startDate, endDate);
        var accountIds = await GetAccessibleAccountIdsAsync(cancellationToken);
        var incomeExpense = await IncomeExpenseTrendAsync(startDate, endDate, cancellationToken);
        var savings = incomeExpense
            .Select(item => new SavingsTrendPoint(
                item.date,
                item.income,
                item.expense,
                Math.Round(item.income - item.expense, 2, MidpointRounding.AwayFromZero)))
            .ToList();

        var transactions = await dbContext.Transactions
            .AsNoTracking()
            .Include(transaction => transaction.Category)
            .Where(transaction => accountIds.Contains(transaction.AccountId)
                                  && transaction.TransactionDate >= range.start
                                  && transaction.TransactionDate <= range.end
                                  && transaction.Type == TransactionType.EXPENSE)
            .ToListAsync(cancellationToken);

        var topCategories = transactions
            .GroupBy(transaction => transaction.Category?.Name ?? "Uncategorized")
            .Select(group => new
            {
                Category = group.Key,
                Total = group.Sum(transaction => transaction.Amount)
            })
            .OrderByDescending(item => item.Total)
            .Take(4)
            .Select(item => item.Category)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var categoryTrends = transactions
            .Where(transaction => topCategories.Contains(transaction.Category?.Name ?? "Uncategorized"))
            .GroupBy(transaction => transaction.Category?.Name ?? "Uncategorized")
            .OrderBy(group => group.Key)
            .Select(group => new CategoryTrendSeries(
                group.Key,
                group.GroupBy(transaction => transaction.TransactionDate)
                    .OrderBy(item => item.Key)
                    .Select(item => new TrendPoint(
                        item.Key,
                        Math.Round(item.Sum(transaction => transaction.Amount), 2, MidpointRounding.AwayFromZero)))
                    .ToList()))
            .ToList();

        return new TrendsResponse(incomeExpense, savings, categoryTrends);
    }

    public async Task<NetWorthResponse> NetWorthAsync(DateOnly? startDate, DateOnly? endDate, CancellationToken cancellationToken)
    {
        var range = NormalizeRange(startDate, endDate);
        var accountIds = await GetAccessibleAccountIdsAsync(cancellationToken);
        var accounts = await dbContext.Accounts
            .AsNoTracking()
            .Where(account => accountIds.Contains(account.Id))
            .ToListAsync(cancellationToken);

        if (accounts.Count == 0)
        {
            return new NetWorthResponse(0m, [new NetWorthPoint(range.start, 0m)]);
        }

        var transactions = await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => accountIds.Contains(transaction.AccountId)
                                  && transaction.TransactionDate <= range.end)
            .OrderBy(transaction => transaction.TransactionDate)
            .ToListAsync(cancellationToken);

        var running = accounts.Sum(account => account.OpeningBalance);
        var balanceAtRangeStart = running;
        var deltaByDate = new Dictionary<DateOnly, decimal>();

        foreach (var transaction in transactions)
        {
            running += SignedAmount(transaction);
            if (transaction.TransactionDate < range.start)
            {
                balanceAtRangeStart = running;
                continue;
            }

            deltaByDate[transaction.TransactionDate] = deltaByDate.TryGetValue(transaction.TransactionDate, out var current)
                ? current + SignedAmount(transaction)
                : SignedAmount(transaction);
        }

        var points = new List<NetWorthPoint>();
        var projected = balanceAtRangeStart;
        for (var date = range.start; date <= range.end; date = date.AddDays(1))
        {
            if (deltaByDate.TryGetValue(date, out var delta))
            {
                projected += delta;
            }

            points.Add(new NetWorthPoint(date, Math.Round(projected, 2, MidpointRounding.AwayFromZero)));
        }

        var currentNetWorth = Math.Round(accounts.Sum(account => account.CurrentBalance), 2, MidpointRounding.AwayFromZero);
        return new NetWorthResponse(currentNetWorth, points);
    }

    public async Task<IReadOnlyList<InsightItem>> InsightsAsync(CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var accountIds = await GetAccessibleAccountIdsAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentStart = new DateOnly(today.Year, today.Month, 1);
        var currentEnd = new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        var previousMonth = currentStart.AddMonths(-1);
        var previousStart = new DateOnly(previousMonth.Year, previousMonth.Month, 1);
        var previousEnd = new DateOnly(previousMonth.Year, previousMonth.Month, DateTime.DaysInMonth(previousMonth.Year, previousMonth.Month));

        var currentTransactions = await dbContext.Transactions
            .AsNoTracking()
            .Include(transaction => transaction.Category)
            .Where(transaction => accountIds.Contains(transaction.AccountId)
                                  && transaction.TransactionDate >= currentStart
                                  && transaction.TransactionDate <= currentEnd)
            .ToListAsync(cancellationToken);

        var insights = new List<InsightItem>();
        AddOverspendingInsight(insights, currentTransactions);
        await AddBudgetInsightsAsync(insights, userId, today, currentStart, currentEnd, cancellationToken);
        await AddMonthOverMonthInsightAsync(insights, accountIds, currentStart, currentEnd, previousStart, previousEnd, cancellationToken);
        EnsureMinimumInsights(insights);
        return insights.Take(5).ToList();
    }

    public async Task<FutureBalancePredictionResponse> FutureBalancePredictionAsync(CancellationToken cancellationToken)
    {
        const int horizonDays = 30;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var horizonEnd = today.AddDays(horizonDays - 1);
        var accountIds = await GetAccessibleAccountIdsAsync(cancellationToken);

        var currentBalance = await dbContext.Accounts
            .AsNoTracking()
            .Where(account => accountIds.Contains(account.Id))
            .Select(account => (decimal?)account.CurrentBalance)
            .SumAsync(cancellationToken) ?? 0m;

        var recurringTransactions = await dbContext.RecurringTransactions
            .AsNoTracking()
            .Where(recurring => accountIds.Contains(recurring.AccountId))
            .OrderBy(recurring => recurring.NextRunDate)
            .ToListAsync(cancellationToken);

        var projectedRecurringNet = recurringTransactions
            .Select(recurring => ProjectedRecurringContribution(recurring, today, horizonEnd))
            .Aggregate(0m, (current, amount) => current + amount);

        var spendStart = today.AddDays(-(horizonDays - 1));
        var totalRecentExpense = await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => accountIds.Contains(transaction.AccountId)
                                  && transaction.TransactionDate >= spendStart
                                  && transaction.TransactionDate <= today
                                  && transaction.Type == TransactionType.EXPENSE)
            .Select(transaction => (decimal?)transaction.Amount)
            .SumAsync(cancellationToken) ?? 0m;

        var averageDailySpending = Math.Round(totalRecentExpense / horizonDays, 2, MidpointRounding.AwayFromZero);
        var projectedVariableExpense = averageDailySpending * horizonDays;
        var predictedBalance = Math.Round(currentBalance + projectedRecurringNet - projectedVariableExpense, 2, MidpointRounding.AwayFromZero);

        return new FutureBalancePredictionResponse(
            Math.Round(currentBalance, 2, MidpointRounding.AwayFromZero),
            Math.Round(projectedRecurringNet, 2, MidpointRounding.AwayFromZero),
            averageDailySpending,
            predictedBalance,
            horizonDays
        );
    }

    private static decimal ProjectedRecurringContribution(RecurringTransaction recurring, DateOnly rangeStart, DateOnly rangeEnd)
    {
        if (recurring.NextRunDate > rangeEnd)
        {
            return 0m;
        }

        var effectiveEnd = recurring.EndDate is null || recurring.EndDate > rangeEnd
            ? rangeEnd
            : recurring.EndDate.Value;

        if (effectiveEnd < rangeStart)
        {
            return 0m;
        }

        var runDate = recurring.NextRunDate;
        while (runDate < rangeStart)
        {
            runDate = NextRunDate(runDate, recurring.Frequency);
            if (runDate > effectiveEnd)
            {
                return 0m;
            }
        }

        var occurrences = 0;
        while (runDate <= effectiveEnd)
        {
            occurrences++;
            runDate = NextRunDate(runDate, recurring.Frequency);
        }

        var signedAmount = recurring.Type is TransactionType.INCOME or TransactionType.TRANSFER_IN
            ? recurring.Amount
            : -recurring.Amount;

        return signedAmount * occurrences;
    }

    private static DateOnly NextRunDate(DateOnly date, RecurringFrequency frequency)
    {
        return frequency switch
        {
            RecurringFrequency.DAILY => date.AddDays(1),
            RecurringFrequency.WEEKLY => date.AddDays(7),
            RecurringFrequency.MONTHLY => date.AddMonths(1),
            RecurringFrequency.YEARLY => date.AddYears(1),
            _ => date
        };
    }

    private static void EnsureMinimumInsights(List<InsightItem> insights)
    {
        var defaults = new[]
        {
            new InsightItem("GENERAL", "No insights yet", "Add more transactions this month to generate personalized insights.", "INFO"),
            new InsightItem("GENERAL", "Track consistently", "Keep logging expenses weekly for more accurate month-over-month insights.", "INFO"),
            new InsightItem("GENERAL", "Set category budgets", "Budgets make overspending alerts and insights more useful.", "INFO")
        };

        foreach (var fallback in defaults)
        {
            if (insights.Count >= 3)
            {
                break;
            }

            if (insights.All(item => item.title != fallback.title))
            {
                insights.Add(fallback);
            }
        }
    }

    private static void AddOverspendingInsight(List<InsightItem> insights, IEnumerable<Transaction> transactions)
    {
        var expenseByCategory = new Dictionary<string, decimal>();
        foreach (var transaction in transactions)
        {
            if (transaction.Type != TransactionType.EXPENSE)
            {
                continue;
            }

            var categoryName = transaction.Category?.Name ?? "Uncategorized";
            expenseByCategory[categoryName] = expenseByCategory.TryGetValue(categoryName, out var current)
                ? current + transaction.Amount
                : transaction.Amount;
        }

        var topCategory = expenseByCategory.OrderByDescending(item => item.Value).FirstOrDefault();
        if (topCategory.Key is null)
        {
            return;
        }

        insights.Add(new InsightItem(
            "SPENDING",
            "Top spending category",
            $"{topCategory.Key} has the highest spend this month at {FormatCurrency(topCategory.Value)}.",
            "MEDIUM"));
    }

    private async Task AddBudgetInsightsAsync(
        List<InsightItem> insights,
        Guid userId,
        DateOnly today,
        DateOnly currentStart,
        DateOnly currentEnd,
        CancellationToken cancellationToken)
    {
        var budgets = await dbContext.Budgets
            .AsNoTracking()
            .Include(budget => budget.Category)
            .Where(budget => budget.UserId == userId
                             && budget.Month == today.Month
                             && budget.Year == today.Year)
            .ToListAsync(cancellationToken);

        var hasRisk = false;
        var riskInsightsAdded = 0;

        foreach (var budget in budgets)
        {
            var spent = await dbContext.Transactions
                .Where(transaction => transaction.UserId == userId
                                      && transaction.CategoryId == budget.CategoryId
                                      && transaction.Type == TransactionType.EXPENSE
                                      && transaction.TransactionDate >= currentStart
                                      && transaction.TransactionDate <= currentEnd)
                .Select(transaction => (decimal?)transaction.Amount)
                .SumAsync(cancellationToken) ?? 0m;

            if (budget.Amount <= 0)
            {
                continue;
            }

            var utilization = Math.Round(spent * 100m / budget.Amount, 1, MidpointRounding.AwayFromZero);
            if (utilization >= 100m)
            {
                hasRisk = true;
                if (riskInsightsAdded < 2)
                {
                    insights.Add(new InsightItem(
                        "BUDGET",
                        "Budget exceeded",
                        $"{budget.Category.Name} budget exceeded at {utilization:0.0}% ({FormatCurrency(spent)} spent).",
                        "HIGH"));
                    riskInsightsAdded++;
                }
            }
            else if (utilization >= 80m)
            {
                hasRisk = true;
                if (riskInsightsAdded < 2)
                {
                    insights.Add(new InsightItem(
                        "BUDGET",
                        "Budget nearing limit",
                        $"{budget.Category.Name} budget is at {utilization:0.0}% ({FormatCurrency(spent)} of {FormatCurrency(budget.Amount)}).",
                        "MEDIUM"));
                    riskInsightsAdded++;
                }
            }
        }

        if (budgets.Count != 0 && !hasRisk)
        {
            insights.Add(new InsightItem(
                "BUDGET",
                "Budgets are on track",
                "No category has crossed 80% of its monthly budget yet.",
                "LOW"));
        }
    }

    private async Task AddMonthOverMonthInsightAsync(
        List<InsightItem> insights,
        IReadOnlyList<Guid> accountIds,
        DateOnly currentStart,
        DateOnly currentEnd,
        DateOnly previousStart,
        DateOnly previousEnd,
        CancellationToken cancellationToken)
    {
        var currentExpense = await TotalExpenseForRangeAsync(accountIds, currentStart, currentEnd, cancellationToken);
        var previousExpense = await TotalExpenseForRangeAsync(accountIds, previousStart, previousEnd, cancellationToken);

        if (currentExpense == 0m && previousExpense == 0m)
        {
            return;
        }

        if (previousExpense == 0m)
        {
            insights.Add(new InsightItem(
                "TREND",
                "Monthly comparison unavailable",
                "Previous month has no expense data yet.",
                "INFO"));
            return;
        }

        var delta = currentExpense - previousExpense;
        var percentChange = Math.Round(delta * 100m / previousExpense, 1, MidpointRounding.AwayFromZero);

        if (delta > 0m)
        {
            insights.Add(new InsightItem(
                "TREND",
                "Spending increased",
                $"Expenses are up {percentChange:0.0}% vs last month.",
                "MEDIUM"));
        }
        else if (delta < 0m)
        {
            insights.Add(new InsightItem(
                "TREND",
                "Spending decreased",
                $"Expenses are down {Math.Abs(percentChange):0.0}% vs last month.",
                "LOW"));
        }
        else
        {
            insights.Add(new InsightItem(
                "TREND",
                "Spending is stable",
                "Expenses are unchanged compared to last month.",
                "LOW"));
        }
    }

    private async Task<decimal> TotalExpenseForRangeAsync(IReadOnlyList<Guid> accountIds, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        return await dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => accountIds.Contains(transaction.AccountId)
                                  && transaction.TransactionDate >= startDate
                                  && transaction.TransactionDate <= endDate
                                  && transaction.Type == TransactionType.EXPENSE)
            .Select(transaction => (decimal?)transaction.Amount)
            .SumAsync(cancellationToken) ?? 0m;
    }

    private async Task<IReadOnlyList<Guid>> GetAccessibleAccountIdsAsync(CancellationToken cancellationToken)
    {
        return await accessControlLayer.GetAccessibleAccountIdsAsync(currentUserService.GetCurrentUserId(), cancellationToken);
    }

    private static decimal SignedAmount(Transaction transaction)
    {
        return transaction.Type is TransactionType.INCOME or TransactionType.TRANSFER_IN
            ? transaction.Amount
            : -transaction.Amount;
    }

    private static string FormatCurrency(decimal amount) => $"${Math.Round(amount, 2, MidpointRounding.AwayFromZero):0.00}";

    private static (DateOnly start, DateOnly end) NormalizeRange(DateOnly? startDate, DateOnly? endDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var defaultStart = new DateOnly(today.Year, today.Month, 1);
        var defaultEnd = new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        var start = startDate ?? defaultStart;
        var end = endDate ?? defaultEnd;
        if (start > end)
        {
            (start, end) = (end, start);
        }

        return (start, end);
    }
}
