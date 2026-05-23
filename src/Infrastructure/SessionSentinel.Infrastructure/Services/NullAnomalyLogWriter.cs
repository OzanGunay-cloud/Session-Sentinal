using SessionSentinel.Application.Abstractions;
using SessionSentinel.Domain.Entities;

namespace SessionSentinel.Infrastructure.Services;

public sealed class NullAnomalyLogWriter : IAnomalyLogWriter
{
    public Task WriteAsync(AnomalyLog anomalyLog, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
