using SessionSentinel.Domain.Models;

namespace SessionSentinel.Domain.Rules;

public static class GeoMath
{
    private const double EarthRadiusKm = 6371d;

    public static double CalculateDistanceKilometers(GeoPoint start, GeoPoint end)
    {
        var latitudeDelta = DegreesToRadians(end.Latitude - start.Latitude);
        var longitudeDelta = DegreesToRadians(end.Longitude - start.Longitude);
        var startLatitude = DegreesToRadians(start.Latitude);
        var endLatitude = DegreesToRadians(end.Latitude);

        var a =
            Math.Sin(latitudeDelta / 2) * Math.Sin(latitudeDelta / 2) +
            Math.Cos(startLatitude) * Math.Cos(endLatitude) *
            Math.Sin(longitudeDelta / 2) * Math.Sin(longitudeDelta / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    public static double CalculateSpeedKilometersPerHour(double distanceKm, TimeSpan elapsed)
    {
        if (elapsed <= TimeSpan.Zero)
        {
            return double.PositiveInfinity;
        }

        return distanceKm / elapsed.TotalHours;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;
}
