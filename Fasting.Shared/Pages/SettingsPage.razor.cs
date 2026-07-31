using Fasting.Models;
using Fasting.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace Fasting.Shared.Pages;

public partial class SettingsPage
{
    [Inject]
    private IUserProfileStore UserProfileStore { get; set; } = default!;

    private UserProfile Profile { get; set; } = new();

    private bool IsSaving { get; set; }

    private bool SaveSucceeded { get; set; }

    private string StatusMessage { get; set; } = string.Empty;

    private static IReadOnlyList<string> GenderOptions { get; } =
    [
        "Male",
        "Female",
        "Non-binary",
        "Prefer not to say"
    ];

    private double BodyMassIndex
    {
        get
        {
            if (Profile.HeightCm <= 0 ||
                Profile.WeightKg <= 0)
            {
                return 0;
            }

            double heightMetres =
                Profile.HeightCm / 100.0;

            return Profile.WeightKg /
                   (heightMetres * heightMetres);
        }
    }

    private string WeightGoalText
    {
        get
        {
            double difference =
                Profile.WeightKg - Profile.TargetWeightKg;

            return difference switch
            {
                > 0.05 =>
                    $"Lose {difference:0.0} kg",

                < -0.05 =>
                    $"Gain {Math.Abs(difference):0.0} kg",

                _ =>
                    "Maintain weight"
            };
        }
    }

    private bool _initialized;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _initialized)
        {
            return;
        }

        _initialized = true;

        try
        {
            UserProfile? savedProfile =
                await UserProfileStore.LoadAsync();

            if (savedProfile is not null)
            {
                Profile = savedProfile;
            }
        }
        catch (Exception exception)
        {
            SaveSucceeded = false;
            StatusMessage =
                $"Unable to load profile settings: {exception.Message}";
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task SaveSettingsAsync()
    {
        if (IsSaving)
        {
            return;
        }

        IsSaving = true;
        StatusMessage = string.Empty;

        try
        {
            Profile.UpdatedAt =
                DateTimeOffset.UtcNow;

            await UserProfileStore.SaveAsync(Profile);

            SaveSucceeded = true;
            StatusMessage =
                "Your profile settings have been saved.";
        }
        catch (Exception)
        {
            SaveSucceeded = false;
            StatusMessage =
                "Your settings could not be saved.";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void ResetForm()
    {
        Profile = new UserProfile();
        StatusMessage = string.Empty;
        SaveSucceeded = false;
    }
}