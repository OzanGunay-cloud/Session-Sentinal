using SessionSentinel.Application.Contracts;

namespace SessionSentinel.Application.Abstractions;

public interface ISignalRNotificationService
{
    Task NotifySessionRevokedAsync(
        SessionRevocationNotification notification,
        CancellationToken cancellationToken = default);
}
