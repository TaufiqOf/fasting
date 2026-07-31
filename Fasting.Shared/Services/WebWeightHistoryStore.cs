using System.Text.Json;
using Fasting.Models;
using Microsoft.JSInterop;

namespace Fasting.Shared.Services;

public sealed class WebWeightHistoryStore : IWeightHistoryStore
{
    private const string StorageKey =
        "fasting-weight-history";

    private readonly IJSRuntime _jsRuntime;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public WebWeightHistoryStore(
        IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<IReadOnlyList<WeightEntry>> LoadAsync()
    {
        string? json =
            await _jsRuntime.InvokeAsync<string?>(
                "localStorage.getItem",
                StorageKey);

        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<WeightEntry>>(
                       json,
                       JsonOptions)
                   ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public async Task SaveAsync(
        IReadOnlyList<WeightEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        string json =
            JsonSerializer.Serialize(
                entries,
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