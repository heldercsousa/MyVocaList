using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DevExpress.Maui.Controls;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Services;
using MyVocaList.UI.Services;
using Microsoft.Extensions.Logging;

namespace MyVocaList.UI.ViewModels;

/// <summary>
/// ViewModel for Venues CRUD page with paging, search, multi-select, and BottomSheet
/// </summary>
public class VenuesViewModel : INotifyPropertyChanged
{
    private readonly IVenueService _venueService;
    private readonly ISnackbarService _snackbarService;
    private readonly ILogger<VenuesViewModel> _logger;

    private const int PageSize = 20;
    private int _currentPage;
    private int _totalCount;
    private string? _currentSearchQuery;
    private Timer? _searchDebounceTimer;
    private Func<Task>? _pendingConfirmAction;

    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);

    private bool _isRefreshing;
    private string _searchText = string.Empty;
    private bool _isMultiSelectMode;
    private int _selectedCount;
    private BottomSheetState _bottomSheetState = BottomSheetState.Hidden;
    private BottomSheetState _confirmSheetState = BottomSheetState.Hidden;
    private string _editingVenueName = string.Empty;
    private int? _editingVenueId;
    private string _bottomSheetTitle = "New Venue";
    private string _characterCounterText = string.Empty;
    private bool _showCharacterCounter;
    private bool _isCharacterCounterWarning;
    private bool _isCharacterCounterError;
    private bool _hasMoreItems = true;
    private bool _isLoading;
    private bool _isInitialLoading = true;
    private bool _venueNameHasError;
    private string _venueNameErrorText = string.Empty;
    private string _confirmMessage = string.Empty;
    private string _confirmActionText = "Delete";

    public VenuesViewModel(
        IVenueService venueService,
        ISnackbarService snackbarService,
        ILogger<VenuesViewModel> logger)
    {
        _venueService = venueService;
        _snackbarService = snackbarService;
        _logger = logger;

        Venues = new ObservableCollection<VenueListItemDto>();
        SelectedVenues = new ObservableCollection<object>();

        RefreshCommand = new Command(async () => await RefreshAsync());
        LoadMoreCommand = new Command(async () => await LoadMoreAsync());
        AddVenueCommand = new Command(OpenCreateBottomSheet);
        SaveVenueCommand = new Command(async () => await SaveVenueAsync());
        CancelEditCommand = new Command(CloseEditSheet);
        SwipeDeleteCommand = new Command<VenueListItemDto>(RequestSwipeDelete);
        DeleteSelectedCommand = new Command(RequestBatchDelete);
        EditSelectedCommand = new Command(EditSelectedVenue);
        CancelSelectionCommand = new Command(ExitMultiSelectMode);
        TapCommand = new Command<VenueListItemDto>(OnItemTapped);
        ConfirmActionCommand = new Command(async () => await ExecuteConfirmActionAsync());
        DismissConfirmCommand = new Command(DismissConfirmSheet);
    }

    public ObservableCollection<VenueListItemDto> Venues { get; }
    public ObservableCollection<object> SelectedVenues { get; }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                OnSearchTextChanged(value);
        }
    }

    public bool IsMultiSelectMode
    {
        get => _isMultiSelectMode;
        set
        {
            if (SetProperty(ref _isMultiSelectMode, value))
            {
                OnPropertyChanged(nameof(SelectionMode));
                OnPropertyChanged(nameof(ShowDefaultTitle));
                OnPropertyChanged(nameof(ShowMultiSelectToolbar));
            }
        }
    }

    public int SelectedCount
    {
        get => _selectedCount;
        set
        {
            if (SetProperty(ref _selectedCount, value))
            {
                OnPropertyChanged(nameof(SelectedCountText));
                OnPropertyChanged(nameof(CanEditSelected));
            }
        }
    }

    public string SelectedCountText => $"{SelectedCount} selected";
    public bool CanEditSelected => SelectedCount == 1;
    public bool ShowDefaultTitle => !IsMultiSelectMode;
    public bool ShowMultiSelectToolbar => IsMultiSelectMode;

    public SelectionMode SelectionMode =>
        IsMultiSelectMode ? SelectionMode.Multiple : SelectionMode.None;

    public BottomSheetState BottomSheetState
    {
        get => _bottomSheetState;
        set => SetProperty(ref _bottomSheetState, value);
    }

    public BottomSheetState ConfirmSheetState
    {
        get => _confirmSheetState;
        set => SetProperty(ref _confirmSheetState, value);
    }

    public string EditingVenueName
    {
        get => _editingVenueName;
        set
        {
            if (SetProperty(ref _editingVenueName, value))
            {
                ClearVenueNameError();
                UpdateCharacterCounter(value?.Length ?? 0);
            }
        }
    }

    public int? EditingVenueId
    {
        get => _editingVenueId;
        set => SetProperty(ref _editingVenueId, value);
    }

    public string BottomSheetTitle
    {
        get => _bottomSheetTitle;
        set => SetProperty(ref _bottomSheetTitle, value);
    }

    public string CharacterCounterText
    {
        get => _characterCounterText;
        set => SetProperty(ref _characterCounterText, value);
    }

    public bool ShowCharacterCounter
    {
        get => _showCharacterCounter;
        set => SetProperty(ref _showCharacterCounter, value);
    }

    public bool IsCharacterCounterWarning
    {
        get => _isCharacterCounterWarning;
        set => SetProperty(ref _isCharacterCounterWarning, value);
    }

    public bool IsCharacterCounterError
    {
        get => _isCharacterCounterError;
        set => SetProperty(ref _isCharacterCounterError, value);
    }

    public bool HasMoreItems
    {
        get => _hasMoreItems;
        set => SetProperty(ref _hasMoreItems, value);
    }

    public bool IsInitialLoading
    {
        get => _isInitialLoading;
        set
        {
            if (SetProperty(ref _isInitialLoading, value))
                OnPropertyChanged(nameof(IsEmpty));
        }
    }

    public bool IsEmpty => !IsInitialLoading && Venues.Count == 0;

    public bool VenueNameHasError
    {
        get => _venueNameHasError;
        set => SetProperty(ref _venueNameHasError, value);
    }

    public string VenueNameErrorText
    {
        get => _venueNameErrorText;
        set => SetProperty(ref _venueNameErrorText, value);
    }

    public string ConfirmMessage
    {
        get => _confirmMessage;
        set => SetProperty(ref _confirmMessage, value);
    }

    public string ConfirmActionText
    {
        get => _confirmActionText;
        set => SetProperty(ref _confirmActionText, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand LoadMoreCommand { get; }
    public ICommand AddVenueCommand { get; }
    public ICommand SaveVenueCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand SwipeDeleteCommand { get; }
    public ICommand DeleteSelectedCommand { get; }
    public ICommand EditSelectedCommand { get; }
    public ICommand CancelSelectionCommand { get; }
    public ICommand TapCommand { get; }
    public ICommand ConfirmActionCommand { get; }
    public ICommand DismissConfirmCommand { get; }

    public async Task InitializeAsync()
    {
        IsInitialLoading = true;
        await Task.Run(() => LoadFirstPageAsync());
        RunOnUiThread(() => IsInitialLoading = false);
    }

    private async Task LoadFirstPageAsync()
    {
        await _loadSemaphore.WaitAsync();
        try
        {
            _currentPage = 1;
            _currentSearchQuery = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();

            RunOnUiThread(() => Venues.Clear());

            var (items, totalCount) = await _venueService.GetPagedVenuesForListAsync(
                _currentPage, PageSize, _currentSearchQuery);

            _totalCount = totalCount;

            RunOnUiThread(() =>
            {
                foreach (var item in items)
                    Venues.Add(item);
                HasMoreItems = (_currentPage * PageSize) < _totalCount;
                OnPropertyChanged(nameof(IsEmpty));
            });
        }
        finally
        {
            _loadSemaphore.Release();
        }
    }

    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadFirstPageAsync();
        RunOnUiThread(() => IsRefreshing = false);
    }

    private async Task LoadMoreAsync()
    {
        if (_isLoading || !HasMoreItems)
            return;

        _isLoading = true;
        HasMoreItems = false;           // dismiss footer immediately

        _currentPage++;
        var (items, totalCount) = await _venueService.GetPagedVenuesForListAsync(
            _currentPage, PageSize, _currentSearchQuery);

        _totalCount = totalCount;

        RunOnUiThread(() => { foreach (var item in items) Venues.Add(item); });

        HasMoreItems = (_currentPage * PageSize) < _totalCount;     // restore
        _isLoading = false;
    }

    private void OnSearchTextChanged(string text)
    {
        _searchDebounceTimer?.Dispose();
        _searchDebounceTimer = new Timer(async _ =>
        {
            await LoadFirstPageAsync();
        }, null, 400, Timeout.Infinite);
    }

    private void OnItemTapped(VenueListItemDto? item)
    {
        if (item == null || IsMultiSelectMode)
            return;

        OpenEditBottomSheet(item);
    }

    private void OpenCreateBottomSheet()
    {
        EditingVenueId = null;
        EditingVenueName = string.Empty;
        ClearVenueNameError();
        BottomSheetTitle = "New Venue";
        BottomSheetState = BottomSheetState.HalfExpanded;
    }

    private void OpenEditBottomSheet(VenueListItemDto item)
    {
        EditingVenueId = item.Id;
        EditingVenueName = item.Name;
        ClearVenueNameError();
        BottomSheetTitle = "Edit Venue";
        BottomSheetState = BottomSheetState.HalfExpanded;
    }

    private void CloseEditSheet()
    {
        BottomSheetState = BottomSheetState.Hidden;
        EditingVenueName = string.Empty;
        EditingVenueId = null;
        ClearVenueNameError();
    }

    private void ClearVenueNameError()
    {
        VenueNameHasError = false;
        VenueNameErrorText = string.Empty;
    }

    private async Task SaveVenueAsync()
    {
        var name = EditingVenueName?.Trim() ?? string.Empty;

        var validation = _venueService.ValidateNameInput(name);
        if (!validation.isValid)
        {
            VenueNameHasError = true;
            VenueNameErrorText = validation.message;
            return;
        }

        if (EditingVenueId.HasValue)
        {
            var (success, message) = await _venueService.UpdateVenueAsync(EditingVenueId.Value, name);
            if (success)
            {
                CloseEditSheet();
                await RefreshAsync();
                await _snackbarService.ShowSuccessAsync("Venue updated");
            }
            else
            {
                VenueNameHasError = true;
                VenueNameErrorText = message;
            }
        }
        else
        {
            var (success, message, _) = await _venueService.CreateVenueAsync(name);
            if (success)
            {
                CloseEditSheet();
                await RefreshAsync();
                await _snackbarService.ShowSuccessAsync("Venue created");
            }
            else
            {
                VenueNameHasError = true;
                VenueNameErrorText = message;
            }
        }
    }

    private void RequestSwipeDelete(VenueListItemDto item)
    {
        ConfirmMessage = $"Delete \"{item.Name}\"?";
        ConfirmActionText = "Delete";
        _pendingConfirmAction = async () =>
        {
            var (success, message) = await _venueService.DeleteVenuesAsync(new[] { item.Id });
            if (success)
            {
                await RefreshAsync();
                await _snackbarService.ShowSuccessAsync(message);
            }
            else
            {
                await _snackbarService.ShowErrorAsync(message);
            }
        };
        ConfirmSheetState = BottomSheetState.HalfExpanded;
    }

    private void RequestBatchDelete()
    {
        var selectedItems = SelectedVenues.OfType<VenueListItemDto>().ToList();
        if (selectedItems.Count == 0)
            return;

        ConfirmMessage = $"Delete {selectedItems.Count} venue(s)?";
        ConfirmActionText = "Delete";
        _pendingConfirmAction = async () =>
        {
            var ids = selectedItems.Select(v => v.Id);
            var (success, message) = await _venueService.DeleteVenuesAsync(ids);
            ExitMultiSelectMode();
            if (success)
            {
                await RefreshAsync();
                await _snackbarService.ShowSuccessAsync(message);
            }
            else
            {
                await _snackbarService.ShowErrorAsync(message);
            }
        };
        ConfirmSheetState = BottomSheetState.HalfExpanded;
    }

    private async Task ExecuteConfirmActionAsync()
    {
        DismissConfirmSheet();
        if (_pendingConfirmAction != null)
        {
            var action = _pendingConfirmAction;
            _pendingConfirmAction = null;
            await action();
        }
    }

    private void DismissConfirmSheet()
    {
        ConfirmSheetState = BottomSheetState.Hidden;
        _pendingConfirmAction = null;
    }

    private void EditSelectedVenue()
    {
        var selectedItem = SelectedVenues.OfType<VenueListItemDto>().FirstOrDefault();
        if (selectedItem == null)
            return;

        ExitMultiSelectMode();
        OpenEditBottomSheet(selectedItem);
    }

    public void EnterMultiSelectMode(VenueListItemDto initialItem)
    {
        IsMultiSelectMode = true;
        RunOnUiThread(() =>
        {
            SelectedVenues.Clear();
            SelectedVenues.Add(initialItem);
        });
        SelectedCount = 1;
    }

    public void ExitMultiSelectMode()
    {
        IsMultiSelectMode = false;
        RunOnUiThread(() => SelectedVenues.Clear());
        SelectedCount = 0;
    }

    public void OnSelectionChanged(int count)
    {
        SelectedCount = count;
        if (IsMultiSelectMode && count == 0)
            ExitMultiSelectMode();
    }

    private void UpdateCharacterCounter(int length)
    {
        ShowCharacterCounter = _venueService.ShouldShowCharacterCounter(length);
        if (ShowCharacterCounter)
        {
            var (text, isWarning, isError) = _venueService.GetCharacterCounterInfo(length);
            CharacterCounterText = text;
            IsCharacterCounterWarning = isWarning;
            IsCharacterCounterError = isError;
        }
    }

    protected void RunOnUiThread(Action action)
    {
        if (Application.Current?.Dispatcher.IsDispatchRequired == true)
            Application.Current.Dispatcher.Dispatch(action);
        else
            action();
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}
