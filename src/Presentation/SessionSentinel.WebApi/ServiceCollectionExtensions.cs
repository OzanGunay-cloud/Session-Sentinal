using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SessionSentinel.Application.Abstractions;
using SessionSentinel.Application.Behaviors;
using SessionSentinel.Application.Handlers;
using SessionSentinel.Application.Options;
using SessionSentinel.Application.Services;
using SessionSentinel.Domain.Rules;
using SessionSentinel.Infrastructure.Caching;
using SessionSentinel.Infrastructure.Realtime;
using SessionSentinel.Infrastructure.Services;
using SessionSentinel.Persistence;
using SessionSentinel.WebApi.Identity;
using StackExchange.Redis;

namespace SessionSentinel.WebApi;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSessionSentinel(
        this IServiceCollection services,
        Action<SessionSentinelOptions>? configure = null)
    {
        var options = new SessionSentinelOptions();
        configure?.Invoke(options);

        services.AddOptions<SessionSentinelOptions>()
            .Configure(configure ?? (_ => { }))
            .ValidateOnStart();
        services.TryAddSingleton<IValidateOptions<SessionSentinelOptions>, SessionSentinelOptionsValidator>();

        // Pure scoring stays singleton because it has no mutable state.
        services.TryAddSingleton<RiskScoreCalculator>();
        services.AddMediatR(typeof(AnalyzeRequestQueryHandler).Assembly);
        services.AddValidatorsFromAssemblyContaining<AnalyzeRequestQueryHandler>();
        services.TryAddEnumerable(
            ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>)));

        services.AddSignalR();
        services.TryAddSingleton<IUserIdProvider, SentinelUserIdProvider>();
        services.TryAddSingleton<ITokenHasher, Sha256TokenHasher>();
        services.TryAddScoped<ISessionRegistrationService, SessionRegistrationService>();
        services.TryAddScoped<ISessionRevocationService, SessionRevocationService>();
        services.TryAddScoped<IUserIdentityAccessor, DefaultUserIdentityAccessor>();
        services.TryAddScoped<ISignalRNotificationService, SignalRNotificationService>();

        ConfigureGeoLocation(services, options);
        ConfigureSessionStores(services, options);
        ConfigurePersistence(services, options);

        return services;
    }

    public static IApplicationBuilder UseSessionSentinel(this IApplicationBuilder app) =>
        app.UseMiddleware<SessionSentinelMiddleware>();

    public static IEndpointConventionBuilder MapSessionSentinelHub(
        this IEndpointRouteBuilder endpoints,
        string? pattern = null)
    {
        var route = pattern;
        if (string.IsNullOrWhiteSpace(route))
        {
            route = endpoints.ServiceProvider.GetRequiredService<IOptions<SessionSentinelOptions>>().Value.HubRoute;
        }

        return endpoints.MapHub<SentinelHub>(route!);
    }

    private static void ConfigureSessionStores(IServiceCollection services, SessionSentinelOptions options)
    {
        if (options.UseRedis)
        {
            // Distributed deployments use Redis for shared session state.
            services.TryAddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(options.RedisConnectionString!));
            services.TryAddSingleton<ISentinelSessionStore, RedisSentinelSessionStore>();
            services.TryAddSingleton<ITokenBlacklistService, RedisTokenBlacklistService>();
            return;
        }

        // Single-node fallback keeps the package usable without Redis.
        services.AddMemoryCache();
        services.TryAddSingleton<ISentinelSessionStore, MemorySentinelSessionStore>();
        services.TryAddSingleton<ITokenBlacklistService, MemoryTokenBlacklistService>();
    }

    private static void ConfigureGeoLocation(IServiceCollection services, SessionSentinelOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.GeoIpBaseUrl))
        {
            services.TryAddSingleton<IGeoLocationService, NoopGeoLocationService>();
            return;
        }

        // A configured base URL upgrades the default noop provider to a real GeoIP lookup.
        services.AddHttpClient<IGeoLocationService, IpWhoIsGeoLocationService>(client =>
        {
            client.BaseAddress = new Uri(options.GeoIpBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(5);
        });
    }

    private static void ConfigurePersistence(IServiceCollection services, SessionSentinelOptions options)
    {
        services.TryAddSingleton<IAnomalyLogWriter, NullAnomalyLogWriter>();

        if (!string.IsNullOrWhiteSpace(options.SqlServerConnectionString))
        {
            services.AddSessionSentinelPersistence(options.SqlServerConnectionString, options);
        }
    }
}
