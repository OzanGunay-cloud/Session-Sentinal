using Microsoft.AspNetCore.SignalR;

namespace SessionSentinel.Infrastructure.Realtime;

public sealed class SentinelHub : Hub
{
    public const string LogoutUserMethod = "LogoutUser";
}
