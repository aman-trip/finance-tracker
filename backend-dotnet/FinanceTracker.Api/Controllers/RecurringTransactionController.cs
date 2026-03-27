using FinanceTracker.Api.Contracts;
using FinanceTracker.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/recurring")]
public sealed class RecurringTransactionController(RecurringTransactionService recurringTransactionService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<RecurringTransactionResponse>> GetAll(CancellationToken cancellationToken)
    {
        return recurringTransactionService.GetAllAsync(cancellationToken);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RecurringTransactionRequest request, CancellationToken cancellationToken)
    {
        var response = await recurringTransactionService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{id}")]
    public Task<RecurringTransactionResponse> Update(Guid id, [FromBody] RecurringTransactionRequest request, CancellationToken cancellationToken)
    {
        return recurringTransactionService.UpdateAsync(id, request, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await recurringTransactionService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
