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

    public bool IsEditMode => PersonId.HasValue;
    public string PageTitle => IsEditMode ? "Edit Singer" : "New Singer";

    public PersonFormViewModel(
        IPersonService personService,
        ISnackbarComponent snackbarService,
        ILogger<PersonFormViewModel> logger)
    {
        _personService = personService;
        _snackbarService = snackbarService;
        _logger = logger;

        SaveCommand = new AsyncRelayCommand(SaveAsync);
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

    partial void OnPersonNameChanged(string value)
    {
        ClearNameError();
        UpdateCharacterCounter(value?.Length ?? 0);
    }

    partial void OnPersonBirthdayChanged(string value) => ClearBirthdayError();
    partial void OnPersonEmailChanged(string value) => ClearEmailError();

    private async Task SaveAsync()
    {
        var name = PersonName?.Trim() ?? string.Empty;
        var birthday = string.IsNullOrWhiteSpace(PersonBirthday) ? null : PersonBirthday.Trim();
        var email = string.IsNullOrWhiteSpace(PersonEmail) ? null : PersonEmail.Trim();

        var nameValidation = _personService.ValidateNameInput(name);
        if (!nameValidation.isValid)
        {
            NameHasError = true;
            NameErrorText = nameValidation.message;
            return;
        }

        var birthdayValidation = _personService.ValidateBirthday(birthday);
        if (!birthdayValidation.isValid)
        {
            BirthdayHasError = true;
            BirthdayErrorText = birthdayValidation.message;
            return;
        }

        var emailValidation = _personService.ValidateEmail(email);
        if (!emailValidation.isValid)
        {
            EmailHasError = true;
            EmailErrorText = emailValidation.message;
            return;
        }

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
                    SetInlineError(message);
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
                    SetInlineError(message);
                }
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetInlineError(string message)
    {
        // Route service error messages to the correct field
        if (message.Contains("Email", StringComparison.OrdinalIgnoreCase))
        {
            EmailHasError = true;
            EmailErrorText = message;
        }
        else if (message.Contains("birthday", StringComparison.OrdinalIgnoreCase) ||
                 message.Contains("DD/MM", StringComparison.OrdinalIgnoreCase))
        {
            BirthdayHasError = true;
            BirthdayErrorText = message;
        }
        else
        {
            NameHasError = true;
            NameErrorText = message;
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

        await (Shell.Current?.GoToAsync(
            $"{Routes.PersonForm}?personId={person.Id}&personName={name}&personBirthday={birthday}&personEmail={email}") ?? Task.CompletedTask);
    }

    private void ClearNameError() { NameHasError = false; NameErrorText = string.Empty; }
    private void ClearBirthdayError() { BirthdayHasError = false; BirthdayErrorText = string.Empty; }
    private void ClearEmailError() { EmailHasError = false; EmailErrorText = string.Empty; }

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
