using MyVocaList.UI.Pages.Queue;
using MyVocaList.UI.ViewModels;

namespace MyVocaList;

public partial class AppShell : Shell
{
    public AppShell(AppShellViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();

        viewModel.ExitRequested += OnExitRequested;
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
}
