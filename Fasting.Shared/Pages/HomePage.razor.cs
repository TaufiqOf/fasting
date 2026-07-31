using System.Globalization;
using Fasting.Models;
using Fasting.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace Fasting.Shared.Pages;

public partial class HomePage : IDisposable
{
    [Inject]
    private FastingManager FastingManager { get; set; } = default!;

    private DateTime DisplayedMonth { get; set; } =
        new(DateTime.Today.Year, DateTime.Today.Month, 1);

    private IReadOnlyList<FastingHistoryEntry> History =>
        FastingManager.History;

    private static IReadOnlyList<string> WeekdayNames { get; } =
    [
        "Mon",
        "Tue",
        "Wed",
        "Thu",
        "Fri",
        "Sat",
        "Sun"
    ];

    private string Greeting =>
        DateTime.Now.Hour switch
        {
            >= 5 and < 12 => "Good morning",
            >= 12 and < 18 => "Good afternoon",
            _ => "Good evening"
        };

    private double ProgressWidth =>
        Math.Clamp(
            FastingManager.ProgressPercentage,
            0,
            100);

    private string PhaseDescription =>
        FastingManager.IsFasting
            ? "Your fasting cycle is currently active."
            : FastingManager.IsEating
                ? "Your eating period is currently active."
                : "No active fasting cycle.";

    private string RemainingLabel =>
        FastingManager.IsFasting
            ? "Fasting period ends"
            : "Eating period ends";

    private string ElapsedTimeText =>
        FormatDuration(FastingManager.ElapsedTime);

    private MarkupString RemainingTimeText =>
        FastingManager.IsPhaseCompleted
            ? new MarkupString("Target completed")
            : new MarkupString(
                $"{FastingManager.ExpectedFinishTime?.ToLocalTime():ddd dd MMM yyyy HH:mm}<br/>" +
                $"({FormatDuration(FastingManager.RemainingTime)})");
    private int CurrentStreak =>
        CalculateCurrentStreak();

    private int CompletedThisMonth =>
        History.Count(entry =>
        {
            DateTime localEnd = entry.EndedAt.LocalDateTime;

            return entry.TargetReached &&
                   localEnd.Year == DisplayedMonth.Year &&
                   localEnd.Month == DisplayedMonth.Month;
        });

    private int TotalCompletedFasts =>
        History.Count(entry => entry.TargetReached);

    private string LongestFastText
    {
        get
        {
            FastingHistoryEntry? longestFast = History
                .Where(entry => entry.TargetReached)
                .OrderByDescending(entry => entry.Duration)
                .FirstOrDefault();

            return longestFast is null
                ? "0h 0m"
                : FormatDuration(longestFast.Duration);
        }
    }

    private IReadOnlyList<FastingHistoryEntry> RecentHistory =>
        History
            .OrderByDescending(entry => entry.EndedAt)
            .Take(5)
            .ToList();

    private IEnumerable<DashboardCalendarDay> CalendarDays
    {
        get
        {
            DateTime firstDay = DisplayedMonth;

            // Convert Sunday-based DayOfWeek into Monday-based positioning.
            int daysBeforeMonth =
                ((int)firstDay.DayOfWeek + 6) % 7;

            DateTime calendarStart =
                firstDay.AddDays(-daysBeforeMonth);

            for (int index = 0; index < 42; index++)
            {
                DateTime date = calendarStart.AddDays(index);
                DateOnly calendarDate = DateOnly.FromDateTime(date);

                List<FastingHistoryEntry> matchingEntries = History
                    .Where(entry =>
                        entry.TargetReached &&
                        DateOnly.FromDateTime(
                            entry.EndedAt.LocalDateTime) == calendarDate)
                    .ToList();

                yield return new DashboardCalendarDay
                {
                    Date = date,

                    IsCurrentMonth =
                        date.Year == DisplayedMonth.Year &&
                        date.Month == DisplayedMonth.Month,

                    IsToday =
                        date.Date == DateTime.Today,

                    CompletedFasts =
                        matchingEntries
                };
            }
        }
    }
    private bool _initialized;

    protected override void OnInitialized()
    {
        FastingManager.StateChanged += HandleFastingManagerStateChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _initialized)
        {
            return;
        }

        _initialized = true;

        await FastingManager.InitializeAsync();

        await InvokeAsync(StateHasChanged);
    }
    private void HandleFastingManagerStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private int CalculateCurrentStreak()
    {
        HashSet<DateOnly> completedDates = History
            .Where(entry => entry.TargetReached)
            .Select(entry =>
                DateOnly.FromDateTime(
                    entry.EndedAt.LocalDateTime))
            .ToHashSet();

        if (completedDates.Count == 0)
        {
            return 0;
        }

        DateOnly today =
            DateOnly.FromDateTime(DateTime.Today);

        DateOnly dateToCheck = today;

        /*
         * Allow the streak to remain active when today's fast
         * has not been completed yet, provided yesterday was completed.
         */
        if (!completedDates.Contains(dateToCheck))
        {
            dateToCheck = dateToCheck.AddDays(-1);
        }

        int streak = 0;

        while (completedDates.Contains(dateToCheck))
        {
            streak++;
            dateToCheck = dateToCheck.AddDays(-1);
        }

        return streak;
    }

    private void ShowPreviousMonth()
    {
        DisplayedMonth =
            DisplayedMonth.AddMonths(-1);
    }

    private void ShowNextMonth()
    {
        DisplayedMonth =
            DisplayedMonth.AddMonths(1);
    }

    private static string GetCalendarDayCss(
        DashboardCalendarDay day)
    {
        List<string> classes =
        [
            "calendar-day"
        ];

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

    private static string GetCalendarDayTooltip(
        DashboardCalendarDay day)
    {
        string dateText =
            day.Date.ToString(
                "D",
                CultureInfo.CurrentCulture);

        if (!day.TargetReached)
        {
            return dateText;
        }

        string fastDescriptions = string.Join(
            ", ",
            day.CompletedFasts.Select(entry =>
                $"{entry.FastingTypeName}: " +
                FormatDuration(entry.Duration)));

        return $"{dateText} — {fastDescriptions}";
    }

    private static string FormatRelativeDate(
        DateTimeOffset date)
    {
        DateTime localDate =
            date.LocalDateTime.Date;

        int days =
            (DateTime.Today - localDate).Days;

        return days switch
        {
            < 0 => date.ToLocalTime().ToString(
                "dd MMM yyyy",
                CultureInfo.CurrentCulture),

            0 => "Today",

            1 => "Yesterday",

            > 1 and < 7 => $"{days} days ago",

            _ => date.ToLocalTime().ToString(
                "dd MMM yyyy",
                CultureInfo.CurrentCulture)
        };
    }

    private static string FormatDuration(
        TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            duration = TimeSpan.Zero;
        }

        int totalHours =
            (int)duration.TotalHours;

        return duration.Seconds > 0
            ? $"{totalHours}h {duration.Minutes:D2}m {duration.Seconds:D2}s"
            : $"{totalHours}h {duration.Minutes:D2}m";
    }

    public void Dispose()
    {
        FastingManager.StateChanged -=
            HandleFastingManagerStateChanged;
    }

    private sealed class DashboardCalendarDay
    {
        public required DateTime Date { get; init; }

        public bool IsCurrentMonth { get; init; }

        public bool IsToday { get; init; }

        public List<FastingHistoryEntry> CompletedFasts { get; init; } = [];

        public bool TargetReached =>
            CompletedFasts.Count > 0;
    }
}