using MyVocaList.UI.ViewModels;

namespace MyVocaList;

public partial class AppShell : Shell
{
    public AppShell(AppShellViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
