using System.Text.Json;
using Fasting.Models;
using Microsoft.Maui.Storage;

namespace Fasting.Shared.Services;

public sealed class UserProfileStore : IUserProfileStore
{
    private const string ProfileKey =
        "fasting-user-profile";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public Task<UserProfile?> LoadAsync()
    {
        string? json =
            Preferences.Default.Get<string?>(
                ProfileKey,
                null);

        if (string.IsNullOrWhiteSpace(json))
        {
            return Task.FromResult<UserProfile?>(null);
        }

        try
        {
            UserProfile? profile =
                JsonSerializer.Deserialize<UserProfile>(
                    json,
                    JsonOptions);

            return Task.FromResult(profile);
        }
        catch (JsonException)
        {
            return Task.FromResult<UserProfile?>(null);
        }
    }

    public Task SaveAsync(UserProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        string json =
            JsonSerializer.Serialize(
                profile,
                JsonOptions);

        Preferences.Default.Set(
            ProfileKey,
            json);

        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        Preferences.Default.Remove(ProfileKey);

        return Task.CompletedTask;
    }
}