using SessionSentinel.Application.Abstractions;
using StackExchange.Redis;

namespace SessionSentinel.Infrastructure.Caching;

public sealed class RedisTokenBlacklistService : ITokenBlacklistService
{
    private readonly IDatabase _database;

    public RedisTokenBlacklistService(IConnectionMultiplexer connectionMultiplexer)
    {
        _database = connectionMultiplexer.GetDatabase();
    }

    public async Task<bool> IsBlacklistedAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        await _database.KeyExistsAsync(RedisKeyNames.Blacklist(tokenHash));

    public Task BlacklistAsync(
        string tokenHash,
        DateTimeOffset? tokenExpiresAtUtc,
        TimeSpan fallbackTtl,
        CancellationToken cancellationToken = default)
    {
        var ttl = ResolveTtl(tokenExpiresAtUtc, fallbackTtl);
        return _database.StringSetAsync(RedisKeyNames.Blacklist(tokenHash), "1", ttl);
    }

    private static TimeSpan ResolveTtl(DateTimeOffset? tokenExpiresAtUtc, TimeSpan fallbackTtl)
    {
        if (tokenExpiresAtUtc is null)
        {
            return fallbackTtl;
        }

        var ttl = tokenExpiresAtUtc.Value - DateTimeOffset.UtcNow;
        return ttl > TimeSpan.Zero ? ttl : TimeSpan.FromSeconds(1);
    }
}
