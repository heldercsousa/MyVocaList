using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.UI.Components.Sheets;
using MyVocaList.UI.Messages;

namespace MyVocaList;

public partial class AppShell : Shell
{
    private readonly IWhatsNewService _whatsNewService;
    private WhatsNewBottomSheet? _whatsNewSheet;
    private UpdateAvailableBottomSheet? _updateAvailableSheet;
    private UpdateRequiredBottomSheet? _updateRequiredSheet;

    public AppShell(AppShellViewModel viewModel, IWhatsNewService whatsNewService)
    {
        _whatsNewService = whatsNewService;
        BindingContext = viewModel;
        InitializeComponent();

        viewModel.ExitRequested += OnExitRequested;

        WeakReferenceMessenger.Default.Register<ShowWhatsNewMessage>(this, OnShowWhatsNew);
        WeakReferenceMessenger.Default.Register<ShowUpdateAvailableMessage>(this, OnShowUpdateAvailable);
        WeakReferenceMessenger.Default.Register<ShowUpdateRequiredMessage>(this, OnShowUpdateRequired);

        Routing.RegisterRoute(Routes.VenueForm, typeof(VenueFormPage));
        Routing.RegisterRoute(Routes.PersonForm, typeof(PersonFormPage));
        Routing.RegisterRoute(Routes.ArtistForm, typeof(ArtistFormPage));
        Routing.RegisterRoute(Routes.SongForm, typeof(SongFormPage));
        Routing.RegisterRoute(Routes.Feedback, typeof(FeedbackPage));
        Routing.RegisterRoute(Routes.ArtistPicker, typeof(ArtistPickerPage));
        Routing.RegisterRoute(Routes.SongPicker, typeof(SongPickerPage));
        Routing.RegisterRoute(Routes.YouTubeSearch, typeof(YouTubeSearchPage));

        _ = viewModel.InitializeAsync();
    }

    // Fallback: catches back press when Shell is at root and VenuesPage.OnBackButtonPressed
    // is not called (e.g. certain MAUI/platform back-button routing edge cases).
    protected override bool OnBackButtonPressed()
    {
        if (Navigation.NavigationStack.Count == 0 && CurrentPage is VenuesPage venuesPage)
        {
            venuesPage.ShowExitConfirmation();
            return true;
        }

        return base.OnBackButtonPressed();
    }

    private void OnExitRequested()
    {
        if (CurrentPage is VenuesPage venuesPage)
            venuesPage.ShowExitConfirmation();
    }

    private void OnShowWhatsNew(object recipient, ShowWhatsNewMessage message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _whatsNewSheet ??= new WhatsNewBottomSheet();
            AttachSheetToCurrentPage(_whatsNewSheet);
            _whatsNewSheet.Show(message.Entry, _whatsNewService);
        });
    }

    private void OnShowUpdateAvailable(object recipient, ShowUpdateAvailableMessage message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _updateAvailableSheet ??= new UpdateAvailableBottomSheet();
            AttachSheetToCurrentPage(_updateAvailableSheet);
            _updateAvailableSheet.Show(message.Result);
        });
    }

    private void OnShowUpdateRequired(object recipient, ShowUpdateRequiredMessage message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _updateRequiredSheet ??= new UpdateRequiredBottomSheet();
            AttachSheetToCurrentPage(_updateRequiredSheet);
            _updateRequiredSheet.Show(message.Result);
        });
    }

    private void AttachSheetToCurrentPage(ContentView sheet)
    {
        if (sheet.Parent is not null) return;
        if (CurrentPage is ContentPage contentPage && contentPage.Content is Layout layout)
            layout.Children.Add(sheet);
    }
}
