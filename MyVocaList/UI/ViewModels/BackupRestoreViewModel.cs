using MyVocaList.Domain.Entity;

namespace MyVocaList.UI.ViewModels;

public partial class BackupRestoreViewModel : ViewModelBase
{
    private readonly IBackupService _backupService;
    private readonly ISnackbarComponent _snackbar;
    private readonly ILogger<BackupRestoreViewModel> _logger;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _lastBackupLabel = "No backups yet";
    [ObservableProperty] private ObservableCollection<BackupHistory> _history = [];

    public BackupRestoreViewModel(
        IBackupService backupService,
        ISnackbarComponent snackbar,
        ILogger<BackupRestoreViewModel> logger)
    {
        _backupService = backupService;
        _snackbar = snackbar;
        _logger = logger;

        BackupNowCommand = new AsyncRelayCommand(BackupNowAsync);
        ExportCommand = new AsyncRelayCommand(ExportAsync);
        RestoreCommand = new AsyncRelayCommand(RestoreAsync);
    }

    public IAsyncRelayCommand BackupNowCommand { get; }
    public IAsyncRelayCommand ExportCommand { get; }
    public IAsyncRelayCommand RestoreCommand { get; }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            var entries = await _backupService.GetHistoryAsync(10, CancellationToken.None);
            RunOnUiThread(() =>
            {
                History.Clear();
                foreach (var e in entries) History.Add(e);
                LastBackupLabel = entries.Count > 0
                    ? $"Last backup: {entries[0].CreatedAt.ToLocalTime():g} — {entries[0].TriggerType}"
                    : "No backups yet";
            });
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task BackupNowAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _backupService.CreateFullBackupAsync(BackupTrigger.Manual, CancellationToken.None);
            if (result.Success)
                await _snackbar.ShowSuccessAsync("Backup created successfully.");
            else
                await _snackbar.ShowErrorAsync(result.Message);
            await InitializeAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExportAsync()
    {
        var (success, message) = await _backupService.ExportBundleAsync(CancellationToken.None);
        if (!success)
            await _snackbar.ShowErrorAsync(message);
    }

    private async Task RestoreAsync()
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Select backup file",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, ["application/zip", "application/octet-stream"] }
                })
            });

            if (result is null) return;

            IsLoading = true;
            var (success, message) = await _backupService.RestoreFromBundleAsync(result.FullPath, CancellationToken.None);
            if (success)
                await _snackbar.ShowSuccessAsync(message);
            else
                await _snackbar.ShowErrorAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restore flow failed");
            await _snackbar.ShowErrorAsync("Could not open backup file.");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
