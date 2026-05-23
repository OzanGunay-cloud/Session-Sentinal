using SessionSentinel.Application.Abstractions;
using SessionSentinel.Domain.Entities;

namespace SessionSentinel.Persistence.AnomalyLogging;

public sealed class QueuedAnomalyLogWriter : IAnomalyLogWriter
{
    private readonly AnomalyLogChannel _channel;

    public QueuedAnomalyLogWriter(AnomalyLogChannel channel)
    {
        _channel = channel;
    }

    public async Task WriteAsync(AnomalyLog anomalyLog, CancellationToken cancellationToken = default)
    {
        // The handler enqueues work and returns; the background worker persists it.
        await _channel.QueueAsync(anomalyLog, cancellationToken);
    }
}
