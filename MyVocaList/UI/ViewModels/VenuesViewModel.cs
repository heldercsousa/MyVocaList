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
    private readonly IThreadSafeDialogService _dialogService;
    private readonly ISnackbarService _snackbarService;
    private readonly ILogger<VenuesViewModel> _logger;

    private const int PageSize = 20;
    private int _currentPage;
    private int _totalCount;
    private string? _currentSearchQuery;
    private Timer? _searchDebounceTimer;

    private bool _isRefreshing;
    private string _searchText = string.Empty;
    private bool _isMultiSelectMode;
    private int _selectedCount;
    private BottomSheetState _bottomSheetState = BottomSheetState.Hidden;
    private string _editingVenueName = string.Empty;
    private int? _editingVenueId;
    private string _bottomSheetTitle = "New Venue";
    private string _characterCounterText = string.Empty;
    private bool _showCharacterCounter;
    private bool _isCharacterCounterWarning;
    private bool _isCharacterCounterError;
    private bool _hasMoreItems = true;
    private bool _isLoading;

    public VenuesViewModel(
        IVenueService venueService,
        IThreadSafeDialogService dialogService,
        ISnackbarService snackbarService,
        ILogger<VenuesViewModel> logger)
    {
        _venueService = venueService;
        _dialogService = dialogService;
        _snackbarService = snackbarService;
        _logger = logger;

        Venues = new ObservableCollection<VenueListItemDto>();
        SelectedVenues = new ObservableCollection<object>();

        RefreshCommand = new Command(async () => await RefreshAsync());
        LoadMoreCommand = new Command(async () => await LoadMoreAsync());
        AddVenueCommand = new Command(OpenCreateBottomSheet);
        SaveVenueCommand = new Command(async () => await SaveVenueAsync());
        CancelEditCommand = new Command(CloseBottomSheet);
        SwipeDeleteCommand = new Command<VenueListItemDto>(async item => await SwipeDeleteAsync(item));
        DeleteSelectedCommand = new Command(async () => await DeleteSelectedAsync());
        EditSelectedCommand = new Command(EditSelectedVenue);
        CancelSelectionCommand = new Command(ExitMultiSelectMode);
        TapCommand = new Command<VenueListItemDto>(OnItemTapped);
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

    public string EditingVenueName
    {
        get => _editingVenueName;
        set
        {
            if (SetProperty(ref _editingVenueName, value))
                UpdateCharacterCounter(value?.Length ?? 0);
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

    public async Task InitializeAsync()
    {
        await LoadFirstPageAsync();
    }

    private async Task LoadFirstPageAsync()
    {
        _currentPage = 1;
        _currentSearchQuery = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();

        var (items, totalCount) = await _venueService.GetPagedVenuesForListAsync(
            _currentPage, PageSize, _currentSearchQuery);

        _totalCount = totalCount;

        RunOnUiThread(() =>
        {
            Venues.Clear();
            foreach (var item in items)
                Venues.Add(item);
        });

        HasMoreItems = (_currentPage * PageSize) < _totalCount;
    }

    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadFirstPageAsync();
        IsRefreshing = false;
    }

    private async Task LoadMoreAsync()
    {
        if (_isLoading || !HasMoreItems)
            return;

        _isLoading = true;

        _currentPage++;
        var (items, totalCount) = await _venueService.GetPagedVenuesForListAsync(
            _currentPage, PageSize, _currentSearchQuery);

        _totalCount = totalCount;

        RunOnUiThread(() =>
        {
            foreach (var item in items)
                Venues.Add(item);
        });

        HasMoreItems = (_currentPage * PageSize) < _totalCount;
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
        BottomSheetTitle = "New Venue";
        BottomSheetState = BottomSheetState.HalfExpanded;
    }

    private void OpenEditBottomSheet(VenueListItemDto item)
    {
        EditingVenueId = item.Id;
        EditingVenueName = item.Name;
        BottomSheetTitle = "Edit Venue";
        BottomSheetState = BottomSheetState.HalfExpanded;
    }

    private void CloseBottomSheet()
    {
        BottomSheetState = BottomSheetState.Hidden;
        EditingVenueName = string.Empty;
        EditingVenueId = null;
    }

    private async Task SaveVenueAsync()
    {
        var name = EditingVenueName?.Trim() ?? string.Empty;

        var validation = _venueService.ValidateNameInput(name);
        if (!validation.isValid)
        {
            await _dialogService.AlertAsync("Validation", validation.message);
            return;
        }

        if (EditingVenueId.HasValue)
        {
            var (success, message) = await _venueService.UpdateVenueAsync(EditingVenueId.Value, name);
            if (success)
            {
                CloseBottomSheet();
                await RefreshAsync();
                await _snackbarService.ShowSuccessAsync("Venue updated");
            }
            else
            {
                await _dialogService.AlertAsync("Error", message);
            }
        }
        else
        {
            var (success, message, _) = await _venueService.CreateVenueAsync(name);
            if (success)
            {
                CloseBottomSheet();
                await RefreshAsync();
                await _snackbarService.ShowSuccessAsync("Venue created");
            }
            else
            {
                await _dialogService.AlertAsync("Error", message);
            }
        }
    }

    private async Task SwipeDeleteAsync(VenueListItemDto item)
    {
        var confirmed = await _dialogService.ConfirmAsync(
            "Delete Venue",
            $"Are you sure you want to delete \"{item.Name}\"?",
            "Delete", "Cancel");

        if (!confirmed)
            return;

        var (success, message) = await _venueService.DeleteVenuesAsync(new[] { item.Id });

        if (success)
        {
            await RefreshAsync();
            await _snackbarService.ShowSuccessAsync(message);
        }
        else
        {
            await _dialogService.AlertAsync("Cannot Delete", message);
        }
    }

    private async Task DeleteSelectedAsync()
    {
        var selectedItems = SelectedVenues.OfType<VenueListItemDto>().ToList();
        if (selectedItems.Count == 0)
            return;

        var confirmed = await _dialogService.ConfirmAsync(
            "Delete Venues",
            $"Are you sure you want to delete {selectedItems.Count} venue(s)?",
            "Delete", "Cancel");

        if (!confirmed)
            return;

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
            await _dialogService.AlertAsync("Cannot Delete", message);
        }
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
