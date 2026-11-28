using Microsoft.Extensions.Logging.Abstractions;
using MyVocaList.UI.ViewModels;

namespace MyVocaList.Tests.Unit.ViewModels;

public class SettingsViewModelTests
{
    // [AC] REQ-MSRC-04: no API key setting remains; a previously stored key is removed
    [Fact]
    public async Task InitializeAsync_RemovesStoredYouTubeKey()
    {
        var secureStorage = new Mock<ISecureStorageWrapper>();
        var sut = new SettingsViewModel(secureStorage.Object, NullLogger<SettingsViewModel>.Instance);

        await sut.InitializeAsync();

        secureStorage.Verify(s => s.Remove("youtube_api_key"), Times.Once);
    }

    // [AC] REQ-MSRC-04: a keystore failure must not crash the page (BUG-020 carry-over)
    [Fact]
    public async Task InitializeAsync_SecureStorageThrows_DoesNotThrow()
    {
        var secureStorage = new Mock<ISecureStorageWrapper>();
        secureStorage.Setup(s => s.Remove(It.IsAny<string>())).Throws(new InvalidOperationException("keystore"));
        var sut = new SettingsViewModel(secureStorage.Object, NullLogger<SettingsViewModel>.Instance);

        var ex = await Record.ExceptionAsync(() => sut.InitializeAsync());

        Assert.Null(ex);
    }
}
