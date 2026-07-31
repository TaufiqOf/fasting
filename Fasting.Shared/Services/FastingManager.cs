using Fasting.Models;
using Microsoft.Maui.Graphics;
using FastingSession = Fasting.Models.Fasting;

namespace Fasting.Shared.Services;

public sealed class FastingManager : IDisposable
{
    private readonly IFastingStateStore _stateStore;
    private readonly IFastingHistoryStore? _historyStore;
    private readonly FastingTipHelper _fastingTipHelper;
    private readonly List<FastingHistoryEntry> _history = [];

    private CancellationTokenSource? _timerCancellation;

    private bool _initialized;
    private bool _disposed;

    public FastingManager(IFastingStateStore stateStore)
    {
        _stateStore = stateStore;
        _historyStore = stateStore as IFastingHistoryStore;
        _fastingTipHelper = new FastingTipHelper();
    }

    public event Action? StateChanged;

    public IReadOnlyList<FastingHistoryEntry> History => _history;

    public CycleState State { get; private set; } =
        CycleState.None;

    public FastingSession? CurrentFasting { get; private set; }

    public Eating? CurrentEating { get; private set; }

    public DateTimeOffset CurrentTime { get; private set; } =
        DateTimeOffset.UtcNow;

    public bool HasActiveCycle =>
        State is CycleState.Fasting or CycleState.Eating;

    public bool IsFasting =>
        State == CycleState.Fasting &&
        CurrentFasting is not null;

    public bool IsEating =>
        State == CycleState.Eating &&
        CurrentEating is not null;

    public FastingType? ActiveType =>
        State switch
        {
            CycleState.Fasting => CurrentFasting?.Type,
            CycleState.Eating => CurrentEating?.Type,
            _ => null
        };

    public DateTimeOffset? ActiveStartedAt =>
        State switch
        {
            CycleState.Fasting => CurrentFasting?.StartedAt,
            CycleState.Eating => CurrentEating?.StartedAt,
            _ => null
        };

    public TimeSpan ActiveDuration =>
        State switch
        {
            CycleState.Fasting when CurrentFasting is not null =>
                TimeSpan.FromHours(
                    CurrentFasting.Type.FastingHours),

            CycleState.Eating when CurrentEating is not null =>
                TimeSpan.FromHours(
                    CurrentEating.Type.EatingHours),

            _ => TimeSpan.Zero
        };

    public DateTimeOffset? ExpectedFinishTime
    {
        get
        {
            if (ActiveStartedAt is null ||
                ActiveDuration <= TimeSpan.Zero)
            {
                return null;
            }

            return ActiveStartedAt.Value.Add(
                ActiveDuration);
        }
    }

    public TimeSpan ElapsedTime
    {
        get
        {
            if (ActiveStartedAt is null)
            {
                return TimeSpan.Zero;
            }

            TimeSpan elapsed =
                CurrentTime - ActiveStartedAt.Value;

            return elapsed > TimeSpan.Zero
                ? elapsed
                : TimeSpan.Zero;
        }
    }

    public TimeSpan RemainingTime
    {
        get
        {
            if (ExpectedFinishTime is null)
            {
                return TimeSpan.Zero;
            }

            TimeSpan remaining =
                ExpectedFinishTime.Value - CurrentTime;

            return remaining > TimeSpan.Zero
                ? remaining
                : TimeSpan.Zero;
        }
    }

    public string FastingTip => _fastingTipHelper.GetTip(ElapsedTime.TotalSeconds);
    public string ProgressColor => _fastingTipHelper.GetColor(ElapsedTime.TotalSeconds);
    public double ProgressPercentage
    {
        get
        {
            double totalSeconds =
                ActiveDuration.TotalSeconds;

            if (totalSeconds <= 0)
            {
                return 0;
            }

            double percentage =
                ElapsedTime.TotalSeconds /
                totalSeconds *
                100;

            return Math.Clamp(
                percentage,
                0,
                100);
        }
    }

    public bool IsPhaseCompleted =>
        HasActiveCycle &&
        ExpectedFinishTime is not null &&
        CurrentTime >= ExpectedFinishTime.Value;

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        CurrentTime = DateTimeOffset.UtcNow;

        if (_historyStore is not null)
        {
            IReadOnlyList<FastingHistoryEntry> savedHistory =
                await _historyStore.LoadHistoryAsync();

            _history.Clear();
            _history.AddRange(
                savedHistory.OrderByDescending(item => item.EndedAt));
        }

        FastingPersistedState? savedState =
            await _stateStore.LoadAsync();

        if (savedState is null ||
            savedState.State == CycleState.None)
        {
            NotifyStateChanged();
            return;
        }

        FastingType? type =
            FastingTypes.All.FirstOrDefault(
                item => item.Id == savedState.FastingTypeId);

        if (type is null)
        {
            await _stateStore.ClearAsync();

            NotifyStateChanged();
            return;
        }

        switch (savedState.State)
        {
            case CycleState.Fasting:
                CurrentFasting = new FastingSession
                {
                    Type = type,
                    StartedAt = savedState.StartedAt
                };

                CurrentEating = null;
                State = CycleState.Fasting;
                break;

            case CycleState.Eating:
                CurrentEating = new Eating
                {
                    Type = type,
                    StartedAt = savedState.StartedAt
                };

                CurrentFasting = null;
                State = CycleState.Eating;
                break;

            default:
                await _stateStore.ClearAsync();
                State = CycleState.None;
                break;
        }

        if (HasActiveCycle)
        {
            StartTimer();
        }

        NotifyStateChanged();
    }

    public async Task StartFastingAsync(FastingType type)
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);

        ArgumentNullException.ThrowIfNull(type);

        if (type.FastingHours <= 0)
        {
            throw new ArgumentException(
                "Fasting hours must be greater than zero.",
                nameof(type));
        }

        StopTimer();

        DateTimeOffset startedAt =
            DateTimeOffset.UtcNow;

        CurrentEating = null;

        CurrentFasting = new FastingSession
        {
            Type = type,
            StartedAt = startedAt
        };

        State = CycleState.Fasting;
        CurrentTime = startedAt;

        await SaveStateAsync();

        StartTimer();
        NotifyStateChanged();
    }

    public async Task EndFastAsync()
    {
        if (!IsFasting ||
            CurrentFasting is null)
        {
            return;
        }

        StopTimer();

        DateTimeOffset endedAt =
            DateTimeOffset.UtcNow;

        FastingType type =
            CurrentFasting.Type;

        CurrentFasting.EndedAt = endedAt;

        await AddHistoryEntryAsync(CurrentFasting);

        CurrentEating = new Eating
        {
            Type = type,
            StartedAt = endedAt
        };

        CurrentFasting = null;
        State = CycleState.Eating;
        CurrentTime = endedAt;

        await SaveStateAsync();

        StartTimer();
        NotifyStateChanged();
    }

    public async Task CancelFastAsync()
    {
        if (!IsFasting)
        {
            return;
        }

        await ResetCycleAsync();
    }

    public async Task StartNextFastAsync()
    {
        if (!IsEating ||
            CurrentEating is null)
        {
            return;
        }

        StopTimer();

        DateTimeOffset startedAt =
            DateTimeOffset.UtcNow;

        FastingType type =
            CurrentEating.Type;

        CurrentEating.EndedAt = startedAt;

        CurrentFasting = new FastingSession
        {
            Type = type,
            StartedAt = startedAt
        };

        CurrentEating = null;
        State = CycleState.Fasting;
        CurrentTime = startedAt;

        await SaveStateAsync();

        StartTimer();
        NotifyStateChanged();
    }

    public async Task CancelEatingPeriodAsync()
    {
        if (!IsEating)
        {
            return;
        }

        await ResetCycleAsync();
    }

    public async Task<bool> UpdateActiveStartTimeAsync(
        DateTimeOffset newStartTime)
    {
        if (!HasActiveCycle)
        {
            return false;
        }

        DateTimeOffset utcStartTime =
            newStartTime.ToUniversalTime();

        if (IsFasting &&
            CurrentFasting is not null)
        {
            CurrentFasting.StartedAt =
                utcStartTime;
        }
        else if (IsEating &&
                 CurrentEating is not null)
        {
            CurrentEating.StartedAt =
                utcStartTime;
        }
        else
        {
            return false;
        }

        CurrentTime = DateTimeOffset.UtcNow;

        await SaveStateAsync();

        NotifyStateChanged();

        return true;
    }

    public async Task ClearHistoryAsync()
    {
        _history.Clear();

        if (_historyStore is not null)
        {
            await _historyStore.ClearHistoryAsync();
        }

        NotifyStateChanged();
    }

    private async Task AddHistoryEntryAsync(
        FastingSession fasting)
    {
        if (fasting.EndedAt is null)
        {
            return;
        }

        _history.Insert(
            0,
            new FastingHistoryEntry
            {
                FastingTypeId = fasting.Type.Id,
                FastingTypeName = fasting.Type.Name,
                StartedAt = fasting.StartedAt,
                EndedAt = fasting.EndedAt.Value,
                TargetHours = fasting.Type.FastingHours
            });

        if (_historyStore is not null)
        {
            await _historyStore.SaveHistoryAsync(_history);
        }
    }

    public async Task ResetCycleAsync()
    {
        StopTimer();

        CurrentFasting = null;
        CurrentEating = null;

        State = CycleState.None;
        CurrentTime = DateTimeOffset.UtcNow;

        await _stateStore.ClearAsync();

        NotifyStateChanged();
    }

    private async Task SaveStateAsync()
    {
        FastingType? type = ActiveType;
        DateTimeOffset? startedAt = ActiveStartedAt;

        if (!HasActiveCycle ||
            type is null ||
            startedAt is null)
        {
            await _stateStore.ClearAsync();
            return;
        }

        await _stateStore.SaveAsync(
            new FastingPersistedState
            {
                State = State,
                FastingTypeId = type.Id,
                StartedAt = startedAt.Value
            });
    }

    private void StartTimer()
    {
        StopTimer();

        _timerCancellation =
            new CancellationTokenSource();

        _ = RunTimerAsync(
            _timerCancellation.Token);
    }

    private async Task RunTimerAsync(
        CancellationToken cancellationToken)
    {
        using PeriodicTimer timer =
            new(TimeSpan.FromSeconds(1));

        try
        {
            while (await timer.WaitForNextTickAsync(
                       cancellationToken))
            {
                if (!HasActiveCycle)
                {
                    break;
                }

                CurrentTime =
                    DateTimeOffset.UtcNow;

                NotifyStateChanged();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the timer is stopped.
        }
    }

    private void StopTimer()
    {
        CancellationTokenSource? cancellation =
            _timerCancellation;

        _timerCancellation = null;

        if (cancellation is null)
        {
            return;
        }

        cancellation.Cancel();
        cancellation.Dispose();
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        StopTimer();
        StateChanged = null;
    }
}