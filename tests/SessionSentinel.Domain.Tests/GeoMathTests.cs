using SessionSentinel.Domain.Models;
using SessionSentinel.Domain.Rules;

namespace SessionSentinel.Domain.Tests;

public sealed class GeoMathTests
{
    [Fact]
    public void Calculate_distance_returns_reasonable_value()
    {
        var distance = GeoMath.CalculateDistanceKilometers(
            new GeoPoint(41.0082, 28.9784),
            new GeoPoint(39.9334, 32.8597));

        Assert.InRange(distance, 300, 400);
    }

    [Fact]
    public void Calculate_speed_returns_positive_value()
    {
        var speed = GeoMath.CalculateSpeedKilometersPerHour(300, TimeSpan.FromHours(2));

        Assert.Equal(150, speed);
    }
}
