using FinanceTracker.Api.Contracts;
using FinanceTracker.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/rules")]
public sealed class RulesController(RulesEngineService rulesEngineService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<RuleResponse>> GetAll(CancellationToken cancellationToken)
    {
        return rulesEngineService.GetAllAsync(cancellationToken);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RuleRequest request, CancellationToken cancellationToken)
    {
        var response = await rulesEngineService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{id}")]
    public Task<RuleResponse> Update(Guid id, [FromBody] RuleRequest request, CancellationToken cancellationToken)
    {
        return rulesEngineService.UpdateAsync(id, request, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await rulesEngineService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
