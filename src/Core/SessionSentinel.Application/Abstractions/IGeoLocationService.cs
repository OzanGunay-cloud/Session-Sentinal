using SessionSentinel.Domain.Models;

namespace SessionSentinel.Application.Abstractions;

public interface IGeoLocationService
{
    Task<GeoPoint?> ResolveAsync(string ipAddress, CancellationToken cancellationToken = default);
}
