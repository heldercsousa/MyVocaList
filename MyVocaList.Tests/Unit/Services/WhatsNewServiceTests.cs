using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using MyVocaList.Contracts.DTOs;

namespace MyVocaList.Tests.Unit.Services;

public class WhatsNewServiceTests
{
    private readonly Mock<IPreferences> _prefsMock = new();
    private readonly Mock<IAppInfo> _appInfoMock = new();
    private readonly Mock<IFileSystem> _fsMock = new();
    private readonly Mock<ILogger<WhatsNewService>> _loggerMock = new();

    private const string ValidJson = """
        [
          {
            "version": "1.2.0",
            "date": "2026-06-01",
            "highlights": ["New queue management"],
            "fixes": ["Fixed crash on empty list"]
          }
        ]
        """;

    private WhatsNewService CreateSut() =>
        new(_prefsMock.Object, _appInfoMock.Object, _fsMock.Object, _loggerMock.Object);

    private void SetupVersion(string version) =>
        _appInfoMock.Setup(a => a.VersionString).Returns(version);

    private void SetupLastSeen(string? value) =>
        _prefsMock.Setup(p => p.Get("last_seen_version", null as string, null))
                  .Returns(value);

    private void SetupReleasesJson(string json)
    {
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        _fsMock.Setup(f => f.OpenAppPackageFileAsync("releases.json"))
               .ReturnsAsync(stream);
    }

    // [AC] AC-WN-02: No modal on fresh install
    [Fact]
    public async Task GetPendingReleaseAsync_FreshInstall_ReturnsNullAndStoresVersion()
    {
        SetupVersion("1.2.0");
        SetupLastSeen(null);
        var sut = CreateSut();

        var result = await sut.GetPendingReleaseAsync();

        Assert.Null(result);
        _prefsMock.Verify(p => p.Set("last_seen_version", "1.2.0", null), Times.Once);
    }

    // [AC] AC-WN-01: Modal not shown again for same version
    [Fact]
    public async Task GetPendingReleaseAsync_SameVersion_ReturnsNull()
    {
        SetupVersion("1.2.0");
        SetupLastSeen("1.2.0");
        var sut = CreateSut();

        var result = await sut.GetPendingReleaseAsync();

        Assert.Null(result);
    }

    // [AC] AC-WN-01: Modal shown on version upgrade with matching entry
    [Fact]
    public async Task GetPendingReleaseAsync_VersionUpgradeWithEntry_ReturnsEntry()
    {
        SetupVersion("1.2.0");
        SetupLastSeen("1.1.0");
        SetupReleasesJson(ValidJson);
        var sut = CreateSut();

        var result = await sut.GetPendingReleaseAsync();

        Assert.NotNull(result);
        Assert.Equal("1.2.0", result.Version);
        Assert.Equal("2026-06-01", result.Date);
        Assert.Single(result.Highlights);
        Assert.Single(result.Fixes);
    }

    // [AC] AC-WN-03: No modal when no entry for current version
    [Fact]
    public async Task GetPendingReleaseAsync_VersionUpgradeNoMatchingEntry_ReturnsNull()
    {
        SetupVersion("9.9.9");
        SetupLastSeen("1.1.0");
        SetupReleasesJson(ValidJson);
        var sut = CreateSut();

        var result = await sut.GetPendingReleaseAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPendingReleaseAsync_MalformedJson_ReturnsNull()
    {
        SetupVersion("1.2.0");
        SetupLastSeen("1.1.0");
        SetupReleasesJson("not valid json {{{{");
        var sut = CreateSut();

        var result = await sut.GetPendingReleaseAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPendingReleaseAsync_MissingFile_ReturnsNull()
    {
        SetupVersion("1.2.0");
        SetupLastSeen("1.1.0");
        _fsMock.Setup(f => f.OpenAppPackageFileAsync("releases.json"))
               .ThrowsAsync(new FileNotFoundException());
        var sut = CreateSut();

        var result = await sut.GetPendingReleaseAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCurrentReleaseAsync_AlreadySeen_StillReturnsEntry()
    {
        SetupVersion("1.2.0");
        SetupLastSeen("1.2.0");
        SetupReleasesJson(ValidJson);
        var sut = CreateSut();

        var result = await sut.GetCurrentReleaseAsync();

        Assert.NotNull(result);
        Assert.Equal("1.2.0", result.Version);
    }

    // [AC] AC-WN-05: Dismiss persists version
    [Fact]
    public void MarkCurrentVersionSeen_StoresCurrentVersion()
    {
        SetupVersion("1.2.0");
        var sut = CreateSut();

        sut.MarkCurrentVersionSeen();

        _prefsMock.Verify(p => p.Set("last_seen_version", "1.2.0", null), Times.Once);
    }
}
