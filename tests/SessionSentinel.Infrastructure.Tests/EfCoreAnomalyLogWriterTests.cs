using Microsoft.EntityFrameworkCore;
using SessionSentinel.Domain.Entities;
using SessionSentinel.Persistence;

namespace SessionSentinel.Infrastructure.Tests;

public sealed class EfCoreAnomalyLogWriterTests
{
    [Fact]
    public async Task Writer_persists_anomaly_logs()
    {
        var options = new DbContextOptionsBuilder<SentinelDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var dbContext = new SentinelDbContext(options);
        var writer = new EfCoreAnomalyLogWriter(dbContext);

        await writer.WriteAsync(
            new AnomalyLog
            {
                SessionId = "session-1",
                UserId = "user-1",
                TriggeredRule = "FingerprintMismatch",
                RiskScore = 50,
                Details = "Suspicious activity"
            });

        Assert.Equal(1, await dbContext.AnomalyLogs.CountAsync());
    }
}
