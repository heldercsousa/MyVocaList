using DevExpress.Maui.Editors;
using MyVocaList.Contracts.Models;

namespace MyVocaList.UI.Pages.People;

public partial class PersonFormPage : ContentPage
{
    private readonly PersonFormViewModel _viewModel;

    /// <summary>Exposed for compiled bindings inside DataTemplates.</summary>
    public PersonFormViewModel ViewModel => _viewModel;

    public PersonFormPage(PersonFormViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Shell has finished applying [QueryProperty] values for this navigation by the time
        // OnAppearing runs — end the hydration window so subsequent edits are tracked as dirty.
        _viewModel.CompleteHydration();

        // Focus the name field only in create mode
        if (!_viewModel.IsEditMode)
            nameField.Focus();
    }

    // ── DX AutoCompleteEdit glue (T4, 2026-07-19 change spec) ────────────────────────────
    // Thin forwarding only: the editor's AsyncItemsSourceProvider owns debounce (RequestDelay=300)
    // and stale-request cancellation; the search runs through the EXISTING VM command path
    // (SearchPersonsCommand → IPersonService) so the 2-char gate stays in the ViewModel.
    private void OnNameItemsRequested(object sender, ItemsRequestEventArgs e)
    {
        var text = e.Text;
        var token = e.CancellationToken;
        e.RequestAsync = async () =>
        {
            await _viewModel.SearchPersonsCommand.ExecuteAsync(text);
            token.ThrowIfCancellationRequested();
            return _viewModel.Suggestions;
        };
    }

    // Forwards drop-down selection to the existing VM selection command.
    private void OnNameSelectionChanged(object sender, EventArgs e)
    {
        if (nameField.SelectedItem is AutocompleteSuggestion suggestion)
            _viewModel.SuggestionSelectedCommand.Execute(suggestion);
    }

    // Blur without a selection → existing VM name validation (same trigger the old component used).
    private void OnNameUnfocused(object sender, FocusEventArgs e)
    {
        if (nameField.SelectedItem is null)
            _viewModel.ValidateNameCommand.Execute(null);
    }

    // Bridges the MAUI Unfocused (blur) events to the ViewModel's validation commands.
    private void OnBirthdayUnfocused(object sender, FocusEventArgs e) =>
        _viewModel.ValidateBirthdayCommand.Execute(null);

    private void OnEmailUnfocused(object sender, FocusEventArgs e) =>
        _viewModel.ValidateEmailCommand.Execute(null);
}
