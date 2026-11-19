using Microsoft.Extensions.Logging.Abstractions;
using MyVocaList.Domain.Rules;

namespace MyVocaList.Tests.Unit.Services;

public class ImageStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ImageStoreTests-" + Guid.NewGuid().ToString("N"));
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

    [Fact]
    // [AC] REQ-PIC-09: two files are written in the owner's folder, named after the reference.
    public async Task ImportAsync_Song_WritesThumbnailAndLargeFiles()
    {
        var (success, message, reference) = await CreateSut().ImportAsync(Photo(), ImageOwnerKind.Song);

        Assert.True(success);
        Assert.Equal(string.Empty, message);
        Assert.NotNull(reference);
        var name = NameOf(reference);
        Assert.True(File.Exists(FilePath("songs", name, "_t.jpg")));
        Assert.True(File.Exists(FilePath("songs", name, "_l.jpg")));
        Assert.Empty(Directory.EnumerateFiles(Path.Combine(_root, "songs"), "*.tmp"));
    }

    [Fact]
    // [AC] REQ-PIC-09: an artist picture goes under artists/ with a well-formed local reference.
    public async Task ImportAsync_Artist_ReferenceIsWellFormedLocalArtistReference()
    {
        var (_, _, reference) = await CreateSut().ImportAsync(Photo(), ImageOwnerKind.Artist);

        Assert.NotNull(reference);
        Assert.StartsWith("local:artists/", reference);
        Assert.True(ImageReferenceRules.IsLocal(reference));
        Assert.True(File.Exists(FilePath("artists", NameOf(reference), "_l.jpg")));
    }

    [Fact]
    // [AC] REQ-PIC-09: every upload gets a new random name.
    public async Task ImportAsync_TwiceWithSameSource_GivesDistinctReferences()
    {
        var sut = CreateSut();

        var (_, _, first) = await sut.ImportAsync(Photo(), ImageOwnerKind.Song);
        var (_, _, second) = await sut.ImportAsync(Photo(), ImageOwnerKind.Song);

        Assert.NotEqual(first, second);
    }

    [Fact]
    // [AC] REQ-PIC-07: the thumbnail is 256 px at quality 0.75 and the large file 512 px at quality 0.80.
    public async Task ImportAsync_CallsProcessorWithBothSizesAndQualities()
    {
        await CreateSut().ImportAsync(Photo(), ImageOwnerKind.Song);

        Assert.Equal([(256, 0.75f), (512, 0.80f)], _processor.Calls);
    }

    [Fact]
    // [AC] REQ-PIC-10: a photo of exactly 25 MB is accepted.
    public async Task ImportAsync_ExactlyLimit_Succeeds()
    {
        var (success, _, _) = await CreateSut().ImportAsync(Photo((int)ImageStore.MaxSourceBytes), ImageOwnerKind.Song);

        Assert.True(success);
    }

    [Fact]
    // [AC] REQ-PIC-10: a photo over 25 MB is refused, nothing is written and the processor is never called.
    public async Task ImportAsync_OverLimit_FailsWithMessageAndWritesNothing()
    {
        var (success, message, reference) = await CreateSut().ImportAsync(
            Photo((int)ImageStore.MaxSourceBytes + 1), ImageOwnerKind.Song);

        Assert.False(success);
        Assert.Equal("Could not use this picture.", message);
        Assert.Null(reference);
        Assert.Empty(_processor.Calls);
        Assert.False(Directory.Exists(_root) && Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories).Any());
    }

    [Fact]
    // [AC] REQ-PIC-10: when the processor cannot decode the photo no file is kept and the failure message is returned.
    public async Task ImportAsync_ProcessorThrows_FailsWithMessageAndLeavesNoFile()
    {
        _processor.ThrowOnCall = true;

        var (success, message, reference) = await CreateSut().ImportAsync(Photo(), ImageOwnerKind.Song);

        Assert.False(success);
        Assert.Equal("Could not use this picture.", message);
        Assert.Null(reference);
        Assert.False(Directory.Exists(_root) && Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories).Any());
    }

    [Fact]
    // [AC] REQ-PIC-10: when the second size fails, the file already written for the first is removed.
    public async Task ImportAsync_LargeSizeFails_RemovesEverythingWritten()
    {
        _processor.ThrowOnCallNumber = 2;

        var (success, _, _) = await CreateSut().ImportAsync(Photo(), ImageOwnerKind.Song);

        Assert.False(success);
        Assert.False(Directory.Exists(_root) && Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories).Any());
    }

    [Fact]
    // [AC] REQ-PIC-10: an unreadable source stream is a failure, not an exception.
    public async Task ImportAsync_UnreadableStream_Fails()
    {
        var (success, message, _) = await CreateSut().ImportAsync(new ThrowingStream(), ImageOwnerKind.Song);

        Assert.False(success);
        Assert.Equal("Could not use this picture.", message);
    }

    [Fact]
    // [AC] REQ-PIC-03: the store resolves display sources against its own root.
    public void ResolveDisplaySource_Local_UsesStoreRoot()
    {
        var reference = "local:songs/0123456789abcdef0123456789abcdef";

        Assert.Equal(
            Path.Combine(_root, "songs", "0123456789abcdef0123456789abcdef_l.jpg"),
            CreateSut().ResolveDisplaySource(reference, ImageSize.Large));
        Assert.Null(CreateSut().ResolveDisplaySource(null, ImageSize.Large));
    }

    [Fact]
    // [AC] REQ-PIC-11: an old file that no record references is deleted.
    public async Task DeleteUnreferencedAsync_OldOrphan_IsDeleted()
    {
        var thumb = Touch("songs", "0123456789abcdef0123456789abcdef", "_t.jpg", TimeSpan.FromHours(48));
        var large = Touch("songs", "0123456789abcdef0123456789abcdef", "_l.jpg", TimeSpan.FromHours(48));

        var deleted = await CreateSut().DeleteUnreferencedAsync([], TimeSpan.FromHours(24));

        Assert.Equal(2, deleted);
        Assert.False(File.Exists(thumb));
        Assert.False(File.Exists(large));
    }

    [Fact]
    // [AC] REQ-PIC-11: files of a referenced picture are kept, whatever their age.
    public async Task DeleteUnreferencedAsync_LiveReference_KeepsBothFiles()
    {
        const string name = "0123456789abcdef0123456789abcdef";
        var thumb = Touch("artists", name, "_t.jpg", TimeSpan.FromDays(30));
        var large = Touch("artists", name, "_l.jpg", TimeSpan.FromDays(30));

        var deleted = await CreateSut().DeleteUnreferencedAsync(["local:artists/" + name], TimeSpan.FromHours(24));

        Assert.Equal(0, deleted);
        Assert.True(File.Exists(thumb));
        Assert.True(File.Exists(large));
    }

    [Fact]
    // [AC] REQ-PIC-11: an unreferenced file younger than the minimum age is never deleted.
    public async Task DeleteUnreferencedAsync_YoungOrphan_IsKept()
    {
        var thumb = Touch("songs", "fedcba9876543210fedcba9876543210", "_t.jpg", TimeSpan.FromHours(1));

        var deleted = await CreateSut().DeleteUnreferencedAsync([], TimeSpan.FromHours(24));

        Assert.Equal(0, deleted);
        Assert.True(File.Exists(thumb));
    }

    [Fact]
    // [AC] REQ-PIC-11: live files survive while orphans next to them are deleted, in both folders.
    public async Task DeleteUnreferencedAsync_MixedFiles_DeletesOnlyOldOrphans()
    {
        const string live = "11111111111111111111111111111111";
        const string orphan = "22222222222222222222222222222222";
        var liveFile = Touch("songs", live, "_t.jpg", TimeSpan.FromDays(3));
        var orphanSong = Touch("songs", orphan, "_l.jpg", TimeSpan.FromDays(3));
        var orphanArtist = Touch("artists", orphan, "_t.jpg", TimeSpan.FromDays(3));
        var leftover = Touch("songs", orphan, "_t.jpg.tmp", TimeSpan.FromDays(3));

        var deleted = await CreateSut().DeleteUnreferencedAsync(["local:songs/" + live, "https://e.com/x.jpg"], TimeSpan.FromHours(24));

        Assert.Equal(3, deleted);
        Assert.True(File.Exists(liveFile));
        Assert.False(File.Exists(orphanSong));
        Assert.False(File.Exists(orphanArtist));
        Assert.False(File.Exists(leftover));
    }

    [Fact]
    // [AC] REQ-PIC-11: a missing images folder is not an error.
    public async Task DeleteUnreferencedAsync_MissingFolders_ReturnsZero()
        => Assert.Equal(0, await CreateSut().DeleteUnreferencedAsync([], TimeSpan.FromHours(24)));

    private sealed class FakeProcessor : IImageProcessor
    {
        public List<(int Side, float Quality)> Calls { get; } = [];
        public bool ThrowOnCall { get; set; }
        public int ThrowOnCallNumber { get; set; }

        public byte[] CropSquareJpeg(byte[] source, int maxSidePixels, float quality)
        {
            if (ThrowOnCall || ThrowOnCallNumber == Calls.Count + 1)
                throw new InvalidOperationException("cannot decode");
            Calls.Add((maxSidePixels, quality));
            return [0xFF, 0xD8, 0xFF, (byte)(maxSidePixels / 4)];
        }
    }

    private sealed class ThrowingStream : MemoryStream
    {
        public override int Read(byte[] buffer, int offset, int count) => throw new IOException("boom");
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => throw new IOException("boom");
    }
}
