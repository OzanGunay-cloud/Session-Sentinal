namespace SessionSentinel.SampleHost.Auth;

public sealed class SampleJwtOptions
{
    public const string SectionName = "SampleJwt";

    public string Issuer { get; set; } = "SessionSentinel.SampleHost";

    public string Audience { get; set; } = "SessionSentinel.SampleHost.Client";

    public string SigningKey { get; set; } = "session-sentinel-sample-signing-key-2026";

    public int AccessTokenMinutes { get; set; } = 60;
}
