using MyVocaList.Domain.Entity;

namespace MyVocaList.UI.ViewModels;

/// <summary>
/// ViewModel for the Add / Edit Singer form page.
/// PersonId null = create mode; PersonId set = edit mode.
/// </summary>
[QueryProperty(nameof(PersonIdRaw), "personId")]
[QueryProperty(nameof(PersonName), "personName")]
[QueryProperty(nameof(PersonBirthday), "personBirthday")]
[QueryProperty(nameof(PersonEmail), "personEmail")]
public partial class PersonFormViewModel : ViewModelBase
{
    private readonly IPersonService _personService;
    private readonly ISnackbarComponent _snackbarService;
    private readonly INavigationService _navigation;
    private readonly ILogger<PersonFormViewModel> _logger;

    // Shell passes all query parameters as strings; parse manually.
    public string PersonIdRaw { set => PersonId = int.TryParse(value, out var id) ? id : null; }

    [ObservableProperty] private int? _personId;
    [ObservableProperty] private string _personName = string.Empty;
    [ObservableProperty] private string _personBirthday = string.Empty;
    [ObservableProperty] private string _personEmail = string.Empty;

    [ObservableProperty] private bool _nameHasError;
    [ObservableProperty] private string _nameErrorText = string.Empty;
    [ObservableProperty] private bool _birthdayHasError;
    [ObservableProperty] private string _birthdayErrorText = string.Empty;
    [ObservableProperty] private bool _emailHasError;
    [ObservableProperty] private string _emailErrorText = string.Empty;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private IEnumerable<AutocompleteSuggestion> _suggestions = [];

    // Character counter
    [ObservableProperty] private string _characterCounterText = string.Empty;
    [ObservableProperty] private bool _showCharacterCounter;
    [ObservableProperty] private bool _isCharacterCounterWarning;
    [ObservableProperty] private bool _isCharacterCounterError;

    // Becomes true once the user edits each field. Guards blur validation so a pristine field the
    // user only tabbed through is not flagged prematurely (Form Validation Standard).
    private bool _nameDirty;
    private bool _birthdayDirty;
    private bool _emailDirty;

    // True until the page's OnAppearing calls CompleteHydration(). While true, Shell QueryProperty
    // pre-population (edit mode) must NOT mark any field dirty — otherwise a pre-filled invalid
    // value would flash an error on the very first blur, before the user has touched anything
    // (edit-mode dirty-guard, Form Validation Standard).
    private bool _isHydrating = true;

    public bool IsEditMode => PersonId.HasValue;
    public string PageTitle => IsEditMode ? "Edit Singer" : "New Singer";

    public PersonFormViewModel(
        IPersonService personService,
        ISnackbarComponent snackbarService,
        INavigationService navigation,
        ILogger<PersonFormViewModel> logger)
    {
        _personService = personService;
        _snackbarService = snackbarService;
        _navigation = navigation;
        _logger = logger;

        // BUG-049: explicit CanExecute tied to IsBusy — combined with the early-return guard in
        // SaveAsync — prevents a fast double-tap from firing SaveAsync twice concurrently, which
        // caused a duplicate "GoToAsync("..")" that overshot the nav stack root.
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy);
        CancelCommand = new AsyncRelayCommand(CancelAsync);
        SearchPersonsCommand = new AsyncRelayCommand<string>(SearchPersonsAsync);
        SuggestionSelectedCommand = new AsyncRelayCommand<AutocompleteSuggestion>(SuggestionSelectedAsync);
    }

    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand CancelCommand { get; }
    public IAsyncRelayCommand<string> SearchPersonsCommand { get; }
    public IAsyncRelayCommand<AutocompleteSuggestion> SuggestionSelectedCommand { get; }

    partial void OnPersonIdChanged(int? value)
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));
    }

    partial void OnIsBusyChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();

    /// <summary>
    /// Called by the page's <c>OnAppearing</c> once Shell has finished applying all
    /// <c>[QueryProperty]</c> values for this navigation. Ends the hydration window so that
    /// subsequent field edits are tracked as dirty (edit-mode dirty-guard).
    /// </summary>
    public void CompleteHydration() => _isHydrating = false;

    /// <summary>
    /// Blur handler (invoked from the page's <c>Unfocused</c>/<c>BlurredWithoutSelectionCommand</c>).
    /// Validates the name field only when it is dirty — a pristine field the user only tabbed
    /// through is left untouched.
    /// </summary>
    [RelayCommand]
    private void ValidateName()
    {
        if (!_nameDirty)
            return;

        ApplyNameValidation(PersonName);
    }

    /// <summary>Blur handler for the birthday field — see <see cref="ValidateName"/>.</summary>
    [RelayCommand]
    private void ValidateBirthday()
    {
        if (!_birthdayDirty)
            return;

        ApplyBirthdayValidation(PersonBirthday);
    }

    /// <summary>Blur handler for the email field — see <see cref="ValidateName"/>.</summary>
    [RelayCommand]
    private void ValidateEmail()
    {
        if (!_emailDirty)
            return;

        ApplyEmailValidation(PersonEmail);
    }

    partial void OnPersonNameChanged(string value)
    {
        // Counter counts the trimmed string — the same string ValidateNameInput checks —
        // so trailing whitespace cannot inflate the count (counter threshold alignment).
        UpdateCharacterCounter(value?.Trim().Length ?? 0);

        if (_isHydrating)
            return;   // edit-mode pre-population — do not mark dirty

        _nameDirty = true;

        // "Reward early": once the field is in error, re-validate on every keystroke so the error
        // clears the instant it becomes valid. Do NOT validate on keystroke before the field is in
        // error (that is the "impatient teacher" anti-pattern) — blur/Save handle the first check.
        if (!NameHasError)
            return;

        ApplyNameValidation(value);
    }

    partial void OnPersonBirthdayChanged(string value)
    {
        if (_isHydrating)
            return;

        _birthdayDirty = true;

        if (!BirthdayHasError)
            return;

        ApplyBirthdayValidation(value);
    }

    partial void OnPersonEmailChanged(string value)
    {
        if (_isHydrating)
            return;

        _emailDirty = true;

        if (!EmailHasError)
            return;

        ApplyEmailValidation(value);
    }

    private void ApplyNameValidation(string value)
    {
        var (isValid, message) = _personService.ValidateNameInput(value ?? string.Empty);
        NameHasError = !isValid;
        NameErrorText = isValid ? string.Empty : message;
    }

    private void ApplyBirthdayValidation(string value)
    {
        var birthday = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        var (isValid, message) = _personService.ValidateBirthday(birthday);
        BirthdayHasError = !isValid;
        BirthdayErrorText = isValid ? string.Empty : message;
    }

    private void ApplyEmailValidation(string value)
    {
        var email = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        var (isValid, message) = _personService.ValidateEmail(email);
        EmailHasError = !isValid;
        EmailErrorText = isValid ? string.Empty : message;
    }

    private async Task SaveAsync()
    {
        if (IsBusy) return;   // BUG-049: re-entrancy guard, defense-in-depth alongside CanExecute

        var name = PersonName?.Trim() ?? string.Empty;

        // Safety net: re-run ALL field validators on Save, regardless of dirty state — an
        // untouched required field (or one whose error was never surfaced) must still be caught.
        ApplyNameValidation(name);
        ApplyBirthdayValidation(PersonBirthday);
        ApplyEmailValidation(PersonEmail);

        if (NameHasError || BirthdayHasError || EmailHasError)
            return;

        var birthday = string.IsNullOrWhiteSpace(PersonBirthday) ? null : PersonBirthday.Trim();
        var email = string.IsNullOrWhiteSpace(PersonEmail) ? null : PersonEmail.Trim();

        IsBusy = true;
        try
        {
            if (IsEditMode)
            {
                var (success, message) = await _personService.UpdatePersonAsync(
                    PersonId.Value, name, birthday, email);
                if (success)
                {
                    await _snackbarService.ShowSuccessAsync(message);
                    await (Shell.Current?.GoToAsync("..") ?? Task.CompletedTask);
                }
                else
                {
                    await ApplyAsyncFailureAsync(message);
                }
            }
            else
            {
                var (success, message, _) = await _personService.CreatePersonAsync(name, birthday, email);
                if (success)
                {
                    await _snackbarService.ShowSuccessAsync(message);
                    await (Shell.Current?.GoToAsync("..") ?? Task.CompletedTask);
                }
                else
                {
                    await ApplyAsyncFailureAsync(message);
                }
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Maps an async CRUD failure to the field that owns it. After the per-field validators above
    /// all pass, the only field-attributable failure the service can still return is the
    /// email-uniqueness check — anything else (e.g. "Singer not found" if the record was removed
    /// concurrently) is not owned by any field and is surfaced as a non-blocking notice instead of
    /// being guessed onto one via substring routing.
    /// </summary>
    private async Task ApplyAsyncFailureAsync(string message)
    {
        if (message.Contains("Email", StringComparison.OrdinalIgnoreCase))
        {
            EmailHasError = true;
            EmailErrorText = message;
        }
        else
        {
            await _snackbarService.ShowErrorAsync(message);
        }
    }

    private Task CancelAsync() => Shell.Current?.GoToAsync("..") ?? Task.CompletedTask;

    private async Task SearchPersonsAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
        {
            Suggestions = [];
            return;
        }

        var results = await _personService.SearchPersonsStartsWithAsync(term, 5);
        Suggestions = results.Select(p => new AutocompleteSuggestion(
            p.FullName,
            p.GetDisplayIdentifier(),
            p)).ToList();
    }

    private async Task SuggestionSelectedAsync(AutocompleteSuggestion suggestion)
    {
        if (suggestion?.Data is not Person person) return;

        Suggestions = [];

        var birthday = Uri.EscapeDataString(person.BirthdayDayMonth ?? string.Empty);
        var email = Uri.EscapeDataString(person.Email ?? string.Empty);
        var name = Uri.EscapeDataString(person.FullName);

        // BUG-044: use a REPLACING relative route ("../person-form?...") — "../" pops the current
        // form page before pushing the edit form. Pushing the edit form on top of the New Singer
        // form left a stale, raw-text-pre-filled form in the Shell stack; after saving the edit
        // form, "GoToAsync(..)" revealed that stale form and saving it inserted a duplicate entity.
        // Routed through INavigationService so the behavior is unit-testable (Shell.Current is
        // null in tests — testing.md anti-pattern).
        await _navigation.GoToAsync(
            $"../{Routes.PersonForm}?personId={person.Id}&personName={name}&personBirthday={birthday}&personEmail={email}");
    }

    private void UpdateCharacterCounter(int length)
    {
        ShowCharacterCounter = _personService.ShouldShowCharacterCounter(length);
        if (ShowCharacterCounter)
        {
            var (text, isWarning, isError) = _personService.GetCharacterCounterInfo(length);
            CharacterCounterText = text;
            IsCharacterCounterWarning = isWarning;
            IsCharacterCounterError = isError;
        }
    }
}
