namespace FinanceTracker.Api.Contracts;

public sealed record HealthScoreBreakdownItem(
    string metric,
    int score,
    int maxScore,
    string detail
);

public sealed record HealthScoreResponse(
    int score,
    IReadOnlyList<HealthScoreBreakdownItem> breakdown,
    IReadOnlyList<string> suggestions
);

public sealed record InsightsOverviewResponse(
    HealthScoreResponse healthScore,
    IReadOnlyList<InsightItem> highlights
);
