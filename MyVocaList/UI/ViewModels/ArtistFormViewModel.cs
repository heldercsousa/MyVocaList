using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs.Suggestions;
using MyVocaList.Contracts.Models;
using MyVocaList.Domain.Constants;
using MyVocaList.Domain.ReadModels;
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
        IArtistSuggestionService artistSuggestions)
    {
        _artistService = artistService;
        _snackbarService = snackbarService;
        _logger = logger;
        _messenger = messenger;
        _navigationService = navigationService;
        _artistSuggestions = artistSuggestions;

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
                var (success, message) = await _artistService.UpdateArtistAsync(ArtistId!.Value, name);
                if (success)
                {
                    await _snackbarService.ShowSuccessAsync("Artist updated");
                    await (Shell.Current?.GoToAsync("..") ?? Task.CompletedTask);
                }
                else
                {
                    ApplyAsyncFailure(message);
                }
            }
            else
            {
                var (success, message, _) = await _artistService.CreateArtistAsync(name);
                if (success)
                {
                    await _snackbarService.ShowSuccessAsync("Artist created");
                    await (Shell.Current?.GoToAsync("..") ?? Task.CompletedTask);
                }
                else
                {
                    ApplyAsyncFailure(message);
                }
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

        return local.Select(dto => new AutocompleteSuggestion(dto.Name, "In your library", dto))
                    .Concat(remote.Remote.Select(dto => new AutocompleteSuggestion(dto.Name, "Deezer", dto)))
                    .ToList();
    }

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
            ArtistName = dto.Name;
            IsNameLocked = true;
            return Task.CompletedTask;
        }

        return _navigationService.GoToAsync(
            $"../{Routes.ArtistForm}?artistId={dto.LocalId.Value}&artistName={Uri.EscapeDataString(dto.Name)}");
    }

    /// <summary>X on Name: empties and unlocks it, drops the note (REQ-SFT-06/49).</summary>
    [RelayCommand]
    private void ClearName()
    {
        ArtistName = string.Empty;
        IsNameLocked = false;
        NameSupportingText = string.Empty;
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
