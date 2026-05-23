using SessionSentinel.Domain;
using SessionSentinel.Domain.Models;
using SessionSentinel.Domain.Rules;

namespace SessionSentinel.Domain.Tests;

public sealed class RiskScoreCalculatorTests
{
    private readonly RiskScoreCalculator _calculator = new();

    [Fact]
    public void Evaluate_adds_points_for_ip_and_fingerprint_mismatch()
    {
        var result = _calculator.Evaluate(
            new RiskEvaluationInput(
                "2.2.2.2",
                "1.1.1.1",
                "new-fingerprint",
                "old-fingerprint",
                null,
                null,
                DateTime.UtcNow,
                DateTime.UtcNow.AddMinutes(-5),
                900));

        Assert.Equal(70, result.Score);
        Assert.Contains(TriggeredRule.IpMismatch, result.TriggeredRules);
        Assert.Contains(TriggeredRule.FingerprintMismatch, result.TriggeredRules);
    }

    [Fact]
    public void Evaluate_flags_impossible_travel_when_speed_exceeds_threshold()
    {
        var result = _calculator.Evaluate(
            new RiskEvaluationInput(
                "1.1.1.1",
                "1.1.1.1",
                "fingerprint",
                "fingerprint",
                new GeoPoint(41.0082, 28.9784),
                new GeoPoint(39.9334, 32.8597),
                DateTime.UtcNow,
                DateTime.UtcNow.AddMinutes(-10),
                900));

        Assert.Equal(100, result.Score);
        Assert.Contains(TriggeredRule.ImpossibleTravel, result.TriggeredRules);
    }
}
