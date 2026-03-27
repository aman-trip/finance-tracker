using FinanceTracker.Api.Contracts;
using FinanceTracker.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/categories")]
public sealed class CategoryController(CategoryService categoryService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<CategoryResponse>> GetAll(CancellationToken cancellationToken)
    {
        return categoryService.GetAllAsync(cancellationToken);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategoryRequest request, CancellationToken cancellationToken)
    {
        var response = await categoryService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{id}")]
    public Task<CategoryResponse> Update(Guid id, [FromBody] CategoryRequest request, CancellationToken cancellationToken)
    {
        return categoryService.UpdateAsync(id, request, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await categoryService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
