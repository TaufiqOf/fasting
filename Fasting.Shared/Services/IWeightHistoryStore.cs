using Fasting.Models;

namespace Fasting.Shared.Services;

public interface IWeightHistoryStore
{
    Task<IReadOnlyList<WeightEntry>> LoadAsync();

    Task SaveAsync(
        IReadOnlyList<WeightEntry> entries);

    Task ClearAsync();
}