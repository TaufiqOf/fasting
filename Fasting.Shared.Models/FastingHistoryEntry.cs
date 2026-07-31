namespace Fasting.Models;

public class FastingHistoryEntry
{
    public string FastingTypeId { get; init; } = string.Empty;

    public string FastingTypeName { get; init; } = string.Empty;

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset EndedAt { get; init; }

    public double TargetHours { get; init; }

    public TimeSpan Duration => EndedAt - StartedAt;

    public bool TargetReached =>
        Duration >= TimeSpan.FromHours(TargetHours);
}