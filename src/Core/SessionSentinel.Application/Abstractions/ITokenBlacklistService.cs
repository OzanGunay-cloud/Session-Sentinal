namespace SessionSentinel.Application.Abstractions;

public interface ITokenBlacklistService
{
    Task<bool> IsBlacklistedAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task BlacklistAsync(
        string tokenHash,
        DateTimeOffset? tokenExpiresAtUtc,
        TimeSpan fallbackTtl,
        CancellationToken cancellationToken = default);
}
