# Session-Sentinal

## TR

Session-Sentinal, .NET API'leri için geliştirilmiş, esnek yapıda bir `net8.0` middleware kütüphanesidir. Gelen isteklerin risk skorunu hesaplar, aktif oturumları izler ve şüpheli token'ları gerçek zamanlı olarak revoke eder.

### Özellikler

- Domain, Application, Persistence, Infrastructure ve WebApi katmanlarına ayrılmış Onion Architecture yapısı
- MediatR + FluentValidation request pipeline
- Redis tabanlı aktif oturum ve blacklist servisleri
- `UseRedis = false` iken `IMemoryCache` fallback desteği
- SignalR ile gerçek zamanlı logout bildirimi
- Anomali kayıtları için EF Core persistence
- SQL persistence için background queue tabanlı anomaly logging
- Yeni oturumları kullanıcının son aktif oturumuna göre değerlendiren baseline kontrolü

### Entegrasyon

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

Başarılı bir login işleminden sonra aktif oturumu bir kez register etmek gerekir:

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

Sample host şu endpoint'leri sunar:

- `POST /auth/login` (`userName`, `password`, `fingerprintHash`)
- `POST /auth/logout` açık oturumu sonlandırmak için
- `GET /demo/me` JWT + Session-Sentinal ile korunan örnek endpoint
- `GET /admin/sessions` ve `POST /admin/revoke/{sessionId}` admin tarafından oturum listeleme ve revoke işlemleri için

Bir istek yeni bir `sessionId` ile geldiğinde, Session-Sentinal bu isteğin riskini kullanıcının son aktif oturumuna göre değerlendirir. Yüksek riskli yeni oturumlar, kullanıcının mevcut aktif oturumunu düşürmeden deny edilir ve blacklist'e alınır.

Örnek kullanıcı bilgileri:

- `demo` / `demo123`
- `admin` / `admin123`

### Sample Host

`samples/SessionSentinel.SampleHost`, tam bir identity provider kurmadan session oluşturma ve korumalı istek akışını test etmeyi sağlayan minimal bir ASP.NET Core host örneğidir.

Tarayıcı arayüzü şu akışları test etmenizi sağlar:

- login olup JWT almak
- `SentinelHub` bağlantısı kurmak
- korumalı endpoint çağrısı yapmak
- mevcut oturumu revoke edip logout event'ini görmek
- admin hesabı ile issued session listesini görmek ve farklı bir oturumu revoke etmek

Sample login endpoint'i ayrı bir `ISampleAuthService` üzerinden çalışır; böylece token üretimi, session registration ve issued-session tracking mantığı `Program.cs` dışına taşınmış olur.

### Redis Fallback

`UseRedis = false` olduğunda Session-Sentinal, aktif oturum store ve token blacklist için `IMemoryCache` implementasyonlarını kullanır.

- Bu mod local development ve tek instance dağıtımlar için uygundur.
- Çoklu node yapılar arasında state paylaşmaz.
- Dağıtık production ortamlarında Redis kullanılmaya devam edilmelidir.

---

## EN

Session-Sentinal is a pluggable `net8.0` middleware library for .NET APIs that scores request risk, tracks active sessions, and revokes suspicious tokens in real time.

### Features

- Onion Architecture split across Domain, Application, Persistence, Infrastructure, and WebApi projects
- MediatR + FluentValidation request pipeline
- Redis-backed active session and blacklist services
- `IMemoryCache` fallback when `UseRedis = false`
- SignalR logout notifications
- EF Core persistence for anomaly audit logs
- Background-queued anomaly logging for SQL persistence
- New-session baseline checks against the user's latest active session

### Integration

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

The sample host exposes:

- `POST /auth/login` with `userName`, `password`, and `fingerprintHash`
- `POST /auth/logout` for explicit session revocation
- `GET /demo/me` as an authenticated endpoint protected by JWT + Session-Sentinal
- `GET /admin/sessions` and `POST /admin/revoke/{sessionId}` for sample admin-driven revoke

When a request arrives with a new session id, Session-Sentinal compares it against the user's latest active session before allowing it to become the new baseline. High-risk new sessions are denied and blacklisted without revoking the user's current active session.

Sample credentials:

- `demo` / `demo123`
- `admin` / `admin123`

### Sample Host

`samples/SessionSentinel.SampleHost` contains a minimal ASP.NET Core host that exercises session creation and protected requests without needing a full identity provider.

It also serves a browser demo that can:

- log in and receive a JWT
- connect to `SentinelHub`
- call a protected endpoint
- revoke the current session and observe the logout event
- list issued sessions and revoke another session from an admin login

The sample login endpoint is wired through a dedicated `ISampleAuthService`, so token issuance, session registration, and issued-session tracking live outside of `Program.cs`.

### Redis Fallback

When `UseRedis = false`, Session-Sentinal registers `IMemoryCache` implementations for the active session store and token blacklist.

- This mode is suitable for local development and single-instance deployments.
- It does not share state across multiple application nodes.
- Distributed production deployments should continue to use Redis.
