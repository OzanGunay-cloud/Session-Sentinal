using System.Collections.Concurrent;

namespace SessionSentinel.SampleHost.Auth;

public sealed class SampleIssuedSessionStore
{
    private readonly ConcurrentDictionary<string, IssuedSessionRecord> _sessions = new(StringComparer.Ordinal);

    public void Store(IssuedSessionRecord session) => _sessions[session.SessionId] = session;

    public bool Remove(string sessionId, out IssuedSessionRecord? session) =>
        _sessions.TryRemove(sessionId, out session);

    public bool TryGet(string sessionId, out IssuedSessionRecord? session) =>
        _sessions.TryGetValue(sessionId, out session);

    public IReadOnlyCollection<IssuedSessionRecord> GetAll() => _sessions.Values
        .OrderByDescending(session => session.ExpiresAtUtc)
        .ToArray();
}
