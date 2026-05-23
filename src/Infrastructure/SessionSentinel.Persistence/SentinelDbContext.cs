using Microsoft.EntityFrameworkCore;
using SessionSentinel.Domain.Entities;

namespace SessionSentinel.Persistence;

public sealed class SentinelDbContext : DbContext
{
    public SentinelDbContext(DbContextOptions<SentinelDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserSession> UserSessions => Set<UserSession>();

    public DbSet<AnomalyLog> AnomalyLogs => Set<AnomalyLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SentinelDbContext).Assembly);
    }
}
