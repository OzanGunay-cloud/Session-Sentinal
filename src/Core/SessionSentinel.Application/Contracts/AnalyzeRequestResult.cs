using SessionSentinel.Domain;

namespace SessionSentinel.Application.Contracts;

public sealed record AnalyzeRequestResult(
    bool IsAllowed,
    int RiskScore,
    IReadOnlyCollection<TriggeredRule> TriggeredRules,
    string? DenyReason = null)
{
    public static AnalyzeRequestResult Allow(int riskScore, IReadOnlyCollection<TriggeredRule>? triggeredRules = null) =>
        new(true, riskScore, triggeredRules ?? Array.Empty<TriggeredRule>());

    public static AnalyzeRequestResult Deny(
        string denyReason,
        int riskScore,
        IReadOnlyCollection<TriggeredRule>? triggeredRules = null) =>
        new(false, riskScore, triggeredRules ?? Array.Empty<TriggeredRule>(), denyReason);
}
