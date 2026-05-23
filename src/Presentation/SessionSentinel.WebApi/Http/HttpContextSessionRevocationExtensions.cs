using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SessionSentinel.Application.Abstractions;
using SessionSentinel.Application.Contracts;
using SessionSentinel.Application.Options;

namespace SessionSentinel.WebApi.Http;

public static class HttpContextSessionRevocationExtensions
{
    public static async Task<IResult> RevokeCurrentSessionAsync(
        this HttpContext context,
        RevokeHttpSessionRequest request,
        ISessionRevocationService sessionRevocationService,
        IUserIdentityAccessor userIdentityAccessor,
        ITokenHasher tokenHasher,
        IOptions<SessionSentinelOptions> options,
        CancellationToken cancellationToken = default)
    {
        var userId = userIdentityAccessor.GetUserId(context.User, options.Value.IdentityClaimType);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        var authorization = context.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrWhiteSpace(authorization) ||
            !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Unauthorized();
        }

        var rawToken = authorization["Bearer ".Length..].Trim();
        var tokenHash = tokenHasher.HashToken(rawToken);
        var sessionId = context.User.FindFirst("jti")?.Value ?? tokenHash;

        await sessionRevocationService.RevokeAsync(
            new RevokeSessionRequest(
                sessionId,
                userId,
                tokenHash,
                JwtTokenReader.ResolveExpiry(context.User, rawToken),
                string.IsNullOrWhiteSpace(request.Reason) ? "User logout" : request.Reason,
                DateTime.UtcNow),
            cancellationToken);

        return Results.Ok(new { Revoked = true, SessionId = sessionId });
    }
}
