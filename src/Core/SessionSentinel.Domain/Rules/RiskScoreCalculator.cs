using SessionSentinel.Domain.Models;

namespace SessionSentinel.Domain.Rules;

public sealed class RiskScoreCalculator
{
    public RiskEvaluationResult Evaluate(RiskEvaluationInput input)
    {
        var score = 0;
        var rules = new List<TriggeredRule>();

        if (!string.Equals(input.StoredIpAddress, input.CurrentIpAddress, StringComparison.OrdinalIgnoreCase))
        {
            score += 20;
            rules.Add(TriggeredRule.IpMismatch);
        }

        if (!string.Equals(input.StoredFingerprintHash, input.CurrentFingerprintHash, StringComparison.Ordinal))
        {
            score += 50;
            rules.Add(TriggeredRule.FingerprintMismatch);
        }

        if (input.StoredLocation is not null && input.CurrentLocation is not null)
        {
            var distance = GeoMath.CalculateDistanceKilometers(input.StoredLocation.Value, input.CurrentLocation.Value);
            var speed = GeoMath.CalculateSpeedKilometersPerHour(distance, input.RequestedAtUtc - input.LastRequestAtUtc);

            if (speed > input.ImpossibleTravelSpeedThresholdKph)
            {
                score += 100;
                rules.Add(TriggeredRule.ImpossibleTravel);
            }
        }

        return new RiskEvaluationResult(Math.Min(score, 100), rules);
    }
}
