using MediatR;
using SessionSentinel.Application.Abstractions;
using SessionSentinel.Application.Contracts;

namespace SessionSentinel.Application.Services;

public sealed class SessionRegistrationService : ISessionRegistrationService
{
    private readonly ISender _sender;

    public SessionRegistrationService(ISender sender)
    {
        _sender = sender;
    }

    public Task RegisterAsync(RegisterSessionRequest request, CancellationToken cancellationToken = default)
    {
        // Registration is forwarded into the existing application command pipeline.
        return _sender.Send(
            new CreateSessionCommand(
                request.SessionId,
                request.UserId,
                request.IpAddress,
                request.FingerprintHash,
                request.UserAgent,
                request.Language,
                request.Location,
                request.RequestedAtUtc),
            cancellationToken);
    }
}
