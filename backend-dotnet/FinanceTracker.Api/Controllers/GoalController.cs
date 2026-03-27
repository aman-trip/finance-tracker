using FinanceTracker.Api.Contracts;
using FinanceTracker.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/goals")]
public sealed class GoalController(GoalService goalService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<GoalResponse>> GetAll(CancellationToken cancellationToken)
    {
        return goalService.GetAllAsync(cancellationToken);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] GoalRequest request, CancellationToken cancellationToken)
    {
        var response = await goalService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{id}")]
    public Task<GoalResponse> Update(Guid id, [FromBody] GoalRequest request, CancellationToken cancellationToken)
    {
        return goalService.UpdateAsync(id, request, cancellationToken);
    }

    [HttpPost("{id}/contribute")]
    public Task<GoalResponse> Contribute(Guid id, [FromBody] GoalContributionRequest request, CancellationToken cancellationToken)
    {
        return goalService.ContributeAsync(id, request, cancellationToken);
    }

    [HttpPost("{id}/withdraw")]
    public Task<GoalResponse> Withdraw(Guid id, [FromBody] GoalContributionRequest request, CancellationToken cancellationToken)
    {
        return goalService.WithdrawAsync(id, request, cancellationToken);
    }
}
