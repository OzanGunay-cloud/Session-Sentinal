namespace SessionSentinel.Application.Options;

public sealed class SessionSentinelOptions
{
    public const string SectionName = "SessionSentinel";

    public int RiskThreshold { get; set; } = 80;

    public bool UseRedis { get; set; }

    public bool TrackImpossibleTravel { get; set; } = true;

    public string? IdentityClaimType { get; set; }

    public double ImpossibleTravelSpeedThresholdKph { get; set; } = 900d;

    public TimeSpan ActiveSessionTtl { get; set; } = TimeSpan.FromHours(12);

    public TimeSpan FallbackBlacklistTtl { get; set; } = TimeSpan.FromHours(1);

    public string? RedisConnectionString { get; set; }

    public string? SqlServerConnectionString { get; set; }

    public string? GeoIpBaseUrl { get; set; }

    public int AnomalyQueueCapacity { get; set; } = 256;

    public string HubRoute { get; set; } = "/hubs/session-sentinel";

    public string[] ExcludedPathPrefixes { get; set; } =
    [
        "/swagger",
        "/health",
        "/favicon.ico"
    ];
}
