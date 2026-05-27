namespace SessionSentinel.SampleHost.Auth;

public sealed record SampleLoginContext(
    string IpAddress,
    string UserAgent,
    string Language,
    DateTime RequestedAtUtc);
