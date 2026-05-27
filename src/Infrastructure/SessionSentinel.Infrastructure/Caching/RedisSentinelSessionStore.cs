using System.Text.Json;
using SessionSentinel.Application.Abstractions;
using SessionSentinel.Domain.Entities;
using StackExchange.Redis;

namespace SessionSentinel.Infrastructure.Caching;

public sealed class RedisSentinelSessionStore : ISentinelSessionStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IDatabase _database;

    public RedisSentinelSessionStore(IConnectionMultiplexer connectionMultiplexer)
    {
        _database = connectionMultiplexer.GetDatabase();
    }

    public async Task<UserSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var payload = await _database.StringGetAsync(RedisKeyNames.Session(sessionId));
        if (payload.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<UserSession>(payload!, SerializerOptions);
    }

    public async Task<UserSession?> GetLatestActiveSessionForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var sessionId = await _database.StringGetAsync(RedisKeyNames.UserLatestSession(userId));
        if (sessionId.IsNullOrEmpty)
        {
            return null;
        }

        var session = await GetSessionAsync(sessionId!, cancellationToken);
        return session is { IsActive: true } ? session : null;
    }

    public Task UpsertAsync(UserSession session, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(session, SerializerOptions);
        var batch = _database.CreateBatch();
        batch.StringSetAsync(RedisKeyNames.Session(session.SessionId), payload, ttl);
        batch.StringSetAsync(RedisKeyNames.UserLatestSession(session.UserId), session.SessionId, ttl);
        batch.Execute();
        return Task.CompletedTask;
    }

    public async Task RevokeAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);
        await _database.KeyDeleteAsync(RedisKeyNames.Session(sessionId));

        if (session is null)
        {
            return;
        }

        var latestSessionKey = RedisKeyNames.UserLatestSession(session.UserId);
        var latestSessionId = await _database.StringGetAsync(latestSessionKey);
        if (!latestSessionId.IsNullOrEmpty && string.Equals(latestSessionId!, sessionId, StringComparison.Ordinal))
        {
            await _database.KeyDeleteAsync(latestSessionKey);
        }
    }
}
