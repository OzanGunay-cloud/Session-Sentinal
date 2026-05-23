using System.Security.Claims;

namespace SessionSentinel.Application.Abstractions;

public interface IUserIdentityAccessor
{
    string? GetUserId(ClaimsPrincipal principal, string? preferredClaimType = null);
}
