using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;

namespace MyVocaList.Tests.Unit.Services;

public class NextSingerAlertServiceTests
{
    private readonly Mock<INotificationService> _notifMock = new();
    private readonly Mock<ILogger<NextSingerAlertService>> _loggerMock = new();

    private NextSingerAlertService CreateSut()
        => new(_notifMock.Object, _loggerMock.Object);

    [Fact]
    // [AC] AC-4.2: null duration → no notifications scheduled
    public async Task ScheduleAlertsAsync_NullDuration_DoesNotSchedule()
    {
        var sut = CreateSut();

        await sut.ScheduleAlertsAsync("Alice", "My Song", durationSeconds: null);

        _notifMock.Verify(n => n.Show(It.IsAny<NotificationRequest>()), Times.Never);
    }

    [Fact]
    // [AC] AC-4.2: duration ≤ 15s → both stages skipped
    public async Task ScheduleAlertsAsync_DurationTooShort_DoesNotSchedule()
    {
        var sut = CreateSut();

        await sut.ScheduleAlertsAsync("Alice", "My Song", durationSeconds: 10);

        _notifMock.Verify(n => n.Show(It.IsAny<NotificationRequest>()), Times.Never);
    }

    [Fact]
    // [AC] AC-4.1: duration > 45s → both stages scheduled
    public async Task ScheduleAlertsAsync_NormalDuration_SchedulesBothStages()
    {
        _notifMock.Setup(n => n.Show(It.IsAny<NotificationRequest>())).ReturnsAsync(true);
        var sut = CreateSut();

        await sut.ScheduleAlertsAsync("Alice", "My Song", durationSeconds: 180);

        _notifMock.Verify(n => n.Show(It.IsAny<NotificationRequest>()), Times.Exactly(2));
    }

    [Fact]
    // [AC] AC-4.1: 15 < duration ≤ 45s → stage 1 skipped, stage 2 scheduled
    public async Task ScheduleAlertsAsync_BetweenEdges_SchedulesOnlyStage2()
    {
        _notifMock.Setup(n => n.Show(It.IsAny<NotificationRequest>())).ReturnsAsync(true);
        var sut = CreateSut();

        await sut.ScheduleAlertsAsync("Alice", "My Song", durationSeconds: 30);

        _notifMock.Verify(n => n.Show(It.IsAny<NotificationRequest>()), Times.Once);
    }

    [Fact]
    // [AC] AC-4.6: CancelAlertsAsync cancels both pending notifications
    public async Task CancelAlertsAsync_CancelsBothIds()
    {
        var sut = CreateSut();

        await sut.CancelAlertsAsync();

        _notifMock.Verify(n => n.Cancel(NextSingerAlertService.Stage1NotificationId), Times.Once);
        _notifMock.Verify(n => n.Cancel(NextSingerAlertService.Stage2NotificationId), Times.Once);
    }
}
