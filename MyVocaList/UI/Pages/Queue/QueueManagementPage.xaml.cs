namespace MyVocaList.UI.Pages.Queue;

using MyVocaList.UI.ViewModels;

public partial class QueueManagementPage : ContentPage
{
    private QueueManagementViewModel? _viewModel;

    public QueueManagementPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _viewModel = BindingContext as QueueManagementViewModel;
        if (_viewModel == null) return;

        // Extract eventId from navigation
        if (Shell.Current?.CurrentState?.Location != null)
        {
            var path = Shell.Current.CurrentState.Location.OriginalString;
            var segments = path.Split('/');
            if (segments.Length > 0 && int.TryParse(segments[^1], out var eventId))
            {
                await _viewModel.InitializeCommand.ExecuteAsync(eventId);
            }
        }

        _viewModel.OnNavigatedTo();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel?.OnNavigatedFrom();
    }

    private async void OnCancelFinish(object sender, EventArgs e)
    {
        await finishEventSheet.DismissAsync();
    }

    private async void OnConfirmFinish(object sender, EventArgs e)
    {
        await finishEventSheet.DismissAsync();
        if (_viewModel != null)
        {
            await _viewModel.FinishEventCommand.ExecuteAsync(null);
        }
    }
}
