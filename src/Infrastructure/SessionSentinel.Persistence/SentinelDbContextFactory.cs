using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SessionSentinel.Persistence;

public sealed class SentinelDbContextFactory : IDesignTimeDbContextFactory<SentinelDbContext>
{
    public SentinelDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("SESSION_SENTINEL_SQL_CONNECTION") ??
            "Server=(localdb)\\MSSQLLocalDB;Database=SessionSentinelSample;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        var optionsBuilder = new DbContextOptionsBuilder<SentinelDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new SentinelDbContext(optionsBuilder.Options);
    }
}
