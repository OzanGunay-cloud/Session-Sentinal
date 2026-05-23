using SessionSentinel.Domain.Models;

namespace SessionSentinel.Application.Contracts;

public sealed record RegisterSessionRequest(
    string SessionId,
    string UserId,
    string IpAddress,
    string FingerprintHash,
    string? UserAgent,
    string? Language,
    GeoPoint? Location,
    DateTime RequestedAtUtc);
