using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SessionSentinel.Application.Abstractions;
using SessionSentinel.Application.Contracts;
using SessionSentinel.Application.Options;
using SessionSentinel.SampleHost.Auth;
using SessionSentinel.WebApi;
using SessionSentinel.WebApi.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SampleJwtOptions>(
    builder.Configuration.GetSection(SampleJwtOptions.SectionName));

var jwtOptions = builder.Configuration
    .GetSection(SampleJwtOptions.SectionName)
    .Get<SampleJwtOptions>() ?? new SampleJwtOptions();
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // The sample validates the JWT that it issues itself.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<SampleUserStore>();
builder.Services.AddSingleton<SampleJwtTokenService>();
builder.Services.AddSingleton<SampleIssuedSessionStore>();
builder.Services.AddScoped<ISampleAuthService, SampleAuthService>();
builder.Services.AddSessionSentinel(options =>
{
    options.UseRedis = false;
    options.TrackImpossibleTravel = true;
    options.GeoIpBaseUrl = "https://ipwho.is/";
});

var app = builder.Build();

// Static sample UI makes the end-to-end flow easy to exercise.
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseSessionSentinel();

app.MapPost(
    "/auth/login",
    async (
        HttpContext context,
        SampleLoginRequest request,
        ISampleAuthService authService,
        CancellationToken cancellationToken) =>
    {
        var token = await authService.LoginAsync(
            request,
            new SampleLoginContext(
                ClientIpResolver.Resolve(context),
                context.Request.Headers["User-Agent"].ToString(),
                context.Request.Headers["Accept-Language"].ToString(),
                DateTime.UtcNow),
            cancellationToken);

        return token is null ? Results.Unauthorized() : Results.Ok(token);
    });

app.MapGet("/demo/ping", () => Results.Ok(new { Message = "Session Sentinel sample host is running." }));
app.MapGet(
    "/demo/me",
    (HttpContext context, IUserIdentityAccessor userIdentityAccessor) =>
        Results.Ok(new
        {
            // The sample endpoint uses the same user-id fallback chain as the middleware.
            UserId = userIdentityAccessor.GetUserId(context.User),
            SessionId = context.User.FindFirst("jti")?.Value
        }))
    .RequireAuthorization();
app.MapPost(
    "/auth/logout",
    async (HttpContext context,
        RevokeHttpSessionRequest request,
        SampleIssuedSessionStore issuedSessionStore,
        ISessionRevocationService sessionRevocationService,
        IUserIdentityAccessor userIdentityAccessor,
        ITokenHasher tokenHasher,
        Microsoft.Extensions.Options.IOptions<SessionSentinel.Application.Options.SessionSentinelOptions> options,
        CancellationToken cancellationToken) =>
    {
        var result = await context.RevokeCurrentSessionAsync(
            request,
            sessionRevocationService,
            userIdentityAccessor,
            tokenHasher,
            options,
            cancellationToken);

        var currentSessionId = context.User.FindFirst("jti")?.Value;
        if (!string.IsNullOrWhiteSpace(currentSessionId))
        {
            issuedSessionStore.Remove(currentSessionId, out _);
        }

        return result;
    })
    .RequireAuthorization();
app.MapGet(
    "/admin/sessions",
    (HttpContext context, SampleIssuedSessionStore issuedSessionStore) =>
    {
        if (!string.Equals(context.User.FindFirst("unique_name")?.Value, "admin", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Forbid();
        }

        return Results.Ok(issuedSessionStore.GetAll());
    })
    .RequireAuthorization();
app.MapPost(
    "/admin/revoke/{sessionId}",
    async (
        HttpContext context,
        string sessionId,
        RevokeHttpSessionRequest request,
        SampleIssuedSessionStore issuedSessionStore,
        ISessionRevocationService sessionRevocationService,
        ITokenHasher tokenHasher,
        CancellationToken cancellationToken) =>
    {
        if (!string.Equals(context.User.FindFirst("unique_name")?.Value, "admin", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Forbid();
        }

        if (!issuedSessionStore.TryGet(sessionId, out var targetSession) || targetSession is null)
        {
            return Results.NotFound(new { SessionId = sessionId });
        }

        await sessionRevocationService.RevokeAsync(
            new RevokeSessionRequest(
                targetSession.SessionId,
                targetSession.UserId,
                tokenHasher.HashToken(targetSession.AccessToken),
                targetSession.ExpiresAtUtc,
                string.IsNullOrWhiteSpace(request.Reason) ? "Admin revoke" : request.Reason,
                DateTime.UtcNow),
            cancellationToken);

        issuedSessionStore.Remove(sessionId, out _);
        return Results.Ok(new { Revoked = true, SessionId = sessionId });
    })
    .RequireAuthorization();
app.MapSessionSentinelHub();

app.Run();
