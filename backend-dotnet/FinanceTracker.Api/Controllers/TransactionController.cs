using FinanceTracker.Api.Contracts;
using FinanceTracker.Api.Entities;
using FinanceTracker.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/transactions")]
public sealed class TransactionController(TransactionService transactionService) : ControllerBase
{
    [HttpGet]
    public Task<PageResponse<TransactionResponse>> GetAll(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? accountId,
        [FromQuery] TransactionType? type,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] int page = 0,
        [FromQuery] int size = 10,
        CancellationToken cancellationToken = default)
    {
        return transactionService.SearchAsync(search, categoryId, accountId, type, startDate, endDate, page, size, cancellationToken);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TransactionRequest request, CancellationToken cancellationToken)
    {
        var response = await transactionService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("{id}")]
    public Task<TransactionResponse> GetById(Guid id, CancellationToken cancellationToken)
    {
        return transactionService.GetByIdAsync(id, cancellationToken);
    }

    [HttpPut("{id}")]
    public Task<TransactionResponse> Update(Guid id, [FromBody] TransactionRequest request, CancellationToken cancellationToken)
    {
        return transactionService.UpdateAsync(id, request, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await transactionService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
