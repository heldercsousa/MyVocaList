using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Entity;
using MyVocaList.Infra;
using MyVocaList.Infra.Repository;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Repositories;

/// <summary>
/// T2: every SongSearchHit builder fills ImageUrl (song) and ArtistImageUrl (original artist):
/// the raw-SQL FTS search (global and per artist), the EF prefix fallback, and the dedup lookups.
/// Real SQLite temp file, schema built with MigrateAsync so the SongSearch index and triggers exist.
/// </summary>
public class SongSearchHitImageTests : IAsyncLifetime
{
    private const string CoverUrl = "https://e.test/cover.jpg";
    private const string ArtistPictureUrl = "https://e.test/artist.jpg";

    private AppDbContext _db;
    private SongRepository _repo;
    private Artist _gunsArtist;

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.MigrateAsync();
        _repo = new SongRepository(_db);

        _gunsArtist = await SeedArtistAsync("Guns N' Roses", ArtistPictureUrl);
        var abba = await SeedArtistAsync("ABBA", null);

        _db.Songs.AddRange(
            MakeSong(_gunsArtist.Id, "Don't Cry", "", CoverUrl, "Deezer", "3135556"),
            MakeSong(abba.Id, "Abba Song", "", null),
            MakeSong(abba.Id, "Abba Cover", "", CoverUrl));
        await _db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    // [AC] REQ-ART-06: FTS (raw SQL) hit carries the song image and the artist image
    [Fact]
    public async Task SearchTitleCandidatesAsync_FtsPath_CarriesBothImageUrls()
    {
        var hits = await _repo.SearchTitleCandidatesAsync("dont cry", 50, CancellationToken.None);

        var hit = hits.Single(h => h.Title == "Don't Cry");
        Assert.Equal(CoverUrl, hit.ImageUrl);
        Assert.Equal(ArtistPictureUrl, hit.ArtistImageUrl);
    }

    // [AC] REQ-ART-06: artist-scoped FTS (raw SQL) hit carries both image URLs
    [Fact]
    public async Task SearchTitleCandidatesForArtistAsync_FtsPath_CarriesBothImageUrls()
    {
        var hits = await _repo.SearchTitleCandidatesForArtistAsync(
            _gunsArtist.Id, "dont cry", 50, CancellationToken.None);

        var hit = Assert.Single(hits);
        Assert.Equal(CoverUrl, hit.ImageUrl);
        Assert.Equal(ArtistPictureUrl, hit.ArtistImageUrl);
    }

    // [AC] REQ-ART-06: EF prefix fallback hit carries both image URLs (song without image: null, artist image kept)
    [Fact]
    public async Task SearchTitleCandidatesAsync_PrefixPath_CarriesImageUrlsAndNullSongImage()
    {
        var hits = await _repo.SearchTitleCandidatesAsync("ab", 50, CancellationToken.None);

        var hit = hits.Single(h => h.Title == "Abba Song");
        Assert.Null(hit.ImageUrl);
        Assert.Null(hit.ArtistImageUrl);
    }

    // [AC] REQ-ART-06: EF prefix fallback hit carries the stored song image when the song has one
    [Fact]
    public async Task SearchTitleCandidatesAsync_PrefixPath_SongWithImage_CarriesSongImage()
    {
        var hits = await _repo.SearchTitleCandidatesAsync("ab", 50, CancellationToken.None);

        var hit = hits.Single(h => h.Title == "Abba Cover");
        Assert.Equal(CoverUrl, hit.ImageUrl);
        Assert.Null(hit.ArtistImageUrl);
    }

    // [AC] REQ-ART-06: external-id lookup (feeds promoted suggestion rows) carries both image URLs
    [Fact]
    public async Task FindByExternalIdsAsync_CarriesBothImageUrls()
    {
        var hits = await _repo.FindByExternalIdsAsync("Deezer", ["3135556"], CancellationToken.None);

        var hit = Assert.Single(hits);
        Assert.Equal(CoverUrl, hit.ImageUrl);
        Assert.Equal(ArtistPictureUrl, hit.ArtistImageUrl);
    }

    // [AC] REQ-ART-06: collated title and artist lookup (feeds promoted suggestion rows) carries both image URLs
    [Fact]
    public async Task FindByTitleAndArtistNamesAsync_CarriesBothImageUrls()
    {
        var hits = await _repo.FindByTitleAndArtistNamesAsync(
            [("DON'T CRY", "guns n' roses", "")], CancellationToken.None);

        var hit = Assert.Single(hits);
        Assert.Equal(CoverUrl, hit.ImageUrl);
        Assert.Equal(ArtistPictureUrl, hit.ArtistImageUrl);
    }

    private async Task<Artist> SeedArtistAsync(string name, string? imageUrl)
    {
        var artist = new Artist
        {
            Name = name,
            ImageUrl = imageUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Artists.Add(artist);
        await _db.SaveChangesAsync();
        return artist;
    }

    private static Song MakeSong(int artistId, string title, string version, string? imageUrl,
        string? provider = null, string? externalId = null) => new()
    {
        ArtistId = artistId,
        Title = title,
        Version = version,
        ImageUrl = imageUrl,
        ExternalProvider = provider,
        ExternalId = externalId,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
