using System.Globalization;
using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs.Venues;
using MyVocaList.Domain.Rules;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Navigation;
using MyVocaList.UI.Messages;
using MyVocaList.UI.Services;

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
        // REQ-VEN-09: the size hint shown while there is no form picture.
        private const string ImageHint = "Square picture (1:1), 512 x 512 px or larger. Other shapes are cropped to the centre.";
        private const string ImagePickFailedMessage = "Could not use this picture.";
        private const string VenueNotFoundMessage = "Venue not found";

        private readonly IVenueService _venueService;
        private readonly ISnackbarComponent _snackbarService;
        private readonly ILogger<VenueFormViewModel> _logger;
        private readonly IImageStore _imageStore;
        private readonly IPhotoPicker _photoPicker;
        private readonly IMessenger _messenger;
        private readonly IVenuePictureLookupService _pictureLookup;
        private readonly IRemoteImageFetcher _remoteImageFetcher;

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

        // ── Address (REQ-VEN-12..16, 20, 30, 33) ──────────────────────────────
        // Each field has its own error; a change of the field's text clears that error.
        [ObservableProperty] private string? _addressLine1;
        [ObservableProperty] private bool _addressLine1HasError;
        [ObservableProperty] private string _addressLine1ErrorText = string.Empty;
        [ObservableProperty] private string? _addressLine2;
        [ObservableProperty] private bool _addressLine2HasError;
        [ObservableProperty] private string _addressLine2ErrorText = string.Empty;
        [ObservableProperty] private string? _city;
        [ObservableProperty] private bool _cityHasError;
        [ObservableProperty] private string _cityErrorText = string.Empty;
        [ObservableProperty] private string? _region;
        [ObservableProperty] private bool _regionHasError;
        [ObservableProperty] private string _regionErrorText = string.Empty;
        [ObservableProperty] private string? _postalCode;
        [ObservableProperty] private bool _postalCodeHasError;
        [ObservableProperty] private string _postalCodeErrorText = string.Empty;
        [ObservableProperty] private string? _country;
        [ObservableProperty] private bool _countryHasError;
        [ObservableProperty] private string _countryErrorText = string.Empty;

        // Coordinates are stored with the venue; a hand edit of an address field never clears them (REQ-VEN-33).
        [ObservableProperty] private double? _latitude;
        [ObservableProperty] private double? _longitude;

        // ── Form picture (REQ-VEN-08..11) ─────────────────────────────────────
        private string? _uploadedImageReference;   // picked or taken in this form; stored on Save
        private string? _storedImageReference;     // edit mode: the venue's stored picture
        private bool _imageRemoved;                // "Remove picture" until a later pick
        private int _pictureChoiceVersion;         // bumps on each user pick or removal; a late map-pick picture is dropped when it changed
        [ObservableProperty] private bool _isImageBusy;

        // Becomes true once the user edits the name field. Guards blur validation so a pristine
        // field the user only tabbed through is not flagged prematurely (Form Validation Standard).
        private bool _nameDirty;

        public bool IsEditMode => VenueId.HasValue;
        public string PageTitle => IsEditMode ? "Edit Venue" : "New Venue";

        /// <summary>REQ-VEN-23: the map button reads "Change on map" once the venue has coordinates.</summary>
        public string MapButtonText => Latitude.HasValue ? "Change on map" : "Pick on map";

        /// <summary>
        /// The form picture's reference (REQ-VEN-10, 11): the picked picture; else the stored one; null after
        /// "Remove picture".
        /// </summary>
        public string? FormImageReference => _imageRemoved ? null : _uploadedImageReference ?? _storedImageReference;

        /// <summary>Display source of the form picture, large size (REQ-VEN-08); null shows the default venue image.</summary>
        public string? FormImageSource => _imageStore.ResolveDisplaySource(FormImageReference, ImageSize.Large);

        /// <summary>Display source of the form picture, thumbnail size, drawn under the large one.</summary>
        public string? FormImageThumbnailSource => _imageStore.ResolveDisplaySource(FormImageReference, ImageSize.Thumbnail);

        /// <summary>True when the form has a picture (REQ-VEN-09 switches the buttons and the hint).</summary>
        public bool HasFormImage => FormImageReference is not null;

        /// <summary>REQ-VEN-09: the size hint shown while there is no form picture.</summary>
        public string ImageHintText => ImageHint;

        // REQ-VEN-10: a picture counts as changed once it is picked or removed; edit mode stores it only then.
        private bool IsImageChanged => _uploadedImageReference is not null || _imageRemoved;

        public VenueFormViewModel(
            IVenueService venueService,
            ISnackbarComponent snackbarService,
            ILogger<VenueFormViewModel> logger,
            IImageStore imageStore,
            IPhotoPicker photoPicker,
            IMessenger messenger,
            IVenuePictureLookupService pictureLookup,
            IRemoteImageFetcher remoteImageFetcher)
        {
            _venueService = venueService;
            _snackbarService = snackbarService;
            _logger = logger;
            _imageStore = imageStore;
            _photoPicker = photoPicker;
            _messenger = messenger;
            _pictureLookup = pictureLookup;
            _remoteImageFetcher = remoteImageFetcher;

            // REQ-VEN-30: the map picker sends the confirmed place; the form takes it in.
            _messenger.Register<VenuePlacePickedMessage>(this,
                static (recipient, message) => ((VenueFormViewModel)recipient).OnPlacePicked(message.Place));

            // BUG-049: explicit CanExecute tied to IsBusy — combined with the early-return guard in
            // SaveAsync — prevents a fast double-tap from firing SaveAsync twice concurrently, which
            // caused a duplicate "GoToAsync("..")" that overshot the nav stack root.
            SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
            CancelCommand = new AsyncRelayCommand(CancelAsync);
        }

        public IAsyncRelayCommand SaveCommand { get; }
        public IAsyncRelayCommand CancelCommand { get; }

        partial void OnVenueIdChanged(int? value)
        {
            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(PageTitle));

            // REQ-VEN-17: edit mode loads the stored venue; the route's name shows until the load completes.
            if (value is { } id)
                _ = LoadAsync(id);
        }

        partial void OnIsBusyChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();

        partial void OnLatitudeChanged(double? value) => OnPropertyChanged(nameof(MapButtonText));

        partial void OnLongitudeChanged(double? value) => OnPropertyChanged(nameof(MapButtonText));

        partial void OnAddressLine1Changed(string? value) => ClearFieldError(VenueField.AddressLine1);
        partial void OnAddressLine2Changed(string? value) => ClearFieldError(VenueField.AddressLine2);
        partial void OnCityChanged(string? value) => ClearFieldError(VenueField.City);
        partial void OnRegionChanged(string? value) => ClearFieldError(VenueField.Region);
        partial void OnPostalCodeChanged(string? value) => ClearFieldError(VenueField.PostalCode);
        partial void OnCountryChanged(string? value) => ClearFieldError(VenueField.Country);

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
            if (IsBusy) return;   // BUG-049: re-entrancy guard, defense-in-depth alongside CanExecute

            var name = VenueName?.Trim() ?? string.Empty;

            // Save validates everything through the service; each failure lands on its own field.
            var nameValidation = _venueService.ValidateNameInput(name);
            if (!nameValidation.isValid)
            {
                await ShowFieldFailureAsync(VenueField.Name, nameValidation.message);
                return;
            }

            var address = CurrentAddress();
            var addressValidation = _venueService.ValidateAddressInput(address);
            if (!addressValidation.isValid)
            {
                await ShowFieldFailureAsync(addressValidation.field, addressValidation.message);
                return;
            }

            IsBusy = true;
            try
            {
                var request = new VenueSaveRequest(VenueId, name, address, Latitude, Longitude,
                    FormImageReference, IsImageChanged);
                var (success, message, field) = await _venueService.SaveVenueAsync(request);
                if (!success)
                {
                    await ShowFieldFailureAsync(field, message);
                    return;
                }

                await _snackbarService.ShowSuccessAsync(IsEditMode ? "Venue updated" : "Venue created");
                await (Shell.Current?.GoToAsync("..") ?? Task.CompletedTask);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private Task CancelAsync() => Shell.Current?.GoToAsync("..") ?? Task.CompletedTask;

        // ── Picture commands (REQ-VEN-08..11) ─────────────────────────────────

        /// <summary>
        /// "Add picture" / "Replace picture": picks one photo and imports it. Cancel changes nothing; an unusable
        /// photo shows the store's message and keeps the form picture. Nothing is stored before Save (REQ-VEN-10).
        /// </summary>
        [RelayCommand]
        private async Task PickImage()
        {
            if (IsImageBusy)
                return;   // re-entry guard: one pick and import at a time

            IsImageBusy = true;
            try
            {
                await using var photo = await _photoPicker.PickPhotoAsync();
                if (photo is null)
                    return;   // cancelled

                var (success, message, reference) = await _imageStore.ImportAsync(photo, ImageOwnerKind.Venue);
                if (!success || reference is null)
                {
                    await _snackbarService.ShowErrorAsync(message);
                    return;
                }

                _uploadedImageReference = reference;
                _imageRemoved = false;
                _pictureChoiceVersion++;
                NotifyFormImageChanged();
            }
            catch (Exception ex)
            {
                // The platform picker can fail (no picker activity, permission); the form stays usable.
                _logger.LogError(ex, "Picking a venue picture failed");
                await _snackbarService.ShowErrorAsync(ImagePickFailedMessage);
            }
            finally
            {
                IsImageBusy = false;
            }
        }

        /// <summary>"Remove picture" (REQ-VEN-09, 10): no form picture until Save; edit mode marks the stored one for removal.</summary>
        [RelayCommand]
        private void RemoveImage()
        {
            _uploadedImageReference = null;
            _imageRemoved = true;
            _pictureChoiceVersion++;
            NotifyFormImageChanged();
        }

        // ── Map pick (REQ-VEN-23, 24, 30) ─────────────────────────────────────

        /// <summary>
        /// Route of the map picker: the form's coordinates (when set) and the address as search text (when not blank),
        /// invariant culture and escaped. The picker decides the first view (REQ-VEN-24).
        /// </summary>
        public string MapPickerRoute
        {
            get
            {
                var parameters = new List<string>();
                if (Latitude is { } latitude)
                    parameters.Add($"lat={Uri.EscapeDataString(latitude.ToString(CultureInfo.InvariantCulture))}");
                if (Longitude is { } longitude)
                    parameters.Add($"lon={Uri.EscapeDataString(longitude.ToString(CultureInfo.InvariantCulture))}");
                if (VenueAddressRules.ToSearchText(CurrentAddress()) is { } searchText)
                    parameters.Add($"q={Uri.EscapeDataString(searchText)}");

                return parameters.Count == 0
                    ? Routes.VenueMapPicker
                    : $"{Routes.VenueMapPicker}?{string.Join("&", parameters)}";
            }
        }

        [RelayCommand]
        private Task PickOnMap() => Shell.Current?.GoToAsync(MapPickerRoute) ?? Task.CompletedTask;

        /// <summary>REQ-VEN-30: a confirmed place fills the form through the rules; the form holds no merge rule of its own.</summary>
        private void OnPlacePicked(VenuePlace place) => RunOnUiThread(() =>
        {
            var (name, address, latitude, longitude) = VenuePlaceRules.Merge(VenueName, CurrentAddress(), place);

            VenueName = name;
            AddressLine1 = address.Line1;
            AddressLine2 = address.Line2;
            City = address.City;
            Region = address.Region;
            PostalCode = address.PostalCode;
            Country = address.Country;
            Latitude = latitude;
            Longitude = longitude;

            // REQ-VEN-38: the picture is fetched after the fields are filled, and only when the form has none.
            if (!HasFormImage && !IsImageBusy && HasPictureSource(place.PictureSource))
                _ = FillPictureFromPlaceAsync(place.PictureSource!);
        });

        /// <summary>
        /// REQ-VEN-38, 39: resolves, downloads and imports the place's picture through the same store call as a picked photo.
        /// Every failure is silent for the user and logged. A pick or removal made meanwhile wins over the late picture.
        /// </summary>
        private async Task FillPictureFromPlaceAsync(VenuePictureSource source)
        {
            var choiceVersion = _pictureChoiceVersion;
            IsImageBusy = true;
            try
            {
                var address = await _pictureLookup.ResolveAsync(source);
                if (address is null)
                    return;

                await using var picture = await _remoteImageFetcher.FetchAsync(address);
                if (picture is null)
                    return;

                var (success, message, reference) = await _imageStore.ImportAsync(picture, ImageOwnerKind.Venue);
                if (!success || reference is null)
                {
                    _logger.LogWarning("Venue picture from the picked place was refused: {Message}", message);
                    return;
                }

                if (choiceVersion != _pictureChoiceVersion || HasFormImage)
                    return;   // the user picked or removed a picture while this ran

                _uploadedImageReference = reference;
                _imageRemoved = false;
                NotifyFormImageChanged();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Filling the venue picture from the picked place failed");
            }
            finally
            {
                IsImageBusy = false;
            }
        }

        private static bool HasPictureSource(VenuePictureSource? source) =>
            source is { } s && (s.WikidataId is not null || s.CommonsFile is not null || s.ImageUrl is not null);

        // ── Edit load (REQ-VEN-17) ────────────────────────────────────────────

        private async Task LoadAsync(int venueId)
        {
            VenueEditDto? venue;
            try
            {
                venue = await _venueService.GetVenueForEditAsync(venueId);
            }
            catch (Exception ex)
            {
                // Fire-and-forget load: a failure must be observed here, or the form would stay blank silently.
                _logger.LogError(ex, "Loading venue {VenueId} for edit failed", venueId);
                await _snackbarService.ShowErrorAsync("The venue couldn't be loaded. Try again.");
                return;
            }

            if (venue is null)
            {
                await _snackbarService.ShowErrorAsync(VenueNotFoundMessage);
                await (Shell.Current?.GoToAsync("..") ?? Task.CompletedTask);
                return;
            }

            RunOnUiThread(() =>
            {
                VenueName = venue.Name;
                AddressLine1 = venue.Address.Line1;
                AddressLine2 = venue.Address.Line2;
                City = venue.Address.City;
                Region = venue.Address.Region;
                PostalCode = venue.Address.PostalCode;
                Country = venue.Address.Country;
                Latitude = venue.Latitude;
                Longitude = venue.Longitude;
                _storedImageReference = venue.ImageUrl;
                NotifyFormImageChanged();
            });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private VenueAddress CurrentAddress() =>
            new(AddressLine1, AddressLine2, City, Region, PostalCode, Country);

        /// <summary>Routes a failure to its field; a failure that names no field is shown as a snackbar (REQ-VEN-11).</summary>
        private async Task ShowFieldFailureAsync(VenueField field, string message)
        {
            switch (field)
            {
                case VenueField.Name:
                    NameHasError = true;
                    NameErrorText = message;
                    break;
                case VenueField.AddressLine1:
                    AddressLine1HasError = true;
                    AddressLine1ErrorText = message;
                    break;
                case VenueField.AddressLine2:
                    AddressLine2HasError = true;
                    AddressLine2ErrorText = message;
                    break;
                case VenueField.City:
                    CityHasError = true;
                    CityErrorText = message;
                    break;
                case VenueField.Region:
                    RegionHasError = true;
                    RegionErrorText = message;
                    break;
                case VenueField.PostalCode:
                    PostalCodeHasError = true;
                    PostalCodeErrorText = message;
                    break;
                case VenueField.Country:
                    CountryHasError = true;
                    CountryErrorText = message;
                    break;
                default:
                    await _snackbarService.ShowErrorAsync(message);
                    break;
            }
        }

        private void ClearFieldError(VenueField field)
        {
            switch (field)
            {
                case VenueField.AddressLine1:
                    AddressLine1HasError = false;
                    AddressLine1ErrorText = string.Empty;
                    break;
                case VenueField.AddressLine2:
                    AddressLine2HasError = false;
                    AddressLine2ErrorText = string.Empty;
                    break;
                case VenueField.City:
                    CityHasError = false;
                    CityErrorText = string.Empty;
                    break;
                case VenueField.Region:
                    RegionHasError = false;
                    RegionErrorText = string.Empty;
                    break;
                case VenueField.PostalCode:
                    PostalCodeHasError = false;
                    PostalCodeErrorText = string.Empty;
                    break;
                case VenueField.Country:
                    CountryHasError = false;
                    CountryErrorText = string.Empty;
                    break;
            }
        }

        private void NotifyFormImageChanged()
        {
            OnPropertyChanged(nameof(FormImageReference));
            OnPropertyChanged(nameof(FormImageSource));
            OnPropertyChanged(nameof(FormImageThumbnailSource));
            OnPropertyChanged(nameof(HasFormImage));
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
