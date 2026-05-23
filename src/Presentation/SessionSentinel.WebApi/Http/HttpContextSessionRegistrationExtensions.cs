using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SessionSentinel.Application.Abstractions;
using SessionSentinel.Application.Contracts;
using SessionSentinel.Application.Options;
using SessionSentinel.WebApi.Identity;

namespace SessionSentinel.WebApi.Http;

public static class HttpContextSessionRegistrationExtensions
{
    public static async Task<IResult> RegisterSessionAsync(
        this HttpContext context,
        RegisterHttpSessionRequest request,
        ISessionRegistrationService sessionRegistrationService,
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
        var sessionId = context.User.FindFirst("jti")?.Value;
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            sessionId = !string.IsNullOrWhiteSpace(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? tokenHasher.HashToken(authorization["Bearer ".Length..].Trim())
                : Guid.NewGuid().ToString("N");
        }

        await sessionRegistrationService.RegisterAsync(
            new RegisterSessionRequest(
                sessionId,
                userId,
                ClientIpResolver.Resolve(context),
                request.FingerprintHash,
                context.Request.Headers["User-Agent"].ToString(),
                context.Request.Headers["Accept-Language"].ToString(),
                null,
                DateTime.UtcNow),
            cancellationToken);

        return Results.Ok(new { Registered = true, SessionId = sessionId });
    }
}
