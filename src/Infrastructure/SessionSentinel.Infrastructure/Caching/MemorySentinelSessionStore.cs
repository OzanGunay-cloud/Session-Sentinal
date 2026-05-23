using Microsoft.Extensions.Caching.Memory;
using SessionSentinel.Application.Abstractions;
using SessionSentinel.Domain.Entities;

namespace SessionSentinel.Infrastructure.Caching;

public sealed class MemorySentinelSessionStore : ISentinelSessionStore
{
    private readonly IMemoryCache _memoryCache;

    public MemorySentinelSessionStore(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task<UserSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _memoryCache.TryGetValue(RedisKeyNames.Session(sessionId), out UserSession? session);
        return Task.FromResult(session);
    }

    public async Task<UserSession?> GetLatestActiveSessionForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        _memoryCache.TryGetValue(RedisKeyNames.UserLatestSession(userId), out string? sessionId);
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        var session = await GetSessionAsync(sessionId, cancellationToken);
        return session is { IsActive: true } ? session : null;
    }

    public Task UpsertAsync(UserSession session, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        _memoryCache.Set(
            RedisKeyNames.Session(session.SessionId),
            session,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            });

        _memoryCache.Set(
            RedisKeyNames.UserLatestSession(session.UserId),
            session.SessionId,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            });

        return Task.CompletedTask;
    }

    public Task RevokeAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(RedisKeyNames.Session(sessionId));
        return Task.CompletedTask;
    }
}
