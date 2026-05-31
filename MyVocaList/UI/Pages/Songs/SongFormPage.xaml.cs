namespace MyVocaList.UI.Pages.Songs;

public partial class SongFormPage : ContentPage
{
    public SongFormPage(SongFormViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        titleEdit.Focus();
        if (BindingContext is SongFormViewModel vm)
            await vm.RefreshApiKeyFlagAsync();
    }
}
