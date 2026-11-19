namespace MyVocaList.Tests.Unit.Services;

public class NullWhatsNewServiceTests
{
    // [AC] AC-AB-07: What's New section hidden when no release entry — stub always returns null
    [Fact]
    public async Task GetCurrentReleaseAsync_AlwaysReturnsNull()
    {
        var sut = new NullWhatsNewService();

        var result = await sut.GetCurrentReleaseAsync();

        Assert.Null(result);
    }

    // [AC] AC-AB-09: No network dependency — stub never throws
    [Fact]
    public async Task GetCurrentReleaseAsync_WithCancellationToken_DoesNotThrow()
    {
        var sut = new NullWhatsNewService();
        using var cts = new CancellationTokenSource();

        var exception = await Record.ExceptionAsync(() => sut.GetCurrentReleaseAsync(cts.Token));

        Assert.Null(exception);
    }
}
