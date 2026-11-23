namespace MyVocaList.UI.Pages.BackupRestore;

public partial class BackupRestoreSfPage : ContentPage
{
    public BackupRestoreSfPage(BackupRestoreViewModel vm)
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
