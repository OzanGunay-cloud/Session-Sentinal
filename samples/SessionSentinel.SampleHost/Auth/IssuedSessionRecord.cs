namespace SessionSentinel.SampleHost.Auth;

public sealed record IssuedSessionRecord(
    string SessionId,
    string UserId,
    string UserName,
    string AccessToken,
    DateTime ExpiresAtUtc);
