using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using SessionSentinel.Application.Options;

namespace SessionSentinel.Infrastructure.Realtime;

public sealed class SentinelUserIdProvider : IUserIdProvider
{
    private static readonly string[] FallbackClaims = { "sub", "nameid", System.Security.Claims.ClaimTypes.NameIdentifier };
    private readonly SessionSentinelOptions _options;

    public SentinelUserIdProvider(IOptions<SessionSentinelOptions> options)
    {
        _options = options.Value;
    }

    public string? GetUserId(HubConnectionContext connection)
    {
        if (!string.IsNullOrWhiteSpace(_options.IdentityClaimType))
        {
            var configured = connection.User?.FindFirst(_options.IdentityClaimType)?.Value;
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured;
            }
        }

        foreach (var claimType in FallbackClaims)
        {
            var value = connection.User?.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
