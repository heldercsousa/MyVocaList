using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services;

public class NextSingerAlertService : INextSingerAlertService
{
    public const int Stage1NotificationId = 9001;
    public const int Stage2NotificationId = 9002;

    private readonly INotificationService _notifications;
    private readonly ILogger<NextSingerAlertService> _logger;

    public NextSingerAlertService(
        INotificationService notifications,
        ILogger<NextSingerAlertService> logger)
    {
        _notifications = notifications;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ScheduleAlertsAsync(
        string singerName,
        string songTitle,
        int? durationSeconds,
        CancellationToken ct = default)
    {
        if (durationSeconds is null or <= 15)
        {
            if (durationSeconds is not null)
                _logger.LogWarning("Duration {Seconds}s too short for alerts; skipping", durationSeconds);
            return;
        }

        var now = DateTime.Now;

        if (durationSeconds > 45)
        {
            var stage1 = new NotificationRequest
            {
                NotificationId = Stage1NotificationId,
                Title = $"Next up — {singerName}",
                Description = $"{songTitle} · preparing in ~45s",
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = now.AddSeconds(durationSeconds.Value - 45)
                },
                Android = new AndroidOptions { Priority = AndroidPriority.Default }
            };
            await _notifications.Show(stage1);
        }

        var stage2 = new NotificationRequest
        {
            NotificationId = Stage2NotificationId,
            Title = $"Next up — {singerName} — mic now!",
            Description = $"{songTitle} · ~15s remaining",
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = now.AddSeconds(durationSeconds.Value - 15)
            },
            Android = new AndroidOptions { Priority = AndroidPriority.High }
        };
        await _notifications.Show(stage2);
    }

    /// <inheritdoc />
    public Task CancelAlertsAsync(CancellationToken ct = default)
    {
        _notifications.Cancel(Stage1NotificationId);
        _notifications.Cancel(Stage2NotificationId);
        return Task.CompletedTask;
    }
}
