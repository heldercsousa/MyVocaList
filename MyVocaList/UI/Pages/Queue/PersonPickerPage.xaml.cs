namespace MyVocaList.UI.Pages.Queue;

using MyVocaList.UI.ViewModels;

public partial class PersonPickerPage : ContentPage
{
    public PersonPickerPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var viewModel = BindingContext as PersonPickerViewModel;
        if (viewModel == null) return;

        // Extract eventId from query params
        if (Shell.Current?.CurrentState?.Location != null)
        {
            var uri = new Uri($"http://dummy{Shell.Current.CurrentState.Location.OriginalString}");
            if (uri.Query.Contains("eventId=") && int.TryParse(
                uri.Query.Split("eventId=")[1].Split('&')[0], out var eventId))
            {
                viewModel.EventId = eventId;
            }
        }

        await viewModel.InitializeAsync();
    }
}
