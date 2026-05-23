using SessionSentinel.Domain.Entities;

namespace SessionSentinel.Persistence;

public interface IAnomalyLogPersistenceWriter
{
    Task WriteAsync(AnomalyLog anomalyLog, CancellationToken cancellationToken = default);
}
