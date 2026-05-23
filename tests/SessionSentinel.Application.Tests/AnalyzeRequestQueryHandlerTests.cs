using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using SessionSentinel.Application.Abstractions;
using SessionSentinel.Application.Contracts;
using SessionSentinel.Application.Handlers;
using SessionSentinel.Application.Options;
using SessionSentinel.Domain;
using SessionSentinel.Domain.Entities;
using SessionSentinel.Domain.Models;
using SessionSentinel.Domain.Rules;

namespace SessionSentinel.Application.Tests;

public sealed class AnalyzeRequestQueryHandlerTests
{
    [Fact]
    public async Task Handle_denies_blacklisted_tokens()
    {
        var blacklist = new FakeTokenBlacklistService { IsBlacklisted = true };
        var handler = CreateHandler(tokenBlacklistService: blacklist);

        var result = await handler.Handle(CreateRequest(), CancellationToken.None);

        Assert.False(result.IsAllowed);
        Assert.Contains(TriggeredRule.TokenBlacklisted, result.TriggeredRules);
    }

    [Fact]
    public async Task Handle_denies_requests_with_missing_fingerprint()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(CreateRequest(fingerprintHash: string.Empty), CancellationToken.None);

        Assert.False(result.IsAllowed);
        Assert.Contains(TriggeredRule.MissingFingerprint, result.TriggeredRules);
    }

    [Fact]
    public async Task Handle_creates_new_session_when_none_exists()
    {
        var sessionStore = new FakeSessionStore();
        var handler = CreateHandler(sessionStore: sessionStore);

        var result = await handler.Handle(CreateRequest(), CancellationToken.None);

        Assert.True(result.IsAllowed);
        Assert.NotNull(await sessionStore.GetSessionAsync("session-1"));
    }

    [Fact]
    public async Task Handle_revokes_session_when_threshold_is_exceeded()
    {
        var sessionStore = new FakeSessionStore();
        await sessionStore.UpsertAsync(
            new UserSession
            {
                SessionId = "session-1",
                UserId = "user-1",
                IpAddress = "1.1.1.1",
                FingerprintHash = "old-fingerprint",
                LastRequestAtUtc = DateTime.UtcNow.AddMinutes(-5),
                IsActive = true
            },
            TimeSpan.FromMinutes(5));

        var blacklist = new FakeTokenBlacklistService();
        var notifications = new FakeNotificationService();
        var anomalyWriter = new FakeAnomalyLogWriter();
        var handler = CreateHandler(
            sessionStore: sessionStore,
            tokenBlacklistService: blacklist,
            anomalyLogWriter: anomalyWriter,
            notificationService: notifications,
            options: new SessionSentinelOptions { RiskThreshold = 50, TrackImpossibleTravel = false });

        var result = await handler.Handle(CreateRequest(fingerprintHash: "new-fingerprint"), CancellationToken.None);

        Assert.False(result.IsAllowed);
        Assert.Contains("token-hash", blacklist.BlacklistedTokens);
        Assert.Single(notifications.Notifications);
        Assert.Single(anomalyWriter.Logs);
    }

    [Fact]
    public async Task Handle_allows_matching_session_when_geo_provider_returns_no_coordinates()
    {
        var sessionStore = new FakeSessionStore();
        await sessionStore.UpsertAsync(
            new UserSession
            {
                SessionId = "session-1",
                UserId = "user-1",
                IpAddress = "1.1.1.1",
                FingerprintHash = "fingerprint",
                LastRequestAtUtc = DateTime.UtcNow.AddMinutes(-5),
                IsActive = true
            },
            TimeSpan.FromMinutes(5));

        var handler = CreateHandler(sessionStore: sessionStore);
        var result = await handler.Handle(CreateRequest(), CancellationToken.None);

        Assert.True(result.IsAllowed);
        Assert.Equal(0, result.RiskScore);
    }

    private static AnalyzeRequestQueryHandler CreateHandler(
        FakeSessionStore? sessionStore = null,
        FakeTokenBlacklistService? tokenBlacklistService = null,
        FakeGeoLocationService? geoLocationService = null,
        FakeAnomalyLogWriter? anomalyLogWriter = null,
        FakeNotificationService? notificationService = null,
        SessionSentinelOptions? options = null)
    {
        return new AnalyzeRequestQueryHandler(
            sessionStore ?? new FakeSessionStore(),
            tokenBlacklistService ?? new FakeTokenBlacklistService(),
            geoLocationService ?? new FakeGeoLocationService(),
            anomalyLogWriter ?? new FakeAnomalyLogWriter(),
            notificationService ?? new FakeNotificationService(),
            Microsoft.Extensions.Options.Options.Create(options ?? new SessionSentinelOptions()),
            new RiskScoreCalculator(),
            NullLogger<AnalyzeRequestQueryHandler>.Instance);
    }

    private static AnalyzeRequestQuery CreateRequest(string fingerprintHash = "fingerprint") =>
        new(
            "session-1",
            "user-1",
            "token-hash",
            DateTimeOffset.UtcNow.AddHours(1),
            "1.1.1.1",
            "agent",
            "en-US",
            fingerprintHash,
            DateTime.UtcNow);

    private sealed class FakeSessionStore : ISentinelSessionStore
    {
        private readonly Dictionary<string, UserSession> _sessions = new(StringComparer.Ordinal);

        public Task<UserSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return Task.FromResult(session);
        }

        public Task UpsertAsync(UserSession session, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            _sessions[session.SessionId] = session;
            return Task.CompletedTask;
        }

        public Task RevokeAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            _sessions.Remove(sessionId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTokenBlacklistService : ITokenBlacklistService
    {
        public bool IsBlacklisted { get; set; }

        public List<string> BlacklistedTokens { get; } = new();

        public Task<bool> IsBlacklistedAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            Task.FromResult(IsBlacklisted);

        public Task BlacklistAsync(string tokenHash, DateTimeOffset? tokenExpiresAtUtc, TimeSpan fallbackTtl, CancellationToken cancellationToken = default)
        {
            BlacklistedTokens.Add(tokenHash);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGeoLocationService : IGeoLocationService
    {
        public GeoPoint? PointToReturn { get; set; }

        public Task<GeoPoint?> ResolveAsync(string ipAddress, CancellationToken cancellationToken = default) =>
            Task.FromResult(PointToReturn);
    }

    private sealed class FakeAnomalyLogWriter : IAnomalyLogWriter
    {
        public List<AnomalyLog> Logs { get; } = new();

        public Task WriteAsync(AnomalyLog anomalyLog, CancellationToken cancellationToken = default)
        {
            Logs.Add(anomalyLog);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeNotificationService : ISignalRNotificationService
    {
        public List<SessionRevocationNotification> Notifications { get; } = new();

        public Task NotifySessionRevokedAsync(SessionRevocationNotification notification, CancellationToken cancellationToken = default)
        {
            Notifications.Add(notification);
            return Task.CompletedTask;
        }
    }
}
