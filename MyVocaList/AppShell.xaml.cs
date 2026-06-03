using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.UI.Components.Sheets;
using MyVocaList.UI.Messages;

namespace MyVocaList;

public partial class AppShell : Shell
{
    private readonly IWhatsNewService _whatsNewService;
    private WhatsNewBottomSheet? _whatsNewSheet;

    public AppShell(AppShellViewModel viewModel, IWhatsNewService whatsNewService)
    {
        _whatsNewService = whatsNewService;
        BindingContext = viewModel;
        InitializeComponent();

        viewModel.ExitRequested += OnExitRequested;
        WeakReferenceMessenger.Default.Register<ShowWhatsNewMessage>(this, OnShowWhatsNew);

        _ = viewModel.InitializeAsync();

        Routing.RegisterRoute(Routes.VenueForm, typeof(VenueFormPage));
        Routing.RegisterRoute(Routes.PersonForm, typeof(PersonFormPage));
        Routing.RegisterRoute(Routes.ArtistForm, typeof(ArtistFormPage));
        Routing.RegisterRoute(Routes.SongForm, typeof(SongFormPage));
    }

    // Fallback: catches back press when Shell is at root and QueuePage.OnBackButtonPressed
    // is not called (e.g. certain MAUI/platform back-button routing edge cases).
    protected override bool OnBackButtonPressed()
    {
        if (Navigation.NavigationStack.Count == 0 && CurrentPage is QueuePage queuePage)
        {
            queuePage.ShowExitConfirmation();
            return true;
        }

        return base.OnBackButtonPressed();
    }

    private void OnExitRequested()
    {
        if (CurrentPage is QueuePage queuePage)
            queuePage.ShowExitConfirmation();
    }

    private void OnShowWhatsNew(object recipient, ShowWhatsNewMessage message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _whatsNewSheet ??= new WhatsNewBottomSheet();
            if (CurrentPage is ContentPage page && _whatsNewSheet.Parent is null)
            {
                if (page.Content is Layout layout)
                    layout.Children.Add(_whatsNewSheet);
            }
            _whatsNewSheet.Show(message.Entry, _whatsNewService);
        });
    }
}
