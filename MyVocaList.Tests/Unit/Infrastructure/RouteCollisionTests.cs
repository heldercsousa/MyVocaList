using MyVocaList.Navigation;

namespace MyVocaList.Tests.Unit.Infrastructure;

public class RouteCollisionTests
{
    [Fact]
    // [AC] BUG-016: QueueSongPickerPage route must be distinct from SongPickerPage route to prevent Shell collision
    public void Routes_QueueSongPicker_IsDistinctFromSongPicker()
    {
        Assert.NotEqual(Routes.SongPicker, Routes.QueueSongPicker);
    }
}
