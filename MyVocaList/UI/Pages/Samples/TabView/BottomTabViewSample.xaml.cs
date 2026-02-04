using DevExpress.Maui.Controls;
using DxTabView = DevExpress.Maui.Controls.TabView;

namespace MyVocaList.UI.Pages.Samples.TabView;

public partial class BottomTabViewSample : ContentPage
{
    public BottomTabViewSample()
    {
        InitializeComponent();
    }

    private void OnSelectedItemChanged(object? sender, EventArgs e)
    {
        if (sender is DxTabView dxTabView && dxTabView.SelectedItem is TabItem selectedTab)
        {
            Console.WriteLine($"Selected Tab: {selectedTab.Title}");
        }
    }
}