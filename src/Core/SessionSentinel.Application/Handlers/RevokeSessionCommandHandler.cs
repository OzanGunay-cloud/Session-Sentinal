using MediatR;
using Microsoft.Extensions.Options;
using SessionSentinel.Application.Abstractions;
using SessionSentinel.Application.Contracts;
using SessionSentinel.Application.Options;

namespace SessionSentinel.Application.Handlers;

public sealed class RevokeSessionCommandHandler : IRequestHandler<RevokeSessionCommand>
{
    private readonly ISentinelSessionStore _sessionStore;
    private readonly ITokenBlacklistService _tokenBlacklistService;
    private readonly ISignalRNotificationService _notificationService;
    private readonly SessionSentinelOptions _options;

    public RevokeSessionCommandHandler(
        ISentinelSessionStore sessionStore,
        ITokenBlacklistService tokenBlacklistService,
        ISignalRNotificationService notificationService,
        IOptions<SessionSentinelOptions> options)
    {
        _sessionStore = sessionStore;
        _tokenBlacklistService = tokenBlacklistService;
        _notificationService = notificationService;
        _options = options.Value;
    }

    public async Task<Unit> Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
    {
        await _sessionStore.RevokeAsync(request.SessionId, cancellationToken);
        await _tokenBlacklistService.BlacklistAsync(
            request.TokenHash,
            request.TokenExpiresAtUtc,
            _options.FallbackBlacklistTtl,
            cancellationToken);

        await _notificationService.NotifySessionRevokedAsync(
            new SessionRevocationNotification(request.UserId, request.SessionId, request.Reason),
            cancellationToken);

        return Unit.Value;
    }
}
