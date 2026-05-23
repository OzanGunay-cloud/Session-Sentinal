namespace SessionSentinel.Domain;

public enum TriggeredRule
{
    MissingFingerprint = 1,
    TokenBlacklisted = 2,
    IpMismatch = 3,
    FingerprintMismatch = 4,
    ImpossibleTravel = 5
}
