using Microsoft.Extensions.Logging.Abstractions;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

public class ImageMaintenanceServiceTests
{
    private readonly Mock<IImageStore> _store = new();
    private readonly Mock<ISongRepository> _songs = new();
    private readonly Mock<IArtistRepository> _artists = new();

    private ImageMaintenanceService CreateSut() => new(
        _songs.Object,
        _artists.Object,
        _store.Object,
        PassthroughUnitOfWork.Over(_songs, _artists),
        NullLogger<ImageMaintenanceService>.Instance);

    [Fact]
    // [AC] REQ-PIC-11: the live references are the union of the song and artist references; the age is 24 hours.
    public async Task SweepOrphansAsync_PassesUnionOfReferencesAnd24Hours()
    {
        const string a = "local:songs/0123456789abcdef0123456789abcdef";
        const string b = "local:artists/fedcba9876543210fedcba9876543210";
        _songs.Setup(r => r.GetLocalImageReferencesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([a]);
        _artists.Setup(r => r.GetLocalImageReferencesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([b]);
        IReadOnlyCollection<string>? passed = null;
        _store.Setup(s => s.DeleteUnreferencedAsync(It.IsAny<IReadOnlyCollection<string>>(), TimeSpan.FromHours(24), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<string>, TimeSpan, CancellationToken>((refs, _, _) => passed = refs)
            .ReturnsAsync(5);

        var deleted = await CreateSut().SweepOrphansAsync();

        Assert.Equal(5, deleted);
        Assert.NotNull(passed);
        Assert.Contains(a, passed);
        Assert.Contains(b, passed);
        Assert.Equal(2, passed.Count);
    }

    [Fact]
    // [AC] REQ-PIC-11: a reference used by both a song and an artist is passed once.
    public async Task SweepOrphansAsync_DuplicateReference_IsPassedOnce()
    {
        const string a = "local:songs/0123456789abcdef0123456789abcdef";
        _songs.Setup(r => r.GetLocalImageReferencesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([a]);
        _artists.Setup(r => r.GetLocalImageReferencesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([a]);
        IReadOnlyCollection<string>? passed = null;
        _store.Setup(s => s.DeleteUnreferencedAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<string>, TimeSpan, CancellationToken>((refs, _, _) => passed = refs)
            .ReturnsAsync(0);

        await CreateSut().SweepOrphansAsync();

        Assert.Single(passed!);
    }
}
