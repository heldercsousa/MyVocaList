using System.Collections.ObjectModel;
using System.Globalization;
using MyVocaList.Domain.Constants;
using CommunityToolkit.Mvvm.Input;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.ReadModels;
using MyVocaList.UI.Collections;
using MyVocaList.UI.Models;

namespace MyVocaList.UI.ViewModels;

/// <summary>
/// ViewModel for the Add / Edit Singer form page.
/// PersonId null = create mode; PersonId set = edit mode.
/// </summary>
[QueryProperty(nameof(PersonIdRaw), "personId")]
[QueryProperty(nameof(PersonName), "personName")]
[QueryProperty(nameof(PersonBirthDayRaw), "personBirthDay")]
[QueryProperty(nameof(PersonBirthMonthRaw), "personBirthMonth")]
[QueryProperty(nameof(PersonEmail), "personEmail")]
public partial class PersonFormViewModel : ViewModelBase
{
    private readonly IPersonService _personService;
    private readonly ISnackbarComponent _snackbarService;
    private readonly INavigationService _navigation;
    private readonly ILogger<PersonFormViewModel> _logger;

    // Shell passes all query parameters as strings; parse manually.
    // A loaded singer (pick or Open) re-applies the query to this form; the name that follows the id is its baseline (REQ-DUP-10).
    public string PersonIdRaw
    {
        set
        {
            PersonId = int.TryParse(value, out var id) ? id : null;
            _awaitingLoadedName = PersonId.HasValue;
        }
    }

    // Birthday day and month arrive as query strings and are parsed into the dropdown selections (REQ-BDAY-09).
    public string PersonBirthDayRaw
    {
        set => SelectedBirthDay = int.TryParse(value, out var day) ? (int?)day : null;
    }

    public string PersonBirthMonthRaw
    {
        set => SelectedBirthMonth = int.TryParse(value, out var month)
            ? Months.FirstOrDefault(m => m.Number == month)
            : null;
    }

    [ObservableProperty] private int? _personId;
    private string _personName = string.Empty;

    public string PersonName
    {
        get => _personName;
        set
        {
            // REQ-DUP-10: the name that follows a loaded id is the baseline. Captured on every write, not only on a change,
            // because a loaded name equal to the typed text raises no change event.
            if (_awaitingLoadedName)
            {
                _awaitingLoadedName = false;
                _loadedName = value?.Trim() ?? string.Empty;
            }

            if (SetProperty(ref _personName, value ?? string.Empty))
                OnPersonNameChanged(_personName);
        }
    }
    [ObservableProperty] private string _personEmail = string.Empty;

    // Birthday dropdowns (REQ-BDAY-01..04). Both optional; the service decides whether the pair is valid.
    [ObservableProperty] private int? _selectedBirthDay;
    [ObservableProperty] private MonthOption? _selectedBirthMonth;

    [ObservableProperty] private bool _nameHasError;
    [ObservableProperty] private string _nameErrorText = string.Empty;
    [ObservableProperty] private bool _birthdayHasError;
    [ObservableProperty] private string _birthdayErrorText = string.Empty;
    // The birthday error sits on the empty dropdown; when both are set it sits on Birth day.
    [ObservableProperty] private bool _birthdayErrorOnDay;
    [ObservableProperty] private bool _birthdayErrorOnMonth;
    [ObservableProperty] private bool _emailHasError;
    [ObservableProperty] private string _emailErrorText = string.Empty;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private IEnumerable<AutocompleteSuggestion> _suggestions = [];

    // BUG-088 same-name soft warning (REQ-DUP-09, REQ-DUP-13a). Rows carry the Person in Data.
    [ObservableProperty] private bool _isSameNameSheetVisible;
    [ObservableProperty] private string _sameNameSheetTitle = string.Empty;
    [ObservableProperty] private string _sameNameMoreText = string.Empty;
    [ObservableProperty] private bool _hasMoreSameName;
    // False when the form is name-only: Save as new / Save anyway is hidden and the hint is shown instead (REQ-DUP-13a).
    [ObservableProperty] private bool _canSaveAsNewFromSheet = true;
    public ObservableRangeCollection<AutocompleteSuggestion> SameNameMatches { get; } = [];
    public string SameNameSaveButtonText => IsEditMode ? "Save anyway" : "Save as new";

    // Character counter
    [ObservableProperty] private string _characterCounterText = string.Empty;
    [ObservableProperty] private bool _showCharacterCounter;
    [ObservableProperty] private bool _isCharacterCounterWarning;
    [ObservableProperty] private bool _isCharacterCounterError;

    // Becomes true once the user edits each field. Guards blur validation so a pristine field the
    // user only tabbed through is not flagged prematurely (Form Validation Standard).
    private bool _nameDirty;
    private bool _emailDirty;

    // True while OnSelectedBirthMonthChanged rebuilds Days, so the clear that the ComboBox performs
    // on an ItemsSource change is not read as a user selection.
    private bool _isRebuildingDays;

    // True until the page's OnAppearing calls CompleteHydration(). While true, Shell QueryProperty
    // pre-population (edit mode) must NOT mark any field dirty — otherwise a pre-filled invalid
    // value would flash an error on the very first blur, before the user has touched anything
    // (edit-mode dirty-guard, Form Validation Standard).
    private bool _isHydrating = true;

    // Trimmed FullName as loaded from the query (edit mode). Set by CompleteHydration on the first call, and
    // again by the next name that follows a loaded id (REQ-DUP-10: a pick or an Open re-loads the same form).
    private string _loadedName = string.Empty;

    // True from a PersonIdRaw query write until the name that follows it is captured as the loaded name.
    private bool _awaitingLoadedName;

    public bool IsEditMode => PersonId.HasValue;
    public string PageTitle => IsEditMode ? "Edit Singer" : "New Singer";

    /// <summary>The twelve months in English, January first (REQ-BDAY-01).</summary>
    public IReadOnlyList<MonthOption> Months { get; } = Enumerable.Range(1, 12)
        .Select(m => new MonthOption(m, CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(m)))
        .ToList();

    /// <summary>Days offered by the Birth day dropdown; rebuilt when the month changes (REQ-BDAY-02).</summary>
    public ObservableCollection<int> Days { get; } = [];

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
        SaveAsNewCommand = new AsyncRelayCommand(SaveAsNewAsync);
        OpenSameNameCommand = new AsyncRelayCommand<AutocompleteSuggestion>(OpenSameNameAsync);
        DismissSameNameSheetCommand = new RelayCommand(() => IsSameNameSheetVisible = false);
        NameOptionSource = new MenuOptionSource((query, ct) => SearchPersonsCoreAsync(query, ct));

        RebuildDays(null);
    }

    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand CancelCommand { get; }
    public IAsyncRelayCommand<string> SearchPersonsCommand { get; }
    public IAsyncRelayCommand<AutocompleteSuggestion> SuggestionSelectedCommand { get; }
    public IAsyncRelayCommand SaveAsNewCommand { get; }
    public IAsyncRelayCommand<AutocompleteSuggestion> OpenSameNameCommand { get; }
    public IRelayCommand DismissSameNameSheetCommand { get; }

    /// <summary>Option source for the Full Name autocomplete field (REQ-ACF-13, -16).</summary>
    public IMenuOptionSource NameOptionSource { get; }

    partial void OnPersonIdChanged(int? value)
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(SameNameSaveButtonText));
    }

    partial void OnIsBusyChanged(bool value) => SaveCommand.NotifyCanExecuteChanged();

    /// <summary>
    /// Called by the page's <c>OnAppearing</c> once Shell has finished applying all
    /// <c>[QueryProperty]</c> values for this navigation. Ends the hydration window so that
    /// subsequent field edits are tracked as dirty (edit-mode dirty-guard).
    /// </summary>
    public void CompleteHydration()
    {
        if (!_isHydrating) return;

        // REQ-DUP-10: the name as loaded is the baseline for "name changed" in edit mode.
        _loadedName = PersonName?.Trim() ?? string.Empty;
        _isHydrating = false;
    }

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

    /// <summary>Blur handler for the email field — see <see cref="ValidateName"/>.</summary>
    [RelayCommand]
    private void ValidateEmail()
    {
        if (!_emailDirty)
            return;

        ApplyEmailValidation(PersonEmail);
    }

    private void OnPersonNameChanged(string value)
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

    // Dropdowns have no blur, so the birthday is validated on Save and on each selection while it is in error
    // (the same "reward early" rule as the text fields). Hydration sets these through the query setters, which
    // never validate, so a stored value cannot show an error before the user acts.
    partial void OnSelectedBirthMonthChanged(MonthOption? value)
    {
        RebuildDays(value?.Number);

        if (BirthdayHasError)
            ApplyBirthdayValidation();
    }

    partial void OnSelectedBirthDayChanged(int? value)
    {
        if (_isRebuildingDays)
            return;

        if (BirthdayHasError)
            ApplyBirthdayValidation();
    }

    /// <summary>
    /// Rebuilds <see cref="Days"/> for <paramref name="month"/> (1 to 31 when null). A selected day that no longer
    /// fits the month is cleared (REQ-BDAY-03); a day that still fits is kept.
    /// </summary>
    private void RebuildDays(int? month)
    {
        var keep = SelectedBirthDay;
        var maxDay = month is int m ? BirthdayRules.DaysInMonth(m) : 31;

        _isRebuildingDays = true;
        try
        {
            Days.Clear();
            for (var day = 1; day <= maxDay; day++)
                Days.Add(day);

            SelectedBirthDay = keep is int kept && kept <= maxDay ? kept : (int?)null;
        }
        finally
        {
            _isRebuildingDays = false;
        }
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

    private void ApplyBirthdayValidation()
    {
        var (isValid, message) = _personService.ValidateBirthday(SelectedBirthDay, SelectedBirthMonth?.Number);
        BirthdayHasError = !isValid;
        BirthdayErrorText = isValid ? string.Empty : message;

        // The error marks the empty dropdown: Birth month when only the day is set, otherwise Birth day.
        var monthIsTheEmptyOne = SelectedBirthDay is not null && SelectedBirthMonth is null;
        BirthdayErrorOnMonth = !isValid && monthIsTheEmptyOne;
        BirthdayErrorOnDay = !isValid && !monthIsTheEmptyOne;
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
        ApplyBirthdayValidation();
        ApplyEmailValidation(PersonEmail);
        var email = NormalizedEmail();

        if (NameHasError || BirthdayHasError || EmailHasError)
            return;

        IsBusy = true;
        try
        {
            // REQ-DUP-09/10: the same-name check is a soft warning and never blocks. Create always checks;
            // edit checks only when the name differs from the loaded value.
            if (!IsEditMode || !string.Equals(name, _loadedName, StringComparison.Ordinal))
            {
                var sameName = await _personService.GetSameNamePersonsAsync(
                    name, email, SelectedBirthDay, SelectedBirthMonth?.Number, PersonId);
                if (sameName.TotalCount > 0)
                {
                    ShowSameNameSheet(sameName);
                    return;
                }
            }

            await SaveCoreAsync(name, email);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Saves the form through the service (insert or update). The caller owns <see cref="IsBusy"/>.</summary>
    private async Task SaveCoreAsync(string name, string? email)
    {
        var birthDay = SelectedBirthDay;
        var birthMonth = SelectedBirthMonth?.Number;

        if (IsEditMode)
        {
            var (success, message) = await _personService.UpdatePersonAsync(
                PersonId.Value, name, birthDay, birthMonth, email);
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
            var (success, message, _) = await _personService.CreatePersonAsync(name, birthDay, birthMonth, email);
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

    private string? NormalizedEmail() => string.IsNullOrWhiteSpace(PersonEmail) ? null : PersonEmail.Trim();

    /// <summary>
    /// REQ-DUP-09: fills the sheet with the listed same-name singers and opens it. Nothing is saved.
    /// The title counts all same-name singers; the more-line counts the ones not listed (TotalCount minus the listed rows).
    /// </summary>
    private void ShowSameNameSheet(SameNameResult sameName)
    {
        SameNameMatches.ReplaceRange(sameName.Matches
            .Select(p => new AutocompleteSuggestion(p.FullName, p.GetDisplayIdentifier(), p))
            .ToList());

        var notListed = sameName.TotalCount - sameName.Matches.Count;
        SameNameSheetTitle = sameName.TotalCount == 1
            ? "A singer with this name already exists"
            : $"{sameName.TotalCount} singers with this name already exist";
        HasMoreSameName = notListed > 0;
        SameNameMoreText = HasMoreSameName
            ? $"…and {notListed} more. Find them in the Singers list."
            : string.Empty;

        // REQ-DUP-13a: a name-only form (no email, no birthday) cannot save a new singer with a shared name.
        CanSaveAsNewFromSheet = !string.IsNullOrWhiteSpace(PersonEmail) || SelectedBirthDay.HasValue || SelectedBirthMonth is not null;
        IsSameNameSheetVisible = true;
    }

    /// <summary>REQ-DUP-09 "Save as new" (insert) or "Save anyway" (edit): closes the sheet and saves the form.</summary>
    private async Task SaveAsNewAsync()
    {
        if (IsBusy) return;

        IsSameNameSheetVisible = false;
        IsBusy = true;
        try
        {
            await SaveCoreAsync(PersonName?.Trim() ?? string.Empty, NormalizedEmail());
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>REQ-DUP-09: a listed singer opens in edit mode, the same as a name-suggestion pick.</summary>
    private async Task OpenSameNameAsync(AutocompleteSuggestion? match)
    {
        IsSameNameSheetVisible = false;

        if (match?.Data is not Person person) return;

        await NavigateToEditAsync(person);
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
        Suggestions = [.. await SearchPersonsCoreAsync(term)];
    }

    /// <summary>
    /// Core Person-name search, shared by <see cref="SearchPersonsAsync"/> and <see cref="NameOptionSource"/>.
    /// Never reads or writes <see cref="Suggestions"/> (BUG-056) — it returns THIS query's own results.
    /// </summary>
    private async Task<IReadOnlyList<AutocompleteSuggestion>> SearchPersonsCoreAsync(string term, CancellationToken ct = default)
    {
        var trimmed = term?.Trim() ?? string.Empty;
        if (trimmed.Length < SearchConstants.MinimumLocalQueryLength)
            return [];

        var results = await _personService.SearchPersonsStartsWithAsync(trimmed, cancellationToken: ct);
        return results.Select(p => new AutocompleteSuggestion(
            p.FullName,
            p.GetDisplayIdentifier(),
            p)).ToList();
    }

    private async Task SuggestionSelectedAsync(AutocompleteSuggestion suggestion)
    {
        if (suggestion?.Data is not Person person) return;

        await NavigateToEditAsync(person);
    }

    /// <summary>
    /// Opens <paramref name="person"/> in edit mode. Shared by the name-suggestion pick and the same-name sheet Open (REQ-DUP-09).
    /// Both paths clear the name suggestions first, so the loaded name never opens them (REQ-DUP-16).
    /// </summary>
    private async Task NavigateToEditAsync(Person person)
    {
        Suggestions = [];

        // Day and month travel as two integer query parameters; both empty when the singer has no birthday.
        var birthDay = person.BirthDay?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        var birthMonth = person.BirthMonth?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        var email = Uri.EscapeDataString(person.Email ?? string.Empty);
        var name = Uri.EscapeDataString(person.FullName);

        // BUG-044: use a REPLACING relative route ("../person-form?...") — "../" pops the current
        // form page before pushing the edit form. Pushing the edit form on top of the New Singer
        // form left a stale, raw-text-pre-filled form in the Shell stack; after saving the edit
        // form, "GoToAsync(..)" revealed that stale form and saving it inserted a duplicate entity.
        // Routed through INavigationService so the behavior is unit-testable (Shell.Current is
        // null in tests — testing.md anti-pattern).
        await _navigation.GoToAsync(
            $"../{Routes.PersonForm}?personId={person.Id}&personName={name}&personBirthDay={birthDay}&personBirthMonth={birthMonth}&personEmail={email}");
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
