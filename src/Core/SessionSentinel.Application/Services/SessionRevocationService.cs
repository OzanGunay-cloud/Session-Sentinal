using MediatR;
using SessionSentinel.Application.Abstractions;
using SessionSentinel.Application.Contracts;

namespace SessionSentinel.Application.Services;

public sealed class SessionRevocationService : ISessionRevocationService
{
    private readonly ISender _sender;

    public SessionRevocationService(ISender sender)
    {
        _sender = sender;
    }

    public Task RevokeAsync(RevokeSessionRequest request, CancellationToken cancellationToken = default)
    {
        // Revocation reuses the application command pipeline.
        return _sender.Send(
            new RevokeSessionCommand(
                request.SessionId,
                request.UserId,
                request.TokenHash,
                request.TokenExpiresAtUtc,
                request.Reason,
                request.RequestedAtUtc),
            cancellationToken);
    }
}
