namespace FinanceTracker.Api.Contracts;

public sealed record ForecastMonthResponse(
    decimal currentBalance,
    decimal forecastBalance,
    decimal safeToSpend,
    decimal averageDailySpend,
    decimal recurringExpenses,
    string risk,
    int horizonDays
);

public sealed record ForecastDailyPointResponse(
    DateOnly date,
    decimal projectedBalance,
    decimal safeToSpend,
    string risk
);

public sealed record ForecastDailyResponse(
    IReadOnlyList<ForecastDailyPointResponse> points,
    decimal forecastBalance,
    decimal safeToSpend,
    string risk
);
