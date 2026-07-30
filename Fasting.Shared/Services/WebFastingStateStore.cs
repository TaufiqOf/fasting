using System.Text.Json;
using Fasting.Shared.Services;
using Microsoft.JSInterop;

namespace Fasting.Shared.Services;

public sealed class WebFastingStateStore : IFastingStateStore
{
    private const string StorageKey = "active_fasting_state";

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

    public async Task ClearAsync()
    {
        await _jsRuntime.InvokeVoidAsync(
            "localStorage.removeItem",
            StorageKey);
    }
}