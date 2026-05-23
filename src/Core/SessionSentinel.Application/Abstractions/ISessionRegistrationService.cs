using SessionSentinel.Application.Contracts;

namespace SessionSentinel.Application.Abstractions;

public interface ISessionRegistrationService
{
    // Host applications call this after a successful login.
    Task RegisterAsync(RegisterSessionRequest request, CancellationToken cancellationToken = default);
}
