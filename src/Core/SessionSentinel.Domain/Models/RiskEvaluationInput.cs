namespace SessionSentinel.Domain.Models;

public sealed record RiskEvaluationInput(
    string CurrentIpAddress,
    string StoredIpAddress,
    string CurrentFingerprintHash,
    string StoredFingerprintHash,
    GeoPoint? CurrentLocation,
    GeoPoint? StoredLocation,
    DateTime RequestedAtUtc,
    DateTime LastRequestAtUtc,
    double ImpossibleTravelSpeedThresholdKph);
