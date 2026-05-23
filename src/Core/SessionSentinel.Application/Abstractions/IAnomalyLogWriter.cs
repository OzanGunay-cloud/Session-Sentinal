using SessionSentinel.Domain.Entities;

namespace SessionSentinel.Application.Abstractions;

public interface IAnomalyLogWriter
{
    // Application only knows it can hand off a log entry.
    Task WriteAsync(AnomalyLog anomalyLog, CancellationToken cancellationToken = default);
}
