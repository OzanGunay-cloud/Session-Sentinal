namespace SessionSentinel.Infrastructure;

internal static class RedisKeyNames
{
    public static string Session(string sessionId) => $"session:{sessionId}";

    public static string UserLatestSession(string userId) => $"user-session:{userId}";

    public static string Blacklist(string tokenHash) => $"blacklist:{tokenHash}";
}
