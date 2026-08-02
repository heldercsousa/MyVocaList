using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.Messages;
using MyVocaList.Domain.ReadModels;
using MyVocaList.Domain.Resolution;
using MyVocaList.UI.Collections;
using CanonicalSongPickedMessage = MyVocaList.Contracts.Messages.SongPickedMessage;

namespace MyVocaList.UI.ViewModels;

[QueryProperty(nameof(SongIdRaw), "songId")]
[QueryProperty(nameof(ArtistIdRaw), "artistId")]
[QueryProperty(nameof(ArtistName), "artistName")]
[QueryProperty(nameof(SongTitle), "songTitle")]
public partial class SongFormViewModel : ViewModelBase
{
    private readonly IArtistService _artistService;
    private readonly ISongService _songService;
    private readonly ISongResolutionService _songResolution;
    private readonly ISnackbarComponent _snackbarService;
    private readonly ILogger<SongFormViewModel> _logger;
    private readonly ISongKaraokeUrlService _karaokeUrlService;
    private readonly ISecureStorageWrapper _secureStorage;
    private readonly IMessenger _messenger;
    private int _searchGeneration; // BUG-051: generation counter — guards SearchArtistsAsync against stale completions

    // ── Query properties ──────────────────────────────────────────────────
    public string SongIdRaw { set => SongId = int.TryParse(value, out var id) ? id : null; }
    public string ArtistIdRaw { set => ArtistId = int.TryParse(value, out var id) ? id : 0; }

    [ObservableProperty] private int? _songId;
    [ObservableProperty] private int _artistId;
    [ObservableProperty] private string _artistName = string.Empty;
    [ObservableProperty] private string _songTitle = string.Empty;
    [ObservableProperty] private string _featuredArtists = string.Empty;
    [ObservableProperty] private bool _titleHasError;
    [ObservableProperty] private string _titleErrorText = string.Empty;
    [ObservableProperty] private string _characterCounterText = string.Empty;
    [ObservableProperty] private bool _showCharacterCounter;
    [ObservableProperty] private bool _isCharacterCounterWarning;
    [ObservableProperty] private bool _isCharacterCounterError;
    [ObservableProperty] private bool _isBusy;

    // ── Version field (Task 4.5) ──────────────────────────────────────────
    [ObservableProperty] private string _songVersion = string.Empty;
    [ObservableProperty] private bool _versionHasError;
    [ObservableProperty] private string _versionErrorText = string.Empty;

    // ── Artist autocomplete ───────────────────────────────────────────────
    [ObservableProperty] private string _artistSearchText = string.Empty;
    [ObservableProperty] private int? _selectedArtistId;
    [ObservableProperty] private string? _selectedArtistName;
    [ObservableProperty] private bool _isArtistLocked;
    [ObservableProperty] private IEnumerable<AutocompleteSuggestion> _artistSuggestions = [];
    [ObservableProperty] private bool _artistHasError;
    [ObservableProperty] private string _artistErrorText = string.Empty;
    // BUG-065/066: two-way bound to AutoCompleteEdit.IsDropDownOpen (dxe:AutoCompleteEdit, ItemsEditBase
    // BindableProperty). Every programmatic ArtistSearchText assignment below also sets this false to
    // force the popup shut (DevExpress's own provider uses the same lever) — replaces the removed
    // _suppressNextArtistSearch flag, which IL evidence proved never suppressed anything: DevExpress's
    // AsyncItemsSourceProvider.OnEditorTextChanged already ignores non-UserInput text changes natively.
    [ObservableProperty] private bool _isArtistDropDownOpen;

    // ── Lyrics ────────────────────────────────────────────────────────────
    [ObservableProperty] private string? _lyrics;

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

    // ── Resolution sheet state (Task 4.5) ────────────────────────────────
    [ObservableProperty] private bool _isResolutionSheetVisible;
    [ObservableProperty] private string _resolutionSheetTitle = "This looks like an existing song";
    [ObservableProperty] private IReadOnlyList<SongMatch> _resolutionCandidates = [];
    [ObservableProperty] private int? _selectedResolutionTargetId;
    [ObservableProperty] private bool _isSaveAsNewVersionMode;
    [ObservableProperty] private bool _versionEntryRequired; // reveals version entry when "Save as new version"

    // ── Merge sheet state (Task 4.5) ──────────────────────────────────────
    [ObservableProperty] private bool _isMergeSheetVisible;
    [ObservableProperty] private IReadOnlyList<MergeFieldRow> _mergeFieldRows = [];

    // ── Stashed resolution context (survives between sheet steps) ─────────
    private SongCandidate? _pendingCandidate;
    private SongResolution? _pendingResolution;

    // Becomes true once the user edits each field. Guards blur validation so a pristine field the
    // user only tabbed through is not flagged prematurely (Form Validation Standard).
    private bool _titleDirty;
    private bool _versionDirty;

    // True until the page's OnAppearing calls CompleteHydration(). While true, Shell QueryProperty
    // pre-population (edit mode) must NOT mark any field dirty — otherwise a pre-filled invalid
    // value would flash an error on the very first blur, before the user has touched anything
    // (edit-mode dirty-guard, Form Validation Standard).
    private bool _isHydrating = true;

    public bool IsEditMode => SongId.HasValue;
    public string PageTitle => IsEditMode ? "Edit Song" : "New Song";

    public SongFormViewModel(
        IArtistService artistService,
        ISongService songService,
        ISongResolutionService songResolution,
        ISnackbarComponent snackbarService,
        ILogger<SongFormViewModel> logger,
        ISongKaraokeUrlService karaokeUrlService,
        ISecureStorageWrapper secureStorage,
        IMessenger messenger)
    {
        _artistService = artistService;
        _songService = songService;
        _songResolution = songResolution;
        _snackbarService = snackbarService;
        _logger = logger;
        _karaokeUrlService = karaokeUrlService;
        _secureStorage = secureStorage;
        _messenger = messenger;

        // BUG-049: explicit CanExecute tied to IsBusy — combined with the early-return guard in
        // SaveAsync — prevents a fast double-tap from firing SaveAsync twice concurrently, which
        // caused a duplicate "GoToAsync("..")" that overshot the nav stack root.
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
        CancelCommand = new AsyncRelayCommand(CancelAsync);
        SearchArtistsCommand = new AsyncRelayCommand<string>(SearchArtistsAsync);
        SelectArtistCommand = new RelayCommand<AutocompleteSuggestion>(SelectArtist);
        CreateArtistInlineCommand = new AsyncRelayCommand<string>(CreateArtistInlineAsync);
        ArtistBlurredWithoutSelectionCommand = new RelayCommand(OnArtistBlurredWithoutSelection);
        NavigateToSongPickerCommand = new AsyncRelayCommand(NavigateToSongPickerAsync,
            AsyncRelayCommandOptions.None);
        NavigateToYouTubeSearchCommand = new AsyncRelayCommand(NavigateToYouTubeSearchAsync,
            AsyncRelayCommandOptions.None);
        AddFromPasteCommand = new AsyncRelayCommand(AddFromPasteAsync);
        RemoveUrlCommand = new AsyncRelayCommand<SongKaraokeUrlDto>(RemoveUrlAsync);
        GoToSettingsCommand = new AsyncRelayCommand(async () => await Shell.Current.GoToAsync("//settings"));

        // Resolution sheet commands
        SelectResolutionCandidateCommand = new AsyncRelayCommand<SongMatch>(SelectResolutionCandidateAsync);
        ConfirmUpdateExistingCommand = new AsyncRelayCommand(ConfirmUpdateExistingAsync);
        ConfirmSaveAsNewVersionCommand = new AsyncRelayCommand(ConfirmSaveAsNewVersionAsync);
        DismissResolutionSheetCommand = new RelayCommand(DismissResolutionSheet);

        // Merge sheet commands
        ConfirmMergeCommand = new AsyncRelayCommand(ConfirmMergeAsync);
        DismissMergeSheetCommand = new RelayCommand(DismissMergeSheet);

        // Register for the canonical SongPickedMessage (from SongPickerViewModel)
        // Uses alias to avoid ambiguity with legacy QueueSongPickerViewModel.SongPickedMessage
        _messenger.Register<CanonicalSongPickedMessage>(this, (_, msg) => OnSongPicked(msg));
    }

    // ── Commands ──────────────────────────────────────────────────────────
    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand CancelCommand { get; }
    public IAsyncRelayCommand<string> SearchArtistsCommand { get; }
    public IRelayCommand<AutocompleteSuggestion> SelectArtistCommand { get; }
    /// <summary>REQ-ACREATE-04/05: creates a new artist inline from the typed name and locks it in on success.</summary>
    public IAsyncRelayCommand<string> CreateArtistInlineCommand { get; }
    /// <summary>Invoked by DevExpress AutoCompleteEdit when user blurs without selecting a suggestion (BUG-008).</summary>
    public IRelayCommand ArtistBlurredWithoutSelectionCommand { get; }
    public IAsyncRelayCommand NavigateToSongPickerCommand { get; }
    public IAsyncRelayCommand NavigateToYouTubeSearchCommand { get; }
    public IAsyncRelayCommand AddFromPasteCommand { get; }
    public IAsyncRelayCommand<SongKaraokeUrlDto> RemoveUrlCommand { get; }
    public IAsyncRelayCommand GoToSettingsCommand { get; }

    // Resolution sheet
    public IAsyncRelayCommand<SongMatch> SelectResolutionCandidateCommand { get; }
    public IAsyncRelayCommand ConfirmUpdateExistingCommand { get; }
    public IAsyncRelayCommand ConfirmSaveAsNewVersionCommand { get; }
    public IRelayCommand DismissResolutionSheetCommand { get; }

    // Merge sheet
    public IAsyncRelayCommand ConfirmMergeCommand { get; }
    public IRelayCommand DismissMergeSheetCommand { get; }

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

    /// <summary>Blur handler for the version field — see <see cref="ValidateTitle"/>.</summary>
    [RelayCommand]
    private void ValidateVersion()
    {
        if (!_versionDirty)
            return;

        ApplyVersionValidation(SongVersion);
    }

    private void ApplyTitleValidation(string value)
    {
        var (isValid, message) = _songService.ValidateTitleInput(value ?? string.Empty);
        TitleHasError = !isValid;
        TitleErrorText = isValid ? string.Empty : message;
    }

    private void ApplyVersionValidation(string value)
    {
        var (isValid, message) = _songService.ValidateVersionInput(value);
        VersionHasError = !isValid;
        VersionErrorText = isValid ? string.Empty : message;
    }

    // ── QueryProperty change hooks ────────────────────────────────────────

    partial void OnSongIdChanged(int? value)
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));
        if (value.HasValue)
            _ = LoadSongForEditAsync(value.Value);
    }

    partial void OnSongTitleChanged(string value)
    {
        // Counter counts the trimmed string — the same string ValidateTitleInput checks —
        // so trailing whitespace cannot inflate the count (counter threshold alignment).
        UpdateCharacterCounter(value?.Trim().Length ?? 0);
        UpdateCanLaunchYouTubeSearch();

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

    partial void OnSongVersionChanged(string value)
    {
        if (_isHydrating)
            return;

        _versionDirty = true;

        if (!VersionHasError)
            return;

        ApplyVersionValidation(value);
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
            .Select(a => new AutocompleteSuggestion(a.Name, a.CatalogCountText, a))
            .ToList();
        // Only the latest query updates the shared observable state; the provider for THIS query
        // still receives its own results below (DX cancels stale provider requests via its token).
        if (gen == _searchGeneration)
            RunOnUiThread(() => ArtistSuggestions = mapped);
        return mapped;
    }

    private void SelectArtist(AutocompleteSuggestion suggestion)
    {
        if (suggestion?.Data is not ArtistListItem artist) return;
        LockArtist(artist.Id, artist.Name);
    }

    /// <summary>
    /// REQ-ACREATE-04/12: single lock path shared by an existing-suggestion pick (BUG-050) and the
    /// inline-create success path. Sets the selected+locked artist, mirrors the name into the search
    /// text, clears suggestions, and clears any pending artist error.
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
    /// REQ-ACREATE-04/05: inline "create new artist" from the Song form. Delegates creation and
    /// validation entirely to <see cref="IArtistService.CreateArtistAsync(string, string?, string?, CancellationToken)"/>
    /// (business logic stays in the Service). On success the created artist becomes the selected+locked
    /// artist via the shared <see cref="LockArtist"/> path; on failure the service message is surfaced
    /// through <see cref="ArtistHasError"/>/<see cref="ArtistErrorText"/> and the typed text is retained.
    /// </summary>
    private async Task CreateArtistInlineAsync(string name)
    {
        var (success, message, artist) = await _artistService.CreateArtistAsync(name);
        if (success && artist is not null)
        {
            LockArtist(artist.Id, artist.Name);
        }
        else
        {
            ArtistHasError = true;
            ArtistErrorText = message;
            ArtistSuggestions = []; // M3: clear stale suggestions so the dropdown doesn't linger behind the error
            // REQ-ACREATE-05: retain ArtistSearchText — no clear, no lock, no dialog.
        }
    }

    /// <summary>
    /// REQ-ACREATE-03: blur-retain rule. If no artist is locked in, keep the typed text
    /// (never clear it) and surface a validation error so the user can retry or use inline-create.
    /// If one was previously selected, restore the name so the field stays consistent.
    /// </summary>
    private void OnArtistBlurredWithoutSelection()
    {
        if (!SelectedArtistId.HasValue || SelectedArtistId.Value == 0)
        {
            ArtistSuggestions = [];
            // REQ-ACREATE-03: empty text is not an "unmatched" state — only flag an error
            // when the user actually typed something that didn't resolve to a selection.
            if (!string.IsNullOrWhiteSpace(ArtistSearchText))
            {
                ArtistHasError = true;
                ArtistErrorText = "Search and select an artist from the list";
            }
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
        SelectedArtistId = null;
        SelectedArtistName = null;
        ArtistSearchText = string.Empty;
        IsArtistDropDownOpen = false; // BUG-065/066: dismiss the popup for this programmatic clear
        IsArtistLocked = false;
        ArtistSuggestions = [];
        ArtistHasError = false;
        ArtistErrorText = string.Empty;
    }

    /// <summary>
    /// BUG-008: called from OnAppearing after all QueryProperties are set.
    /// Initialises artist field reliably regardless of query-property arrival order.
    /// </summary>
    public void InitializeArtistField()
    {
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
    /// <c>UpdateSongAsync</c> persists (Title, Version, FeaturedArtists, Lyrics, external identity)
    /// so Save cannot silently wipe stored data. Also loads karaoke URLs and determines
    /// IsArtistLocked (API-imported song: ExternalId set AND no manual edits).
    /// </summary>
    private async Task LoadSongForEditAsync(int songId)
    {
        await LoadKaraokeUrlsAsync();

        var song = await _songService.GetSongByIdAsync(songId);
        if (song is null)
        {
            _logger.LogWarning("Edit mode: song {SongId} not found during hydration", songId);
            return;
        }

        // Stash external identity so ExecuteEditSaveAsync round-trips it unchanged.
        SelectedExternalId = song.ExternalId ?? string.Empty;
        SelectedProvider = song.ExternalProvider ?? string.Empty;

        RunOnUiThread(() =>
        {
            // This may run after the page's OnAppearing already called CompleteHydration()
            // (the entity load is async) — re-open the hydration window while applying stored
            // values so pre-population does not mark fields dirty (edit-mode dirty-guard).
            var wasHydrating = _isHydrating;
            _isHydrating = true;
            try
            {
                SongTitle = song.Title ?? string.Empty;
                SongVersion = song.Version ?? string.Empty;
                FeaturedArtists = song.FeaturedArtists ?? string.Empty;
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

                IsArtistLocked = true; // edit-mode artist is locked (REQ-ACREATE-14); user cannot change it inline
            }
            finally
            {
                _isHydrating = wasHydrating;
            }
        });
    }

    // ── SongPickedMessage handler (canonical, from SongPickerViewModel) ───

    private void OnSongPicked(CanonicalSongPickedMessage msg)
    {
        var result = msg.Result;

        RunOnUiThread(() =>
        {
            SongTitle = result.SongTitle ?? string.Empty;
            FeaturedArtists = result.FeaturedArtists ?? string.Empty;

            // Stash external identity so SaveAsync can persist it
            SelectedExternalId = result.ExternalId ?? string.Empty;
            SelectedProvider = result.Provider ?? string.Empty;
        });

        // Resolve the artist: if exact match found, lock it in; else leave for user
        _ = ResolveAndLockArtistAsync(result.ArtistName, result.ExternalId, result.Provider);
    }

    private async Task ResolveAndLockArtistAsync(string? artistName, string? externalId, string? provider)
    {
        if (string.IsNullOrWhiteSpace(artistName)) return;

        try
        {
            var results = await _artistService.SearchArtistsByNameAsync(artistName, maxResults: 1);
            var match = results.FirstOrDefault(a =>
                string.Equals(a.Name, artistName, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                RunOnUiThread(() =>
                {
                    SelectedArtistId = match.Id;
                    SelectedArtistName = match.Name;
                    ArtistSearchText = match.Name;
                    IsArtistDropDownOpen = false; // BUG-065/066: dismiss the popup for this programmatic lock
                    ArtistSuggestions = [];
                    ArtistHasError = false;
                    IsArtistLocked = true; // API import: lock artist
                });
            }
            else
            {
                RunOnUiThread(() =>
                {
                    // No exact match — pre-fill text so user can confirm or adjust
                    ArtistSearchText = artistName;
                    IsArtistDropDownOpen = false; // BUG-065/066: dismiss the popup for this programmatic prefill
                    SelectedArtistId = null;
                    IsArtistLocked = false;
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve artist '{ArtistName}' from picker result", artistName);
        }
    }

    // ── Save orchestration (Task 4.4 BUG-005 + Task 4.5 resolution) ──────

    private async Task SaveAsync()
    {
        if (IsBusy) return;   // BUG-049: re-entrancy guard, defense-in-depth alongside CanExecute

        var title = SongTitle?.Trim() ?? string.Empty;
        var version = SongVersion?.Trim() ?? string.Empty;

        // Safety net: re-run ALL field validators on Save, regardless of dirty state — an
        // untouched required field (or one whose error was never surfaced) must still be caught.
        // This also clears any stale Version error left over from the "Save as new version" flow.
        ApplyTitleValidation(title);
        ApplyVersionValidation(version);

        if (TitleHasError || VersionHasError)
            return;

        if (!SelectedArtistId.HasValue || SelectedArtistId.Value == 0)
        {
            ArtistHasError = true;
            ArtistErrorText = string.IsNullOrWhiteSpace(ArtistSearchText)
                ? "Artist is required"
                : "Search and select an artist from the list";
            return;
        }

        IsBusy = true;
        try
        {
            if (IsEditMode)
            {
                await ExecuteEditSaveAsync(title, version);
            }
            else
            {
                await ExecuteNewSongSaveAsync(title, version);
            }
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

    private async Task ExecuteEditSaveAsync(string title, string version)
    {
        // BUG-024: send the complete current form data — FeaturedArtists/Lyrics are hydrated by
        // LoadSongForEditAsync, and the (possibly edited) Version is passed through explicitly.
        // BUG-067 (REQ-ACREATE-16): the currently selected artist is part of "the complete current
        // form data" — it must be sent too, or a changed artist is silently discarded. The
        // artist-required guard above has already proven SelectedArtistId has a non-zero value.
        var (success, message) = await _songService.UpdateSongAsync(
            SongId!.Value, title, FeaturedArtists?.Trim(), Lyrics?.Trim(), true,
            string.IsNullOrEmpty(SelectedExternalId) ? null : SelectedExternalId,
            string.IsNullOrEmpty(SelectedProvider) ? null : SelectedProvider,
            version,
            SelectedArtistId);
        if (success)
        {
            await _snackbarService.ShowSuccessAsync("Song updated");
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            TitleHasError = true;
            TitleErrorText = message;
        }
    }

    private async Task ExecuteNewSongSaveAsync(string title, string version)
    {
        // Build candidate for resolution engine
        var candidate = new SongCandidate(
            title,
            version,
            FeaturedArtists?.Trim(),
            Lyrics?.Trim(),
            new ArtistCandidate(
                SelectedArtistName ?? string.Empty,
                string.IsNullOrEmpty(SelectedProvider) ? null : SelectedProvider,
                string.IsNullOrEmpty(SelectedExternalId) ? null : SelectedExternalId),
            string.IsNullOrEmpty(SelectedProvider) ? null : SelectedProvider,
            string.IsNullOrEmpty(SelectedExternalId) ? null : SelectedExternalId);

        var resolution = await _songResolution.ResolveAsync(candidate);

        if (resolution.Kind == ResolutionKind.NoMatch)
        {
            // Direct create — no duplicate concern
            await CommitNewSongAsync(candidate, version);
        }
        else
        {
            // Store context and show resolution sheet
            _pendingCandidate = candidate;
            _pendingResolution = resolution;

            var candidates = resolution.Kind == ResolutionKind.FuzzyCandidates
                ? resolution.FuzzyCandidates
                : resolution.ExactMatchSongId.HasValue
                    ? [new SongMatch(resolution.ExactMatchSongId.Value, title, version, SelectedArtistName ?? string.Empty, 1.0)]
                    : (IReadOnlyList<SongMatch>)[];

            RunOnUiThread(() =>
            {
                ResolutionCandidates = candidates;
                IsSaveAsNewVersionMode = false;
                VersionEntryRequired = false;
                IsResolutionSheetVisible = true;
            });
        }
    }

    private async Task CommitNewSongAsync(SongCandidate candidate, string version)
    {
        var (success, message, _) = await _songService.CreateSongWithUrlsAsync(
            SelectedArtistId!.Value,
            candidate.Title,
            version,
            candidate.FeaturedArtists,
            candidate.Lyrics,
            candidate.ExternalId,
            candidate.ExternalProvider,
            _pendingRawUrls);

        if (success)
        {
            await _snackbarService.ShowSuccessAsync("Song created");
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            TitleHasError = true;
            TitleErrorText = message;
        }
    }

    // ── Resolution sheet actions ──────────────────────────────────────────

    /// <summary>Tapping a candidate row selects it and immediately triggers the update flow.</summary>
    private async Task SelectResolutionCandidateAsync(SongMatch match)
    {
        if (match is null) return;
        SelectedResolutionTargetId = match.SongId;
        await ConfirmUpdateExistingAsync();
    }

    /// <summary>User chose "Update existing" — target is SelectedResolutionTargetId (or ExactMatchSongId).</summary>
    private async Task ConfirmUpdateExistingAsync()
    {
        if (_pendingCandidate is null || _pendingResolution is null) return;

        var targetId = SelectedResolutionTargetId
            ?? _pendingResolution.ExactMatchSongId;

        if (!targetId.HasValue) return;

        RunOnUiThread(() => IsResolutionSheetVisible = false);

        IsBusy = true;
        try
        {
            if (_pendingResolution.TargetHasManualEdits && _pendingResolution.FieldDiffs.Count > 0)
            {
                // Show merge sheet
                var rows = _pendingResolution.FieldDiffs
                    .Select(f => new MergeFieldRow(f.Field, f.CurrentValue, f.ApiValue))
                    .ToList();

                RunOnUiThread(() =>
                {
                    MergeFieldRows = rows;
                    _selectedMergeTargetId = targetId.Value;
                    IsMergeSheetVisible = true;
                });
            }
            else
            {
                // No manual edits — overwrite directly
                var (success, message, _) = await _songResolution.CommitAsync(
                    _pendingCandidate, ResolutionChoice.UpdateExisting, targetId, null);

                await HandleCommitResultAsync(success, message, "Song updated");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit update-existing resolution");
            await _snackbarService.ShowErrorAsync("Failed to update song. Please try again.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>User chose "Save as new version" — requires a non-empty Version (AC-1.2).</summary>
    private async Task ConfirmSaveAsNewVersionAsync()
    {
        var version = SongVersion?.Trim() ?? string.Empty;

        // AC-1.2: cannot create two rows with identical (ArtistId, Title, "")
        // Version validation runs before candidate check so the UI error is always surfaced.
        var (isValid, message) = _songService.ValidateVersionInput(version, isRequired: true);
        if (!isValid)
        {
            VersionHasError = true;
            VersionErrorText = message;
            VersionEntryRequired = true;
            return;
        }

        if (_pendingCandidate is null) return;

        RunOnUiThread(() => IsResolutionSheetVisible = false);

        IsBusy = true;
        try
        {
            await CommitNewSongAsync(_pendingCandidate, version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save as new version");
            await _snackbarService.ShowErrorAsync("Failed to save song. Please try again.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void DismissResolutionSheet()
    {
        IsResolutionSheetVisible = false;
        _pendingCandidate = null;
        _pendingResolution = null;
        SelectedResolutionTargetId = null;
        IsSaveAsNewVersionMode = false;
        VersionEntryRequired = false;
    }

    // ── Merge sheet actions ───────────────────────────────────────────────

    private int _selectedMergeTargetId;

    private async Task ConfirmMergeAsync()
    {
        if (_pendingCandidate is null) return;

        var acceptedFields = MergeFieldRows
            .Where(r => r.AcceptApiValue)
            .Select(r => r.Field)
            .ToList();

        RunOnUiThread(() => IsMergeSheetVisible = false);

        IsBusy = true;
        try
        {
            var (success, message, _) = await _songResolution.CommitAsync(
                _pendingCandidate, ResolutionChoice.UpdateExisting, _selectedMergeTargetId,
                acceptedFields.Count > 0 ? acceptedFields : null);

            await HandleCommitResultAsync(success, message, "Song updated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit merge resolution");
            await _snackbarService.ShowErrorAsync("Failed to update song. Please try again.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void DismissMergeSheet()
    {
        IsMergeSheetVisible = false;
        _pendingCandidate = null;
        _pendingResolution = null;
        MergeFieldRows = [];
    }

    private async Task HandleCommitResultAsync(bool success, string message, string successText)
    {
        if (success)
        {
            await _snackbarService.ShowSuccessAsync(successText);
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            TitleHasError = true;
            TitleErrorText = message;
        }
    }

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
        var urls = await _karaokeUrlService.GetUrlsForSongAsync(SongId.Value, ct);
        foreach (var u in urls) _addedVideoIds.Add(u.VideoId);
        RunOnUiThread(() => KaraokeUrls.ReplaceRange(urls));
    }

    // ── YouTube URL section ───────────────────────────────────────────────

    private async Task NavigateToSongPickerAsync()
    {
        await Shell.Current.GoToAsync(Routes.SongPicker);
    }

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

/// <summary>
/// Represents one row in the Merge BottomSheet — a per-field diff between the API candidate value
/// and the current local value. The user can toggle <see cref="AcceptApiValue"/> per row.
/// Mergeable fields: Title, FeaturedArtists, Lyrics, Version (ArtistId is never mergeable).
/// </summary>
public sealed class MergeFieldRow : ObservableObject
{
    public string Field { get; }
    public string? CurrentValue { get; }
    public string? ApiValue { get; }

    private bool _acceptApiValue;
    public bool AcceptApiValue
    {
        get => _acceptApiValue;
        set => SetProperty(ref _acceptApiValue, value);
    }

    public MergeFieldRow(string field, string? currentValue, string? apiValue)
    {
        Field = field;
        CurrentValue = currentValue;
        ApiValue = apiValue;
    }
}
