using SessionSentinel.Application.Abstractions;
using SessionSentinel.Domain.Entities;

namespace SessionSentinel.Persistence;

public sealed class EfCoreAnomalyLogWriter : IAnomalyLogPersistenceWriter
{
    private readonly SentinelDbContext _dbContext;

    public EfCoreAnomalyLogWriter(SentinelDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task WriteAsync(AnomalyLog anomalyLog, CancellationToken cancellationToken = default)
    {
        // SQL writes happen off the request thread through the queue worker.
        _dbContext.AnomalyLogs.Add(anomalyLog);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
