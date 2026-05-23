namespace SessionSentinel.Domain.Entities;

public sealed class AnomalyLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string SessionId { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string TriggeredRule { get; set; } = string.Empty;

    public int RiskScore { get; set; }

    public string Details { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
