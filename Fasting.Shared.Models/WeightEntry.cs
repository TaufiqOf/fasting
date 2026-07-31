namespace Fasting.Models;

public sealed class WeightEntry
{
    public Guid Id { get; init; }

    public double WeightKg { get; init; }

    public DateTimeOffset RecordedAt { get; init; }

    public string Notes { get; init; } = string.Empty;
}