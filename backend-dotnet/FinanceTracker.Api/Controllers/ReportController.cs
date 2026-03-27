using FinanceTracker.Api.Contracts;
using FinanceTracker.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public sealed class ReportController(ReportService reportService) : ControllerBase
{
    [HttpGet("category-spend")]
    public Task<CategorySpendResponse> CategorySpend([FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate, CancellationToken cancellationToken)
    {
        return reportService.CategorySpendAsync(startDate, endDate, cancellationToken);
    }

    [HttpGet("income-vs-expense")]
    public Task<IReadOnlyList<IncomeExpenseTrendItem>> IncomeVsExpense([FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate, CancellationToken cancellationToken)
    {
        return reportService.IncomeExpenseTrendAsync(startDate, endDate, cancellationToken);
    }

    [HttpGet("account-balance-trend")]
    public Task<IReadOnlyList<AccountBalanceTrendItem>> AccountBalanceTrend([FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate, CancellationToken cancellationToken)
    {
        return reportService.AccountBalanceTrendAsync(startDate, endDate, cancellationToken);
    }

    [HttpGet("insights")]
    public Task<IReadOnlyList<InsightItem>> Insights(CancellationToken cancellationToken)
    {
        return reportService.InsightsAsync(cancellationToken);
    }

    [HttpGet("future-balance-prediction")]
    public Task<FutureBalancePredictionResponse> FutureBalancePrediction(CancellationToken cancellationToken)
    {
        return reportService.FutureBalancePredictionAsync(cancellationToken);
    }

    [HttpGet("trends")]
    public Task<TrendsResponse> Trends([FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate, CancellationToken cancellationToken)
    {
        return reportService.TrendsAsync(startDate, endDate, cancellationToken);
    }

    [HttpGet("net-worth")]
    public Task<NetWorthResponse> NetWorth([FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate, CancellationToken cancellationToken)
    {
        return reportService.NetWorthAsync(startDate, endDate, cancellationToken);
    }
}
