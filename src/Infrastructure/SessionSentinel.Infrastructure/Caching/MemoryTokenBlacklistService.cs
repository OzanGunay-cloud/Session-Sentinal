using Microsoft.Extensions.Caching.Memory;
using SessionSentinel.Application.Abstractions;

namespace SessionSentinel.Infrastructure.Caching;

public sealed class MemoryTokenBlacklistService : ITokenBlacklistService
{
    private readonly IMemoryCache _memoryCache;

    public MemoryTokenBlacklistService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task<bool> IsBlacklistedAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        Task.FromResult(_memoryCache.TryGetValue(RedisKeyNames.Blacklist(tokenHash), out _));

    public Task BlacklistAsync(
        string tokenHash,
        DateTimeOffset? tokenExpiresAtUtc,
        TimeSpan fallbackTtl,
        CancellationToken cancellationToken = default)
    {
        _memoryCache.Set(
            RedisKeyNames.Blacklist(tokenHash),
            true,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ResolveTtl(tokenExpiresAtUtc, fallbackTtl)
            });

        return Task.CompletedTask;
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
