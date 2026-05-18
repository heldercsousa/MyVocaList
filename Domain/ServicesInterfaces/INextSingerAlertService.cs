namespace MyVocaList.Domain.ServicesInterfaces;

public interface INextSingerAlertService
{
    /// <summary>
    /// Schedules Stage 1 (T-45s) and Stage 2 (T-15s) local notifications.
    /// No-op when durationSeconds is null or too short.
    /// </summary>
    Task ScheduleAlertsAsync(
        string singerName,
        string songTitle,
        int? durationSeconds,
        CancellationToken ct = default);

    /// <summary>Cancels any pending Stage 1 and Stage 2 notifications.</summary>
    Task CancelAlertsAsync(CancellationToken ct = default);
}
