using FinanceTracker.Api.Contracts;
using FinanceTracker.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/accounts")]
public sealed class AccountController(
    AccountService accountService,
    AccountMembershipService accountMembershipService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AccountResponse>> GetAll(CancellationToken cancellationToken)
    {
        return accountService.GetAllAsync(cancellationToken);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AccountRequest request, CancellationToken cancellationToken)
    {
        var response = await accountService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{id}")]
    public Task<AccountResponse> Update(Guid id, [FromBody] AccountRequest request, CancellationToken cancellationToken)
    {
        return accountService.UpdateAsync(id, request, cancellationToken);
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request, CancellationToken cancellationToken)
    {
        await accountService.TransferAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/invite")]
    public async Task<IActionResult> Invite(Guid id, [FromBody] AccountInviteRequest request, CancellationToken cancellationToken)
    {
        var response = await accountMembershipService.InviteAsync(id, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("{id}/members")]
    public Task<IReadOnlyList<AccountMemberResponse>> GetMembers(Guid id, CancellationToken cancellationToken)
    {
        return accountMembershipService.GetMembersAsync(id, cancellationToken);
    }

    [HttpPut("{id}/members/{userId}")]
    public Task<AccountMemberResponse> UpdateMember(Guid id, Guid userId, [FromBody] AccountMemberUpdateRequest request, CancellationToken cancellationToken)
    {
        return accountMembershipService.UpdateRoleAsync(id, userId, request, cancellationToken);
    }
}
