using System.Security.Claims;
using FinanceTracker.Api.Exceptions;

namespace FinanceTracker.Api.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor)
{
    public Guid GetCurrentUserId()
    {
        var rawUserId = httpContextAccessor.HttpContext?.User.FindFirstValue("uid");
        if (!Guid.TryParse(rawUserId, out var userId))
        {
            throw new UnauthorizedException("Authentication required");
        }

        return userId;
    }
}
