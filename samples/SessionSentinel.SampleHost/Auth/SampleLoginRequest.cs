namespace SessionSentinel.SampleHost.Auth;

public sealed record SampleLoginRequest(string UserName, string Password, string FingerprintHash);
