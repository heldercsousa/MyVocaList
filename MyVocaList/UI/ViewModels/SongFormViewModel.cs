using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Contracts.Messages;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Domain.Resolution;
using MyVocaList.Domain.Rules;
using MyVocaList.UI.Collections;
using MyVocaList.UI.Constants;
using MyVocaList.UI.Models;
using MyVocaList.UI.Services;

namespace MyVocaList.UI.ViewModels;

[QueryProperty(nameof(SongIdRaw), "songId")]
[QueryProperty(nameof(ArtistIdRaw), "artistId")]
[QueryProperty(nameof(ArtistName), "artistName")]
[QueryProperty(nameof(SongTitle), "songTitle")]
public partial class SongFormViewModel : ViewModelBase
{
    private readonly IArtistService _artistService;
    private readonly ISongService _songService;
    private readonly ISnackbarComponent _snackbarService;
    private readonly ILogger<SongFormViewModel> _logger;
    private readonly ISongKaraokeUrlService _karaokeUrlService;
    private readonly ISecureStorageWrapper _secureStorage;
    private readonly IMessenger _messenger;
    private readonly INavigationService _navigationService;
    private readonly ISongSuggestionService _songSuggestions;
    private readonly IImageStore _imageStore;
    private readonly IPhotoPicker _photoPicker;
    private readonly ISongLyricsService _songLyricsService;
    private int _searchGeneration; // BUG-051: generation counter — guards SearchArtistsAsync against stale completions

    // ── Query properties ──────────────────────────────────────────────────
    public string SongIdRaw { set => SongId = int.TryParse(value, out var id) ? id : null; }
    public string ArtistIdRaw { set => ArtistId = int.TryParse(value, out var id) ? id : 0; }

    [ObservableProperty] private int? _songId;
    [ObservableProperty] private int _artistId;
    [ObservableProperty] private string _artistName = string.Empty;
    [ObservableProperty] private string _songTitle = string.Empty;
    [ObservableProperty] private bool _titleHasError;
    [ObservableProperty] private string _titleErrorText = string.Empty;
    [ObservableProperty] private string _characterCounterText = string.Empty;
    [ObservableProperty] private bool _showCharacterCounter;
    [ObservableProperty] private bool _isCharacterCounterWarning;
    [ObservableProperty] private bool _isCharacterCounterError;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isSongLoading;   // REQ-SLD-32: edit-mode load of the stored song is running

    // ── Artist autocomplete ───────────────────────────────────────────────
    [ObservableProperty] private string _artistSearchText = string.Empty;
    [ObservableProperty] private int? _selectedArtistId;
    [ObservableProperty] private string? _selectedArtistName;
    [ObservableProperty] private bool _isArtistLocked;
    [ObservableProperty] private IEnumerable<AutocompleteSuggestion> _artistSuggestions = [];
    [ObservableProperty] private bool _artistHasError;
    [ObservableProperty] private string _artistErrorText = string.Empty;
    // BUG-065/066: two-way bound to the artist field's IsDropDownOpen. Every programmatic ArtistSearchText
    // assignment below also sets this false to force the popup shut; the removed _suppressNextArtistSearch
    // flag never suppressed anything, because the field ignores text changes that are not user input.
    [ObservableProperty] private bool _isArtistDropDownOpen;

    // ── Lyrics ────────────────────────────────────────────────────────────
    [ObservableProperty] private string? _lyrics;
    [ObservableProperty] private bool _isLyricsLoading;
    /// <summary>The running lyrics lookup; tests await it. The pick itself never awaits it (design § 2.5).</summary>
    public Task LyricsLookupTask { get; private set; } = Task.CompletedTask;
    /// <summary>REQ-SLD-19: the lookup on open; a completed null task when none runs. Tests await it.</summary>
    public Task<string?> LyricsOnOpenTask { get; private set; } = Task.FromResult<string?>(null);
    /// <summary>REQ-LYR-09: the Lyrics placeholder while a lookup runs.</summary>
    public string LyricsPlaceholder => IsLyricsLoading ? "Looking for lyrics…" : "Song lyrics (optional)";

    // ── YouTube URLs ──────────────────────────────────────────────────────
    [ObservableProperty] private ObservableRangeCollection<SongKaraokeUrlDto> _karaokeUrls = [];
    private readonly HashSet<string> _addedVideoIds = [];
    private readonly List<string> _pendingRawUrls = [];   // BUG-009: buffer for new-song mode
    [ObservableProperty] private bool _hasYouTubeApiKey;
    [ObservableProperty] private string _pasteUrlInput = string.Empty;
    [ObservableProperty] private string _pasteUrlError = string.Empty;
    [ObservableProperty] private bool _hasPasteUrlError;
    [ObservableProperty] private bool _canLaunchYouTubeSearch;

    // ── External identity (stashed on picker import) ──────────────────────
    public string SelectedExternalId { get; private set; } = string.Empty;
    public string SelectedProvider { get; private set; } = string.Empty;

    // ── Title-first search (Task 5.2, REQ-SFT-04..14, 16, 53) ─────────────
    private const string OfflineNote = "You're offline — showing songs from your library only.";

    /// <summary>Title field option source: local rows, then promoted, then MusicBrainz (REQ-SFT-04/05).</summary>
    public IMenuOptionSource TitleOptionSource { get; }

    /// <summary>Artist field option source: local artists only, no remote call (REQ-SFT-16).</summary>
    public IMenuOptionSource ArtistOptionSource { get; }

    [ObservableProperty] private string _titleSupportingText = string.Empty;
    [ObservableProperty] private bool _isTitleLocked;

    private CancellationTokenSource? _lyricsLookupCts;   // REQ-LYR-10: the running lyrics lookup, if any
    private CancellationTokenSource? _openLyricsLookupCts;   // REQ-SLD-19/23: the lookup on open of this form, if one was started
    private string? _pickFilledLyrics;                   // REQ-LYR-11/12: the Lyrics text the last lookup wrote
    private LyricsLookupResult? _pickLyricsLookup;       // REQ-SLD-26: the outcome of the pick lookup of the held pick
    private string? _pendingImageUrl;        // REQ-ART-11: MusicBrainz cover held between a pick and Save
    private string? _pendingArtistImageUrl;  // REQ-ART-11: MusicBrainz artist picture held between a pick and Save
    private const string SongPlaceholderIcon = "music_note_outlined";
    private const string ArtistPlaceholderIcon = "account_circle_outlined";
    private const string LibrarySourceText = "Library";
    private const string RemoteSourceText = ProviderNames.MusicBrainz;

    // ── Form picture (REQ-PIC-17..22) ─────────────────────────────────────
    private const string ImageHint = "Square picture (1:1), 512 x 512 px or larger. Other shapes are cropped to the centre.";
    private const string ImagePickFailedMessage = "Could not use this picture.";
    private const string SongLoadFailedMessage = "This song could not be loaded.";   // REQ-SLD-34

    private string? _uploadedImageReference;   // REQ-PIC-18 (1): the picture uploaded in this form
    private string? _storedImageReference;     // REQ-PIC-18 (2), edit mode: the song's stored picture
    private bool _imageRemoved;                // REQ-PIC-20: "Remove picture" until a later pick or upload

    [ObservableProperty] private bool _isImageBusy;
    [ObservableProperty] private bool _isTitleEditing;    // TwoWay with the Title TapToEditField (REQ-PIC-25/26)
    [ObservableProperty] private bool _isArtistEditing;   // TwoWay with the Artist TapToEditField (REQ-PIC-25/26)

    // REQ-PIC-27/28: what the last MusicBrainz pick wrote, so a release by a tap keeps a pending value only while
    // the text is unchanged. Exact (ordinal) comparisons that choose which pending value Save sends; they are
    // not search or de-duplication.
    private string? _pickedTitle;
    private string? _pickedArtistName;
    private bool _isClearingArtist;   // ClearArtist unlocks the field itself: not a release by a tap

    /// <summary>The form picture's reference (REQ-PIC-18, 20): upload, else stored (edit) or pending MusicBrainz cover (add).</summary>
    private string? FormImageReference =>
        _imageRemoved ? null : _uploadedImageReference ?? (IsEditMode ? _storedImageReference : _pendingImageUrl);

    /// <summary>Display source of the form picture, large size (REQ-PIC-13); null shows the placeholder.</summary>
    public string? FormImageSource => _imageStore.ResolveDisplaySource(FormImageReference, ImageSize.Large);

    /// <summary>Display source of the form picture, thumbnail size, drawn under the large one.</summary>
    public string? FormImageThumbnailSource => _imageStore.ResolveDisplaySource(FormImageReference, ImageSize.Thumbnail);

    /// <summary>True when the form has a picture (REQ-PIC-15/16 switch the buttons and the hint).</summary>
    public bool HasFormImage => FormImageReference is not null;

    /// <summary>REQ-PIC-16: the size hint shown while there is no form picture.</summary>
    public string ImageHintText => ImageHint;

    // ── Save-outcome sheets (Task 5.3, REQ-SFT-19, REQ-SFT-22/23) ─────────
    private const string ExactDuplicateSheetTitle = "This looks like an existing song";
    private const string PossibleDuplicatesSheetTitle = "Possible duplicates";

    [ObservableProperty] private bool _isResolutionSheetVisible;
    [ObservableProperty] private string _resolutionSheetTitle = ExactDuplicateSheetTitle;
    [ObservableProperty] private IReadOnlyList<SongMatch> _resolutionCandidates = [];
    [ObservableProperty] private bool _isExactDuplicate;   // true: exact sheet (Open existing / Cancel only, REQ-RMV-07); false: possible duplicates (Save as new song)

    [ObservableProperty] private bool _isArtistMatchSheetVisible;
    [ObservableProperty] private IReadOnlyList<ArtistMatch> _artistMatchCandidates = [];
    [ObservableProperty] private string _artistMatchTypedName = string.Empty;

    // Decisions held for ONE Save attempt (REQ-SFT-51). A user Save tap starts a new attempt; a sheet
    // choice re-invokes the Service with the decision set; an Artist or Title edit clears both.
    private ArtistDecision _artistDecision = new ArtistDecision.None();
    private SongDecision _songDecision = SongDecision.None;

    // Becomes true once the user edits each field. Guards blur validation so a pristine field the
    // user only tabbed through is not flagged prematurely (Form Validation Standard).
    private bool _titleDirty;

    // True until the page's OnAppearing calls CompleteHydration(). While true, Shell QueryProperty
    // pre-population (edit mode) must NOT mark any field dirty — otherwise a pre-filled invalid
    // value would flash an error on the very first blur, before the user has touched anything
    // (edit-mode dirty-guard, Form Validation Standard).
    private bool _isHydrating = true;

    public bool IsEditMode => SongId.HasValue;
    public string PageTitle => IsEditMode ? "Edit Song" : "New Song";

    /// <summary>Add mode: a Save now would store the pick's external identity (REQ-PIC-27, REQ-SLD-14).</summary>
    private bool HoldsRemotePick =>
        !IsEditMode && SongIdentityRules.IsRemoteSong(SelectedProvider, SelectedExternalId)
        && string.Equals(SongTitle, _pickedTitle, StringComparison.Ordinal);

    /// <summary>REQ-SLD-12/14: the Artist is plain text. Edit mode: frozen until the stored song has loaded, then while it is a MusicBrainz song. Add mode: the pick is held.</summary>
    public bool IsArtistFrozen => IsEditMode ? (!_storedSongLoaded || _storedIsRemoteSong) : HoldsRemotePick;
    public bool IsArtistEditable => !IsArtistFrozen;   // for the page, no converter needed

    /// <summary>REQ-SLD-36: the Title of a stored MusicBrainz song is plain text. Edit mode: frozen until the stored song has loaded, then while it is a MusicBrainz song. Add mode: editable (C37).</summary>
    public bool IsTitleFrozen => IsEditMode && (!_storedSongLoaded || _storedIsRemoteSong);
    public bool IsTitleEditable => !IsTitleFrozen;   // for the page, no converter needed

    private bool _storedSongLoaded;     // false until LoadSongForEditAsync has set the stored song (or found none)
    private bool _storedIsRemoteSong;   // set in LoadSongForEditAsync from the loaded entity
    private int _storedArtistId;        // set in LoadSongForEditAsync
    private string _storedTitle = string.Empty;   // REQ-SLD-38: the title as loaded; a frozen Title is saved with it

    /// <summary>Raises the change of the frozen state of the Artist and the Title; called wherever the pick, the Title or the stored song changes it.</summary>
    private void NotifyArtistFrozenChanged()
    {
        OnPropertyChanged(nameof(IsArtistFrozen));
        OnPropertyChanged(nameof(IsArtistEditable));
        OnPropertyChanged(nameof(IsTitleFrozen));   // REQ-SLD-36
        OnPropertyChanged(nameof(IsTitleEditable));
    }

    /// <summary>REQ-SFT-27/02: Title is the search box in add mode only; edit mode shows plain editable text.</summary>
    public bool IsTitleSearchEnabled => !IsEditMode;

    public SongFormViewModel(
        IArtistService artistService,
        ISongService songService,
        ISnackbarComponent snackbarService,
        ILogger<SongFormViewModel> logger,
        ISongKaraokeUrlService karaokeUrlService,
        ISecureStorageWrapper secureStorage,
        IMessenger messenger,
        INavigationService navigationService,
        ISongSuggestionService songSuggestions,
        IImageStore imageStore,
        IPhotoPicker photoPicker,
        ISongLyricsService songLyricsService)
    {
        _artistService = artistService;
        _songLyricsService = songLyricsService;
        _songService = songService;
        _snackbarService = snackbarService;
        _logger = logger;
        _karaokeUrlService = karaokeUrlService;
        _secureStorage = secureStorage;
        _messenger = messenger;
        _navigationService = navigationService;
        _songSuggestions = songSuggestions;
        _imageStore = imageStore;
        _photoPicker = photoPicker;

        TitleOptionSource = new MenuOptionSource(SearchTitleAsync);
        ArtistOptionSource = new MenuOptionSource(SearchArtistOptionsAsync);

        // BUG-049: explicit CanExecute tied to IsBusy — combined with the early-return guard in
        // SaveAsync — prevents a fast double-tap from firing SaveAsync twice concurrently, which
        // caused a duplicate "GoToAsync("..")" that overshot the nav stack root.
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
        CancelCommand = new AsyncRelayCommand(CancelAsync);
        SearchArtistsCommand = new AsyncRelayCommand<string>(SearchArtistsAsync);
        SelectArtistCommand = new RelayCommand<AutocompleteSuggestion>(SelectArtist);
        ArtistBlurredWithoutSelectionCommand = new RelayCommand(OnArtistBlurredWithoutSelection);
        NavigateToYouTubeSearchCommand = new AsyncRelayCommand(NavigateToYouTubeSearchAsync,
            AsyncRelayCommandOptions.None);
        AddFromPasteCommand = new AsyncRelayCommand(AddFromPasteAsync);
        RemoveUrlCommand = new AsyncRelayCommand<SongKaraokeUrlDto>(RemoveUrlAsync);
        GoToSettingsCommand = new AsyncRelayCommand(async () => await Shell.Current.GoToAsync("//settings"));

        // Duplicate-song and artist-match sheet commands
        OpenExistingSongCommand = new AsyncRelayCommand<SongMatch>(OpenExistingSongAsync);
        ConfirmSaveAsNewSongCommand = new AsyncRelayCommand(ConfirmSaveAsNewSongAsync);
        DismissResolutionSheetCommand = new RelayCommand(DismissResolutionSheet);
        UseExistingArtistCommand = new AsyncRelayCommand<ArtistMatch>(UseExistingArtistAsync);
        CreateTypedArtistCommand = new AsyncRelayCommand(CreateTypedArtistAsync);
        DismissArtistMatchSheetCommand = new RelayCommand(DismissArtistMatchSheet);
    }

    // ── Commands ──────────────────────────────────────────────────────────
    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand CancelCommand { get; }
    public IAsyncRelayCommand<string> SearchArtistsCommand { get; }
    public IRelayCommand<AutocompleteSuggestion> SelectArtistCommand { get; }
    /// <summary>Invoked by the artist autocomplete field when the user blurs without selecting a suggestion (BUG-008).</summary>
    public IRelayCommand ArtistBlurredWithoutSelectionCommand { get; }
    public IAsyncRelayCommand NavigateToYouTubeSearchCommand { get; }
    public IAsyncRelayCommand AddFromPasteCommand { get; }
    public IAsyncRelayCommand<SongKaraokeUrlDto> RemoveUrlCommand { get; }
    public IAsyncRelayCommand GoToSettingsCommand { get; }

    // Duplicate-song sheet
    /// <summary>Opens the stored song's edit form in place of this add form (REQ-SFT-22/24).</summary>
    public IAsyncRelayCommand<SongMatch> OpenExistingSongCommand { get; }
    /// <summary>"Save as new song": re-invokes the Service with SaveAsNewSong (REQ-SFT-23; near-duplicates only, REQ-RMV-07).</summary>
    public IAsyncRelayCommand ConfirmSaveAsNewSongCommand { get; }
    public IRelayCommand DismissResolutionSheetCommand { get; }

    // Artist-match sheet (REQ-SFT-19): display and re-invoke only; the Service decides
    /// <summary>"Use '&lt;existing&gt;'": re-invokes the Service with UseExisting(id).</summary>
    public IAsyncRelayCommand<ArtistMatch> UseExistingArtistCommand { get; }
    /// <summary>"Create '&lt;typed&gt;'": re-invokes the Service with CreateTyped.</summary>
    public IAsyncRelayCommand CreateTypedArtistCommand { get; }
    /// <summary>Back or scrim: saves nothing and hides the sheet.</summary>
    public IRelayCommand DismissArtistMatchSheetCommand { get; }

    /// <summary>
    /// Called by the page's <c>OnAppearing</c> once Shell has finished applying all
    /// <c>[QueryProperty]</c> values for this navigation. Ends the hydration window so that
    /// subsequent field edits are tracked as dirty (edit-mode dirty-guard).
    /// </summary>
    public void CompleteHydration() => _isHydrating = false;

    partial void OnIsBusyChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();

    /// <summary>
    /// Blur handler (invoked from the page's <c>Unfocused</c> event). Validates the title field
    /// only when it is dirty — a pristine field the user only tabbed through is left untouched.
    /// </summary>
    [RelayCommand]
    private void ValidateTitle()
    {
        if (!_titleDirty)
            return;

        ApplyTitleValidation(SongTitle);
    }

    private void ApplyTitleValidation(string value)
    {
        var (isValid, message) = _songService.ValidateTitleInput(value ?? string.Empty);
        TitleHasError = !isValid;
        TitleErrorText = isValid ? string.Empty : message;
    }

    // ── QueryProperty change hooks ────────────────────────────────────────

    partial void OnSongIdChanged(int? value)
    {
        _storedSongLoaded = false;          // a new song id: the stored song of the previous id no longer counts
        _storedIsRemoteSong = false;
        IsSongLoading = value.HasValue;     // REQ-SLD-32: edit mode shows the placeholder until the load ends
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(IsTitleSearchEnabled));
        OnPropertyChanged(nameof(PageTitle));
        NotifyArtistFrozenChanged();
        NotifyFormImageChanged();   // REQ-PIC-18: the mode decides the second source of the form picture
        if (value.HasValue)
            _ = LoadSongForEditAsync(value.Value);
    }

    partial void OnSongTitleChanged(string value)
    {
        ClearSaveDecisions();   // REQ-SFT-51: a Title edit clears the Save decisions

        if (string.IsNullOrWhiteSpace(value) && TitleSupportingText.Length > 0)
            TitleSupportingText = string.Empty;   // REQ-SFT-06: the Field never queries an empty string

        // Counter counts the trimmed string — the same string ValidateTitleInput checks —
        // so trailing whitespace cannot inflate the count (counter threshold alignment).
        UpdateCharacterCounter(value?.Trim().Length ?? 0);
        UpdateCanLaunchYouTubeSearch();
        // REQ-SLD-39 (closes C25): in add mode a Title text that differs from the picked title releases the pick for
        // good. The Title text, the pending song image and the pick lyrics outcome (REQ-SLD-26) stay.
        if (!IsEditMode && _pickedTitle is not null && !string.Equals(value, _pickedTitle, StringComparison.Ordinal))
            ReleaseRemotePickOnTitleChange();
        NotifyArtistFrozenChanged();   // REQ-SLD-14: a Title text change away from the picked title releases the artist

        if (_isHydrating)
            return;   // edit-mode pre-population — do not mark dirty

        _titleDirty = true;

        // "Reward early": once the field is in error, re-validate on every keystroke so the error
        // clears the instant it becomes valid. Do NOT validate on keystroke before the field is in
        // error (that is the "impatient teacher" anti-pattern) — blur/Save handle the first check.
        if (!TitleHasError)
            return;

        ApplyTitleValidation(value);
    }

    partial void OnArtistIdChanged(int value)
    {
        // Handled via InitializeArtistField() from OnAppearing — do not overwrite here
        // because query property arrival order is not guaranteed.
    }

    partial void OnArtistNameChanged(string value)
    {
        UpdateCanLaunchYouTubeSearch();
    }

    /// <summary>REQ-SFT-51: an Artist edit clears the Save decisions (programmatic restores clear too).</summary>
    partial void OnArtistSearchTextChanged(string value) => ClearSaveDecisions();

    // ── Artist autocomplete ───────────────────────────────────────────────

    private Task SearchArtistsAsync(string term) => SearchArtistsCoreAsync(term);

    /// <summary>
    /// BUG-056: runs the artist search and RETURNS the current query's mapped suggestions so the
    /// page's <c>AutoCompleteEdit</c> provider receives them directly (no read-before-dispatch gap
    /// against <see cref="ArtistSuggestions"/>). The observable <see cref="ArtistSuggestions"/> is
    /// still updated for the latest query only (BUG-051 generation guard) so other observers see
    /// latest-query-wins, but the returned list is always this call's own results.
    /// </summary>
    public async Task<IReadOnlyList<AutocompleteSuggestion>> SearchArtistsCoreAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            RunOnUiThread(() => ArtistSuggestions = []);
            return [];
        }
        var gen = Interlocked.Increment(ref _searchGeneration); // BUG-051: guards against a slower earlier query clobbering a newer one
        var results = await _artistService.SearchArtistsByNameAsync(term, maxResults: 5);
        var mapped = results
            .Select(ToArtistOption)
            .ToList();
        // Only the latest query updates the shared observable state; the provider for THIS query
        // still receives its own results below (DX cancels stale provider requests via its token).
        if (gen == _searchGeneration)
            RunOnUiThread(() => ArtistSuggestions = mapped);
        return mapped;
    }

    /// <summary>REQ-ART-09/10: a leading avatar, the real catalog count, no trailing text. REQ-PIC-39: thumbnail source.</summary>
    private AutocompleteSuggestion ToArtistOption(ArtistListItem artist) =>
        new(artist.Name, artist.CatalogCountText, artist)
        {
            LeadingKind = AutocompleteLeadingKind.Avatar,
            ImageUrl = _imageStore.ResolveDisplaySource(artist.ImageUrl, ImageSize.Thumbnail),
            PlaceholderIcon = ArtistPlaceholderIcon
        };

    private void SelectArtist(AutocompleteSuggestion suggestion)
    {
        if (suggestion?.Data is not ArtistListItem artist) return;
        LockArtist(artist.Id, artist.Name);
    }

    /// <summary>
    /// REQ-PIC-28/31: the Artist's tap-to-edit wrapper releases a picked artist by setting
    /// <see cref="IsArtistLocked"/> to false and keeping the text. The local id is dropped so Save sends
    /// the text and the Service resolves it (exact match → same artist, near duplicates → sheet, else
    /// create). Not a release: hydration (it only ever locks) and <see cref="ClearArtist"/> (X), which
    /// resets the selection itself. A later pick locks again through <see cref="LockArtist"/>.
    /// </summary>
    partial void OnIsArtistLockedChanged(bool value)
    {
        if (value || _isHydrating || _isClearingArtist)
            return;

        SelectedArtistId = 0;
        SelectedArtistName = null;
    }

    /// <summary>
    /// REQ-ACREATE-04/12 (BUG-050): sets the selected+locked artist from an existing-suggestion
    /// pick, mirrors the name into the search text, clears suggestions, and clears any pending
    /// artist error.
    /// </summary>
    private void LockArtist(int id, string name)
    {
        SelectedArtistId = id;
        SelectedArtistName = name;
        ArtistSearchText = name;
        IsArtistDropDownOpen = false; // BUG-065/066: dismiss the popup for this programmatic assignment
        IsArtistLocked = true;
        ArtistSuggestions = [];
        ArtistHasError = false;
        ArtistErrorText = string.Empty;
    }

    /// <summary>
    /// REQ-SFT-17: blur-retain rule. If no artist is locked in, keep the typed text (never clear
    /// it) and set no error — the text stays available for a later search/pick. If one was
    /// previously selected, restore the name so the field stays consistent.
    /// </summary>
    private void OnArtistBlurredWithoutSelection()
    {
        if (!SelectedArtistId.HasValue || SelectedArtistId.Value == 0)
        {
            ArtistSuggestions = [];
        }
        else
        {
            // User typed-then-blurred without choosing a new suggestion — restore prior selection
            ArtistSearchText = SelectedArtistName ?? string.Empty;
            IsArtistDropDownOpen = false; // BUG-065/066: dismiss the popup for this programmatic restore
            ArtistSuggestions = [];
        }
    }

    /// <summary>
    /// REQ-ACREATE-15 (BUG-060): invoked when the user taps the Artist field's clear (X) icon.
    /// A locked artist field must be changeable — clearing unlocks it and drops the selection so
    /// the field returns to a normal searchable state. Because <see cref="SelectedArtistId"/> is
    /// nulled here, a subsequent blur takes the "no artist selected" branch of
    /// <see cref="OnArtistBlurredWithoutSelection"/> instead of the restore-prior-selection branch —
    /// an intentional clear is never silently overwritten with the prior artist.
    /// </summary>
    [RelayCommand]
    private void ClearArtist()
    {
        CancelLyricsLookup();            // REQ-LYR-10
        _pickLyricsLookup = null;        // REQ-SLD-26: X on Artist drops the pick and its outcome
        DropUntouchedPickFilledLyrics(); // REQ-LYR-11
        SelectedArtistId = null;
        SelectedArtistName = null;
        ArtistSearchText = string.Empty;
        IsArtistDropDownOpen = false; // BUG-065/066: dismiss the popup for this programmatic clear
        _isClearingArtist = true;     // REQ-PIC-28: this unlock is X, not a release by a tap
        try
        {
            IsArtistLocked = false;
        }
        finally
        {
            _isClearingArtist = false;
        }
        ArtistSuggestions = [];
        ArtistHasError = false;
        ArtistErrorText = string.Empty;
        SelectedExternalId = string.Empty;   // REQ-SFT-14: the pending identity belongs to the pair
        SelectedProvider = string.Empty;
        _pendingImageUrl = null;             // REQ-ART-13: X on Artist drops both pending images
        _pendingArtistImageUrl = null;
        _pickedTitle = null;
        _pickedArtistName = null;
        NotifyFormImageChanged();            // REQ-PIC-19: an upload stays the form picture
        NotifyArtistFrozenChanged();
    }

    // ── Title-first search and picks (Task 5.2) ───────────────────────────

    private async Task<IReadOnlyList<AutocompleteSuggestion>> SearchArtistOptionsAsync(string query, CancellationToken ct) =>
        await SearchArtistsCoreAsync(query);

    private async Task<IReadOnlyList<AutocompleteSuggestion>> SearchTitleAsync(string query, CancellationToken ct)
    {
        var local = await _songSuggestions.SearchTitleLocalAsync(query, ct);
        var shownIds = local.Where(d => d.LocalId.HasValue).Select(d => d.LocalId!.Value).ToList();
        var remote = await _songSuggestions.SearchTitleRemoteAsync(query, shownIds, ct);

        ct.ThrowIfCancellationRequested();   // stale query: never touch the note

        if (query.Trim().Length >= SearchConstants.MinimumRemoteQueryLength)
        {
            var note = remote.Status switch
            {
                RemoteSearchStatus.Offline => OfflineNote,
                RemoteSearchStatus.Unavailable => SearchNotes.Unavailable,
                _ => string.Empty
            };
            RunOnUiThread(() => TitleSupportingText = note);
        }

        return local.Concat(remote.Promoted).Concat(remote.Remote).Select(ToTitleOption).ToList();
    }

    private AutocompleteSuggestion ToTitleOption(SongSuggestionDto dto)
    {
        // REQ-ART-07 (Version part removed, REQ-RMV-01): artist · album; album only on MusicBrainz rows, never when it repeats the title.
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(dto.ArtistName))
            parts.Add(dto.ArtistName);
        if (dto.IsRemote
            && !string.IsNullOrWhiteSpace(dto.AlbumTitle)
            && !string.Equals(dto.AlbumTitle, dto.Title, StringComparison.OrdinalIgnoreCase))
            parts.Add(dto.AlbumTitle);

        return new AutocompleteSuggestion(dto.Title, string.Join(" · ", parts), dto)
        {
            LeadingKind = AutocompleteLeadingKind.Image,
            ImageUrl = _imageStore.ResolveDisplaySource(dto.RowImageUrl, ImageSize.Thumbnail),   // REQ-PIC-39
            PlaceholderIcon = SongPlaceholderIcon,
            TrailingText = dto.IsRemote ? RemoteSourceText : LibrarySourceText   // REQ-ART-08
        };
    }

    [RelayCommand]
    private Task SelectTitle(AutocompleteSuggestion? option)
    {
        if (option?.Data is not SongSuggestionDto dto)
            return Task.CompletedTask;

        if (dto.IsRemote || !dto.LocalId.HasValue)
        {
            ApplyRemotePick(dto);
            return Task.CompletedTask;
        }

        // REQ-SFT-10: a local pick always opens the edit form; other input is not guarded.
        return OpenSongAsync(dto.LocalId.Value);
    }

    /// <summary>REQ-SLD-39: a Title text change releases the pick: the identity and the picked title go, the Title text stays.</summary>
    private void ReleaseRemotePickOnTitleChange()
    {
        SelectedExternalId = string.Empty;
        SelectedProvider = string.Empty;
        _pickedTitle = null;
    }

    private void ApplyRemotePick(SongSuggestionDto dto)
    {
        _pickedTitle = null;         // the pick's own Title write must not release the pick it is making
        SongTitle = dto.Title;
        IsTitleLocked = true;
        SelectedExternalId = dto.ExternalId ?? string.Empty;
        SelectedProvider = dto.ExternalProvider ?? string.Empty;
        _pendingImageUrl = dto.ImageUrl;             // REQ-ART-11: the cover, not the row-image fallback
        _pendingArtistImageUrl = dto.ArtistImageUrl;
        _imageRemoved = false;                       // REQ-PIC-20: a later pick sets a picture again
        _pickedTitle = dto.Title;                    // REQ-PIC-27: the identity survives an unchanged Title
        NotifyArtistFrozenChanged();                 // REQ-SLD-14: the pick now holds the identity
        NotifyFormImageChanged();                    // REQ-PIC-19: an upload stays first
        StartLyricsLookup(dto);                      // REQ-LYR-01: before the artist branch, so every pick starts it

        if (dto.LocalArtistId.HasValue)
        {
            LockArtist(dto.LocalArtistId.Value, dto.LocalArtistName ?? dto.ArtistName);
            _pickedArtistName = ArtistSearchText;    // REQ-PIC-28
            return;
        }

        SelectedArtistId = null;   // REQ-SFT-13: the artist is created on Save
        SelectedArtistName = null;
        ArtistSearchText = dto.ArtistName;
        _pickedArtistName = ArtistSearchText;        // REQ-PIC-28
        IsArtistDropDownOpen = false;
        IsArtistLocked = true;
        ArtistSuggestions = [];
        ArtistHasError = false;
        ArtistErrorText = string.Empty;
    }

    // ── Lyrics lookup on a MusicBrainz pick (REQ-LYR-01…04, 09…13) ───────────

    partial void OnIsLyricsLoadingChanged(bool value) => OnPropertyChanged(nameof(LyricsPlaceholder));

    /// <summary>
    /// Starts the lyrics lookup for a MusicBrainz pick. Cancels a running lookup first; drops pick-filled lyrics
    /// the user did not touch (a new pick replaces them, REQ-LYR-12); starts nothing when Lyrics holds text
    /// (REQ-LYR-04). The pick does not await the lookup.
    /// </summary>
    private void StartLyricsLookup(SongSuggestionDto dto)
    {
        CancelLyricsLookup();
        _pickLyricsLookup = null;   // REQ-SLD-26: a newer pick replaces the outcome of the held pick
        DropUntouchedPickFilledLyrics();
        if (!string.IsNullOrWhiteSpace(Lyrics))
            return;

        var cts = new CancellationTokenSource();
        _lyricsLookupCts = cts;
        IsLyricsLoading = true;
        LyricsLookupTask = RunLyricsLookupAsync(
            new LyricsQuery(dto.Title, dto.ArtistName, dto.AlbumTitle, dto.DurationSeconds), cts);
    }

    /// <summary>
    /// Awaits the lookup and hands its answer to the UI thread. A cancelled lookup is the expected outcome of
    /// REQ-LYR-10 and is swallowed; the service never throws any other exception for a lookup.
    /// </summary>
    private async Task RunLyricsLookupAsync(LyricsQuery query, CancellationTokenSource cts)
    {
        LyricsLookupResult answer;
        try
        {
            answer = await _songLyricsService.LookupLyricsAsync(query, cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            return;
        }

        RunOnUiThread(() => ApplyLyricsAnswer(cts, answer));
    }

    /// <summary>
    /// Writes a lookup answer into the form only while that lookup is still the current one (REQ-LYR-10):
    /// a stale answer touches nothing, so a newer pick keeps its hint.
    /// </summary>
    private void ApplyLyricsAnswer(CancellationTokenSource cts, LyricsLookupResult answer)
    {
        if (!ReferenceEquals(_lyricsLookupCts, cts) || cts.IsCancellationRequested)
            return;

        _pickLyricsLookup = answer;   // REQ-SLD-26: kept for Save; a Title text change does not clear it
        IsLyricsLoading = false;
        if (answer is { Status: LyricsLookupStatus.Found, Lyrics: { } found } && string.IsNullOrWhiteSpace(Lyrics))
        {
            Lyrics = found;
            _pickFilledLyrics = found;   // REQ-LYR-11/12: remembered so X or a new pick can take it back
        }
    }

    /// <summary>Cancels the running lyrics lookup, drops its token and clears the hint (REQ-LYR-10).</summary>
    private void CancelLyricsLookup()
    {
        _lyricsLookupCts?.Cancel();
        _lyricsLookupCts?.Dispose();
        _lyricsLookupCts = null;
        IsLyricsLoading = false;
    }

    /// <summary>REQ-LYR-11/12: removes pick-filled lyrics only while the field still equals them (exact comparison).</summary>
    private void DropUntouchedPickFilledLyrics()
    {
        if (_pickFilledLyrics is not null && string.Equals(Lyrics, _pickFilledLyrics, StringComparison.Ordinal))
            Lyrics = null;
        _pickFilledLyrics = null;
    }

    private Task OpenSongAsync(int songId) =>
        _navigationService.GoToAsync($"../{Routes.SongForm}?songId={songId}");

    /// <summary>X on Title: empties and unlocks it, drops the pending identity, keeps Artist (REQ-SFT-06/14).</summary>
    [RelayCommand]
    private void ClearTitle()
    {
        CancelLyricsLookup();            // REQ-LYR-10
        _pickLyricsLookup = null;        // REQ-SLD-26: X on Title drops the pick and its outcome
        DropUntouchedPickFilledLyrics(); // REQ-LYR-11
        SongTitle = string.Empty;
        IsTitleLocked = false;
        SelectedExternalId = string.Empty;
        SelectedProvider = string.Empty;
        TitleSupportingText = string.Empty;
        _pickedTitle = null;
        _pendingImageUrl = null;             // REQ-ART-13: X on Title drops the song image, keeps the artist image
        NotifyFormImageChanged();            // REQ-PIC-19: an upload stays the form picture
        NotifyArtistFrozenChanged();
    }

    // ── Form picture commands (REQ-PIC-05, 10, 15, 17, 20) ────────────────

    /// <summary>
    /// "Add picture" / "Replace picture" / a tap on the picture: picks one photo and imports it as an
    /// uploaded picture. Cancel changes nothing; an unusable photo shows the store's message and keeps
    /// the form picture (REQ-PIC-10). Nothing is written to the song before Save (REQ-PIC-17).
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

            var (success, message, reference) = await _imageStore.ImportAsync(photo, ImageOwnerKind.Song);
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
            _logger.LogError(ex, "Picking a song picture failed");
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

    /// <summary>
    /// BUG-008: called from OnAppearing after all QueryProperties are set.
    /// Initialises artist field reliably regardless of query-property arrival order.
    /// </summary>
    public void InitializeArtistField()
    {
        // REQ-SLD-30: the stored artist of a MusicBrainz song is loaded from the entity; catalog query values must not replace it.
        if (IsEditMode && _storedIsRemoteSong)
            return;

        if (ArtistId > 0)
        {
            SelectedArtistId = ArtistId;
            SelectedArtistName = ArtistName;
            ArtistSearchText = ArtistName;
            IsArtistDropDownOpen = false; // BUG-065/066: dismiss the popup for this programmatic hydration
            IsArtistLocked = true; // BUG-052: edit-mode hydration must show the artist as locked
        }
    }

    // ── Edit mode: load song entity ───────────────────────────────────────

    /// <summary>
    /// BUG-008/BUG-024: loads the full Song entity in edit mode and hydrates every field that
    /// <c>SaveFromFormAsync</c> persists (Title, Lyrics, external identity)
    /// so Save cannot silently wipe stored data. Also loads karaoke URLs and determines
    /// IsArtistLocked (API-imported song: ExternalId set AND no manual edits).
    /// </summary>
    private async Task LoadSongForEditAsync(int songId)
    {
        // REQ-DLH-01: the debug-only shimmer hold starts with the load; it is awaited in finally, so the placeholder
        // lasts max(load, hold). Zero duration (every build but Debug, and every unit test) = completed task.
        var hold = LoadingHold.WaitAsync(CancellationToken.None);
        try
        {
            if (await HydrateStoredSongAsync(songId))
                return;

            // REQ-SLD-34: a missing song is a failed load, the same as an exception.
            await FailSongLoadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loading song {SongId} for edit failed", songId);
            await FailSongLoadAsync();
        }
        finally
        {
            await hold;
            IsSongLoading = false;   // REQ-SLD-32: the placeholder ends however the load ended
        }
    }

    /// <summary>REQ-SLD-34: the message through the snackbar, then back to where the form was opened from. Nothing is stored.</summary>
    private async Task FailSongLoadAsync()
    {
        await _snackbarService.ShowErrorAsync(SongLoadFailedMessage);
        await _navigationService.GoToAsync("..");
    }

    /// <summary>
    /// Hydrates the form from the stored song. Returns false when the song does not exist (the caller treats it as a failed load).
    /// </summary>
    private async Task<bool> HydrateStoredSongAsync(int songId)
    {
        await LoadKaraokeUrlsAsync();

        // Bug fix: the database call runs on a background thread (Microsoft.Data.Sqlite runs "async" calls synchronously,
        // which blocked the UI thread before the loading indicator was drawn). The continuation is back on the UI thread.
        var song = await Task.Run(() => _songService.GetSongByIdAsync(songId));
        if (song is null)
        {
            _logger.LogWarning("Edit mode: song {SongId} not found during hydration", songId);
            _storedSongLoaded = true;   // nothing stored to freeze the Artist for: the form stays editable
            await RunOnUiThreadAsync(() =>
            {
                NotifyArtistFrozenChanged();
                return Task.CompletedTask;
            });
            return false;
        }

        // Stash external identity so the edit Save round-trips it unchanged.
        SelectedExternalId = song.ExternalId ?? string.Empty;
        SelectedProvider = song.ExternalProvider ?? string.Empty;

        // REQ-SLD-12/30: the frozen state and the stored artist come from the loaded entity, not from the pick fields.
        _storedIsRemoteSong = SongIdentityRules.IsRemoteSong(song.ExternalProvider, song.ExternalId);
        _storedArtistId = song.ArtistId;
        _storedTitle = song.Title ?? string.Empty;   // REQ-SLD-38: the title a frozen Title is saved with
        _storedSongLoaded = true;   // set before the UI block below, whose NotifyArtistFrozenChanged raises the change

        // Awaited, so IsSongLoading (LoadSongForEditAsync, finally) turns false only after the stored values are applied.
        await RunOnUiThreadAsync(() =>
        {
            // This may run after the page's OnAppearing already called CompleteHydration()
            // (the entity load is async) — re-open the hydration window while applying stored
            // values so pre-population does not mark fields dirty (edit-mode dirty-guard).
            var wasHydrating = _isHydrating;
            _isHydrating = true;
            try
            {
                SongTitle = song.Title ?? string.Empty;
                Lyrics = song.Lyrics;

                // BUG-055: hydrate the stored artist so the field shows it (name) and Save preserves
                // the artist link (the artist-required guard reads SelectedArtistId). InitializeArtistField
                // only fires for the artistId QueryProperty (0 in normal edit), so hydrate from the
                // loaded entity here. Runs inside the _isHydrating window above → fires no search
                // (REQ-ACREATE-14). song.OriginalArtist is eager-loaded by SongRepository.GetByIdAsync.
                if (song.ArtistId > 0)
                {
                    SelectedArtistId = song.ArtistId;
                    SelectedArtistName = song.OriginalArtist?.Name ?? ArtistName;
                    ArtistSearchText = SelectedArtistName ?? string.Empty;
                    IsArtistDropDownOpen = false; // BUG-065/066: dismiss the popup for this programmatic hydration
                }

                // The stored artist shows as a pick (REQ-ACREATE-14); a tap on the Artist text releases it so
                // the artist of a stored song can be changed (REQ-PIC-31, OnIsArtistLockedChanged).
                IsArtistLocked = true;

                _storedImageReference = song.ImageUrl;   // REQ-PIC-18 (2): edit mode shows the stored picture
                NotifyFormImageChanged();
            }
            finally
            {
                _isHydrating = wasHydrating;
            }
            NotifyArtistFrozenChanged();
            return Task.CompletedTask;
        });

        // REQ-SLD-19: a MusicBrainz song without lyrics or recorded origin looks for them on open; the form stays usable.
        if (_openLyricsLookupCts is null
            && LyricsOriginRules.NeedsLookupOnOpen(song.ExternalProvider, song.ExternalId, song.Lyrics, song.LyricsOrigin))
            StartLyricsLookupOnOpen(songId);
        return true;
    }

    /// <summary>Called by the page when the form is left: asks the lookup on open to stop (REQ-SLD-23).</summary>
    public void CancelLyricsLookupOnOpen() => _openLyricsLookupCts?.Cancel();

    /// <summary>REQ-SLD-19: starts the lookup on open without awaiting it. At most one per form instance.</summary>
    private void StartLyricsLookupOnOpen(int songId)
    {
        var cts = new CancellationTokenSource();
        _openLyricsLookupCts = cts;
        RunOnUiThread(() => IsLyricsLoading = true);
        LyricsOnOpenTask = RunLyricsLookupOnOpenAsync(songId, cts);
    }

    /// <summary>
    /// Asks the service on a background thread (Microsoft.Data.Sqlite runs "async" calls synchronously) and hands the
    /// answer to the UI thread. A cancelled lookup yields null; the service does not throw for expected failures, so any
    /// other exception is logged and yields null. The answer is applied on the UI thread before this task ends.
    /// </summary>
    private async Task<string?> RunLyricsLookupOnOpenAsync(int songId, CancellationTokenSource cts)
    {
        string? found;
        try
        {
            found = await Task.Run(() => _songService.EnsureLyricsAsync(songId, cts.Token));
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            found = null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lyrics lookup on open failed for song {SongId}", songId);
            found = null;
        }

        await RunOnUiThreadAsync(() =>
        {
            ApplyLyricsOnOpenAnswer(songId, found);
            return Task.CompletedTask;
        });
        return found;
    }

    /// <summary>
    /// REQ-SLD-21/25: hides the hint; writes the lookup's lyrics only into a blank Lyrics field of the song still shown,
    /// so text the user typed meanwhile is kept.
    /// </summary>
    private void ApplyLyricsOnOpenAnswer(int songId, string? found)
    {
        IsLyricsLoading = false;
        if (found is not null && string.IsNullOrWhiteSpace(Lyrics) && SongId == songId)
            Lyrics = found;
    }

    // ── Save orchestration (Task 4.4 BUG-005 + Task 4.5 resolution) ──────

    /// <summary>
    /// The user's Save tap (REQ-SFT-51). Every tap is a new Save attempt: both decisions start empty.
    /// The Service decides everything; this method only maps its outcome to fields and sheets.
    /// </summary>
    private async Task SaveAsync()
    {
        if (IsSongLoading) return;   // REQ-SLD-33: a Save of a form not yet filled would overwrite the stored song with blanks
        CancelLyricsLookup();   // REQ-LYR-10: Save starts; a late lookup answer must not reach the form
        if (IsBusy) return;   // BUG-049: re-entrancy guard, defense-in-depth alongside CanExecute

        await WaitForLyricsOnOpenAsync();   // REQ-SLD-31: the write never races a lookup on open
        if (IsBusy) return;   // a second tap during the wait is the same Save

        ClearSaveDecisions();
        await RunSaveAsync();
    }

    /// <summary>
    /// REQ-SLD-31: stops a lookup on open that still runs and waits for it to end. The answer is applied on the UI
    /// thread before the task ends (REQ-SLD-21), so a stored match reaches the request.
    /// </summary>
    private async Task WaitForLyricsOnOpenAsync()
    {
        CancelLyricsLookupOnOpen();
        await LyricsOnOpenTask;
    }

    /// <summary>
    /// Sends the form with the decisions held for this attempt and maps the outcome (REQ-SFT-51).
    /// Shared by the Save tap and every sheet choice, so the decisions travel with each re-invocation.
    /// </summary>
    private async Task RunSaveAsync()
    {
        if (IsBusy) return;   // BUG-049: re-entrancy guard, defense-in-depth alongside CanExecute

        // REQ-SLD-38: a frozen Title (plain text of a stored MusicBrainz song) is saved as it was loaded.
        var title = IsTitleFrozen ? _storedTitle.Trim() : (SongTitle?.Trim() ?? string.Empty);

        // Safety net: re-run the Title validator on every call, regardless of dirty state — an
        // untouched required field must still be caught.
        ApplyTitleValidation(title);

        if (TitleHasError)
            return;

        IsBusy = true;
        try
        {
            ArtistHasError = false;   // a blank or unpicked Artist is the Service's decision (REQ-SFT-21)
            ArtistErrorText = string.Empty;

            var outcome = await _songService.SaveFromFormAsync(BuildSaveRequest(title));
            await ApplyOutcomeAsync(outcome);
        }
        catch (Exception ex)
        {
            // BUG-005: never swallow save exceptions silently
            _logger.LogError(ex, "Save failed in {Mode} mode", IsEditMode ? "Edit" : "Add");
            await _snackbarService.ShowErrorAsync("Failed to save song. Please try again.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Builds the Service request from the form (REQ-SFT-18, REQ-SFT-25, REQ-SFT-28, REQ-SFT-46).
    /// A picked artist travels as <c>ArtistId</c>; otherwise the trimmed typed name travels as
    /// <c>ArtistText</c> and the Service resolves it.
    /// </summary>
    private SongFormSaveRequest BuildSaveRequest(string title)
    {
        // REQ-SLD-30: a frozen (MusicBrainz) song in edit mode sends the stored artist id, never artist text or a decision.
        var frozenEdit = IsEditMode && IsArtistFrozen;
        var artistId = frozenEdit
            ? (int?)_storedArtistId
            : SelectedArtistId is { } id && id != 0 ? id : (int?)null;

        // REQ-PIC-27 / REQ-SLD-14: the MusicBrainz identity travels as a pair, and only while HoldsRemotePick
        // (one source of truth with the frozen state of the screen).
        var holdsIdentity = HoldsRemotePick;

        // REQ-PIC-28: the MusicBrainz artist picture travels only while the Artist text is the picked name.
        var artistImage = !IsEditMode && string.Equals(ArtistSearchText, _pickedArtistName, StringComparison.Ordinal)
            ? _pendingArtistImageUrl
            : null;

        return new SongFormSaveRequest(
            SongId,
            title,
            Lyrics?.Trim(),
            artistId,
            artistId.HasValue ? null : ArtistSearchText?.Trim(),
            // REQ-SFT-27 / design F6: edit mode sends a null identity = keep the stored identity.
            holdsIdentity ? SelectedProvider : null,
            holdsIdentity ? SelectedExternalId : null,
            _pendingRawUrls.ToArray(),
            frozenEdit ? new ArtistDecision.None() : _artistDecision,
            _songDecision,
            // REQ-PIC-21: the form picture; edit mode stores it only when uploaded or removed (ImageChanged).
            ImageUrl: FormImageReference,
            // REQ-ART-16: the MusicBrainz artist picture exists in add mode only (edit mode has no MusicBrainz pick).
            ArtistImageUrl: artistImage,
            ImageChanged: IsEditMode && (_uploadedImageReference is not null || _imageRemoved),
            // REQ-SLD-26: the outcome of the held pick's lookup; the Service decides the origin from it.
            PickLyricsLookup: IsEditMode ? null : _pickLyricsLookup);
    }

    /// <summary>Maps the Service outcome to fields, sheets, the snackbar and navigation (REQ-SFT-51).</summary>
    private async Task ApplyOutcomeAsync(SongFormSaveOutcome outcome)
    {
        switch (outcome.Kind)
        {
            case SongFormSaveKind.Saved:
                CloseSaveSheets();
                await _snackbarService.ShowSuccessAsync(IsEditMode ? "Song updated" : "Song created");
                await _navigationService.GoToAsync("..");
                break;

            case SongFormSaveKind.Failed:
                await ApplyFailureAsync(outcome);
                break;

            case SongFormSaveKind.ArtistChoiceNeeded:
                IsResolutionSheetVisible = false;
                ArtistMatchCandidates = outcome.ArtistCandidates;   // the Service's order, no re-sort or re-cap
                ArtistMatchTypedName = outcome.TypedName;
                IsArtistMatchSheetVisible = true;
                break;

            case SongFormSaveKind.SongChoiceNeeded:
                IsArtistMatchSheetVisible = false;
                IsExactDuplicate = outcome.IsExactSongMatch;
                ResolutionSheetTitle = outcome.IsExactSongMatch ? ExactDuplicateSheetTitle : PossibleDuplicatesSheetTitle;
                ResolutionCandidates = outcome.SongCandidates;
                IsResolutionSheetVisible = true;
                break;
        }
    }

    private async Task ApplyFailureAsync(SongFormSaveOutcome outcome)
    {
        IsArtistMatchSheetVisible = false;
        IsResolutionSheetVisible = false;

        switch (outcome.FailedField)
        {
            case SaveField.Title:
                TitleHasError = true;
                TitleErrorText = outcome.Message;
                break;
            case SaveField.Artist:
                ArtistHasError = true;
                ArtistErrorText = outcome.Message;
                break;
            default:   // SaveField.None: a write refusal with no form field (REQ-SFT-26)
                await _snackbarService.ShowErrorAsync(outcome.Message);
                break;
        }
    }

    private void CloseSaveSheets()
    {
        IsArtistMatchSheetVisible = false;
        IsResolutionSheetVisible = false;
    }

    private void ClearSaveDecisions()
    {
        _artistDecision = new ArtistDecision.None();
        _songDecision = SongDecision.None;
    }

    // ── Resolution sheet actions ──────────────────────────────────────────

    /// <summary>
    /// User chose "Open existing" (or tapped a candidate row): closes the sheet and opens the stored
    /// song's edit form in place of this add form. Nothing typed here is written (REQ-SFT-22/24).
    /// </summary>
    private async Task OpenExistingSongAsync(SongMatch? match)
    {
        if (match is null) return;

        RunOnUiThread(() => IsResolutionSheetVisible = false);

        await _navigationService.GoToAsync($"../{Routes.SongForm}?songId={match.SongId}");
    }

    /// <summary>"Save as new song" (REQ-SFT-23, near-duplicates only, REQ-RMV-07): holds the SaveAsNewSong decision.</summary>
    private async Task ConfirmSaveAsNewSongAsync()
    {
        _songDecision = SongDecision.SaveAsNewSong;
        IsResolutionSheetVisible = false;
        await RunSaveAsync();
    }

    private void DismissResolutionSheet()
    {
        IsResolutionSheetVisible = false;
    }

    // ── Artist-match sheet actions (REQ-SFT-19) ───────────────────────────

    /// <summary>"Use '&lt;existing&gt;'": the song is saved with that artist; no resolution runs.</summary>
    private async Task UseExistingArtistAsync(ArtistMatch? match)
    {
        if (match is null) return;

        _artistDecision = new ArtistDecision.UseExisting(match.ArtistId);
        IsArtistMatchSheetVisible = false;
        await RunSaveAsync();
    }

    /// <summary>"Create '&lt;typed&gt;'": the save continues with a new artist; no resolution runs.</summary>
    private async Task CreateTypedArtistAsync()
    {
        _artistDecision = new ArtistDecision.CreateTyped();
        IsArtistMatchSheetVisible = false;
        await RunSaveAsync();
    }

    /// <summary>Back or scrim: saves nothing; the form is unchanged.</summary>
    private void DismissArtistMatchSheet() => IsArtistMatchSheetVisible = false;

    // ── Common helpers ────────────────────────────────────────────────────

    private Task CancelAsync() => Shell.Current.GoToAsync("..");

    private void UpdateCharacterCounter(int length)
    {
        ShowCharacterCounter = _songService.ShouldShowCharacterCounter(length);
        if (ShowCharacterCounter)
        {
            var (text, isWarning, isError) = _songService.GetCharacterCounterInfo(length);
            CharacterCounterText = text;
            IsCharacterCounterWarning = isWarning;
            IsCharacterCounterError = isError;
        }
    }

    public async Task RefreshApiKeyFlagAsync()
    {
        // BUG-020: SecureStorage.GetAsync can throw (e.g. corrupted Android keystore alias).
        // This is awaited from SongFormPage.OnAppearing, which is `async void` — an unhandled
        // exception here escapes to AppDomain.UnhandledException and crashes the app. Treat a
        // storage failure the same as "no key configured" instead of crashing.
        try
        {
            var key = await _secureStorage.GetAsync("youtube_api_key");
            HasYouTubeApiKey = !string.IsNullOrWhiteSpace(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read YouTube API key from secure storage");
            HasYouTubeApiKey = false;
        }
    }

    private async Task LoadKaraokeUrlsAsync(CancellationToken ct = default)
    {
        if (!SongId.HasValue) return;
        var apiKey = await _secureStorage.GetAsync("youtube_api_key");
        HasYouTubeApiKey = !string.IsNullOrWhiteSpace(apiKey);
        var songId = SongId.Value;
        // Bug fix: the database read runs on a background thread (see HydrateStoredSongAsync).
        var urls = await Task.Run(() => _karaokeUrlService.GetUrlsForSongAsync(songId, ct));
        foreach (var u in urls) _addedVideoIds.Add(u.VideoId);
        RunOnUiThread(() => KaraokeUrls.ReplaceRange(urls));
    }

    // ── YouTube URL section ───────────────────────────────────────────────

    private async Task NavigateToYouTubeSearchAsync()
    {
        _messenger.Register<YouTubeVideoPickedMessage>(this, (_, msg) =>
        {
            var rawUrl = $"https://youtu.be/{msg.Result.VideoId}";
            if (!SongId.HasValue)
            {
                // BUG-009: buffer in new-song mode
                _ = BufferUrlAsync(rawUrl, msg.Result.VideoId);
            }
            else
            {
                _ = AddUrlFromPickerAsync(rawUrl, msg.Result.VideoId);
            }
            _messenger.Unregister<YouTubeVideoPickedMessage>(this);
        });
        await Shell.Current.GoToAsync(Routes.YouTubeSearch);
    }

    private async Task AddUrlFromPickerAsync(string rawUrl, string videoId, CancellationToken ct = default)
    {
        if (IsVideoIdAdded(videoId)) return;
        var (success, message, dto) = await _karaokeUrlService.AddUrlAsync(SongId!.Value, rawUrl, ct: ct);
        if (success && dto is not null)
        {
            _addedVideoIds.Add(videoId);
            RunOnUiThread(() => KaraokeUrls.Add(dto));
            await _snackbarService.ShowSuccessAsync("URL added");
        }
        else
        {
            await _snackbarService.ShowErrorAsync(message);
        }
    }

    public bool IsVideoIdAdded(string videoId) => _addedVideoIds.Contains(videoId)
        || KaraokeUrls.Any(u => u.VideoId == videoId);

    private async Task AddFromPasteAsync(CancellationToken ct = default)
    {
        var raw = PasteUrlInput?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(raw)) return;

        if (!SongId.HasValue)
        {
            // BUG-009: buffer URL in new-song mode instead of blocking
            var videoId = _karaokeUrlService.ExtractVideoId(raw);
            if (string.IsNullOrEmpty(videoId))
            {
                PasteUrlError = "Not a valid YouTube URL.";
                HasPasteUrlError = true;
                return;
            }

            await BufferUrlAsync(raw, videoId);
            return;
        }

        var (success, message, dto) = await _karaokeUrlService.AddUrlAsync(SongId.Value, raw, ct: ct);
        if (success && dto is not null)
        {
            RunOnUiThread(() =>
            {
                KaraokeUrls.Add(dto);
                PasteUrlInput = string.Empty;
                PasteUrlError = string.Empty;
                HasPasteUrlError = false;
            });
        }
        else
        {
            PasteUrlError = message;
            HasPasteUrlError = true;
        }
    }

    /// <summary>BUG-009: holds a URL in memory until the song is saved.</summary>
    private Task BufferUrlAsync(string rawUrl, string videoId)
    {
        if (_pendingRawUrls.Contains(rawUrl) || _addedVideoIds.Contains(videoId))
        {
            PasteUrlError = "This URL is already added.";
            HasPasteUrlError = true;
            return Task.CompletedTask;
        }

        _pendingRawUrls.Add(rawUrl);
        _addedVideoIds.Add(videoId);

        // Synthesise a display DTO (zero play count, not suggested — purely visual)
        var pending = new SongKaraokeUrlDto(videoId, 0, 0, null, null, DateTime.UtcNow, null, false);
        RunOnUiThread(() =>
        {
            KaraokeUrls.Add(pending);
            PasteUrlInput = string.Empty;
            PasteUrlError = string.Empty;
            HasPasteUrlError = false;
        });

        return Task.CompletedTask;
    }

    private async Task RemoveUrlAsync(SongKaraokeUrlDto dto, CancellationToken ct = default)
    {
        if (dto is null) return;

        if (!SongId.HasValue)
        {
            // BUG-009: new-song mode — remove from pending buffer, no DB call, no undo
            _pendingRawUrls.RemoveAll(u =>
            {
                var vid = _karaokeUrlService.ExtractVideoId(u);
                return vid == dto.VideoId;
            });
            _addedVideoIds.Remove(dto.VideoId);
            RunOnUiThread(() => KaraokeUrls.Remove(dto));
            return;
        }

        var songId = SongId.Value;

        // Commit-first: delete from DB immediately so undo can re-insert cleanly
        var (success, message) = await _karaokeUrlService.RemoveUrlAsync(songId, dto.VideoId, ct);
        if (!success)
        {
            await _snackbarService.ShowErrorAsync(message);
            return;
        }

        _addedVideoIds.Remove(dto.VideoId);
        RunOnUiThread(() => KaraokeUrls.Remove(dto));

        // Show snackbar; if UNDO tapped, re-insert via AddUrlAsync
        await _snackbarService.ShowWithUndoAsync("URL removed", "UNDO", async () =>
        {
            var rawUrl = $"https://youtu.be/{dto.VideoId}";
            var (reAddSuccess, _, reAdded) = await _karaokeUrlService.AddUrlAsync(songId, rawUrl);
            if (reAddSuccess && reAdded is not null)
            {
                _addedVideoIds.Add(dto.VideoId);
                RunOnUiThread(() => KaraokeUrls.Add(reAdded));
            }
        });
    }

    [RelayCommand]
    private async Task LaunchYouTubeSearch()
    {
        var title = SongTitle?.Trim() ?? string.Empty;
        var artist = ArtistName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(artist))
            return;

        var query = $"karaoke {title} {artist}";
        var encodedQuery = Uri.EscapeDataString(query);
        var youtubeUri = new Uri($"https://www.youtube.com/results?search_query={encodedQuery}");

        if (!await Launcher.TryOpenAsync(youtubeUri))
            await Browser.OpenAsync(youtubeUri, BrowserLaunchMode.SystemPreferred);
    }

    private void UpdateCanLaunchYouTubeSearch()
    {
        CanLaunchYouTubeSearch = !string.IsNullOrWhiteSpace(SongTitle)
                             && !string.IsNullOrWhiteSpace(ArtistName);
    }
}
