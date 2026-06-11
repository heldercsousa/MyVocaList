namespace MyVocaList.UI.Pages.Queue;

using MyVocaList.UI.ViewModels;

public partial class QueueSongPickerPage : ContentPage
{
    public QueueSongPickerPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var viewModel = BindingContext as QueueSongPickerViewModel;
        if (viewModel == null) return;

        if (Shell.Current?.CurrentState?.Location != null)
        {
            var uri = new Uri($"http://dummy{Shell.Current.CurrentState.Location.OriginalString}");
            if (uri.Query.Contains("entryId=") && int.TryParse(
                uri.Query.Split("entryId=")[1].Split('&')[0], out var entryId))
            {
                viewModel.QueueEntryId = entryId;
            }
        }

        await viewModel.InitializeCommand.ExecuteAsync(null);
    }
}
