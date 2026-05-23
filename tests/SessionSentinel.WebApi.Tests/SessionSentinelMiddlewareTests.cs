using System.Net;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SessionSentinel.Application.Abstractions;
using SessionSentinel.Application.Contracts;
using SessionSentinel.Application.Options;
using SessionSentinel.Infrastructure.Caching;
using SessionSentinel.Infrastructure.Services;
using SessionSentinel.WebApi;
using SessionSentinel.WebApi.Http;
using SessionSentinel.WebApi.Identity;

namespace SessionSentinel.WebApi.Tests;

public sealed class SessionSentinelMiddlewareTests
{
    [Fact]
    public async Task Middleware_skips_anonymous_requests()
    {
        var nextCalled = false;
        var middleware = new SessionSentinelMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, NullLogger<SessionSentinelMiddleware>.Instance);

        var context = new DefaultHttpContext();
        await middleware.InvokeAsync(
            context,
            new FakeSender(AnalyzeRequestResult.Allow(0)),
            new Sha256TokenHasher(),
            new DefaultUserIdentityAccessor(),
            Options.Create(new SessionSentinelOptions()));

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Middleware_returns_unauthorized_when_fingerprint_is_missing()
    {
        var middleware = new SessionSentinelMiddleware(_ => Task.CompletedTask, NullLogger<SessionSentinelMiddleware>.Instance);
        var context = CreateAuthenticatedContext(includeFingerprint: false);

        await middleware.InvokeAsync(
            context,
            new FakeSender(AnalyzeRequestResult.Deny("Missing fingerprint", 100)),
            new Sha256TokenHasher(),
            new DefaultUserIdentityAccessor(),
            Options.Create(new SessionSentinelOptions()));

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task Middleware_allows_authenticated_requests_with_valid_fingerprint()
    {
        var nextCalled = false;
        var middleware = new SessionSentinelMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, NullLogger<SessionSentinelMiddleware>.Instance);

        var sender = new FakeSender(AnalyzeRequestResult.Allow(0));
        var context = CreateAuthenticatedContext(includeFingerprint: true);

        await middleware.InvokeAsync(
            context,
            sender,
            new Sha256TokenHasher(),
            new DefaultUserIdentityAccessor(),
            Options.Create(new SessionSentinelOptions()));

        Assert.True(nextCalled);
        Assert.NotNull(sender.LastRequest);
    }

    [Fact]
    public void AddSessionSentinel_uses_memory_fallback_when_redis_is_disabled()
    {
        var services = new ServiceCollection();

        services.AddSessionSentinel(options => options.UseRedis = false);

        using var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<ISentinelSessionStore>();

        Assert.IsType<MemorySentinelSessionStore>(store);
    }

    [Fact]
    public void AddSessionSentinel_throws_when_redis_is_enabled_without_connection_string()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSessionSentinel(options => options.UseRedis = true);

        using var provider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<SessionSentinelOptions>>().Value);
    }

    [Fact]
    public async Task Middleware_skips_excluded_paths_even_when_authenticated()
    {
        var nextCalled = false;
        var middleware = new SessionSentinelMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, NullLogger<SessionSentinelMiddleware>.Instance);

        var context = CreateAuthenticatedContext(includeFingerprint: false);
        context.Request.Path = "/swagger/index.html";

        await middleware.InvokeAsync(
            context,
            new FakeSender(AnalyzeRequestResult.Deny("should not run", 100)),
            new Sha256TokenHasher(),
            new DefaultUserIdentityAccessor(),
            Options.Create(new SessionSentinelOptions()));

        Assert.True(nextCalled);
    }

    [Fact]
    public void AddSessionSentinel_validates_invalid_options_on_resolution()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSessionSentinel(options => options.RiskThreshold = 0);

        using var provider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<SessionSentinelOptions>>().Value);
    }

    [Fact]
    public async Task RegisterSessionAsync_uses_authenticated_user_context()
    {
        var context = CreateAuthenticatedContext(includeFingerprint: true);
        var service = new FakeSessionRegistrationService();

        var result = await context.RegisterSessionAsync(
            new RegisterHttpSessionRequest("fingerprint"),
            service,
            new DefaultUserIdentityAccessor(),
            new Sha256TokenHasher(),
            Options.Create(new SessionSentinelOptions()));

        Assert.NotNull(result);
        Assert.NotNull(service.Request);
        Assert.Equal("session-1", service.Request.SessionId);
        Assert.Equal("user-1", service.Request.UserId);
    }

    [Fact]
    public async Task RevokeCurrentSessionAsync_uses_authenticated_user_context()
    {
        var context = CreateAuthenticatedContext(includeFingerprint: true);
        var service = new FakeSessionRevocationService();

        var result = await context.RevokeCurrentSessionAsync(
            new RevokeHttpSessionRequest("logout"),
            service,
            new DefaultUserIdentityAccessor(),
            new Sha256TokenHasher(),
            Options.Create(new SessionSentinelOptions()));

        Assert.NotNull(result);
        Assert.NotNull(service.Request);
        Assert.Equal("session-1", service.Request.SessionId);
        Assert.Equal("user-1", service.Request.UserId);
        Assert.Equal("logout", service.Request.Reason);
    }

    private static DefaultHttpContext CreateAuthenticatedContext(bool includeFingerprint)
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim("sub", "user-1"),
                new Claim("jti", "session-1")
            },
            authenticationType: "Bearer");

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        context.Request.Headers["Authorization"] = "Bearer header.payload.signature";
        if (includeFingerprint)
        {
            context.Request.Headers[SessionSentinelMiddleware.FingerprintHeaderName] = "fingerprint";
        }

        return context;
    }

    private sealed class FakeSender : ISender
    {
        private readonly AnalyzeRequestResult _result;

        public FakeSender(AnalyzeRequestResult result)
        {
            _result = result;
        }

        public AnalyzeRequestQuery? LastRequest { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            LastRequest = request as AnalyzeRequestQuery;
            return Task.FromResult((TResponse)(object)_result);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            LastRequest = request as AnalyzeRequestQuery;
            return Task.FromResult<object?>(_result);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            Empty<TResponse>();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            Empty<object?>();

        private static async IAsyncEnumerable<T> Empty<T>()
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private sealed class FakeSessionRegistrationService : ISessionRegistrationService
    {
        public RegisterSessionRequest? Request { get; private set; }

        public Task RegisterAsync(RegisterSessionRequest request, CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSessionRevocationService : ISessionRevocationService
    {
        public RevokeSessionRequest? Request { get; private set; }

        public Task RevokeAsync(RevokeSessionRequest request, CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.CompletedTask;
        }
    }
}
