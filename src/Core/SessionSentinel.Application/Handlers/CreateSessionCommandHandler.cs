using MediatR;
using Microsoft.Extensions.Options;
using SessionSentinel.Application.Abstractions;
using SessionSentinel.Application.Contracts;
using SessionSentinel.Application.Options;
using SessionSentinel.Domain.Entities;

namespace SessionSentinel.Application.Handlers;

public sealed class CreateSessionCommandHandler : IRequestHandler<CreateSessionCommand>
{
    private readonly ISentinelSessionStore _sessionStore;
    private readonly SessionSentinelOptions _options;

    public CreateSessionCommandHandler(ISentinelSessionStore sessionStore, IOptions<SessionSentinelOptions> options)
    {
        _sessionStore = sessionStore;
        _options = options.Value;
    }

    public async Task<Unit> Handle(CreateSessionCommand request, CancellationToken cancellationToken)
    {
        var session = new UserSession
        {
            SessionId = request.SessionId,
            UserId = request.UserId,
            IpAddress = request.IpAddress,
            FingerprintHash = request.FingerprintHash,
            UserAgent = request.UserAgent,
            Language = request.Language,
            Latitude = request.Location?.Latitude,
            Longitude = request.Location?.Longitude,
            CreatedAtUtc = request.RequestedAtUtc,
            LastRequestAtUtc = request.RequestedAtUtc,
            IsActive = true
        };

        await _sessionStore.UpsertAsync(session, _options.ActiveSessionTtl, cancellationToken);
        return Unit.Value;
    }
}
