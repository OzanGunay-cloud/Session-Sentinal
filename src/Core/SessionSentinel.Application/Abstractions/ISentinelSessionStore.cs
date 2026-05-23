using SessionSentinel.Domain.Entities;

namespace SessionSentinel.Application.Abstractions;

public interface ISentinelSessionStore
{
    Task<UserSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    Task<UserSession?> GetLatestActiveSessionForUserAsync(string userId, CancellationToken cancellationToken = default);

    Task UpsertAsync(UserSession session, TimeSpan ttl, CancellationToken cancellationToken = default);

    Task RevokeAsync(string sessionId, CancellationToken cancellationToken = default);
}
