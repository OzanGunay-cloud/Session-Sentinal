using System.Security.Claims;
using SessionSentinel.Application.Abstractions;

namespace SessionSentinel.WebApi.Identity;

public sealed class DefaultUserIdentityAccessor : IUserIdentityAccessor
{
    private static readonly string[] FallbackClaims =
    {
        "sub",
        "nameid",
        ClaimTypes.NameIdentifier
    };

    public string? GetUserId(ClaimsPrincipal principal, string? preferredClaimType = null)
    {
        if (!string.IsNullOrWhiteSpace(preferredClaimType))
        {
            var preferred = principal.FindFirst(preferredClaimType)?.Value;
            if (!string.IsNullOrWhiteSpace(preferred))
            {
                return preferred;
            }
        }

        foreach (var claimType in FallbackClaims)
        {
            var value = principal.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
