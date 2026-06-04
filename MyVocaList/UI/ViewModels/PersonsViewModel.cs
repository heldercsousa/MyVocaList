using MyVocaList.Domain.Entity;
using MyVocaList.UI.Collections;

namespace MyVocaList.UI.ViewModels;

/// <summary>
/// ViewModel for the Singers list page: paging, search, always-on selection, confirm-delete.
/// Add navigates to PersonFormPage via FAB. Edit navigates via FloatingToolbar (single select).
/// </summary>
public partial class PersonsViewModel : CrudListViewModelBase<PersonListItemDto>
{
    private readonly IPersonService _personService;
    private readonly ISnackbarComponent _snackbarService;

    public PersonsViewModel(
        IPersonService personService,
        ISnackbarComponent snackbarService,
        ILogger<PersonsViewModel> logger) : base(logger)
    {
        _personService = personService;
        _snackbarService = snackbarService;

        Persons = [];
        SelectedPersons = [];

        AddPersonCommand = new AsyncRelayCommand(NavigateToAddAsync);
    }

    public ObservableRangeCollection<PersonListItemDto> Persons { get; }
    public ObservableRangeCollection<PersonListItemDto> SelectedPersons { get; }

    /// <summary>Non-generic wrapper for binding to DXCollectionView SelectedItems (requires IList).</summary>
    public System.Collections.IList SelectedPersonsRaw => SelectedPersons;

    public IAsyncRelayCommand AddPersonCommand { get; }

    public string AppBarTitle => SelectedCount == 0 ? "Singers" : $"{SelectedCount} selected";
    public bool IsEmptyNoPersons => IsEmpty && string.IsNullOrWhiteSpace(SearchText);
    public bool IsEmptyNoResults => IsEmpty && !string.IsNullOrWhiteSpace(SearchText);

    protected override ObservableRangeCollection<PersonListItemDto> Items => Persons;
    protected override ObservableRangeCollection<PersonListItemDto> SelectedItems => SelectedPersons;

    protected override void OnSelectedCountUpdated(int value)
        => OnPropertyChanged(nameof(AppBarTitle));

    protected override Task<(IEnumerable<PersonListItemDto> items, int totalCount)> FetchPageAsync(
        int page, int pageSize, string query, CancellationToken ct)
        => _personService.GetPagedPersonsForListAsync(page, pageSize, query, ct);

    protected override Task<(IEnumerable<PersonListItemDto> items, int totalCount)> FetchMoreAsync(
        int page, int pageSize, string query, CancellationToken ct = default)
        => _personService.GetPagedPersonsForListAsync(page, pageSize, query, ct);

    protected override string BuildDeleteConfirmMessage(IList<PersonListItemDto> items)
        => $"Delete {items.Count} singer(s)?";

    protected override async Task ExecuteDeleteAsync(IEnumerable<PersonListItemDto> items)
    {
        var ids = items.Select(p => p.Id);
        var (success, message) = await _personService.DeletePersonsAsync(ids);
        if (success)
        {
            await RefreshAsync();
            await _snackbarService.ShowSuccessAsync(message);
        }
        else
        {
            await _snackbarService.ShowErrorAsync(message);
        }
    }

    protected override Task NavigateToAddAsync()
        => Shell.Current?.GoToAsync(Routes.PersonForm) ?? Task.CompletedTask;

    protected override async Task NavigateToEditAsync(PersonListItemDto item)
    {
        var name = Uri.EscapeDataString(item.FullName);
        var birthday = Uri.EscapeDataString(item.BirthdayDayMonth ?? string.Empty);
        var email = Uri.EscapeDataString(item.Email ?? string.Empty);
        await (Shell.Current?.GoToAsync(
            $"{Routes.PersonForm}?personId={item.Id}&personName={name}&personBirthday={birthday}&personEmail={email}") ?? Task.CompletedTask);
    }

    protected override void RaiseEntityEmptyStateProperties()
    {
        OnPropertyChanged(nameof(IsEmptyNoPersons));
        OnPropertyChanged(nameof(IsEmptyNoResults));
    }

}
