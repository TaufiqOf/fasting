namespace Fasting.Models;

public class Eating
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    public required FastingType Type { get; init; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public DateTimeOffset ExpectedEnd =>
        StartedAt.AddHours(Type.EatingHours);

    public bool IsEnded => EndedAt is not null;
}