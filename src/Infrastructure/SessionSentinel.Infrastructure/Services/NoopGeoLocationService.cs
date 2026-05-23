using SessionSentinel.Application.Abstractions;
using SessionSentinel.Domain.Models;

namespace SessionSentinel.Infrastructure.Services;

public sealed class NoopGeoLocationService : IGeoLocationService
{
    public Task<GeoPoint?> ResolveAsync(string ipAddress, CancellationToken cancellationToken = default) =>
        Task.FromResult<GeoPoint?>(null);
}
