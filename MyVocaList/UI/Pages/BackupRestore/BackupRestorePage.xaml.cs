namespace MyVocaList.UI.Pages.BackupRestore;

public partial class BackupRestorePage : ContentPage
{
    public BackupRestorePage(BackupRestoreViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is BackupRestoreViewModel vm)
            await vm.InitializeAsync();
    }
}
