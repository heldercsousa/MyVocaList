using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace MyVocaList
{
    public partial class App : Application
    {
        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            // AppShell is resolved after InitializeComponent() so that Application.Resources
            // already contains MaterialColors.xaml when AppShell.InitializeComponent() runs.
            MainPage = serviceProvider.GetRequiredService<AppShell>();

            _ = WarmUpDevExpressAsync();
        }

        private async Task WarmUpDevExpressAsync()
        {
            try
            {
                await Task.Delay(200);

                Application.Current?.Dispatcher?.Dispatch(() =>
                {
                    try
                    {
                        _ = typeof(DevExpress.Maui.CollectionView.DXCollectionView);

                        var cv = new DevExpress.Maui.CollectionView.DXCollectionView
                        {
                            ItemsSource = new[] { string.Empty },
                            HeightRequest = 1,
                            WidthRequest = 1,
                            IsVisible = false
                        };

                        var grid = new Microsoft.Maui.Controls.Grid { IsVisible = false };
                        grid.Children.Add(cv);
                        grid.Children.Clear();
                    }
                    catch
                    {
                        // Fail silently
                    }
                });
            }
            catch
            {
                // ignore
            }
        }
    }
}
