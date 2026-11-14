using Microsoft.Extensions.Logging.Abstractions;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Unit.Services;

public class ImageMaintenanceServiceVenueTests
{
    private readonly Mock<IImageStore> _store = new();
    private readonly Mock<ISongRepository> _songs = new();
    private readonly Mock<IArtistRepository> _artists = new();
    private readonly Mock<IVenueRepository> _venues = new();

    private ImageMaintenanceService CreateSut() => new(
        _songs.Object,
        _artists.Object,
        _venues.Object,
        _store.Object,
        PassthroughUnitOfWork.Over(_songs, _artists, _venues),
        NullLogger<ImageMaintenanceService>.Instance);

    [Fact]
    // [AC] REQ-VEN-04: the live references are the union of the song, artist and venue references; the age is 24 hours.
    public async Task SweepOrphansAsync_PassesUnionOfSongArtistAndVenueReferencesAnd24Hours()
    {
        const string song = "local:songs/0123456789abcdef0123456789abcdef";
        const string artist = "local:artists/fedcba9876543210fedcba9876543210";
        const string venue = "local:venues/11111111111111111111111111111111";
        _songs.Setup(r => r.GetLocalImageReferencesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([song]);
        _artists.Setup(r => r.GetLocalImageReferencesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([artist]);
        _venues.Setup(r => r.GetLocalImageReferencesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([venue]);
        IReadOnlyCollection<string>? passed = null;
        _store.Setup(s => s.DeleteUnreferencedAsync(It.IsAny<IReadOnlyCollection<string>>(), TimeSpan.FromHours(24), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<string>, TimeSpan, CancellationToken>((refs, _, _) => passed = refs)
            .ReturnsAsync(4);

        var deleted = await CreateSut().SweepOrphansAsync();

        Assert.Equal(4, deleted);
        Assert.NotNull(passed);
        Assert.Contains(song, passed);
        Assert.Contains(artist, passed);
        Assert.Contains(venue, passed);
        Assert.Equal(3, passed.Count);
        _store.Verify(s => s.DeleteUnreferencedAsync(It.IsAny<IReadOnlyCollection<string>>(), TimeSpan.FromHours(24), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-VEN-04: when no venue has a picture, the song and artist references reach the store unchanged.
    public async Task SweepOrphansAsync_NoVenueReferences_SongAndArtistReferencesUnchanged()
    {
        const string song = "local:songs/0123456789abcdef0123456789abcdef";
        const string artist = "local:artists/fedcba9876543210fedcba9876543210";
        _songs.Setup(r => r.GetLocalImageReferencesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([song]);
        _artists.Setup(r => r.GetLocalImageReferencesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([artist]);
        _venues.Setup(r => r.GetLocalImageReferencesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        IReadOnlyCollection<string>? passed = null;
        _store.Setup(s => s.DeleteUnreferencedAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<string>, TimeSpan, CancellationToken>((refs, _, _) => passed = refs)
            .ReturnsAsync(0);

        await CreateSut().SweepOrphansAsync();

        Assert.NotNull(passed);
        Assert.Equal(2, passed.Count);
        Assert.Contains(song, passed);
        Assert.Contains(artist, passed);
    }
}
