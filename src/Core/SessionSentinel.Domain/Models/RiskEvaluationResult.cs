namespace SessionSentinel.Domain.Models;

public sealed record RiskEvaluationResult(int Score, IReadOnlyCollection<TriggeredRule> TriggeredRules);
