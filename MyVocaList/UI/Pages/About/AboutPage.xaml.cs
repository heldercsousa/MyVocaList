namespace MyVocaList.UI.Pages.About;

public partial class AboutPage : ContentPage
{
    public AboutPage(AboutViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is AboutViewModel vm)
            await vm.InitializeAsync();
    }
}
