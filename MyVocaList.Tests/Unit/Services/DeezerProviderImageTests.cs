using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>
/// Deezer artwork and album mapping (REQ-ART-01…04). Uses a fake <see cref="HttpMessageHandler"/>; no network.
/// Lives beside <see cref="DeezerProviderTests"/>, which is not edited by this item.
/// </summary>
public class DeezerProviderImageTests
{
    private const string Cover = "https://cdn-images.dzcdn.net/images/cover/cc0c69196d44c6ea479dcc48aef2464a/250x250-000000-80-0-0.jpg";
    private const string ArtistPicture = "https://cdn-images.dzcdn.net/images/artist/3f2a9c1e7b4d8a60c5e1f2a3b4c5d6e7/250x250-000000-80-0-0.jpg";

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(request.RequestUri!);
            return Task.FromResult(respond(request));
        }
    }

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static (DeezerProvider Provider, StubHandler Handler) Create(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new StubHandler(respond);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.deezer.com/") };
        return (new DeezerProvider(client, NullLogger<DeezerProvider>.Instance), handler);
    }

    /// <summary>One track in a search/track response; <paramref name="fields"/> are extra <c>"key":value</c> pairs.</summary>
    private static string TrackBody(params string[] fields) =>
        "{\"data\":[{\"id\":42,\"title\":\"Commando (1999 Remaster)\",\"title_short\":\"Commando\"," +
        "\"title_version\":\"(1999 Remaster)\"" +
        string.Concat(fields.Select(f => "," + f)) +
        "}]}";

    private static string ArtistBody(params string[] fields) =>
        "{\"data\":[{\"id\":119,\"name\":\"Ramones\"" + string.Concat(fields.Select(f => "," + f)) + "}]}";

    private static string Album(string title, string cover) =>
        $"\"album\":{{\"id\":9,\"title\":\"{title}\",\"cover_medium\":\"{cover}\"}}";

    private static string Artist(string picture) =>
        $"\"artist\":{{\"id\":7,\"name\":\"Ramones\",\"picture_medium\":\"{picture}\"}}";

    [Fact]
    public async Task SearchSongsAsync_TrackWithAlbumAndArtistPicture_MapsCoverArtistPictureAndAlbumTitle()
    {
        // [AC] REQ-ART-01: a track result carries the album cover, the artist picture and the album title
        var (provider, _) = Create(_ => Json(TrackBody(Album("Greatest Hits", Cover), Artist(ArtistPicture))));

        var item = Assert.Single(await provider.SearchSongsAsync("commando ramones"));

        Assert.Equal(Cover, item.ImageUrl);
        Assert.Equal(ArtistPicture, item.ArtistImageUrl);
        Assert.Equal("Greatest Hits", item.AlbumTitle);
    }

    [Fact]
    public async Task SearchSongsAsync_AlbumTitleSurroundedBySpaces_IsTrimmed()
    {
        // [AC] REQ-ART-01: the album title is carried trimmed
        var (provider, _) = Create(_ => Json(TrackBody(Album("  Greatest Hits  ", Cover))));

        var item = Assert.Single(await provider.SearchSongsAsync("commando ramones"));

        Assert.Equal("Greatest Hits", item.AlbumTitle);
    }

    [Theory]
    [InlineData("\"album\":{\"id\":9,\"cover_medium\":\"" + "https://x.test/a.jpg" + "\"}")]
    [InlineData("\"album\":null")]
    [InlineData("\"album\":{}")]
    public async Task SearchSongsAsync_NoAlbumTitle_AlbumTitleIsNullAndSearchSucceeds(string albumField)
    {
        // [AC] REQ-ART-01: a missing album title, or a missing or null album, gives null and does not fail the search
        var (provider, _) = Create(_ => Json(TrackBody(albumField)));

        var item = Assert.Single(await provider.SearchSongsAsync("commando"));

        Assert.Null(item.AlbumTitle);
        Assert.Equal("Commando", item.SongTitle);
    }

    [Fact]
    public async Task SearchSongsAsync_NoAlbumObjectNoArtistPicture_ImagesAreNullAndSearchSucceeds()
    {
        // [AC] REQ-ART-01: an absent album object and an artist without picture_medium give null images and a result
        var (provider, _) = Create(_ => Json(TrackBody("\"artist\":{\"id\":7,\"name\":\"Ramones\"}")));

        var item = Assert.Single(await provider.SearchSongsAsync("commando"));

        Assert.Null(item.ImageUrl);
        Assert.Null(item.ArtistImageUrl);
        Assert.Null(item.AlbumTitle);
        Assert.Equal("Ramones", item.ArtistName);
    }

    [Fact]
    public async Task SearchSongsAsync_NullAlbumAndNullArtistPicture_ImagesAreNullAndSearchSucceeds()
    {
        // [AC] REQ-ART-01: explicit nulls behave like absent fields
        var (provider, _) = Create(_ => Json(TrackBody("\"album\":null", "\"artist\":{\"id\":7,\"name\":\"Ramones\",\"picture_medium\":null}")));

        var item = Assert.Single(await provider.SearchSongsAsync("commando"));

        Assert.Null(item.ImageUrl);
        Assert.Null(item.ArtistImageUrl);
        Assert.Equal("Ramones", item.ArtistName);
    }

    [Fact]
    public async Task SearchSongsAsync_EmptyCoverAndBlankAlbumTitle_ImageNullAndAlbumTitleNull()
    {
        // [AC] REQ-ART-03: cover_medium "" is no image; a whitespace album title is no album title
        var (provider, _) = Create(_ => Json(TrackBody(Album("   ", ""), Artist(ArtistPicture))));

        var item = Assert.Single(await provider.SearchSongsAsync("commando"));

        Assert.Null(item.ImageUrl);
        Assert.Null(item.AlbumTitle);
        Assert.Equal(ArtistPicture, item.ArtistImageUrl);
    }

    [Fact]
    public async Task SearchSongsAsync_HttpCover_IsDroppedAsNoImage()
    {
        // [AC] REQ-ART-03: an http:// cover is not an image URL and maps to null
        var (provider, _) = Create(_ => Json(TrackBody(Album("Greatest Hits", "http://cdn-images.dzcdn.net/images/cover/x/250x250.jpg"))));

        var item = Assert.Single(await provider.SearchSongsAsync("commando"));

        Assert.Null(item.ImageUrl);
        Assert.Equal("Greatest Hits", item.AlbumTitle);
    }

    [Fact]
    public async Task SearchArtistsAsync_MapsPictureMediumAsImageUrl()
    {
        // [AC] REQ-ART-02: an artist result carries picture_medium as its image URL
        var (provider, _) = Create(_ => Json(ArtistBody($"\"picture_medium\":\"{ArtistPicture}\"")));

        var item = Assert.Single(await provider.SearchArtistsAsync("ramones"));

        Assert.Equal(ArtistPicture, item.ImageUrl);
        Assert.Equal("Ramones", item.ArtistName);
    }

    [Fact]
    public async Task SearchArtistsAsync_NoPictureMedium_ImageUrlIsNull()
    {
        // [AC] REQ-ART-02: an artist without picture_medium has no image and the search still succeeds
        var (provider, _) = Create(_ => Json(ArtistBody()));

        var item = Assert.Single(await provider.SearchArtistsAsync("ramones"));

        Assert.Null(item.ImageUrl);
    }

    [Fact]
    public async Task SearchSongsAsync_OneSearch_MakesExactlyOneRequest()
    {
        // [AC] REQ-ART-04: images come from the search response; a track search makes one request
        var (provider, handler) = Create(_ => Json(TrackBody(Album("Greatest Hits", Cover), Artist(ArtistPicture))));

        await provider.SearchSongsAsync("commando ramones");

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task SearchArtistsAsync_OneSearch_MakesExactlyOneRequest()
    {
        // [AC] REQ-ART-04: an artist search with pictures makes one request and no image request
        var (provider, handler) = Create(_ => Json(ArtistBody($"\"picture_medium\":\"{ArtistPicture}\"")));

        await provider.SearchArtistsAsync("ramones");

        Assert.Single(handler.Requests);
    }
}
