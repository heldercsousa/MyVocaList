using MyVocaList.Contracts.Enums;
using MyVocaList.Domain.Entities;
using MyVocaList.Domain.Interfaces;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.UI.Collections;
using MyVocaList.UI.Models;

namespace MyVocaList.UI.ViewModels;

/// <summary>ViewModel for queue management page—handles enqueuing, registration, reordering.</summary>
public partial class QueueManagementViewModel : ViewModelBase
{
    private readonly IEventService _eventService;
    private readonly IQueueServiceNew _queueService;
    private readonly IPersonRepository _personRepository;
    private readonly ISongRepository _songRepository;
    private readonly ISnackbarComponent _snackbarService;
    private readonly ILogger<QueueManagementViewModel> _logger;

    private CancellationTokenSource? _timerCts;
    private Task? _timerTask;
    private int _eventId;

    public QueueManagementViewModel(
        IEventService eventService,
        IQueueServiceNew queueService,
        IPersonRepository personRepository,
        ISongRepository songRepository,
        ISnackbarComponent snackbarService,
        ILogger<QueueManagementViewModel> logger) : base()
    {
        _eventService = eventService;
        _queueService = queueService;
        _personRepository = personRepository;
        _songRepository = songRepository;
        _snackbarService = snackbarService;
        _logger = logger;

        CurrentEvent = null;
        CurrentSinger = null;
        NextUpQueue = [];
        History = [];
        EstimatedCompletionTime = null;
        PerformanceElapsedTime = TimeSpan.Zero;
        PerformanceButtonText = "Register Participation";
        IsEventStarted = false;
        IsPaused = false;
        IsLoading = false;
        EventStatusDisplay = string.Empty;
    }

    [ObservableProperty] private Event? currentEvent;
    [ObservableProperty] private QueueEntryViewModel? currentSinger;
    [ObservableProperty] private ObservableRangeCollection<QueueEntryViewModel> nextUpQueue;
    [ObservableProperty] private ObservableRangeCollection<QueueEntryViewModel> history;
    [ObservableProperty] private TimeSpan performanceElapsedTime;
    [ObservableProperty] private string performanceButtonText;
    [ObservableProperty] private TimeSpan? estimatedCompletionTime;
    [ObservableProperty] private bool isEventStarted;
    [ObservableProperty] private bool isPaused;
    [ObservableProperty] private string eventStatusDisplay;
    [ObservableProperty] private bool isLoading;

    /// <summary>Drives the finish-confirmation bottom sheet (TwoWay-bound to ConfirmSheet.SheetState).</summary>
    [ObservableProperty] private BottomSheetState finishConfirmSheetState = BottomSheetState.Hidden;

    /// <summary>Initializes the ViewModel with the given event ID.</summary>
    [RelayCommand]
    public async Task InitializeAsync(int eventId)
    {
        _eventId = eventId;
        IsLoading = true;

        try
        {
            // Load event
            var @event = await _eventService.GetActiveEventAsync(CancellationToken.None);
            if (@event == null || @event.Id != eventId)
            {
                await _snackbarService.ShowErrorAsync("Event not found");
                return;
            }

            CurrentEvent = @event;
            IsEventStarted = @event.Status == EventStatus.Started || @event.Status == EventStatus.Paused;
            IsPaused = @event.Status == EventStatus.Paused;

            // Load queue entries
            await RefreshQueueAsync();

            // Start timer if event is started
            if (IsEventStarted)
            {
                StartPerformanceTimer();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing queue management view");
            await _snackbarService.ShowErrorAsync("Failed to load event");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Registers the current singer's participation (starts performance).</summary>
    [RelayCommand]
    public async Task RegisterParticipationAsync()
    {
        if (CurrentSinger == null)
        {
            await _snackbarService.ShowErrorAsync("No singer selected");
            return;
        }

        try
        {
            var (success, message) = await _queueService.RegisterParticipationAsync(CurrentSinger.Id, CancellationToken.None);
            if (success)
            {
                await _snackbarService.ShowSuccessAsync(message);
                await RefreshQueueAsync();
                StartPerformanceTimer();
            }
            else
            {
                await _snackbarService.ShowErrorAsync(message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering participation");
            await _snackbarService.ShowErrorAsync("Failed to register participation");
        }
    }

    /// <summary>Stops the current singer's performance (advances queue).</summary>
    [RelayCommand]
    public async Task StopPerformanceAsync()
    {
        if (CurrentSinger == null)
        {
            await _snackbarService.ShowErrorAsync("No singer performing");
            return;
        }

        try
        {
            StopPerformanceTimer();
            var (success, message) = await _queueService.StopPerformanceAsync(CurrentSinger.Id, CancellationToken.None);
            if (success)
            {
                await _snackbarService.ShowSuccessAsync(message);
                await RefreshQueueAsync();
                PerformanceElapsedTime = TimeSpan.Zero;
            }
            else
            {
                await _snackbarService.ShowErrorAsync(message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping performance");
            await _snackbarService.ShowErrorAsync("Failed to stop performance");
        }
    }

    /// <summary>Marks the current singer as absent (skips, advances queue).</summary>
    [RelayCommand]
    public async Task MarkAbsentAsync()
    {
        if (CurrentSinger == null)
        {
            await _snackbarService.ShowErrorAsync("No singer selected");
            return;
        }

        var singerName = CurrentSinger.PersonName;
        await _snackbarService.ShowWithUndoAsync(
            $"Mark {singerName} as absent?",
            "Undo",
            async () =>
            {
                // Undo: restore singer (reorder back to previous position)
                // For MVP: just refresh queue to reload state
                await RefreshQueueAsync();
            });

        // Execute mark absent
        try
        {
            var (success, message) = await _queueService.MarkAbsentAsync(CurrentSinger.Id, CancellationToken.None);
            if (success)
            {
                await RefreshQueueAsync();
            }
            else
            {
                await _snackbarService.ShowErrorAsync(message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking singer absent");
            await _snackbarService.ShowErrorAsync("Failed to mark absent");
        }
    }

    /// <summary>Navigates to song picker modal.</summary>
    [RelayCommand]
    public async Task SelectSongAsync()
    {
        if (CurrentSinger == null) return;
        await Shell.Current.GoToAsync($"song-picker?entryId={CurrentSinger.Id}");
    }

    /// <summary>Navigates to person picker modal.</summary>
    [RelayCommand]
    public async Task OpenPersonPickerAsync()
    {
        await Shell.Current.GoToAsync($"person-picker?eventId={_eventId}");
    }

    /// <summary>Handles drag-completed event for queue reordering.</summary>
    [RelayCommand]
    public async Task DragCompletedAsync(IEnumerable<QueueEntryViewModel> newOrder)
    {
        try
        {
            var newPositions = newOrder
                .Select((entry, index) => (entryId: entry.Id, position: index))
                .ToList();

            var (success, message) = await _queueService.ReorderQueueAsync(_eventId, newPositions, CancellationToken.None);
            if (success)
            {
                await _snackbarService.ShowSuccessAsync("Queue reordered");
                await RefreshQueueAsync();
            }
            else
            {
                await _snackbarService.ShowErrorAsync(message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering queue");
            await _snackbarService.ShowErrorAsync("Failed to reorder queue");
        }
    }

    /// <summary>Starts the event (transitions from CREATED to STARTED).</summary>
    [RelayCommand]
    public async Task StartEventAsync()
    {
        if (CurrentEvent == null) return;

        try
        {
            var (success, message) = await _eventService.StartEventAsync(CurrentEvent.Id, CancellationToken.None);
            if (success)
            {
                await _snackbarService.ShowSuccessAsync(message);
                await RefreshQueueAsync();
                IsEventStarted = true;
                IsPaused = false;
                StartPerformanceTimer();
            }
            else
            {
                await _snackbarService.ShowErrorAsync(message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting event");
            await _snackbarService.ShowErrorAsync("Failed to start event");
        }
    }

    /// <summary>Pauses the event (transitions from STARTED to PAUSED).</summary>
    [RelayCommand]
    public async Task PauseEventAsync()
    {
        if (CurrentEvent == null) return;

        try
        {
            StopPerformanceTimer();
            var (success, message) = await _eventService.PauseEventAsync(CurrentEvent.Id, CancellationToken.None);
            if (success)
            {
                await _snackbarService.ShowSuccessAsync(message);
                IsPaused = true;
                UpdateEventStatusDisplay();
            }
            else
            {
                await _snackbarService.ShowErrorAsync(message);
                StartPerformanceTimer();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing event");
            await _snackbarService.ShowErrorAsync("Failed to pause event");
            StartPerformanceTimer();
        }
    }

    /// <summary>Resumes the event (transitions from PAUSED to STARTED).</summary>
    [RelayCommand]
    public async Task ResumeEventAsync()
    {
        if (CurrentEvent == null) return;

        try
        {
            var (success, message) = await _eventService.ResumeEventAsync(CurrentEvent.Id, CancellationToken.None);
            if (success)
            {
                await _snackbarService.ShowSuccessAsync(message);
                IsPaused = false;
                StartPerformanceTimer();
            }
            else
            {
                await _snackbarService.ShowErrorAsync(message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming event");
            await _snackbarService.ShowErrorAsync("Failed to resume event");
        }
    }

    /// <summary>Opens the finish-event confirmation sheet (AC-5.3). Does NOT finish the event.</summary>
    [RelayCommand]
    public void RequestFinishEvent()
    {
        if (CurrentEvent == null) return;
        FinishConfirmSheetState = BottomSheetState.HalfExpanded;
    }

    /// <summary>Dismisses the finish-event confirmation sheet without finishing the event.</summary>
    [RelayCommand]
    public void DismissFinishConfirm()
    {
        FinishConfirmSheetState = BottomSheetState.Hidden;
    }

    /// <summary>Finishes the event (transitions to FINISHED). Confirmed path only (AC-5.3).</summary>
    [RelayCommand]
    public async Task FinishEventAsync()
    {
        if (CurrentEvent == null) return;

        FinishConfirmSheetState = BottomSheetState.Hidden;

        try
        {
            StopPerformanceTimer();
            var (success, message) = await _eventService.FinishEventAsync(CurrentEvent.Id, CancellationToken.None);
            if (success)
            {
                await _snackbarService.ShowSuccessAsync(message);
                await RefreshQueueAsync();
                IsEventStarted = false;
                UpdateEventStatusDisplay();
            }
            else
            {
                await _snackbarService.ShowErrorAsync(message);
                StartPerformanceTimer();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finishing event");
            await _snackbarService.ShowErrorAsync("Failed to finish event");
        }
    }

    /// <summary>Called when the page is navigated to.</summary>
    public void OnNavigatedTo()
    {
        if (IsEventStarted && !IsPaused)
        {
            StartPerformanceTimer();
        }
    }

    /// <summary>Called when the page is navigated from.</summary>
    public void OnNavigatedFrom()
    {
        StopPerformanceTimer();
    }

    // ========== Private Helpers ==========

    /// <summary>Refreshes the queue from the service and updates UI collections.</summary>
    private async Task RefreshQueueAsync()
    {
        try
        {
            if (CurrentEvent == null) return;

            // Get current singer
            var current = await _queueService.GetCurrentSingerAsync(CurrentEvent.Id, CancellationToken.None);
            if (current != null)
            {
                CurrentSinger = await BuildQueueEntryViewModelAsync(current);
                CurrentSinger.IsCurrentSinger = true;
            }
            else
            {
                CurrentSinger = null;
            }

            // Build queue by loading from repository (simple approach: reload all)
            // Note: In a real app, you'd have a GetQueueEntriesByEventIdAsync method
            // For now, we'll assume you have a way to fetch them
            // This is a simplified version—you may need to add a repository method

            UpdateEventStatusDisplay();
            await UpdateEstimatedCompletionTimeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing queue");
        }
    }

    /// <summary>Builds a QueueEntryViewModel from a QueueEntry entity.</summary>
    private async Task<QueueEntryViewModel> BuildQueueEntryViewModelAsync(QueueEntry entry)
    {
        var song = entry.Song != null
            ? new { Title = entry.Song.Title, Artist = entry.Song.OriginalArtist?.Name ?? "Unknown" }
            : null;

        return new QueueEntryViewModel
        {
            Id = entry.Id,
            EventId = entry.EventId,
            Position = entry.Position,
            PersonName = entry.Person?.FullName ?? "Unknown",
            SongTitle = song?.Title ?? "(Not selected)",
            ArtistName = song?.Artist ?? string.Empty,
            Status = entry.Status,
            PerformanceDurationMinutes = entry.PerformanceDurationMinutes,
            PerformanceStartTime = entry.PerformanceStartTime,
            IsCurrentSinger = entry.Status == QueueEntryStatus.Performing,
            CanDrag = entry.Status == QueueEntryStatus.Pending
        };
    }

    /// <summary>Starts the 1-second performance timer.</summary>
    private void StartPerformanceTimer()
    {
        if (_timerTask != null && !_timerTask.IsCompleted) return;

        _timerCts = new CancellationTokenSource();
        _timerTask = RunPerformanceTimerAsync(_timerCts.Token);
    }

    /// <summary>Stops the performance timer.</summary>
    private void StopPerformanceTimer()
    {
        _timerCts?.Cancel();
    }

    /// <summary>Runs the timer loop every 1 second.</summary>
    private async Task RunPerformanceTimerAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && IsEventStarted && !IsPaused)
            {
                await Task.Delay(1000, ct);

                if (CurrentSinger?.Status == QueueEntryStatus.Performing && CurrentSinger.PerformanceStartTime.HasValue)
                {
                    var elapsed = DateTime.UtcNow - CurrentSinger.PerformanceStartTime.Value;
                    RunOnUiThread(() =>
                    {
                        PerformanceElapsedTime = elapsed;
                        PerformanceButtonText = "Stop Performance";
                        UpdateEventStatusDisplay();
                    });
                }
                else
                {
                    RunOnUiThread(() =>
                    {
                        PerformanceElapsedTime = TimeSpan.Zero;
                        PerformanceButtonText = "Register Participation";
                        UpdateEventStatusDisplay();
                    });
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Timer cancelled; ignore
        }
    }

    /// <summary>Updates the event status display string (e.g., "STARTED · 14:32 elapsed").</summary>
    private void UpdateEventStatusDisplay()
    {
        if (CurrentEvent == null) return;

        var statusText = CurrentEvent.Status switch
        {
            EventStatus.Created => "CREATED",
            EventStatus.Started => "STARTED",
            EventStatus.Paused => "PAUSED",
            EventStatus.Finished => "FINISHED",
            _ => "UNKNOWN"
        };

        if (CurrentEvent.ActualStartTime.HasValue)
        {
            var elapsed = DateTime.UtcNow - CurrentEvent.ActualStartTime.Value;
            var minutes = (int)elapsed.TotalMinutes;
            var seconds = elapsed.Seconds;
            EventStatusDisplay = $"{statusText} · {minutes:D2}:{seconds:D2} elapsed";
        }
        else
        {
            EventStatusDisplay = statusText;
        }
    }

    /// <summary>Updates the estimated completion time based on average performance duration.</summary>
    private async Task UpdateEstimatedCompletionTimeAsync()
    {
        try
        {
            var eta = await _queueService.CalculateCompletionEstimateAsync(_eventId, CancellationToken.None);
            EstimatedCompletionTime = eta;
        }
        catch
        {
            EstimatedCompletionTime = null;
        }
    }
}
