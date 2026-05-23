using SessionSentinel.Domain.Models;

namespace SessionSentinel.Domain.Entities;

public sealed class UserSession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string SessionId { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string IpAddress { get; set; } = string.Empty;

    public string FingerprintHash { get; set; } = string.Empty;

    public string? UserAgent { get; set; }

    public string? Language { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime LastRequestAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public GeoPoint? GetLastKnownLocation()
    {
        if (Latitude is null || Longitude is null)
        {
            return null;
        }

        return new GeoPoint(Latitude.Value, Longitude.Value);
    }

    public void UpdateLastSeen(
        string ipAddress,
        string fingerprintHash,
        string? userAgent,
        string? language,
        GeoPoint? location,
        DateTime requestedAtUtc)
    {
        IpAddress = ipAddress;
        FingerprintHash = fingerprintHash;
        UserAgent = userAgent;
        Language = language;
        Latitude = location?.Latitude;
        Longitude = location?.Longitude;
        LastRequestAtUtc = requestedAtUtc;
        IsActive = true;
    }

    public void Revoke(DateTime revokedAtUtc)
    {
        IsActive = false;
        LastRequestAtUtc = revokedAtUtc;
    }
}
