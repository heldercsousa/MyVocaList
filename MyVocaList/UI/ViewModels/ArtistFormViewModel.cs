using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Contracts.Models;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Domain.Rules;
using MyVocaList.UI.Constants;
using MyVocaList.UI.Models;

namespace MyVocaList.UI.ViewModels;

[QueryProperty(nameof(ArtistIdRaw), "artistId")]
[QueryProperty(nameof(ArtistName), "artistName")]
public partial class ArtistFormViewModel : ViewModelBase
{
    private readonly IArtistService _artistService;
    private readonly ISnackbarComponent _snackbarService;
    private readonly ILogger<ArtistFormViewModel> _logger;
    private readonly IMessenger _messenger;
    private readonly INavigationService _navigationService;
    private readonly IArtistSuggestionService _artistSuggestions;
    private readonly IImageStore _imageStore;
    private readonly IPhotoPicker _photoPicker;

    public string ArtistIdRaw { set => ArtistId = int.TryParse(value, out var id) ? id : null; }

    [ObservableProperty] private int? _artistId;
    [ObservableProperty] private string _artistName = string.Empty;
    [ObservableProperty] private bool _nameHasError;
    [ObservableProperty] private string _nameErrorText = string.Empty;
    [ObservableProperty] private string _characterCounterText = string.Empty;
    [ObservableProperty] private bool _showCharacterCounter;
    [ObservableProperty] private bool _isCharacterCounterWarning;
    [ObservableProperty] private bool _isCharacterCounterError;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private IEnumerable<ArtistListItem> _duplicateSuggestions = [];
    [ObservableProperty] private bool _hasDuplicateSuggestions;

    // ── Name search (Task 6.2, REQ-SFT-47…50, REQ-SFT-06/09) ──────────────
    // Offline wording stays local (artists); the unavailable note is SearchNotes.Unavailable (UI/Constants), shared with the Song form.
    private const string OfflineNote = "You're offline — showing artists from your library only.";

    /// <summary>Name field option source: local artists ("In your library"), then Deezer ("Deezer") (REQ-SFT-47).</summary>
    public IMenuOptionSource NameOptionSource { get; }

    [ObservableProperty] private string _nameSupportingText = string.Empty;
    [ObservableProperty] private bool _isNameLocked;

    /// <summary>REQ-SFT-47: Name is the search field in add mode only; edit mode keeps plain rename.</summary>
    public bool IsNameSearchEnabled => !IsEditMode;

    /// <summary>TwoWay with the Name <c>TapToEditField</c>: the editor state requested by a tap (REQ-PIC-25/26/37).</summary>
    [ObservableProperty] private bool _isNameEditing;

    // ── Form picture (REQ-PIC-12..17, REQ-PIC-35/36) ──────────────────────
    private const string ArtistPlaceholderIcon = "account_circle_outlined";
    private const string ImageHint = "Square picture (1:1), 512 x 512 px or larger. Other shapes are cropped to the centre.";
    private const string ImagePickFailedMessage = "Could not use this picture.";

    private string? _uploadedImageReference;   // REQ-PIC-35 (1): the picture uploaded in this form
    private string? _storedImageReference;     // REQ-PIC-35 (2), edit mode: the artist's stored picture
    private string? _pendingImageUrl;          // REQ-PIC-35 (2), add mode: the picture of the picked Deezer artist
    private bool _imageRemoved;                // REQ-PIC-20: "Remove picture" until a later pick or upload

    // REQ-PIC-37: the Name the last Deezer pick wrote. The Deezer picture belongs to the form only while
    // Name still equals it. An exact (ordinal) comparison that decides which pending value applies; it is
    // not search or de-duplication.
    private string? _pickedName;

    [ObservableProperty] private bool _isImageBusy;

    /// <summary>
    /// The form picture's reference (REQ-PIC-35, 20, 37): the upload; else the stored picture (edit mode) or
    /// the picked Deezer artist's picture while Name is unchanged (add mode); null after "Remove picture".
    /// </summary>
    private string? FormImageReference =>
        _imageRemoved ? null : _uploadedImageReference ?? (IsEditMode ? _storedImageReference : PickedImageReference);

    private string? PickedImageReference =>
        _pendingImageUrl is not null && string.Equals(ArtistName?.Trim(), _pickedName?.Trim(), StringComparison.Ordinal)
            ? _pendingImageUrl
            : null;

    // REQ-PIC-36 (edit mode): the stored reference changes only after an upload or a removal.
    private bool IsImageChanged => _uploadedImageReference is not null || _imageRemoved;

    /// <summary>Display source of the form picture, large size (REQ-PIC-13); null shows the placeholder.</summary>
    public string? FormImageSource => _imageStore.ResolveDisplaySource(FormImageReference, ImageSize.Large);

    /// <summary>Display source of the form picture, thumbnail size, drawn under the large one.</summary>
    public string? FormImageThumbnailSource => _imageStore.ResolveDisplaySource(FormImageReference, ImageSize.Thumbnail);

    /// <summary>True when the form has a picture (REQ-PIC-15/16 switch the buttons and the hint).</summary>
    public bool HasFormImage => FormImageReference is not null;

    /// <summary>REQ-PIC-16: the size hint shown while there is no form picture.</summary>
    public string ImageHintText => ImageHint;

    // Becomes true once the user edits the field. Guards blur validation so a pristine field the
    // user only tabbed through is not flagged prematurely (Form Validation Standard, R8).
    private bool _nameDirty;

    // True until the page's OnAppearing calls CompleteHydration(). While true, Shell QueryProperty
    // pre-population (edit mode / duplicate selection) must NOT mark the field dirty — otherwise a
    // pre-filled value would flash an error on the very first blur, before the user has touched
    // anything (edit-mode dirty-guard, Form Validation Standard).
    private bool _isHydrating = true;

    public bool IsEditMode => ArtistId.HasValue;
    public string PageTitle => IsEditMode ? "Edit Artist" : "New Artist";

    public ArtistFormViewModel(
        IArtistService artistService,
        ISnackbarComponent snackbarService,
        ILogger<ArtistFormViewModel> logger,
        IMessenger messenger,
        INavigationService navigationService,
        IArtistSuggestionService artistSuggestions,
        IImageStore imageStore,
        IPhotoPicker photoPicker)
    {
        _artistService = artistService;
        _snackbarService = snackbarService;
        _logger = logger;
        _messenger = messenger;
        _navigationService = navigationService;
        _artistSuggestions = artistSuggestions;
        _imageStore = imageStore;
        _photoPicker = photoPicker;

        NameOptionSource = new MenuOptionSource(SearchNameAsync);

        // BUG-049: explicit CanExecute tied to IsBusy — combined with the early-return guard in
        // SaveAsync — prevents a fast double-tap from firing SaveAsync twice concurrently, which
        // caused a duplicate "GoToAsync("..")" that overshot the nav stack root.
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
        CancelCommand = new AsyncRelayCommand(CancelAsync);
        SelectDuplicateCommand = new AsyncRelayCommand<ArtistListItem>(SelectDuplicateAsync);
    }

    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand CancelCommand { get; }
    public IAsyncRelayCommand<ArtistListItem> SelectDuplicateCommand { get; }

    partial void OnArtistIdChanged(int? value)
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(IsNameSearchEnabled));
        NotifyFormImageChanged();   // REQ-PIC-35: the mode decides the second source of the form picture
        if (value.HasValue)
            _ = LoadArtistForEditAsync(value.Value);
    }

    /// <summary>Edit mode: loads the stored picture so the form shows it (REQ-PIC-35). The name comes from the route.</summary>
    private async Task LoadArtistForEditAsync(int artistId)
    {
        var artist = await _artistService.GetArtistByIdAsync(artistId);
        if (artist is null)
        {
            _logger.LogWarning("Edit mode: artist {ArtistId} not found while loading its picture", artistId);
            return;
        }

        RunOnUiThread(() =>
        {
            _storedImageReference = artist.ImageUrl;
            NotifyFormImageChanged();
        });
    }

    partial void OnIsBusyChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();

    /// <summary>
    /// Called by the page's <c>OnAppearing</c> once Shell has finished applying all
    /// <c>[QueryProperty]</c> values for this navigation. Ends the hydration window so that
    /// subsequent field edits are tracked as dirty (edit-mode dirty-guard).
    /// </summary>
    public void CompleteHydration() => _isHydrating = false;

    /// <summary>
    /// Blur handler (invoked from the page's <c>Unfocused</c> event). Validates the name field
    /// only when it is dirty — a pristine field the user only tabbed through is left untouched.
    /// </summary>
    [RelayCommand]
    private void ValidateName()
    {
        if (!_nameDirty)
            return;

        ApplyNameValidation(ArtistName);
    }

    partial void OnArtistNameChanged(string value)
    {
        // Counter guidance uses the trimmed length — the same string ValidateNameInput checks —
        // so the counter never reports an error the validator would not.
        UpdateCharacterCounter(value?.Trim().Length ?? 0);

        // REQ-PIC-37: the picked Deezer picture follows whether Name still equals the picked name.
        if (_pendingImageUrl is not null)
            NotifyFormImageChanged();

        if (_isHydrating)
            return;   // edit-mode / picker pre-population — do not mark dirty

        _nameDirty = true;

        // "Reward early": once the field is in error, re-validate on every keystroke so the error
        // clears the instant it becomes valid. Do NOT validate on keystroke before the field is in
        // error (that is the "impatient teacher" anti-pattern) — blur/Save handle the first check.
        if (!NameHasError)
            return;

        ApplyNameValidation(value);
    }

    private void ApplyNameValidation(string value)
    {
        var (isValid, message) = _artistService.ValidateNameInput(value ?? string.Empty);
        NameHasError = !isValid;
        NameErrorText = isValid ? string.Empty : message;
    }

    private async Task SaveAsync()
    {
        if (IsBusy) return;   // BUG-049: re-entrancy guard, defense-in-depth alongside CanExecute

        var name = ArtistName?.Trim() ?? string.Empty;

        // Safety net: re-run the field validator on Save regardless of dirty state — an untouched
        // required field (or one whose error was never surfaced) must still be caught.
        ApplyNameValidation(name);
        if (NameHasError)
            return;

        IsBusy = true;
        try
        {
            if (IsEditMode)
            {
                var artistId = ArtistId!.Value;
                var (success, message) = await _artistService.UpdateArtistAsync(artistId, name);
                if (!success)
                {
                    ApplyAsyncFailure(message);
                    return;
                }

                // REQ-PIC-36: edit mode stores the picture only after an upload or a removal.
                if (IsImageChanged && !await TryStoreImageAsync(artistId, FormImageReference))
                    return;

                await _snackbarService.ShowSuccessAsync("Artist updated");
                await (Shell.Current?.GoToAsync("..") ?? Task.CompletedTask);
            }
            else
            {
                var imageReference = FormImageReference;   // read before the Name could change
                var (success, message, artist) = await _artistService.CreateArtistAsync(name);
                if (!success)
                {
                    ApplyAsyncFailure(message);
                    return;
                }

                // REQ-PIC-36: add mode stores the form picture, when there is one, on the new artist.
                if (imageReference is not null && artist is not null
                    && !await TryStoreImageAsync(artist.Id, imageReference))
                {
                    KeepCreatedArtistInEditMode(artist.Id, imageReference);
                    return;
                }

                await _snackbarService.ShowSuccessAsync("Artist created");
                await (Shell.Current?.GoToAsync("..") ?? Task.CompletedTask);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Maps an async CRUD failure to the Artist Name field. This is an explicitly documented
    /// single-field mapping, not substring routing: the Artist form has exactly one editable
    /// field, so every field-attributable failure the service can return after the local
    /// validator passes (the duplicate-name uniqueness check) belongs to the Name field.
    /// </summary>
    private void ApplyAsyncFailure(string message)
    {
        NameHasError = true;
        NameErrorText = message;
    }

    /// <summary>
    /// Second step of Save (REQ-PIC-36): stores the picture reference through the service. A failure shows
    /// the service's message and keeps the form open.
    /// </summary>
    private async Task<bool> TryStoreImageAsync(int artistId, string? imageReference)
    {
        var (success, message) = await _artistService.SetArtistImageAsync(artistId, imageReference);
        if (!success)
            await _snackbarService.ShowErrorAsync(message);
        return success;
    }

    /// <summary>
    /// The artist was created but its picture was not stored: the form becomes the edit form of that artist
    /// (design.md § 2.6) and keeps the picture as a change, so the next Save updates instead of creating a
    /// second artist and stores the picture.
    /// </summary>
    private void KeepCreatedArtistInEditMode(int artistId, string imageReference)
    {
        _uploadedImageReference = imageReference;
        _pendingImageUrl = null;
        _pickedName = null;
        _storedImageReference = null;
        _imageRemoved = false;
        ArtistId = artistId;
    }

    // ── Form picture commands (REQ-PIC-05, 10, 15, 17, 20) ────────────────

    /// <summary>
    /// "Add picture" / "Replace picture" / a tap on the picture: picks one photo and imports it as an
    /// uploaded artist picture. Cancel changes nothing; an unusable photo shows the store's message and keeps
    /// the form picture (REQ-PIC-10). Nothing is written to the artist before Save (REQ-PIC-17).
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
                return;   // REQ-PIC-05: cancelled

            var (success, message, reference) = await _imageStore.ImportAsync(photo, ImageOwnerKind.Artist);
            if (!success || reference is null)
            {
                await _snackbarService.ShowErrorAsync(message);
                return;
            }

            _uploadedImageReference = reference;
            _imageRemoved = false;
            NotifyFormImageChanged();
        }
        catch (Exception ex)
        {
            // The platform picker can fail (no picker activity, permission); the form stays usable (REQ-PIC-10).
            _logger.LogError(ex, "Picking an artist picture failed");
            await _snackbarService.ShowErrorAsync(ImagePickFailedMessage);
        }
        finally
        {
            IsImageBusy = false;
        }
    }

    /// <summary>"Remove picture" (REQ-PIC-20): no form picture; edit mode marks the stored picture for removal.</summary>
    [RelayCommand]
    private void RemoveImage()
    {
        _uploadedImageReference = null;
        _pendingImageUrl = null;
        _imageRemoved = true;
        NotifyFormImageChanged();
    }

    private void NotifyFormImageChanged()
    {
        OnPropertyChanged(nameof(FormImageSource));
        OnPropertyChanged(nameof(FormImageThumbnailSource));
        OnPropertyChanged(nameof(HasFormImage));
    }

    private Task CancelAsync() => Shell.Current?.GoToAsync("..") ?? Task.CompletedTask;

    private Task SelectDuplicateAsync(ArtistListItem artist)
    {
        if (artist is null) return Task.CompletedTask;
        return Shell.Current.GoToAsync($"..?artistId={artist.Id}&artistName={Uri.EscapeDataString(artist.Name)}");
    }

    // ── Name search and picks (Task 6.2) ──────────────────────────────────

    private async Task<IReadOnlyList<AutocompleteSuggestion>> SearchNameAsync(string query, CancellationToken ct)
    {
        // Edit mode: Name is a plain rename field, so no search is issued (REQ-SFT-47).
        if (IsEditMode)
            return [];

        var local = await _artistSuggestions.SearchNameLocalAsync(query, ct);
        var remote = await _artistSuggestions.SearchNameRemoteAsync(query, ct);

        ct.ThrowIfCancellationRequested();   // stale query: never touch the note

        if (query.Trim().Length >= SearchConstants.MinimumRemoteQueryLength)
        {
            var note = remote.Status switch
            {
                RemoteSearchStatus.Offline => OfflineNote,
                RemoteSearchStatus.Unavailable => SearchNotes.Unavailable,
                _ => string.Empty
            };
            RunOnUiThread(() => NameSupportingText = note);
        }

        return local.Select(dto => ToNameOption(dto, "In your library"))
                    .Concat(remote.Remote.Select(dto => ToNameOption(dto, "Deezer")))
                    .ToList();
    }

    /// <summary>REQ-PIC-34 / 39: a leading avatar with the artist's picture (thumbnail), or the placeholder.</summary>
    private AutocompleteSuggestion ToNameOption(ArtistSuggestionDto dto, string supportingText) =>
        new(dto.Name, supportingText, dto)
        {
            LeadingKind = AutocompleteLeadingKind.Avatar,
            ImageUrl = _imageStore.ResolveDisplaySource(dto.ImageUrl, ImageSize.Thumbnail),
            PlaceholderIcon = ArtistPlaceholderIcon
        };

    /// <summary>
    /// Local pick opens that artist's edit form, replacing the add form (REQ-SFT-48). Deezer pick fills
    /// and locks Name only; the provider id is not stored (REQ-SFT-49).
    /// </summary>
    [RelayCommand]
    private Task SelectName(AutocompleteSuggestion? option)
    {
        if (option?.Data is not ArtistSuggestionDto dto)
            return Task.CompletedTask;

        if (dto.IsRemote || !dto.LocalId.HasValue)
        {
            // REQ-PIC-35: the picked artist's picture becomes the form picture (an upload stays first);
            // a pick after "Remove picture" sets a picture again (REQ-PIC-20).
            _pendingImageUrl = dto.ImageUrl;
            _pickedName = dto.Name;
            _imageRemoved = false;
            ArtistName = dto.Name;
            IsNameLocked = true;
            NotifyFormImageChanged();
            return Task.CompletedTask;
        }

        return _navigationService.GoToAsync(
            $"../{Routes.ArtistForm}?artistId={dto.LocalId.Value}&artistName={Uri.EscapeDataString(dto.Name)}");
    }

    /// <summary>X on Name: empties and unlocks it, drops the note (REQ-SFT-06/49) and the Deezer picture, not an upload (REQ-PIC-35).</summary>
    [RelayCommand]
    private void ClearName()
    {
        _pendingImageUrl = null;
        _pickedName = null;
        ArtistName = string.Empty;
        IsNameLocked = false;
        NameSupportingText = string.Empty;
        NotifyFormImageChanged();
    }

    private void UpdateCharacterCounter(int length)
    {
        ShowCharacterCounter = _artistService.ShouldShowCharacterCounter(length);
        if (ShowCharacterCounter)
        {
            var (text, isWarning, isError) = _artistService.GetCharacterCounterInfo(length);
            CharacterCounterText = text;
            IsCharacterCounterWarning = isWarning;
            IsCharacterCounterError = isError;
        }
    }
}
