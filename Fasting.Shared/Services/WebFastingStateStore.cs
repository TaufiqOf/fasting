using System.Text.Json;
using Fasting.Models;
using Fasting.Shared.Services;
using Microsoft.JSInterop;

namespace Fasting.Shared.Services;

public sealed class WebFastingStateStore : IFastingStateStore, IFastingHistoryStore
{
    private const string StorageKey = "active_fasting_state";
    private const string HistoryStorageKey = "fasting_history";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IJSRuntime _jsRuntime;

    public WebFastingStateStore(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<FastingPersistedState?> LoadAsync()
    {
        try
        {
            string? json =
                await _jsRuntime.InvokeAsync<string?>(
                    "localStorage.getItem",
                    StorageKey);

            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<FastingPersistedState>(
                json,
                JsonOptions);
        }
        catch (JSException)
        {
            return null;
        }
        catch (JsonException)
        {
            await ClearAsync();

            return null;
        }
    }

    public async Task SaveAsync(
        FastingPersistedState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        string json =
            JsonSerializer.Serialize(
                state,
                JsonOptions);

        await _jsRuntime.InvokeVoidAsync(
            "localStorage.setItem",
            StorageKey,
            json);
    }

    public async Task<IReadOnlyList<FastingHistoryEntry>> LoadHistoryAsync()
    {
        try
        {
            string? json =
                await _jsRuntime.InvokeAsync<string?>(
                    "localStorage.getItem",
                    HistoryStorageKey);

            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<FastingHistoryEntry>();
            }

            return JsonSerializer.Deserialize<List<FastingHistoryEntry>>(
                       json,
                       JsonOptions)
                   ?? new List<FastingHistoryEntry>();
        }
        catch (JSException)
        {
            return Array.Empty<FastingHistoryEntry>();
        }
        catch (JsonException)
        {
            await ClearHistoryAsync();
            return Array.Empty<FastingHistoryEntry>();
        }
    }

    public async Task SaveHistoryAsync(
        IReadOnlyList<FastingHistoryEntry> history)
    {
        ArgumentNullException.ThrowIfNull(history);

        string json = JsonSerializer.Serialize(history, JsonOptions);

        await _jsRuntime.InvokeVoidAsync(
            "localStorage.setItem",
            HistoryStorageKey,
            json);
    }

    public async Task ClearHistoryAsync()
    {
        await _jsRuntime.InvokeVoidAsync(
            "localStorage.removeItem",
            HistoryStorageKey);
    }

    public async Task ClearAsync()
    {
        await _jsRuntime.InvokeVoidAsync(
            "localStorage.removeItem",
            StorageKey);
    }
}