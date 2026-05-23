using MediatR;

namespace SessionSentinel.Application.Contracts;

public sealed record RevokeSessionCommand(
    string SessionId,
    string UserId,
    string TokenHash,
    DateTimeOffset? TokenExpiresAtUtc,
    string Reason,
    DateTime RequestedAtUtc) : IRequest;
