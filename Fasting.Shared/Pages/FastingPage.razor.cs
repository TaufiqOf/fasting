using System.Globalization;
using Fasting.Models;
using Fasting.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Maui.Graphics;

namespace Fasting.Shared.Pages;

public partial class FastingPage : IDisposable
{
    [Inject]
    public FastingManager FastingManager { get; set; } = default!;

    public IReadOnlyList<FastingType> Types { get; } =
        FastingTypes.All;

    public string SelectedTypeId { get; set; } =
        FastingTypes.SixteenEight.Id;

    public bool IsEditingStartTime { get; private set; }

    public string EditableStartLocalText { get; set; } =
        string.Empty;

    public string? EditError { get; private set; }

    public string ProgressWidth =>
        FastingManager.ProgressPercentage.ToString(
            "0.##",
            CultureInfo.InvariantCulture);

    public string PhaseTitle =>
        FastingManager.State switch
        {
            CycleState.Fasting => "Fasting",
            CycleState.Eating => "Eating period",
            _ => "No active cycle"
        };

    public string StartLabel =>
        FastingManager.State switch
        {
            CycleState.Fasting => "Fast started",
            CycleState.Eating => "Eating started",
            _ => "Started"
        };

    public string FinishLabel =>
        FastingManager.State switch
        {
            CycleState.Fasting => "Fast finishes",
            CycleState.Eating => "Eating period finishes",
            _ => "Finishes"
        };

    public string RemainingLabel =>
        FastingManager.State switch
        {
            CycleState.Fasting =>
                "Time remaining in fast",

            CycleState.Eating =>
                "Time remaining in eating period",

            _ => string.Empty
        };

    public string ActiveStartTimeText =>
        FastingManager.ActiveStartedAt?
            .ToLocalTime()
            .ToString(
                "ddd, dd MMM yyyy HH:mm",
                CultureInfo.CurrentCulture)
        ?? string.Empty;
    
    public string FinishTimeText =>
        FastingManager.ExpectedFinishTime?
            .ToLocalTime()
            .ToString(
                "ddd, dd MMM yyyy HH:mm",
                CultureInfo.CurrentCulture)
        ?? string.Empty;

    public string RemainingTimeText =>
        FormatDuration(FastingManager.RemainingTime);
    
    public string EscapedTimeText =>
        FormatDuration(FastingManager.ElapsedTime);

    public IReadOnlyList<FastingHistoryEntry> History =>
        FastingManager.History;

    
    protected override async Task OnInitializedAsync()
    {
        FastingManager.StateChanged +=
            HandleManagerStateChanged;

        await FastingManager.InitializeAsync();
    }

    private async Task StartFasting()
    {
        FastingType? selectedType =
            Types.FirstOrDefault(
                type => type.Id == SelectedTypeId);

        if (selectedType is null)
        {
            return;
        }

        CloseStartTimeEditor();

        await FastingManager.StartFastingAsync(
            selectedType);
    }

    private async Task EndFast()
    {
        CloseStartTimeEditor();

        await FastingManager.EndFastAsync();
    }

    private async Task CancelFast()
    {
        CloseStartTimeEditor();

        await FastingManager.CancelFastAsync();
    }

    private async Task StartNextFast()
    {
        CloseStartTimeEditor();

        await FastingManager.StartNextFastAsync();
    }

    private async Task ClearHistory()
    {
        await FastingManager.ClearHistoryAsync();
    }

    private async Task CancelEatingPeriod()
    {
        CloseStartTimeEditor();

        await FastingManager.CancelEatingPeriodAsync();
    }

    private void BeginEditStartTime()
    {
        DateTimeOffset? activeStartTime =
            FastingManager.ActiveStartedAt;

        if (activeStartTime is null)
        {
            return;
        }

        EditableStartLocalText =
            activeStartTime.Value
                .ToLocalTime()
                .ToString(
                    "yyyy-MM-ddTHH:mm",
                    CultureInfo.InvariantCulture);

        EditError = null;
        IsEditingStartTime = true;
    }

    private void HandleStartTimeInput(
        ChangeEventArgs args)
    {
        EditableStartLocalText =
            args.Value?.ToString() ?? string.Empty;

        EditError = null;
    }

    private async Task SaveStartTime()
    {
        string[] supportedFormats =
        [
            "yyyy-MM-ddTHH:mm",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.F",
            "yyyy-MM-ddTHH:mm:ss.FF",
            "yyyy-MM-ddTHH:mm:ss.FFF",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFF"
        ];

        bool parsed =
            DateTime.TryParseExact(
                EditableStartLocalText,
                supportedFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime localDateTime);

        if (!parsed)
        {
            EditError =
                "Enter a valid start date and time.";

            return;
        }

        localDateTime =
            DateTime.SpecifyKind(
                localDateTime,
                DateTimeKind.Local);

        DateTimeOffset newStartTime =
            new(localDateTime);

        bool updated =
            await FastingManager.UpdateActiveStartTimeAsync(
                newStartTime);

        if (!updated)
        {
            EditError =
                "There is no active period to update.";

            return;
        }

        CloseStartTimeEditor();

        await InvokeAsync(StateHasChanged);
    }

    private void CancelEditStartTime()
    {
        CloseStartTimeEditor();

        StateHasChanged();
    }

    private void CloseStartTimeEditor()
    {
        IsEditingStartTime = false;
        EditableStartLocalText = string.Empty;
        EditError = null;
    }

    private void HandleManagerStateChanged()
    {
        // The manager updates every second. Avoid rendering
        // while the user is modifying the datetime input.
        if (IsEditingStartTime)
        {
            return;
        }

        _ = InvokeAsync(StateHasChanged);
    }

    public static string FormatHistoryDuration(TimeSpan duration) =>
        FormatDuration(duration);

    private static string FormatDuration(
        TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            duration = TimeSpan.Zero;
        }

        if (duration.TotalDays >= 1)
        {
            return
                $"{(int)duration.TotalDays}d " +
                $"{duration.Hours:00}:" +
                $"{duration.Minutes:00}:" +
                $"{duration.Seconds:00}";
        }

        return
            $"{duration.Hours:00}:" +
            $"{duration.Minutes:00}:" +
            $"{duration.Seconds:00}";
    }

    private DateTime DisplayedMonth { get; set; } =
        new(DateTime.Today.Year, DateTime.Today.Month, 1);

    private static string[] WeekdayNames =>
    [
        "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"
    ];

    private IEnumerable<CalendarDay> CalendarDays
    {
        get
        {
            DateTime firstDay = DisplayedMonth;

            int daysBeforeMonth =
                ((int)firstDay.DayOfWeek + 6) % 7;

            DateTime calendarStart =
                firstDay.AddDays(-daysBeforeMonth);

            for (int index = 0; index < 42; index++)
            {
                DateTime date = calendarStart.AddDays(index);

                List<FastingHistoryEntry> completedFasts = History
                    .Where(entry =>
                        entry.TargetReached &&
                        DateOnly.FromDateTime(entry.EndedAt.LocalDateTime) ==
                        DateOnly.FromDateTime(date))
                    .ToList();

                yield return new CalendarDay
                {
                    Date = date,
                    IsCurrentMonth = date.Month == DisplayedMonth.Month &&
                                     date.Year == DisplayedMonth.Year,
                    IsToday = date.Date == DateTime.Today,
                    CompletedFasts = completedFasts
                };
            }
        }
    }

    private void ShowPreviousMonth()
    {
        DisplayedMonth = DisplayedMonth.AddMonths(-1);
    }

    private void ShowNextMonth()
    {
        DisplayedMonth = DisplayedMonth.AddMonths(1);
    }

    private static string GetDayCssClass(CalendarDay day)
    {
        List<string> classes = ["calendar-day"];

        if (!day.IsCurrentMonth)
        {
            classes.Add("outside-month");
        }

        if (day.IsToday)
        {
            classes.Add("today");
        }

        if (day.TargetReached)
        {
            classes.Add("target-reached");
        }

        return string.Join(" ", classes);
    }

    private static string GetDayTooltip(CalendarDay day)
    {
        if (!day.TargetReached)
        {
            return day.Date.ToString("D", CultureInfo.CurrentCulture);
        }

        string fasts = string.Join(
            ", ",
            day.CompletedFasts.Select(entry =>
                $"{entry.FastingTypeName}: {FormatHistoryDuration(entry.Duration)}"));

        return $"{day.Date:D} — {fasts}";
    }

    
    
    public void Dispose()
    {
        FastingManager.StateChanged -=
            HandleManagerStateChanged;
    }
}