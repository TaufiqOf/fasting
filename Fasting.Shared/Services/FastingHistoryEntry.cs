using Fasting.Models;

namespace Fasting.Shared.Services;


public interface IFastingHistoryStore
{
    Task<IReadOnlyList<FastingHistoryEntry>> LoadHistoryAsync();

    Task SaveHistoryAsync(
        IReadOnlyList<FastingHistoryEntry> history);

    Task ClearHistoryAsync();
}
