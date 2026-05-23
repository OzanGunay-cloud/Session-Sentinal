using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SessionSentinel.Application.Options;
using SessionSentinel.Persistence.AnomalyLogging;

namespace SessionSentinel.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSessionSentinelPersistence(
        this IServiceCollection services,
        string connectionString,
        SessionSentinelOptions options)
    {
        // SQL persistence is optional, but when enabled logs are written through a background queue.
        services.AddDbContext<SentinelDbContext>(options => options.UseSqlServer(connectionString));
        services.AddSingleton(new AnomalyLogChannel(options.AnomalyQueueCapacity));
        services.AddScoped<IAnomalyLogPersistenceWriter, EfCoreAnomalyLogWriter>();
        services.AddSingleton<SessionSentinel.Application.Abstractions.IAnomalyLogWriter, QueuedAnomalyLogWriter>();
        services.AddSingleton<IHostedService, AnomalyLogBackgroundService>();
        return services;
    }
}
