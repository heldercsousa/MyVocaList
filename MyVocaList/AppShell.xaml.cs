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
        Routing.RegisterRoute(Routes.VenueMapPicker, typeof(VenueMapPickerPage));
        Routing.RegisterRoute(Routes.PersonForm, typeof(PersonFormPage));
        Routing.RegisterRoute(Routes.ArtistForm, typeof(ArtistFormPage));
        Routing.RegisterRoute(Routes.SongForm, typeof(SongFormPage));
        Routing.RegisterRoute(Routes.YouTubePlayer, typeof(YouTubePlayerPage));
        Routing.RegisterRoute(Routes.Feedback, typeof(FeedbackPage));

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
        var logger = IPlatformApplication.Current?.Services.GetService<ILogger<AppShell>>();

        if (CurrentPage is not ContentPage page || page.Content is null)
        {
            logger?.LogWarning("Cannot attach {Sheet}: current page is {Page} with no content", sheet.GetType().Name, CurrentPage?.GetType().Name ?? "null");
            return;
        }

        // Pages whose root is not a Layout (e.g. an SfBottomSheet host) are wrapped in a Grid so the sheet can overlay them.
        if (page.Content is not Layout host)
        {
            var root = page.Content;
            page.Content = null;
            host = new Grid();
            host.Children.Add(root);
            page.Content = host;
        }

        if (ReferenceEquals(sheet.Parent, host)) return;
        if (sheet.Parent is Layout oldHost) oldHost.Children.Remove(sheet);

        host.Children.Add(sheet);
    }
}
