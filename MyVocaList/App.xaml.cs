using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Infra;

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
            _ = MigrateAsync();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // AppShell is resolved after InitializeComponent() so that Application.Resources
            // already contains MaterialColors.xaml when AppShell.InitializeComponent() runs.
            var window = new Window(_serviceProvider.GetRequiredService<AppShell>());
            window.Stopped += OnWindowStopped;
            return window;
        }

        private void OnWindowStopped(object? sender, EventArgs e)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();
                    await backupService.CreateFullBackupAsync(BackupTrigger.AppStop, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    // GlobalExceptionHandler will catch unobserved — log only
                    System.Diagnostics.Debug.WriteLine($"Auto-backup on stop failed: {ex.Message}");
                }
            });
        }

        private async Task MigrateAsync()
        {
            // Run EF Core migrations off the main thread so Android startup is never blocked.
            // Clear any stale __EFMigrationsLock row first; EF Core 9+ spins forever if it exists.
            // SQLite on mobile is single-user so no concurrent migration concern.
            await Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM __EFMigrationsLock");
                }
                catch { /* Table does not exist on first run — safe to ignore. */ }

                await dbContext.Database.MigrateAsync();
            });
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
