using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SessionSentinel.Persistence.AnomalyLogging;

public sealed class AnomalyLogBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AnomalyLogChannel _channel;
    private readonly ILogger<AnomalyLogBackgroundService> _logger;

    public AnomalyLogBackgroundService(
        IServiceScopeFactory scopeFactory,
        AnomalyLogChannel channel,
        ILogger<AnomalyLogBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _channel = channel;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var anomalyLog in _channel.ReadAllAsync(stoppingToken))
        {
            // A scoped writer keeps DbContext lifetime aligned with each write.
            using var scope = _scopeFactory.CreateScope();
            var writer = scope.ServiceProvider.GetRequiredService<IAnomalyLogPersistenceWriter>();
            await writer.WriteAsync(anomalyLog, stoppingToken);
            _logger.LogInformation("SessionSentinel persisted anomaly log for session {SessionId}.", anomalyLog.SessionId);
        }
    }
}
