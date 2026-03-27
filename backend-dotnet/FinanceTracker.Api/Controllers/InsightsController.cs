using FinanceTracker.Api.Contracts;
using FinanceTracker.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/insights")]
public sealed class InsightsController(InsightsService insightsService) : ControllerBase
{
    [HttpGet]
    public Task<InsightsOverviewResponse> GetOverview(CancellationToken cancellationToken)
    {
        return insightsService.GetOverviewAsync(cancellationToken);
    }

    [HttpGet("health-score")]
    public Task<HealthScoreResponse> GetHealthScore(CancellationToken cancellationToken)
    {
        return insightsService.GetHealthScoreAsync(cancellationToken);
    }
}
