namespace SessionSentinel.Application.Contracts;

public sealed record SessionRevocationNotification(string UserId, string SessionId, string Reason);
