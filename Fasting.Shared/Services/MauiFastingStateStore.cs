using System.Text.Json;
using Microsoft.Maui.Storage;

namespace Fasting.Shared.Services;

public sealed class MauiFastingStateStore : IFastingStateStore
{
    private const string StorageKey = "active_fasting_state";

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

    public Task ClearAsync()
    {
        Preferences.Default.Remove(StorageKey);

        return Task.CompletedTask;
    }
}