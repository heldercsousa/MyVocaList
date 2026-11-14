using Microsoft.Extensions.Logging.Abstractions;
using MyVocaList.Domain.Rules;

namespace MyVocaList.Tests.Unit.Services;

public class ImageStoreVenueTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ImageStoreVenueTests-" + Guid.NewGuid().ToString("N"));
    private readonly FakeProcessor _processor = new();

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private ImageStore CreateSut() => new(new ImageStoreOptions(_root), _processor, NullLogger<ImageStore>.Instance);

    private static MemoryStream Photo(int bytes = 1000) => new(new byte[bytes]);

    private string FilePath(string folder, string name, string suffix) => Path.Combine(_root, folder, name + suffix);

    private string Touch(string folder, string name, string suffix, TimeSpan age)
    {
        var path = FilePath(folder, name, suffix);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, [1, 2, 3]);
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow - age);
        return path;
    }

    private static string NameOf(string reference) => reference[(reference.LastIndexOf('/') + 1)..];

    private bool AnyFileUnder(string folder)
    {
        var directory = Path.Combine(_root, folder);
        return Directory.Exists(directory) && Directory.EnumerateFiles(directory).Any();
    }

    [Fact]
    // [AC] REQ-VEN-02: a venue picture is written as two files under venues/ and referenced as local:venues/<32 hex>.
    public async Task ImportAsync_Venue_WritesBothFilesUnderVenuesAndReturnsVenueReference()
    {
        var (success, message, reference) = await CreateSut().ImportAsync(Photo(), ImageOwnerKind.Venue);

        Assert.True(success);
        Assert.Equal(string.Empty, message);
        Assert.NotNull(reference);
        Assert.Matches("^local:venues/[0-9a-f]{32}$", reference);
        Assert.True(File.Exists(FilePath("venues", NameOf(reference), "_t.jpg")));
        Assert.True(File.Exists(FilePath("venues", NameOf(reference), "_l.jpg")));
    }

    [Fact]
    // [AC] REQ-VEN-02: a venue picture the processor cannot decode fails with the shared message and leaves no file under venues/.
    public async Task ImportAsync_Venue_ProcessorThrows_FailsWithMessageAndLeavesNoVenueFile()
    {
        _processor.ThrowOnCall = true;

        var (success, message, reference) = await CreateSut().ImportAsync(Photo(), ImageOwnerKind.Venue);

        Assert.False(success);
        Assert.Equal("Could not use this picture.", message);
        Assert.Null(reference);
        Assert.False(AnyFileUnder("venues"));
    }

    [Fact]
    // [AC] REQ-VEN-02: a venue reference resolves to its thumbnail path or its large path under venues/.
    public void ResolveDisplaySource_VenueReference_UsesVenuesFolder()
    {
        const string reference = "local:venues/0123456789abcdef0123456789abcdef";

        Assert.Equal(
            Path.Combine(_root, "venues", "0123456789abcdef0123456789abcdef_t.jpg"),
            CreateSut().ResolveDisplaySource(reference, ImageSize.Thumbnail));
        Assert.Equal(
            Path.Combine(_root, "venues", "0123456789abcdef0123456789abcdef_l.jpg"),
            CreateSut().ResolveDisplaySource(reference, ImageSize.Large));
    }

    [Fact]
    // [AC] REQ-VEN-04: a venue picture referenced by a venue survives the sweep even when it is older than the minimum age.
    public async Task DeleteUnreferencedAsync_LiveVenueReference_KeepsBothFiles()
    {
        const string name = "0123456789abcdef0123456789abcdef";
        var thumb = Touch("venues", name, "_t.jpg", TimeSpan.FromDays(30));
        var large = Touch("venues", name, "_l.jpg", TimeSpan.FromDays(30));

        var deleted = await CreateSut().DeleteUnreferencedAsync(["local:venues/" + name], TimeSpan.FromHours(24));

        Assert.Equal(0, deleted);
        Assert.True(File.Exists(thumb));
        Assert.True(File.Exists(large));
    }

    [Fact]
    // [AC] REQ-VEN-04: an unreferenced venue picture older than the minimum age is deleted and counted.
    public async Task DeleteUnreferencedAsync_OldUnreferencedVenueFile_IsDeletedAndCounted()
    {
        var thumb = Touch("venues", "0123456789abcdef0123456789abcdef", "_t.jpg", TimeSpan.FromHours(48));
        var large = Touch("venues", "0123456789abcdef0123456789abcdef", "_l.jpg", TimeSpan.FromHours(48));

        var deleted = await CreateSut().DeleteUnreferencedAsync([], TimeSpan.FromHours(24));

        Assert.Equal(2, deleted);
        Assert.False(File.Exists(thumb));
        Assert.False(File.Exists(large));
    }

    [Fact]
    // [AC] REQ-VEN-04: an unreferenced venue picture younger than the minimum age survives the sweep.
    public async Task DeleteUnreferencedAsync_YoungUnreferencedVenueFile_IsKept()
    {
        var thumb = Touch("venues", "fedcba9876543210fedcba9876543210", "_t.jpg", TimeSpan.FromHours(1));

        var deleted = await CreateSut().DeleteUnreferencedAsync([], TimeSpan.FromHours(24));

        Assert.Equal(0, deleted);
        Assert.True(File.Exists(thumb));
    }

    [Fact]
    // [AC] REQ-VEN-04: song and artist files keep following the same rules in the same sweep as venue files.
    public async Task DeleteUnreferencedAsync_SongsArtistsAndVenues_SameRulesInOneCall()
    {
        const string live = "11111111111111111111111111111111";
        const string orphan = "22222222222222222222222222222222";
        var liveVenue = Touch("venues", live, "_l.jpg", TimeSpan.FromDays(3));
        var orphanVenue = Touch("venues", orphan, "_t.jpg", TimeSpan.FromDays(3));
        var liveSong = Touch("songs", live, "_t.jpg", TimeSpan.FromDays(3));
        var orphanSong = Touch("songs", orphan, "_l.jpg", TimeSpan.FromDays(3));
        var orphanArtist = Touch("artists", orphan, "_t.jpg", TimeSpan.FromDays(3));

        var deleted = await CreateSut().DeleteUnreferencedAsync(
            ["local:venues/" + live, "local:songs/" + live], TimeSpan.FromHours(24));

        Assert.Equal(3, deleted);
        Assert.True(File.Exists(liveVenue));
        Assert.False(File.Exists(orphanVenue));
        Assert.True(File.Exists(liveSong));
        Assert.False(File.Exists(orphanSong));
        Assert.False(File.Exists(orphanArtist));
    }

    private sealed class FakeProcessor : IImageProcessor
    {
        public List<(int Side, float Quality)> Calls { get; } = [];
        public bool ThrowOnCall { get; set; }

        public byte[] CropSquareJpeg(byte[] source, int maxSidePixels, float quality)
        {
            if (ThrowOnCall)
                throw new InvalidOperationException("cannot decode");
            Calls.Add((maxSidePixels, quality));
            return [0xFF, 0xD8, 0xFF, (byte)(maxSidePixels / 4)];
        }
    }
}
