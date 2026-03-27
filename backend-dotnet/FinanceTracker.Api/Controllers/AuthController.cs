using FinanceTracker.Api.Contracts;
using FinanceTracker.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.RegisterAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("login")]
    public Task<AuthResponse> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        return authService.LoginAsync(request, cancellationToken);
    }

    [HttpPost("refresh")]
    public Task<AuthResponse> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        return authService.RefreshAsync(request, cancellationToken);
    }

    [HttpPost("forgot-password")]
    public Task<MessageResponse> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        return authService.ForgotPasswordAsync(request, cancellationToken);
    }

    [HttpPost("reset-password")]
    public Task<MessageResponse> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        return authService.ResetPasswordAsync(request, cancellationToken);
    }
}
