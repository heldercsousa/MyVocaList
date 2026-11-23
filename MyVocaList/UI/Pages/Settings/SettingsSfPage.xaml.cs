namespace MyVocaList.UI.Pages.Settings;

public partial class SettingsSfPage : ContentPage
{
    public SettingsSfPage(SettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is SettingsViewModel vm)
            await vm.InitializeAsync();
    }
}
