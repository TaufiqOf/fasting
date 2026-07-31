using Fasting.Models;
using Fasting.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace Fasting.Shared.Pages;

public partial class WeightPage
{
    [Inject]
    private IWeightHistoryStore WeightHistoryStore { get; set; } = default!;

    [Inject]
    private IUserProfileStore UserProfileStore { get; set; } = default!;

    private WeightEntryForm Form { get; set; } = new();

    private List<WeightEntry> Entries { get; set; } = [];

    private bool IsLoaded { get; set; }

    private bool IsSaving { get; set; }

    private bool SaveSucceeded { get; set; }

    private string StatusMessage { get; set; } = string.Empty;

    private double? TargetWeightKg { get; set; }

    private WeightEntry? LatestEntry =>
        Entries
            .OrderByDescending(entry => entry.RecordedAt)
            .FirstOrDefault();

    private WeightEntry? StartingEntry =>
        Entries
            .OrderBy(entry => entry.RecordedAt)
            .FirstOrDefault();

    private string StartingWeightText =>
        StartingEntry is null
            ? "—"
            : $"{StartingEntry.WeightKg:0.0} kg";

    private string TargetWeightText =>
        TargetWeightKg is > 0
            ? $"{TargetWeightKg.Value:0.0} kg"
            : "Not set";

    private double WeightChange
    {
        get
        {
            if (StartingEntry is null ||
                LatestEntry is null)
            {
                return 0;
            }

            return LatestEntry.WeightKg -
                   StartingEntry.WeightKg;
        }
    }

    private string WeightChangeText =>
        WeightChange switch
        {
            > 0.05 => $"+{WeightChange:0.0} kg",
            < -0.05 => $"{WeightChange:0.0} kg",
            _ => "No change"
        };

    private string WeightChangeCss =>
        WeightChange switch
        {
            < -0.05 => "text-success",
            > 0.05 => "text-danger",
            _ => "text-muted"
        };

    private double TargetProgressPercentage
    {
        get
        {
            if (StartingEntry is null ||
                LatestEntry is null ||
                TargetWeightKg is not > 0)
            {
                return 0;
            }

            double startWeight =
                StartingEntry.WeightKg;

            double currentWeight =
                LatestEntry.WeightKg;

            double targetWeight =
                TargetWeightKg.Value;

            double totalDifference =
                startWeight - targetWeight;

            if (Math.Abs(totalDifference) < 0.01)
            {
                return currentWeight == targetWeight
                    ? 100
                    : 0;
            }

            double completedDifference =
                startWeight - currentWeight;

            double percentage =
                completedDifference /
                totalDifference *
                100;

            return Math.Clamp(percentage, 0, 100);
        }
    }

    private string TargetProgressText
    {
        get
        {
            if (LatestEntry is null ||
                TargetWeightKg is not > 0)
            {
                return string.Empty;
            }

            double difference =
                LatestEntry.WeightKg -
                TargetWeightKg.Value;

            return difference switch
            {
                > 0.05 =>
                    $"{difference:0.0} kg remaining",

                < -0.05 =>
                    $"{Math.Abs(difference):0.0} kg below target",

                _ =>
                    "Target reached"
            };
        }
    }

    protected override async Task OnAfterRenderAsync(
        bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            IReadOnlyList<WeightEntry> savedEntries =
                await WeightHistoryStore.LoadAsync();

            Entries = savedEntries
                .OrderByDescending(entry => entry.RecordedAt)
                .ToList();

            UserProfile? profile =
                await UserProfileStore.LoadAsync();

            TargetWeightKg =
                profile?.TargetWeightKg > 0
                    ? profile.TargetWeightKg
                    : null;
        }
        catch (Exception exception)
        {
            SaveSucceeded = false;
            StatusMessage =
                $"Unable to load weight history: {exception.Message}";
        }
        finally
        {
            IsLoaded = true;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task SaveWeightAsync()
    {
        if (IsSaving)
        {
            return;
        }

        IsSaving = true;
        StatusMessage = string.Empty;

        try
        {
            DateTime localDate =
                Form.RecordedAt.Date.Add(DateTime.Now.TimeOfDay);

            WeightEntry entry = new()
            {
                Id = Guid.NewGuid(),
                WeightKg = Form.WeightKg,

                RecordedAt = new DateTimeOffset(
                        DateTime.SpecifyKind(
                            Form.RecordedAt,
                            DateTimeKind.Local))
                    .ToUniversalTime(),

                Notes = Form.Notes.Trim()
            };

            Entries.Add(entry);

            Entries = Entries
                .OrderByDescending(item => item.RecordedAt)
                .ToList();

            await WeightHistoryStore.SaveAsync(Entries);

            Form = new WeightEntryForm
            {
                RecordedAt = DateTime.Now
            };

            SaveSucceeded = true;
            StatusMessage =
                "Your weight has been recorded.";
        }
        catch (Exception exception)
        {
            SaveSucceeded = false;
            StatusMessage =
                $"Your weight could not be saved: {exception.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task DeleteEntryAsync(Guid id)
    {
        Entries.RemoveAll(entry => entry.Id == id);

        await WeightHistoryStore.SaveAsync(Entries);
    }

    private async Task ClearHistoryAsync()
    {
        Entries.Clear();

        await WeightHistoryStore.ClearAsync();

        SaveSucceeded = true;
        StatusMessage =
            "Weight history cleared.";
    }

    private string GetEntryChangeText(
        WeightEntry entry)
    {
        WeightEntry? previousEntry = Entries
            .Where(item => item.RecordedAt < entry.RecordedAt)
            .OrderByDescending(item => item.RecordedAt)
            .FirstOrDefault();

        if (previousEntry is null)
        {
            return "—";
        }

        double difference =
            entry.WeightKg -
            previousEntry.WeightKg;

        return difference switch
        {
            > 0.05 => $"+{difference:0.0} kg",
            < -0.05 => $"{difference:0.0} kg",
            _ => "0.0 kg"
        };
    }

    private string GetEntryChangeCss(
        WeightEntry entry)
    {
        WeightEntry? previousEntry = Entries
            .Where(item => item.RecordedAt < entry.RecordedAt)
            .OrderByDescending(item => item.RecordedAt)
            .FirstOrDefault();

        if (previousEntry is null)
        {
            return "text-muted";
        }

        double difference =
            entry.WeightKg -
            previousEntry.WeightKg;

        return difference switch
        {
            < -0.05 => "text-success fw-semibold",
            > 0.05 => "text-danger fw-semibold",
            _ => "text-muted"
        };
    }
}