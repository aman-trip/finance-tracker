using FinanceTracker.Api.Contracts;
using FinanceTracker.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/forecast")]
public sealed class ForecastController(ForecastService forecastService) : ControllerBase
{
    [HttpGet("month")]
    public Task<ForecastMonthResponse> GetMonth(CancellationToken cancellationToken)
    {
        return forecastService.GetMonthForecastAsync(cancellationToken);
    }

    [HttpGet("daily")]
    public Task<ForecastDailyResponse> GetDaily(CancellationToken cancellationToken)
    {
        return forecastService.GetDailyForecastAsync(cancellationToken);
    }
}
