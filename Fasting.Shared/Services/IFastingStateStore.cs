using Fasting.Models;

namespace Fasting.Shared.Services;

public interface IFastingStateStore
{
    Task<FastingPersistedState?> LoadAsync();

    Task SaveAsync(FastingPersistedState state);

    Task ClearAsync();
}

public sealed class FastingPersistedState
{
    public CycleState State { get; init; }

    public string FastingTypeId { get; init; } = string.Empty;

    public DateTimeOffset StartedAt { get; init; }
}