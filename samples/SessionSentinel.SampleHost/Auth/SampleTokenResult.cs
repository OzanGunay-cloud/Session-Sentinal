namespace SessionSentinel.SampleHost.Auth;

public sealed record SampleTokenResult(string AccessToken, string SessionId, DateTime ExpiresAtUtc);
