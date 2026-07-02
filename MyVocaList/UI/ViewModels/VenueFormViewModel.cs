namespace MyVocaList.UI.ViewModels
{
    /// <summary>
    /// ViewModel for the Add / Edit Venue form page.
    /// Receives venue ID via shell query parameter; null = create mode.
    /// </summary>
    [QueryProperty(nameof(VenueIdRaw), "venueId")]
    [QueryProperty(nameof(VenueName), "venueName")]
    public partial class VenueFormViewModel : ViewModelBase
    {
        private readonly IVenueService _venueService;
        private readonly ISnackbarComponent _snackbarService;
        private readonly ILogger<VenueFormViewModel> _logger;

        // Shell passes all query parameters as strings; parse manually to avoid InvalidCastException.
        public string VenueIdRaw { set => VenueId = int.TryParse(value, out var id) ? id : null; }

        [ObservableProperty] private int? _venueId;
        [ObservableProperty] private string _venueName = string.Empty;
        [ObservableProperty] private bool _nameHasError;
        [ObservableProperty] private string _nameErrorText = string.Empty;
        [ObservableProperty] private string _characterCounterText = string.Empty;
        [ObservableProperty] private bool _showCharacterCounter;
        [ObservableProperty] private bool _isCharacterCounterWarning;
        [ObservableProperty] private bool _isCharacterCounterError;
        [ObservableProperty] private bool _isBusy;

        // Becomes true once the user edits the name field. Guards blur validation so a pristine
        // field the user only tabbed through is not flagged prematurely (Form Validation Standard).
        private bool _nameDirty;

        public bool IsEditMode => VenueId.HasValue;
        public string PageTitle => IsEditMode ? "Edit Venue" : "New Venue";

        public VenueFormViewModel(
            IVenueService venueService,
            ISnackbarComponent snackbarService,
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

        /// <summary>
        /// Blur handler (invoked from the page's <c>Unfocused</c> event). Validates the name field
        /// only when it is dirty — a pristine field the user only tabbed through is left untouched.
        /// </summary>
        [RelayCommand]
        private void ValidateName()
        {
            if (!_nameDirty)
                return;

            ApplyNameValidation(VenueName);
        }

        partial void OnVenueNameChanged(string value)
        {
            _nameDirty = true;
            // Counter counts the trimmed string — the same string ValidateNameInput checks —
            // so trailing whitespace cannot inflate the count (counter threshold alignment).
            UpdateCharacterCounter(value?.Trim().Length ?? 0);

            // "Reward early": once the field is in error, re-validate on every keystroke so the error
            // clears the instant it becomes valid. Do NOT validate on keystroke before the field is in
            // error (that is the "impatient teacher" anti-pattern) — blur/Save handle the first check.
            if (!NameHasError)
                return;

            ApplyNameValidation(value);
        }

        private void ApplyNameValidation(string value)
        {
            var (isValid, message) = _venueService.ValidateNameInput(value ?? string.Empty);
            NameHasError = !isValid;
            NameErrorText = isValid ? string.Empty : message;
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
