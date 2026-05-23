using System.Text.Json;
using SessionSentinel.Application.Abstractions;
using SessionSentinel.Domain.Models;

namespace SessionSentinel.Infrastructure.Services;

public sealed class IpWhoIsGeoLocationService : IGeoLocationService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public IpWhoIsGeoLocationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GeoPoint?> ResolveAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(ipAddress, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<IpWhoIsResponse>(stream, SerializerOptions, cancellationToken);

        if (payload is null || !payload.Success || payload.Latitude is null || payload.Longitude is null)
        {
            return null;
        }

        return new GeoPoint(payload.Latitude.Value, payload.Longitude.Value);
    }

    internal sealed record IpWhoIsResponse(bool Success, double? Latitude, double? Longitude);
}
