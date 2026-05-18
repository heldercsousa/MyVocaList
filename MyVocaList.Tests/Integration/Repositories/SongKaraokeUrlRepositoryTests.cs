using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Infra;
using MyVocaList.Infra.Repository;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Repositories;

public class SongKaraokeUrlRepositoryTests : IAsyncLifetime
{
    private AppDbContext _db;
    private SongKaraokeUrlRepository _repo;
    public int SongId { get; private set; }

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.EnsureCreatedAsync();
        _repo = new SongKaraokeUrlRepository(_db);

        // Seed Artist + Song so FK constraints are satisfied
        var artist = new Artist { Name = "Test Artist", NameNormalized = "test artist", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Artists.Add(artist);
        await _db.SaveChangesAsync();

        var song = new Song
        {
            ArtistId = artist.Id,
            Title = "Test Song",
            TitleNormalized = "test song",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Songs.Add(song);
        await _db.SaveChangesAsync();
        SongId = song.Id;
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    [Fact]
    // [AC] AC-1.3: saved URLs appear in list
    public async Task GetBySongIdAsync_AfterAdd_ReturnsUrl()
    {
        var url = new SongKaraokeUrl { SongId = SongId, VideoId = "dQw4w9WgXcQ", AddedAt = DateTime.UtcNow };
        await _repo.AddAsync(url);
        await _repo.SaveChangesAsync();

        var list = await _repo.GetBySongIdAsync(SongId);

        Assert.Single(list);
        Assert.Equal("dQw4w9WgXcQ", list[0].VideoId);
    }

    [Fact]
    // [AC] AC-1.9: duplicate video ID per song rejected at DB level
    public async Task AddAsync_DuplicateVideoId_ThrowsDbUpdateException()
    {
        var first = new SongKaraokeUrl { SongId = SongId, VideoId = "dQw4w9WgXcQ", AddedAt = DateTime.UtcNow };
        _db.SongKaraokeUrls.Add(first);
        await _db.SaveChangesAsync();

        // Detach first entry so EF does not detect the PK collision in the change tracker
        _db.Entry(first).State = EntityState.Detached;
        _db.SongKaraokeUrls.Add(new SongKaraokeUrl { SongId = SongId, VideoId = "dQw4w9WgXcQ", AddedAt = DateTime.UtcNow });

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    [Fact]
    // [AC] AC-1.4: highest play count is suggested
    public async Task GetSuggestedAsync_ReturnsHighestPlayCount()
    {
        _db.SongKaraokeUrls.AddRange(
            new SongKaraokeUrl { SongId = SongId, VideoId = "aaaaaaaaaaa", PlayCount = 1, AddedAt = DateTime.UtcNow },
            new SongKaraokeUrl { SongId = SongId, VideoId = "bbbbbbbbbbb", PlayCount = 5, AddedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var suggested = await _repo.GetSuggestedAsync(SongId);

        Assert.NotNull(suggested);
        Assert.Equal("bbbbbbbbbbb", suggested!.VideoId);
    }

    [Fact]
    // [AC] AC-1.5: remove URL removes it from list
    public async Task RemoveAsync_RemovesFromList()
    {
        _db.SongKaraokeUrls.Add(new SongKaraokeUrl { SongId = SongId, VideoId = "dQw4w9WgXcQ", AddedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        await _repo.RemoveAsync(SongId, "dQw4w9WgXcQ");
        await _repo.SaveChangesAsync();

        var list = await _repo.GetBySongIdAsync(SongId);
        Assert.Empty(list);
    }

    [Fact]
    // [AC] AC-3.4: PlayCount incremented on confirmed launch
    public async Task IncrementPlayCountAsync_IncrementsByOne()
    {
        _db.SongKaraokeUrls.Add(new SongKaraokeUrl { SongId = SongId, VideoId = "dQw4w9WgXcQ", PlayCount = 2, AddedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        await _repo.IncrementPlayCountAsync(SongId, "dQw4w9WgXcQ");
        await _repo.SaveChangesAsync();

        var reloaded = await _repo.GetBySongIdAsync(SongId);
        Assert.Equal(3, reloaded[0].PlayCount);
    }

    [Fact]
    public async Task CascadeDelete_WhenSongDeleted_UrlsAreRemoved()
    {
        _db.SongKaraokeUrls.Add(new SongKaraokeUrl { SongId = SongId, VideoId = "dQw4w9WgXcQ", AddedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var song = await _db.Songs.FindAsync(SongId);
        _db.Songs.Remove(song!);
        await _db.SaveChangesAsync();

        var list = await _repo.GetBySongIdAsync(SongId);
        Assert.Empty(list);
    }
}
