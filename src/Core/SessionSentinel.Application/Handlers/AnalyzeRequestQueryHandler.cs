using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SessionSentinel.Application.Abstractions;
using SessionSentinel.Application.Contracts;
using SessionSentinel.Application.Options;
using SessionSentinel.Domain;
using SessionSentinel.Domain.Entities;
using SessionSentinel.Domain.Models;
using SessionSentinel.Domain.Rules;

namespace SessionSentinel.Application.Handlers;

public sealed class AnalyzeRequestQueryHandler : IRequestHandler<AnalyzeRequestQuery, AnalyzeRequestResult>
{
    private readonly ISentinelSessionStore _sessionStore;
    private readonly ITokenBlacklistService _tokenBlacklistService;
    private readonly IGeoLocationService _geoLocationService;
    private readonly IAnomalyLogWriter _anomalyLogWriter;
    private readonly ISignalRNotificationService _notificationService;
    private readonly SessionSentinelOptions _options;
    private readonly RiskScoreCalculator _riskScoreCalculator;
    private readonly ILogger<AnalyzeRequestQueryHandler> _logger;

    public AnalyzeRequestQueryHandler(
        ISentinelSessionStore sessionStore,
        ITokenBlacklistService tokenBlacklistService,
        IGeoLocationService geoLocationService,
        IAnomalyLogWriter anomalyLogWriter,
        ISignalRNotificationService notificationService,
        IOptions<SessionSentinelOptions> options,
        RiskScoreCalculator riskScoreCalculator,
        ILogger<AnalyzeRequestQueryHandler> logger)
    {
        _sessionStore = sessionStore;
        _tokenBlacklistService = tokenBlacklistService;
        _geoLocationService = geoLocationService;
        _anomalyLogWriter = anomalyLogWriter;
        _notificationService = notificationService;
        _options = options.Value;
        _riskScoreCalculator = riskScoreCalculator;
        _logger = logger;
    }

    public async Task<AnalyzeRequestResult> Handle(AnalyzeRequestQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FingerprintHash))
        {
            return AnalyzeRequestResult.Deny(
                "Missing fingerprint header.",
                100,
                new[] { TriggeredRule.MissingFingerprint });
        }

        if (await _tokenBlacklistService.IsBlacklistedAsync(request.TokenHash, cancellationToken))
        {
            return AnalyzeRequestResult.Deny(
                "Token is blacklisted.",
                100,
                new[] { TriggeredRule.TokenBlacklisted });
        }

        var currentLocation = await ResolveLocationAsync(request.IpAddress, cancellationToken);
        var existingSession = await _sessionStore.GetSessionAsync(request.SessionId, cancellationToken);

        if (existingSession is null || !existingSession.IsActive)
        {
            // First authenticated request becomes the baseline session snapshot.
            await _sessionStore.UpsertAsync(
                CreateSession(request, currentLocation),
                _options.ActiveSessionTtl,
                cancellationToken);

            _logger.LogInformation("SessionSentinel created a new active session snapshot for user {UserId}.", request.UserId);
            return AnalyzeRequestResult.Allow(0);
        }

        var evaluationResult = _riskScoreCalculator.Evaluate(
            new RiskEvaluationInput(
                request.IpAddress,
                existingSession.IpAddress,
                request.FingerprintHash,
                existingSession.FingerprintHash,
                currentLocation,
                existingSession.GetLastKnownLocation(),
                request.RequestedAtUtc,
                existingSession.LastRequestAtUtc,
                _options.ImpossibleTravelSpeedThresholdKph));

        if (evaluationResult.Score >= _options.RiskThreshold)
        {
            // High-risk requests are revoked immediately and logged asynchronously.
            await _tokenBlacklistService.BlacklistAsync(
                request.TokenHash,
                request.TokenExpiresAtUtc,
                _options.FallbackBlacklistTtl,
                cancellationToken);

            existingSession.Revoke(request.RequestedAtUtc);
            await _sessionStore.RevokeAsync(request.SessionId, cancellationToken);

            await _notificationService.NotifySessionRevokedAsync(
                new SessionRevocationNotification(request.UserId, request.SessionId, "Risk threshold exceeded."),
                cancellationToken);

            await _anomalyLogWriter.WriteAsync(
                CreateAnomalyLog(request, evaluationResult),
                cancellationToken);

            _logger.LogWarning(
                "SessionSentinel revoked session {SessionId} for user {UserId}. RiskScore={RiskScore}, Rules={Rules}.",
                request.SessionId,
                request.UserId,
                evaluationResult.Score,
                string.Join(',', evaluationResult.TriggeredRules));
            return AnalyzeRequestResult.Deny("Risk threshold exceeded.", evaluationResult.Score, evaluationResult.TriggeredRules);
        }

        existingSession.UpdateLastSeen(
            request.IpAddress,
            request.FingerprintHash,
            request.UserAgent,
            request.Language,
            currentLocation,
            request.RequestedAtUtc);

        await _sessionStore.UpsertAsync(existingSession, _options.ActiveSessionTtl, cancellationToken);
        _logger.LogDebug("SessionSentinel refreshed session {SessionId} for user {UserId}.", request.SessionId, request.UserId);
        return AnalyzeRequestResult.Allow(evaluationResult.Score, evaluationResult.TriggeredRules);
    }

    private async Task<GeoPoint?> ResolveLocationAsync(string ipAddress, CancellationToken cancellationToken)
    {
        if (!_options.TrackImpossibleTravel)
        {
            return null;
        }

        return await _geoLocationService.ResolveAsync(ipAddress, cancellationToken);
    }

    private static UserSession CreateSession(AnalyzeRequestQuery request, GeoPoint? currentLocation) =>
        new()
        {
            SessionId = request.SessionId,
            UserId = request.UserId,
            IpAddress = request.IpAddress,
            FingerprintHash = request.FingerprintHash,
            UserAgent = request.UserAgent,
            Language = request.Language,
            Latitude = currentLocation?.Latitude,
            Longitude = currentLocation?.Longitude,
            CreatedAtUtc = request.RequestedAtUtc,
            LastRequestAtUtc = request.RequestedAtUtc,
            IsActive = true
        };

    private static AnomalyLog CreateAnomalyLog(AnalyzeRequestQuery request, RiskEvaluationResult evaluationResult) =>
        new()
        {
            SessionId = request.SessionId,
            UserId = request.UserId,
            RiskScore = evaluationResult.Score,
            TriggeredRule = string.Join(',', evaluationResult.TriggeredRules),
            Details = $"Request from IP '{request.IpAddress}' exceeded threshold.",
            CreatedAtUtc = request.RequestedAtUtc
        };
}
