namespace MyVocaList.UI.Pages.Songs;

public partial class SongFormPage : ContentPage
{
    public SongFormPage(SongFormViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        titleEdit.Focus();
    }
}
