using FinanceTracker.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("actuator")]
public sealed class HealthController : ControllerBase
{
    [HttpGet("health")]
    public HealthResponse GetHealth()
    {
        return new HealthResponse("UP");
    }
}
