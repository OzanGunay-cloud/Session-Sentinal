using Microsoft.Extensions.Options;
using SessionSentinel.Application.Options;

namespace SessionSentinel.WebApi;

public sealed class SessionSentinelOptionsValidator : IValidateOptions<SessionSentinelOptions>
{
    public ValidateOptionsResult Validate(string? name, SessionSentinelOptions options)
    {
        var errors = new List<string>();

        if (options.RiskThreshold is < 1 or > 100)
        {
            errors.Add("RiskThreshold must be between 1 and 100.");
        }

        if (options.ImpossibleTravelSpeedThresholdKph <= 0)
        {
            errors.Add("ImpossibleTravelSpeedThresholdKph must be greater than 0.");
        }

        if (options.ActiveSessionTtl <= TimeSpan.Zero)
        {
            errors.Add("ActiveSessionTtl must be greater than 0.");
        }

        if (options.FallbackBlacklistTtl <= TimeSpan.Zero)
        {
            errors.Add("FallbackBlacklistTtl must be greater than 0.");
        }

        if (options.AnomalyQueueCapacity <= 0)
        {
            errors.Add("AnomalyQueueCapacity must be greater than 0.");
        }

        if (string.IsNullOrWhiteSpace(options.HubRoute) || !options.HubRoute.StartsWith('/'))
        {
            errors.Add("HubRoute must start with '/'.");
        }

        if (!string.IsNullOrWhiteSpace(options.GeoIpBaseUrl) &&
            !Uri.TryCreate(options.GeoIpBaseUrl, UriKind.Absolute, out _))
        {
            errors.Add("GeoIpBaseUrl must be a valid absolute URL.");
        }

        if (options.UseRedis && string.IsNullOrWhiteSpace(options.RedisConnectionString))
        {
            errors.Add("RedisConnectionString must be provided when UseRedis is enabled.");
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
