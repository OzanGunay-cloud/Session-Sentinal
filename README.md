# Session-Sentinel

Session-Sentinel is a pluggable `net8.0` middleware library for .NET APIs that scores request risk, tracks active sessions, and revokes suspicious tokens in real time.

## Features

- Onion Architecture split across Domain, Application, Persistence, Infrastructure, and WebApi projects
- MediatR + FluentValidation request pipeline
- Redis-backed active session and blacklist services
- `IMemoryCache` fallback when `UseRedis = false`
- SignalR logout notifications
- EF Core persistence for anomaly audit logs
- Background-queued anomaly logging for SQL persistence

## Integration

```csharp
builder.Services.AddSessionSentinel(options =>
{
    options.RiskThreshold = 80;
    options.UseRedis = false;
    options.TrackImpossibleTravel = true;
    options.SqlServerConnectionString = builder.Configuration.GetConnectionString("SessionSentinel");
});

var app = builder.Build();

app.UseSessionSentinel();
app.MapSessionSentinelHub();
```

After a successful login, register the active session once:

```csharp
app.MapPost("/auth/session", async (
    HttpContext context,
    RegisterHttpSessionRequest request,
    ISessionRegistrationService sessionRegistrationService,
    IUserIdentityAccessor userIdentityAccessor,
    ITokenHasher tokenHasher,
    IOptions<SessionSentinelOptions> options,
    CancellationToken cancellationToken) =>
    await context.RegisterSessionAsync(
        request,
        sessionRegistrationService,
        userIdentityAccessor,
        tokenHasher,
        options,
        cancellationToken));
```

The sample host now exposes:

- `POST /auth/login` with `userName`, `password`, and `fingerprintHash`
- `POST /auth/logout` for explicit session revocation
- `GET /demo/me` as an authenticated endpoint protected by JWT + Session-Sentinel
- `GET /admin/sessions` and `POST /admin/revoke/{sessionId}` for sample admin-driven revoke

Sample credentials:

- `demo` / `demo123`
- `admin` / `admin123`

## Sample Host

`samples/SessionSentinel.SampleHost` contains a minimal ASP.NET Core host that exercises session creation and protected requests without needing a full identity provider.

It also serves a browser demo at `/` that can:

- log in and receive a JWT
- connect to `SentinelHub`
- call a protected endpoint
- revoke the current session and observe the logout event
- list issued sessions and revoke another session from an admin login

## Redis Fallback

When `UseRedis = false`, Session-Sentinel registers `IMemoryCache` implementations for the active session store and token blacklist.

- This mode is suitable for local development and single-instance deployments.
- It does not share state across multiple application nodes.
- Distributed production deployments should continue to use Redis.
