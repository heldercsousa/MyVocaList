using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.UI.Components.Sheets;
using MyVocaList.UI.Messages;

namespace MyVocaList;

public partial class AppShell : Shell
{
    private UpdateAvailableBottomSheet? _updateAvailableSheet;
    private UpdateRequiredBottomSheet? _updateRequiredSheet;

    public AppShell(AppShellViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();

        viewModel.ExitRequested += OnExitRequested;

        Routing.RegisterRoute(Routes.VenueForm, typeof(VenueFormPage));
        Routing.RegisterRoute(Routes.PersonForm, typeof(PersonFormPage));
        Routing.RegisterRoute(Routes.ArtistForm, typeof(ArtistFormPage));
        Routing.RegisterRoute(Routes.SongForm, typeof(SongFormPage));

        WeakReferenceMessenger.Default.Register<ShowUpdateAvailableMessage>(this, OnShowUpdateAvailable);
        WeakReferenceMessenger.Default.Register<ShowUpdateRequiredMessage>(this, OnShowUpdateRequired);

        // Fire-and-forget startup checks; exceptions are handled inside InitializeAsync.
        _ = viewModel.InitializeAsync();
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
