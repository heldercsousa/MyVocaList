namespace MyVocaList.UI.Pages.Samples;

public partial class SamplesMenuPage : ContentPage
{
    public SamplesMenuPage()
    {
        InitializeComponent();
    }

    private async void OnSwipeContainerClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("sample/swipe");
    }

    private async void OnDragDropClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("sample/dragdrop");
    }

    private async void OnFilterChipsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("sample/filterchips");
    }

    private async void OnEditFormClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("sample/editform");
    }

    private async void OnComboBoxEditorClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("sample/combobox");
    }

    private async void OnEditorsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("sample/editors");
    }

    private async void OnTabViewClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("sample/tabview");
    }

    private async void OnPopupClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("sample/popup");
    }

    private async void OnBottomSheetClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("sample/bottomsheet");
    }
}
