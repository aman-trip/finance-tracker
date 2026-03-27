namespace FinanceTracker.Api.Contracts;

public sealed record CategorySpendItem(
    string category,
    decimal amount
);

public sealed record CategorySpendResponse(
    IReadOnlyList<CategorySpendItem> items,
    decimal total
);

public sealed record IncomeExpenseTrendItem(
    DateOnly date,
    decimal income,
    decimal expense
);

public sealed record Point(
    DateOnly date,
    decimal balance
);

public sealed record AccountBalanceTrendItem(
    string accountName,
    IReadOnlyList<Point> points
);

public sealed record TrendPoint(
    DateOnly date,
    decimal amount
);

public sealed record InsightItem(
    string type,
    string title,
    string description,
    string severity
);

public sealed record FutureBalancePredictionResponse(
    decimal currentBalance,
    decimal projectedRecurringNet,
    decimal averageDailySpending,
    decimal predictedBalance,
    int horizonDays
);

public sealed record SavingsTrendPoint(
    DateOnly date,
    decimal income,
    decimal expense,
    decimal savings
);

public sealed record CategoryTrendSeries(
    string category,
    IReadOnlyList<TrendPoint> points
);

public sealed record TrendsResponse(
    IReadOnlyList<IncomeExpenseTrendItem> incomeExpense,
    IReadOnlyList<SavingsTrendPoint> savings,
    IReadOnlyList<CategoryTrendSeries> categoryTrends
);

public sealed record NetWorthPoint(
    DateOnly date,
    decimal netWorth
);

public sealed record NetWorthResponse(
    decimal currentNetWorth,
    IReadOnlyList<NetWorthPoint> points
);
