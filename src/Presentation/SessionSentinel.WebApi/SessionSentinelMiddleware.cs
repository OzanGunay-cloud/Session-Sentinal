using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SessionSentinel.Application.Abstractions;
using SessionSentinel.Application.Contracts;
using SessionSentinel.Application.Options;

namespace SessionSentinel.WebApi;

public sealed class SessionSentinelMiddleware
{
    public const string FingerprintHeaderName = "X-Sentinel-Fingerprint";

    private readonly RequestDelegate _next;
    private readonly ILogger<SessionSentinelMiddleware> _logger;

    public SessionSentinelMiddleware(RequestDelegate next, ILogger<SessionSentinelMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ISender sender,
        ITokenHasher tokenHasher,
        IUserIdentityAccessor userIdentityAccessor,
        IOptions<SessionSentinelOptions> options)
    {
        if (ShouldSkip(context, options.Value))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        if (!TryReadBearerToken(context, out var token))
        {
            _logger.LogWarning("SessionSentinel denied request because no bearer token was present for path {Path}.", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var userId = userIdentityAccessor.GetUserId(context.User, options.Value.IdentityClaimType);
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("SessionSentinel denied request because no user identifier could be resolved for path {Path}.", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var tokenHash = tokenHasher.HashToken(token);
        var result = await sender.Send(
            new AnalyzeRequestQuery(
                JwtTokenReader.ResolveSessionId(context.User, tokenHash),
                userId,
                tokenHash,
                JwtTokenReader.ResolveExpiry(context.User, token),
                ClientIpResolver.Resolve(context),
                context.Request.Headers["User-Agent"].ToString(),
                context.Request.Headers["Accept-Language"].ToString(),
                context.Request.Headers[FingerprintHeaderName].ToString(),
                DateTime.UtcNow),
            context.RequestAborted);

        if (!result.IsAllowed)
        {
            _logger.LogWarning(
                "SessionSentinel denied request for user {UserId} on path {Path}. RiskScore={RiskScore}, Reason={Reason}.",
                userId,
                context.Request.Path,
                result.RiskScore,
                result.DenyReason);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await _next(context);
    }

    private static bool ShouldSkip(HttpContext context, SessionSentinelOptions options)
    {
        var path = context.Request.Path.Value;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        foreach (var excludedPrefix in options.ExcludedPathPrefixes)
        {
            if (!string.IsNullOrWhiteSpace(excludedPrefix) &&
                path.StartsWith(excludedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return context.Request.Path.StartsWithSegments(options.HubRoute, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryReadBearerToken(HttpContext context, out string token)
    {
        token = string.Empty;
        var authorization = context.Request.Headers["Authorization"].ToString();

        if (string.IsNullOrWhiteSpace(authorization) ||
            !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        token = authorization["Bearer ".Length..].Trim();
        return !string.IsNullOrWhiteSpace(token);
    }
}
