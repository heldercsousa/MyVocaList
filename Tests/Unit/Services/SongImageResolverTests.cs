using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.DTOs.Suggestions;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>Song image fallback priority: song picture, album cover, artist picture (REQ-MSRC-16/18).</summary>
public class SongImageResolverTests
{
    private const string Mbid = "76df3287-6cda-33eb-8e9a-044b5e15ffdd";
    private const string Cover = "https://coverartarchive.org/release/76df3287-6cda-33eb-8e9a-044b5e15ffdd/front-250";

    [Fact]
    public void CoverArtUrl_ValidReleaseId_BuildsFront250Url()
    {
        // [AC] REQ-MSRC-18: the album cover is the CAA release/{mbid}/front-250 URL, built with no call
        Assert.Equal(Cover, SongImageResolver.CoverArtUrl(Mbid));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("not-a-guid")]
    public void CoverArtUrl_MissingOrInvalidReleaseId_ReturnsNull(string? mbid)
    {
        // [AC] REQ-MSRC-18: no release id, no album cover URL (no malformed request)
        Assert.Null(SongImageResolver.CoverArtUrl(mbid));
    }

    [Fact]
    public void Resolve_SongPictureWins()
    {
        // [AC] REQ-MSRC-16: the song's own picture comes first
        Assert.Equal("song", SongImageResolver.Resolve("song", Mbid, "artist"));
    }

    [Fact]
    public void Resolve_NoSongPicture_UsesAlbumCoverBeforeArtist()
    {
        // [AC] REQ-MSRC-16: album cover before the artist picture
        Assert.Equal(Cover, SongImageResolver.Resolve(null, Mbid, "artist"));
    }

    [Fact]
    public void Resolve_NoSongNoRelease_UsesArtistPicture()
    {
        // [AC] REQ-MSRC-16: artist picture next
        Assert.Equal("artist", SongImageResolver.Resolve(null, null, "artist"));
    }

    [Fact]
    public void Resolve_NothingKnown_ReturnsNullForTheIcon()
    {
        // [AC] REQ-MSRC-16: the music note icon is last
        Assert.Null(SongImageResolver.Resolve(null, null, null));
    }

    [Fact]
    public void SuggestionRowImage_FollowsThePriority()
    {
        // [AC] REQ-MSRC-18: suggestion row picture: song, album cover, artist
        var withRelease = new SongSuggestionDto(null, "T", "A", null, "x", "MusicBrainz", true,
            ArtistImageUrl: "artist", ReleaseMbid: Mbid);
        var song = withRelease with { ImageUrl = "song" };
        var artistOnly = withRelease with { ReleaseMbid = null };

        Assert.Equal(Cover, withRelease.RowImageUrl);
        Assert.Equal("song", song.RowImageUrl);
        Assert.Equal("artist", artistOnly.RowImageUrl);
    }
}
