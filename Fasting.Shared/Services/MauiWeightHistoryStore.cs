using System.Text.Json;
using Fasting.Models;
using Microsoft.Maui.Storage;

namespace Fasting.Shared.Services;

public sealed class MauiWeightHistoryStore : IWeightHistoryStore
{
    private const string StorageKey =
        "fasting-weight-history";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public Task<IReadOnlyList<WeightEntry>> LoadAsync()
    {
        string? json = Preferences.Default.Get<string?>(StorageKey, null);

        if (string.IsNullOrWhiteSpace(json))
        {
            return Task.FromResult<IReadOnlyList<WeightEntry>>([]);
        }

        try
        {
            List<WeightEntry> entries =
                JsonSerializer.Deserialize<List<WeightEntry>>(
                    json,
                    JsonOptions)
                ?? [];

            IReadOnlyList<WeightEntry> result = entries;

            return Task.FromResult(result);
        }
        catch (JsonException)
        {
            return Task.FromResult<IReadOnlyList<WeightEntry>>([]);
        }
    }

    public Task SaveAsync(
        IReadOnlyList<WeightEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        string json =
            JsonSerializer.Serialize(
                entries,
                JsonOptions);

        Preferences.Default.Set(StorageKey,json);

        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        Preferences.Default.Remove(StorageKey);
        return Task.CompletedTask;
    }
}