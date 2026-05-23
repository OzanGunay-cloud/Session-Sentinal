using SessionSentinel.Application.Contracts;

namespace SessionSentinel.Application.Abstractions;

public interface ISessionRevocationService
{
    // Host applications call this for explicit logout or forced admin revoke flows.
    Task RevokeAsync(RevokeSessionRequest request, CancellationToken cancellationToken = default);
}
