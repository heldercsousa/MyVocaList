using MyVocaList.UI.ViewModels;

namespace MyVocaList;

public partial class AppShell : Shell
{
    public AppShell()
    {
        BindingContext = new AppShellViewModel();
        InitializeComponent();
    }
}
