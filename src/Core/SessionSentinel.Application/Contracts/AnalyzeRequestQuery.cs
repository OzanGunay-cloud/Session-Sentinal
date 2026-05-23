using MediatR;

namespace SessionSentinel.Application.Contracts;

public sealed record AnalyzeRequestQuery(
    string SessionId,
    string UserId,
    string TokenHash,
    DateTimeOffset? TokenExpiresAtUtc,
    string IpAddress,
    string? UserAgent,
    string? Language,
    string FingerprintHash,
    DateTime RequestedAtUtc) : IRequest<AnalyzeRequestResult>;
