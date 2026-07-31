using System.Text.Json;
using Fasting.Models;
using Microsoft.JSInterop;

namespace Fasting.Shared.Services;

public sealed class WebUserProfileStore : IUserProfileStore
{
    private const string StorageKey = "fasting-user-profile";

    private readonly IJSRuntime _jsRuntime;

    public WebUserProfileStore(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<UserProfile?> LoadAsync()
    {
        string? json = await _jsRuntime.InvokeAsync<string?>(
            "localStorage.getItem",
            StorageKey);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<UserProfile>(json);
    }

    public async Task SaveAsync(UserProfile profile)
    {
        string json = JsonSerializer.Serialize(profile);

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