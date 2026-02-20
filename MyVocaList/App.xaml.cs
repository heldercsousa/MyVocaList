namespace MyVocaList
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;

        public App(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            InitializeComponent();
            _ = WarmUpDevExpressAsync();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // AppShell is resolved after InitializeComponent() so that Application.Resources
            // already contains MaterialColors.xaml when AppShell.InitializeComponent() runs.
            return new Window(_serviceProvider.GetRequiredService<AppShell>());
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
