using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.UI.Collections;

namespace MyVocaList.UI.ViewModels;

/// <summary>ViewModel for person/singer picker modal—search and enqueue people to queue.</summary>
public partial class PersonPickerViewModel : ViewModelBase
{
    private readonly IPersonRepository _personRepository;
    private readonly ILogger<PersonPickerViewModel> _logger;

    public PersonPickerViewModel(IPersonRepository personRepository, ILogger<PersonPickerViewModel> logger)
    {
        _personRepository = personRepository;
        _logger = logger;
        Results = [];
    }

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private ObservableRangeCollection<PersonDto> results;
    [ObservableProperty] private int eventId;

    partial void OnSearchTextChanged(string value)
    {
        _ = SearchAsync(value);
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await SearchAsync(SearchText);
    }

    [RelayCommand]
    public async Task SelectPersonAsync(PersonDto? person)
    {
        if (person == null) return;

        // Send message to QueueManagementViewModel
        WeakReferenceMessenger.Default.Send(new PersonPickedMessage { PersonId = person.Id, EventId = EventId });

        // Close modal
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    public async Task AddNewPersonAsync()
    {
        await Shell.Current.GoToAsync("person-form");
    }

    private async Task SearchAsync(string query)
    {
        try
        {
            var (people, _) = await _personRepository.GetPagedAsync(1, int.MaxValue, query, CancellationToken.None);
            var filtered = string.IsNullOrWhiteSpace(query)
                ? people
                : people.Where(p => p.FullName.Contains(query, StringComparison.OrdinalIgnoreCase));

            var dtos = filtered.Select(p => new PersonDto { Id = p.Id, Name = p.FullName }).ToList();
            RunOnUiThread(() => Results.ReplaceRange(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching people");
        }
    }
}

public partial class PersonDto : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string name = string.Empty;
}

public class PersonPickedMessage
{
    public int PersonId { get; set; }
    public int EventId { get; set; }
}
