using Microsoft.Extensions.Caching.Memory;
using SessionSentinel.Domain.Entities;
using SessionSentinel.Infrastructure.Caching;

namespace SessionSentinel.Infrastructure.Tests;

public sealed class MemoryStoreTests
{
    [Fact]
    public async Task Session_store_round_trips_sessions()
    {
        var store = new MemorySentinelSessionStore(new MemoryCache(new MemoryCacheOptions()));

        await store.UpsertAsync(
            new UserSession
            {
                SessionId = "session-1",
                UserId = "user-1",
                IpAddress = "1.1.1.1",
                FingerprintHash = "fingerprint"
            },
            TimeSpan.FromMinutes(1));

        var session = await store.GetSessionAsync("session-1");

        Assert.NotNull(session);
        Assert.Equal("user-1", session.UserId);
    }

    [Fact]
    public async Task Token_blacklist_respects_ttl()
    {
        var service = new MemoryTokenBlacklistService(new MemoryCache(new MemoryCacheOptions()));

        await service.BlacklistAsync("token", null, TimeSpan.FromMilliseconds(50));
        Assert.True(await service.IsBlacklistedAsync("token"));

        await Task.Delay(200);
        Assert.False(await service.IsBlacklistedAsync("token"));
    }
}
