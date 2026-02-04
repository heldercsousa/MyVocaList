using DevExpress.Maui.Controls;
using DevExpress.Maui.Editors;

namespace MyVocaList.UI.Pages.Samples.BottomSheet;

public partial class BottomSheetSample : ContentPage
{
    private BottomSheetSampleViewModel ViewModel => (BottomSheetSampleViewModel)BindingContext;

    public BottomSheetSample()
    {
        InitializeComponent();
    }

    private void OnShowFilterBottomSheet(object? sender, EventArgs e)
    {
        filterBottomSheet.State = BottomSheetState.HalfExpanded;
    }

    private void OnShowActionBottomSheet(object? sender, EventArgs e)
    {
        actionBottomSheet.State = BottomSheetState.FullExpanded;
    }

    private void OnGenreChipSelected(object? sender, EventArgs e)
    {
        // ChipGroup SelectionChanged event handler
        // In production, you would track the selected genre and update filters accordingly
        Console.WriteLine("Genre chip selection changed");
    }

    private void OnClearFilters(object? sender, EventArgs e)
    {
        ViewModel.ClearFilters();
        filterBottomSheet.State = BottomSheetState.Hidden;
    }

    private void OnApplyFilters(object? sender, EventArgs e)
    {
        ViewModel.ApplyFilters();
        filterBottomSheet.State = BottomSheetState.Hidden;
    }

    private async void OnActionPlay(object? sender, EventArgs e)
    {
        actionBottomSheet.State = BottomSheetState.Hidden;
        await DisplayAlert("Action", "Playing next...", "OK");
    }

    private async void OnActionFavorite(object? sender, EventArgs e)
    {
        actionBottomSheet.State = BottomSheetState.Hidden;
        await DisplayAlert("Action", "Added to favorites!", "OK");
    }

    private async void OnActionShare(object? sender, EventArgs e)
    {
        actionBottomSheet.State = BottomSheetState.Hidden;
        await DisplayAlert("Action", "Sharing...", "OK");
    }

    private async void OnActionRemove(object? sender, EventArgs e)
    {
        actionBottomSheet.State = BottomSheetState.Hidden;
        bool confirmed = await DisplayAlert("Confirm", "Remove from queue?", "Yes", "No");
        if (confirmed)
        {
            await DisplayAlert("Removed", "Song removed from queue", "OK");
        }
    }
}