using FinanceTracker.Api.Contracts;
using FinanceTracker.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/budgets")]
public sealed class BudgetController(BudgetService budgetService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<BudgetResponse>> GetAll(CancellationToken cancellationToken)
    {
        return budgetService.GetAllAsync(cancellationToken);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BudgetRequest request, CancellationToken cancellationToken)
    {
        var response = await budgetService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{id}")]
    public Task<BudgetResponse> Update(Guid id, [FromBody] BudgetRequest request, CancellationToken cancellationToken)
    {
        return budgetService.UpdateAsync(id, request, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await budgetService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
