namespace SessionSentinel.Application.Contracts;

public sealed record RevokeSessionRequest(
    string SessionId,
    string UserId,
    string TokenHash,
    DateTimeOffset? TokenExpiresAtUtc,
    string Reason,
    DateTime RequestedAtUtc);
