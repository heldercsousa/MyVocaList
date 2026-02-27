namespace MyVocaList.UI.ViewModels
{
    /// <summary>
    /// ViewModel for the Add / Edit Venue form page.
    /// Receives venue ID via shell query parameter; null = create mode.
    /// </summary>
    [QueryProperty(nameof(VenueId), "venueId")]
    [QueryProperty(nameof(VenueName), "venueName")]
    public partial class VenueFormViewModel : ViewModelBase
    {
        private readonly IVenueService _venueService;
        private readonly ISnackbarService _snackbarService;
        private readonly ILogger<VenueFormViewModel> _logger;

        [ObservableProperty] private int? _venueId;
        [ObservableProperty] private string _venueName = string.Empty;
        [ObservableProperty] private bool _nameHasError;
        [ObservableProperty] private string _nameErrorText = string.Empty;
        [ObservableProperty] private string _characterCounterText = string.Empty;
        [ObservableProperty] private bool _showCharacterCounter;
        [ObservableProperty] private bool _isCharacterCounterWarning;
        [ObservableProperty] private bool _isCharacterCounterError;
        [ObservableProperty] private bool _isBusy;

        public bool IsEditMode => VenueId.HasValue;
        public string PageTitle => IsEditMode ? "Edit Venue" : "New Venue";

        public VenueFormViewModel(
            IVenueService venueService,
            ISnackbarService snackbarService,
            ILogger<VenueFormViewModel> logger)
        {
            _venueService = venueService;
            _snackbarService = snackbarService;
            _logger = logger;

            SaveCommand = new AsyncRelayCommand(SaveAsync);
            CancelCommand = new AsyncRelayCommand(CancelAsync);
        }

        public IAsyncRelayCommand SaveCommand { get; }
        public IAsyncRelayCommand CancelCommand { get; }

        partial void OnVenueIdChanged(int? value)
        {
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(PageTitle));
        }

        partial void OnVenueNameChanged(string value)
        {
            ClearError();
            UpdateCharacterCounter(value?.Length ?? 0);
        }

        private async Task SaveAsync()
        {
            var name = VenueName?.Trim() ?? string.Empty;

            var validation = _venueService.ValidateNameInput(name);
            if (!validation.isValid)
            {
                NameHasError = true;
                NameErrorText = validation.message;
                return;
            }

            IsBusy = true;
            try
            {
                if (IsEditMode)
                {
                    var (success, message) = await _venueService.UpdateVenueAsync(VenueId.Value, name);
                    if (success)
                    {
                        await _snackbarService.ShowSuccessAsync("Venue updated");
                        await Shell.Current.GoToAsync("..");
                    }
                    else
                    {
                        NameHasError = true;
                        NameErrorText = message;
                    }
                }
                else
                {
                    var (success, message) = await _venueService.CreateVenueAsync(name);
                    if (success)
                    {
                        await _snackbarService.ShowSuccessAsync("Venue created");
                        await Shell.Current.GoToAsync("..");
                    }
                    else
                    {
                        NameHasError = true;
                        NameErrorText = message;
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private Task CancelAsync() => Shell.Current.GoToAsync("..");

        private void ClearError()
        {
            NameHasError = false;
            NameErrorText = string.Empty;
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
    }
}
