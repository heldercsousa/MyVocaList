using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Maui.Controls;
using DevExpress.Maui.Core;
using Microsoft.Extensions.Logging;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Services;
using MyVocaList.UI.Collections;
using MyVocaList.UI.Services;

namespace MyVocaList.UI.ViewModels
{
    /// <summary>
    /// ViewModel for Venues CRUD page with paging, search, multi-select, and BottomSheet.
    /// </summary>
    public partial class VenuesViewModel : ViewModelBase
    {
        private readonly IVenueService _venueService;
        private readonly ISnackbarService _snackbarService;
        private readonly ILogger<VenuesViewModel> _logger;

        private const int PageSize = 20;
        private int _currentPage;
        private int _totalCount;
        private string? _currentSearchQuery;
        private CancellationTokenSource? _searchCts;
        private Func<Task>? _pendingConfirmAction;

        private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
        private bool _suppressSelectionChangedExit;
        private bool _isLoading;

        [ObservableProperty] private bool _isRefreshing;
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private bool _isMultiSelectMode;
        [ObservableProperty] private int _selectedCount;
        [ObservableProperty] private BottomSheetState _bottomSheetState = BottomSheetState.Hidden;
        [ObservableProperty] private BottomSheetState _confirmSheetState = BottomSheetState.Hidden;
        [ObservableProperty] private string _editingVenueName = string.Empty;
        [ObservableProperty] private int? _editingVenueId;
        [ObservableProperty] private string _bottomSheetTitle = "New Venue";
        [ObservableProperty] private string _characterCounterText = string.Empty;
        [ObservableProperty] private bool _showCharacterCounter;
        [ObservableProperty] private bool _isCharacterCounterWarning;
        [ObservableProperty] private bool _isCharacterCounterError;
        [ObservableProperty] private bool _hasMoreItems = true;
        [ObservableProperty] private bool _isInitialLoading = true;
        [ObservableProperty] private bool _venueNameHasError;
        [ObservableProperty] private string _venueNameErrorText = string.Empty;
        [ObservableProperty] private string _confirmMessage = string.Empty;
        [ObservableProperty] private string _confirmActionText = "Delete";

        public VenuesViewModel(
            IVenueService venueService,
            ISnackbarService snackbarService,
            ILogger<VenuesViewModel> logger)
        {
            _venueService = venueService;
            _snackbarService = snackbarService;
            _logger = logger;

            Venues = new ObservableRangeCollection<VenueListItemDto>();
            SelectedVenues = new ObservableRangeCollection<VenueListItemDto>();

            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            LoadMoreCommand = new AsyncRelayCommand(LoadMoreAsync);
            AddVenueCommand = new RelayCommand(OpenCreateBottomSheet);
            SaveVenueCommand = new AsyncRelayCommand(SaveVenueAsync);
            CancelEditCommand = new RelayCommand(CloseEditSheet);
            SwipeDeleteCommand = new RelayCommand<VenueListItemDto>(item => RequestSwipeDelete(item!));
            DeleteSelectedCommand = new RelayCommand(RequestBatchDelete);
            EditSelectedCommand = new RelayCommand(EditSelectedVenue);
            CancelSelectionCommand = new RelayCommand(ExitMultiSelectMode);
            TapCommand = new RelayCommand<VenueListItemDto>(OnItemTapped);
            SelectAllCommand = new RelayCommand(ToggleSelectAll);
            ConfirmActionCommand = new AsyncRelayCommand(ExecuteConfirmActionAsync);
            DismissConfirmCommand = new RelayCommand(DismissConfirmSheet);
        }

        public ObservableRangeCollection<VenueListItemDto> Venues { get; }
        public ObservableRangeCollection<VenueListItemDto> SelectedVenues { get; }

        /// <summary>Non-generic wrapper for binding to DXCollectionView SelectedItems.</summary>
        public System.Collections.IList SelectedVenuesRaw => SelectedVenues;

        public string SelectedCountText => $"{SelectedCount} selected";
        public bool CanEditSelected => SelectedCount == 1;
        public bool ShowDefaultTitle => !IsMultiSelectMode;
        public bool ShowMultiSelectToolbar => IsMultiSelectMode;

        public SelectionMode SelectionMode =>
            IsMultiSelectMode ? SelectionMode.Multiple : SelectionMode.None;

        public bool IsAllSelected => IsMultiSelectMode && Venues.Count > 0 && SelectedCount == Venues.Count;

        public bool IsEmpty => !IsInitialLoading && Venues.Count == 0;
        public bool IsEmptyNoVenues => IsEmpty && string.IsNullOrWhiteSpace(SearchText);
        public bool IsEmptyNoResults => IsEmpty && !string.IsNullOrWhiteSpace(SearchText);

        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand LoadMoreCommand { get; }
        public IRelayCommand AddVenueCommand { get; }
        public IAsyncRelayCommand SaveVenueCommand { get; }
        public IRelayCommand CancelEditCommand { get; }
        public IRelayCommand<VenueListItemDto> SwipeDeleteCommand { get; }
        public IRelayCommand DeleteSelectedCommand { get; }
        public IRelayCommand EditSelectedCommand { get; }
        public IRelayCommand CancelSelectionCommand { get; }
        public IRelayCommand<VenueListItemDto> TapCommand { get; }
        public IRelayCommand SelectAllCommand { get; }
        public IAsyncRelayCommand ConfirmActionCommand { get; }
        public IRelayCommand DismissConfirmCommand { get; }

        partial void OnSearchTextChanged(string value)
        {
            NotifyEmptyStates();
            TriggerSearchDebounce(value);
        }

        partial void OnIsMultiSelectModeChanged(bool value)
        {
            OnPropertyChanged(nameof(SelectionMode));
            OnPropertyChanged(nameof(ShowDefaultTitle));
            OnPropertyChanged(nameof(ShowMultiSelectToolbar));
            OnPropertyChanged(nameof(IsAllSelected));
        }

        partial void OnSelectedCountChanged(int value)
        {
            OnPropertyChanged(nameof(SelectedCountText));
            OnPropertyChanged(nameof(CanEditSelected));
            OnPropertyChanged(nameof(IsAllSelected));
        }

        partial void OnEditingVenueNameChanged(string value)
        {
            ClearVenueNameError();
            UpdateCharacterCounter(value?.Length ?? 0);
        }

        partial void OnIsInitialLoadingChanged(bool value) => NotifyEmptyStates();

        public async Task InitializeAsync()
        {
            IsInitialLoading = true;

            // Yield to UI so Shimmer can render before starting the load
            await Task.Yield();

            await LoadFirstPageAsync(CancellationToken.None);
            RunOnUiThread(() => IsInitialLoading = false);
        }

        private async Task LoadFirstPageAsync(CancellationToken cancellationToken)
        {
            var entered = false;
            try
            {
                await _loadSemaphore.WaitAsync(cancellationToken);
                entered = true;

                _currentPage = 1;
                _currentSearchQuery = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();

                // Preserve selected ids before replacing items
                var selectedIds = SelectedVenues.Select(v => v.Id).ToHashSet();

                var (itemsEnumerable, totalCount) = await _venueService.GetPagedVenuesForListAsync(
                    _currentPage, PageSize, _currentSearchQuery);

                if (cancellationToken.IsCancellationRequested) return;

                _totalCount = totalCount;
                if (totalCount < PageSize)
                {
                    HasMoreItems = false;
                }
                var list = itemsEnumerable.ToList();

                RunOnUiThread(() =>
                {
                    Venues.ReplaceRange(list);

                    // Restore selection by Id (instances come from Venues)
                    var restored = Venues.Where(v => selectedIds.Contains(v.Id)).ToList();
                    SelectedVenues.ReplaceRange(restored);

                    SelectedCount = SelectedVenues.Count;
                    NotifyEmptyStates();
                });
            }
            catch (OperationCanceledException)
            {
                // Cancellation requested — silently return
            }
            finally
            {
                if (entered)
                    _loadSemaphore.Release();
            }
        }

        private async Task RefreshAsync()
        {
            RunOnUiThread(() => IsRefreshing = true);
            await LoadFirstPageAsync(CancellationToken.None);
            RunOnUiThread(() => IsRefreshing = false);
        }

        private async Task LoadMoreAsync()
        {
            if (_isLoading || !HasMoreItems)
                return;

            _isLoading = true;
            RunOnUiThread(() => HasMoreItems = false);
            var loadingPage = _currentPage + 1;

            try
            {
                var (itemsEnumerable, totalCount) = await _venueService.GetPagedVenuesForListAsync(
                    loadingPage, PageSize, _currentSearchQuery);

                _totalCount = totalCount;
                var list = itemsEnumerable.ToList();
                var hasMore = list.Count >= PageSize;

                _currentPage = loadingPage;

                RunOnUiThread(() =>
                {
                    Venues.AddRange(list);
                    HasMoreItems = hasMore;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load more venues (page {Page})", loadingPage);
                RunOnUiThread(() => HasMoreItems = true);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void TriggerSearchDebounce(string text)
        {
            try
            {
                _searchCts?.Cancel();
                _searchCts?.Dispose();
            }
            catch { /* ignore disposal races */ }

            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(400, token);
                    if (token.IsCancellationRequested) return;
                    await LoadFirstPageAsync(token);
                }
                catch (OperationCanceledException) { /* ignore */ }
            }, token);
        }

        private void OnItemTapped(VenueListItemDto? item)
        {
            if (item == null || IsMultiSelectMode)
                return;

            EnterMultiSelectMode(item);
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
            var selectedItems = SelectedVenues.ToList();
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
            var action = _pendingConfirmAction;
            DismissConfirmSheet();
            if (action != null)
                await action();
        }

        private void DismissConfirmSheet()
        {
            ConfirmSheetState = BottomSheetState.Hidden;
            _pendingConfirmAction = null;
        }

        private void EditSelectedVenue()
        {
            var selectedItem = SelectedVenues.FirstOrDefault();
            if (selectedItem == null)
                return;

            ExitMultiSelectMode();
            OpenEditBottomSheet(selectedItem);
        }

        public void EnterMultiSelectMode(VenueListItemDto initialItem)
        {
            IsMultiSelectMode = true;
            _suppressSelectionChangedExit = true;
            RunOnUiThread(() =>
            {
                SelectedVenues.ClearRange();
                SelectedVenues.AddRange(new[] { initialItem });
                _suppressSelectionChangedExit = false;
            });
            SelectedCount = 1;
        }

        public void ExitMultiSelectMode()
        {
            IsMultiSelectMode = false;
            RunOnUiThread(() => SelectedVenues.ClearRange());
            SelectedCount = 0;
        }

        private void ToggleSelectAll()
        {
            if (IsAllSelected)
            {
                ExitMultiSelectMode();
                return;
            }
            IsMultiSelectMode = true;
            _suppressSelectionChangedExit = true;
            RunOnUiThread(() =>
            {
                SelectedVenues.ReplaceRange(Venues.ToList());
                _suppressSelectionChangedExit = false;
            });
            SelectedCount = Venues.Count;
        }

        public void OnSelectionChanged(int count)
        {
            SelectedCount = count;
            if (IsMultiSelectMode && count == 0 && !_suppressSelectionChangedExit)
            {
                ExitMultiSelectMode();
            }
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

        private void NotifyEmptyStates()
        {
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsEmptyNoVenues));
            OnPropertyChanged(nameof(IsEmptyNoResults));
            OnPropertyChanged(nameof(IsAllSelected));
        }
    }
}
