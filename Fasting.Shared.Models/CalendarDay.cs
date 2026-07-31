namespace Fasting.Models;

public sealed class CalendarDay
{
    public required DateTime Date { get; init; }

    public bool IsCurrentMonth { get; init; }

    public bool IsToday { get; init; }

    public List<FastingHistoryEntry> CompletedFasts { get; init; } = [];

    public bool TargetReached => CompletedFasts.Count > 0;
}