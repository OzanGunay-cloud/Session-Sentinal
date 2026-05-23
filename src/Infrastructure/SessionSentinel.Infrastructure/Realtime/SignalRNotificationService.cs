using Microsoft.AspNetCore.SignalR;
using SessionSentinel.Application.Abstractions;
using SessionSentinel.Application.Contracts;

namespace SessionSentinel.Infrastructure.Realtime;

public sealed class SignalRNotificationService : ISignalRNotificationService
{
    private readonly IHubContext<SentinelHub> _hubContext;

    public SignalRNotificationService(IHubContext<SentinelHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifySessionRevokedAsync(
        SessionRevocationNotification notification,
        CancellationToken cancellationToken = default) =>
        _hubContext.Clients.User(notification.UserId).SendAsync(
            SentinelHub.LogoutUserMethod,
            new
            {
                notification.SessionId,
                notification.Reason
            },
            cancellationToken);
}
