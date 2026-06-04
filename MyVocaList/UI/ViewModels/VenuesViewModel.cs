using MyVocaList.UI.Collections;

namespace MyVocaList.UI.ViewModels
{
    /// <summary>
    /// ViewModel for the Venues list page: paging, search, always-on selection, confirm-delete.
    /// Add navigates to VenueFormPage via FAB. Edit navigates via FloatingToolbar (single select).
    /// </summary>
    public partial class VenuesViewModel : CrudListViewModelBase<VenueListItemDto>
    {
        private readonly IVenueService _venueService;
        private readonly ISnackbarComponent _snackbarService;

        public VenuesViewModel(
            IVenueService venueService,
            ISnackbarComponent snackbarService,
            ILogger<VenuesViewModel> logger) : base(logger)
        {
            _venueService = venueService;
            _snackbarService = snackbarService;

            Venues = [];
            SelectedVenues = [];

            AddVenueCommand = new AsyncRelayCommand(NavigateToAddAsync);
        }

        public ObservableRangeCollection<VenueListItemDto> Venues { get; }
        public ObservableRangeCollection<VenueListItemDto> SelectedVenues { get; }

        /// <summary>Non-generic wrapper for binding to DXCollectionView SelectedItems (requires IList).</summary>
        public System.Collections.IList SelectedVenuesRaw => SelectedVenues;

        public IAsyncRelayCommand AddVenueCommand { get; }

        public string AppBarTitle => SelectedCount == 0 ? "Venues" : $"{SelectedCount} selected";
        public bool IsEmptyNoVenues => IsEmpty && string.IsNullOrWhiteSpace(SearchText);
        public bool IsEmptyNoResults => IsEmpty && !string.IsNullOrWhiteSpace(SearchText);

        protected override ObservableRangeCollection<VenueListItemDto> Items => Venues;
        protected override ObservableRangeCollection<VenueListItemDto> SelectedItems => SelectedVenues;

        protected override void OnSelectedCountUpdated(int value)
            => OnPropertyChanged(nameof(AppBarTitle));

        protected override Task<(IEnumerable<VenueListItemDto> items, int totalCount)> FetchPageAsync(
            int page, int pageSize, string query, CancellationToken ct)
            => _venueService.GetPagedVenuesForListAsync(page, pageSize, query);

        protected override Task<(IEnumerable<VenueListItemDto> items, int totalCount)> FetchMoreAsync(
            int page, int pageSize, string query, CancellationToken ct = default)
            => _venueService.GetPagedVenuesForListAsync(page, pageSize, query);

        protected override string BuildDeleteConfirmMessage(IList<VenueListItemDto> items)
            => $"Delete {items.Count} venue(s)?";

        protected override async Task ExecuteDeleteAsync(IEnumerable<VenueListItemDto> items)
        {
            var ids = items.Select(v => v.Id);
            var (success, message) = await _venueService.DeleteVenuesAsync(ids);
            if (success)
            {
                await RefreshAsync();
                await _snackbarService.ShowSuccessAsync(message);
            }
            else
            {
                await _snackbarService.ShowErrorAsync(message);
            }
        }

        protected override Task NavigateToAddAsync()
            => Shell.Current.GoToAsync(Routes.VenueForm);

        protected override async Task NavigateToEditAsync(VenueListItemDto item)
            => await Shell.Current.GoToAsync($"{Routes.VenueForm}?venueId={item.Id}&venueName={Uri.EscapeDataString(item.Name)}");

        protected override void RaiseEntityEmptyStateProperties()
        {
            OnPropertyChanged(nameof(IsEmptyNoVenues));
            OnPropertyChanged(nameof(IsEmptyNoResults));
        }

    }
}
