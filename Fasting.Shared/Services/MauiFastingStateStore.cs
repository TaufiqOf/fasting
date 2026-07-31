using System.Text.Json;
using Fasting.Models;
using Microsoft.Maui.Storage;

namespace Fasting.Shared.Services;

public sealed class MauiFastingStateStore : IFastingStateStore, IFastingHistoryStore
{
    private const string StorageKey = "active_fasting_state";
    private const string HistoryStorageKey = "fasting_history";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public Task<FastingPersistedState?> LoadAsync()
    {
        try
        {
            string? json = Preferences.Default.Get<string?>(StorageKey,null);

            if (string.IsNullOrWhiteSpace(json))
            {
                return Task.FromResult<FastingPersistedState?>(null);
            }

            FastingPersistedState? state =
                JsonSerializer.Deserialize<FastingPersistedState>(
                    json,
                    JsonOptions);

            return Task.FromResult(state);
        }
        catch
        {
            Preferences.Default.Remove(StorageKey);

            return Task.FromResult<FastingPersistedState?>(null);
        }
    }

    public Task SaveAsync(FastingPersistedState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        string json = JsonSerializer.Serialize(
            state,
            JsonOptions);

        Preferences.Default.Set(StorageKey, json);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<FastingHistoryEntry>> LoadHistoryAsync()
    {
        try
        {
            string? json = Preferences.Default.Get<string?>(HistoryStorageKey, null);

            if (string.IsNullOrWhiteSpace(json))
            {
                return Task.FromResult<IReadOnlyList<FastingHistoryEntry>>(
                    Array.Empty<FastingHistoryEntry>());
            }

            IReadOnlyList<FastingHistoryEntry> history =
                JsonSerializer.Deserialize<List<FastingHistoryEntry>>(json,JsonOptions) ?? new List<FastingHistoryEntry>();

            return Task.FromResult(history);
        }
        catch
        {
            Preferences.Default.Remove(HistoryStorageKey);

            return Task.FromResult<IReadOnlyList<FastingHistoryEntry>>(
                Array.Empty<FastingHistoryEntry>());
        }
    }

    public Task SaveHistoryAsync(
        IReadOnlyList<FastingHistoryEntry> history)
    {
        ArgumentNullException.ThrowIfNull(history);

        string json = JsonSerializer.Serialize(history, JsonOptions);
        Preferences.Default.Set(HistoryStorageKey, json);

        return Task.CompletedTask;
    }

    public Task ClearHistoryAsync()
    {
        Preferences.Default.Remove(HistoryStorageKey);
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        Preferences.Default.Remove(StorageKey);

        return Task.CompletedTask;
    }
}