using DevExpress.Maui.Controls;
using MyVocaList.Contracts.Enums;
using Event = MyVocaList.Domain.Entities.Event;
using Venue = MyVocaList.Domain.Entity.Venue;

namespace MyVocaList.Tests.Unit.ViewModels;

public class QueueManagementViewModelTests
{
    private readonly Mock<IEventService> _eventServiceMock = new();
    private readonly Mock<IQueueServiceNew> _queueServiceMock = new();
    private readonly Mock<IPersonRepository> _personRepoMock = new();
    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<ISnackbarComponent> _snackMock = new();
    private readonly Mock<ILogger<QueueManagementViewModel>> _loggerMock = new();

    private QueueManagementViewModel CreateSut() =>
        new(_eventServiceMock.Object,
            _queueServiceMock.Object,
            _personRepoMock.Object,
            _songRepoMock.Object,
            _snackMock.Object,
            _loggerMock.Object);

    // [AC] AC-5.3: Finishing an event requires a confirmation; tapping "Finish Event"
    // opens the confirmation and does NOT finish the event by itself.
    [Fact]
    public void RequestFinishEvent_StartedEvent_OpensConfirmationWithoutFinishing()
    {
        var sut = CreateSut();
        sut.CurrentEvent = new Event { Id = 1, Name = "Test Event", Status = EventStatus.Started, Venue = new Venue { Id = 1, Name = "Test Venue" } };

        sut.RequestFinishEventCommand.Execute(null);

        Assert.Equal(BottomSheetState.HalfExpanded, sut.FinishConfirmSheetState);
        _eventServiceMock.Verify(
            s => s.FinishEventAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // [AC] AC-5.3: Only the confirmed path transitions the event STARTED -> FINISHED
    // and closes the confirmation sheet.
    [Fact]
    public async Task FinishEvent_AfterConfirmation_FinishesEventAndClosesSheet()
    {
        _eventServiceMock
            .Setup(s => s.FinishEventAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Event finished"));
        var sut = CreateSut();
        sut.CurrentEvent = new Event { Id = 1, Name = "Test Event", Status = EventStatus.Started, Venue = new Venue { Id = 1, Name = "Test Venue" } };
        sut.RequestFinishEventCommand.Execute(null); // open confirmation first

        await sut.FinishEventCommand.ExecuteAsync(null); // confirmed path

        Assert.Equal(BottomSheetState.Hidden, sut.FinishConfirmSheetState);
        Assert.False(sut.IsEventStarted);
        _eventServiceMock.Verify(
            s => s.FinishEventAsync(1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // [AC] AC-5.3: Dismissing the confirmation leaves the event unchanged (not finished).
    [Fact]
    public void DismissFinishConfirm_AfterOpening_ClosesSheetWithoutFinishing()
    {
        var sut = CreateSut();
        sut.CurrentEvent = new Event { Id = 1, Name = "Test Event", Status = EventStatus.Started, Venue = new Venue { Id = 1, Name = "Test Venue" } };
        sut.RequestFinishEventCommand.Execute(null);

        sut.DismissFinishConfirmCommand.Execute(null);

        Assert.Equal(BottomSheetState.Hidden, sut.FinishConfirmSheetState);
        _eventServiceMock.Verify(
            s => s.FinishEventAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
