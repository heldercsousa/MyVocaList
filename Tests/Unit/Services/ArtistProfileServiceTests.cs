using MyVocaList.Contracts.DTOs;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Tests.Infrastructure;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.Rules;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Tests.Unit.Services;

/// <summary>ArtistProfileService: MusicBrainz -> Wikidata -> Commons chain (REQ-MSRC-08, -09, -10). Fake handler; no network.</summary>
public class ArtistProfileServiceTests
{
    private const string Mbid = "b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d";

    private sealed class StubHandler(Func<Uri, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request.RequestUri!));
    }

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private readonly Mock<IArtistRepository> _artists = new();
    private readonly Mock<IImageStore> _images = new();

    private ArtistProfileService Create(Func<Uri, HttpResponseMessage> respond) =>
        new(new HttpClient(new StubHandler(respond)), new MusicBrainzRateLimiter(), _artists.Object, PassthroughUnitOfWork.Over(_artists), _images.Object,
            NullLogger<ArtistProfileService>.Instance);

    private const string MbWithWikidata = """
        {"relations":[{"type":"wikidata","url":{"resource":"https://www.wikidata.org/wiki/Q1299"}}],
         "release-groups":[{"id":"rg2","title":"Untitled","first-release-date":"","primary-type":"Album","secondary-types":[]},{"id":"rg1","title":"Help!","first-release-date":"1965-08-06","primary-type":"Album","secondary-types":[]}]}
        """;
    private const string MbNoWikidata = """
        {"relations":[{"type":"official homepage","url":{"resource":"https://example.org"}}],
         "release-groups":[{"id":"rg1","title":"Help!","first-release-date":"1965","primary-type":"Album","secondary-types":[]}]}
        """;
    private const string WdFull = """
        {"entities":{"Q1299":{"claims":{"P18":[{"mainsnak":{"datavalue":{"value":"The Beatles.jpg"}}}]},
         "descriptions":{"pt":{"value":"banda inglesa"},"en":{"value":"English band"}}}}}
        """;
    private const string WdNoP18 = """
        {"entities":{"Q1299":{"claims":{},"descriptions":{"en":{"value":"English band"}}}}}
        """;
    private const string CommonsFull = """
        {"query":{"pages":{"1":{"imageinfo":[{"thumburl":"https://upload.wikimedia.org/thumb/500px-x.jpg","url":"https://upload.wikimedia.org/x.jpg",
         "extmetadata":{"Artist":{"value":"<a href=\"//x\">Jane Doe</a>"},"LicenseShortName":{"value":"CC BY-SA 4.0"},"LicenseUrl":{"value":"https://creativecommons.org/licenses/by-sa/4.0"}}}]}}}}
        """;
    private const string CommonsNoLicense = """
        {"query":{"pages":{"1":{"imageinfo":[{"thumburl":"https://upload.wikimedia.org/thumb/500px-x.jpg","url":"https://upload.wikimedia.org/x.jpg",
         "extmetadata":{"Artist":{"value":"Jane Doe"}}}]}}}}
        """;

    private static Func<Uri, HttpResponseMessage> Chain(string mb, string wd, string commons) => uri =>
        uri.Host switch
        {
            "musicbrainz.org" => Json(mb),
            "www.wikidata.org" => Json(wd),
            "commons.wikimedia.org" => Json(commons),
            _ => Json("{}", HttpStatusCode.NotFound),
        };

    [Fact]
    public async Task GetAsync_FullChain_ReturnsUrlCreditDescriptionAndDiscography()
    {
        // [AC] REQ-MSRC-08: photo from Commons with author + licence credit; REQ-MSRC-09: description + release groups
        var sut = Create(Chain(MbWithWikidata, WdFull, CommonsFull));

        var profile = await sut.GetAsync(Mbid);

        Assert.NotNull(profile);
        Assert.Equal("https://upload.wikimedia.org/thumb/500px-x.jpg", profile.ImageUrl);
        Assert.Equal("Jane Doe, CC BY-SA 4.0", profile.ImageCredit);
        Assert.Equal("https://creativecommons.org/licenses/by-sa/4.0", profile.ImageLicenseUrl);
        Assert.Equal("banda inglesa", profile.Description);
        Assert.Equal(2, profile.Discography.Count);
        Assert.Equal(new ReleaseGroupDto("rg1", "Help!", "1965"), profile.Discography[0]);
        Assert.Null(profile.Discography[1].FirstReleaseYear);
    }

    private const string MbMixedGroups = """
        {"release-groups":[
         {"id":"a","title":"Late","first-release-date":"1990-01-01","primary-type":"Album","secondary-types":[]},
         {"id":"b","title":"Early","first-release-date":"1975-05-05","primary-type":"Album","secondary-types":[]},
         {"id":"c","title":"Single One","first-release-date":"1980","primary-type":"Single","secondary-types":[]},
         {"id":"d","title":"Live Show","first-release-date":"1985","primary-type":"Album","secondary-types":["Live"]},
         {"id":"e","title":"EARLY","first-release-date":"1976","primary-type":"Album","secondary-types":[]},
         {"id":"f","title":"No Date","primary-type":"Album","secondary-types":[]},
         {"id":"g","title":"Typeless","first-release-date":"1970"}]}
        """;

    [Fact]
    public async Task GetAsync_Discography_OnlyStudioAlbumsDedupedByTitleOldestFirst()
    {
        // [AC] REQ-MSRC-09: discography lists Album-only release groups, one per title, oldest first, unknown year last
        var sut = Create(Chain(MbMixedGroups, WdFull, CommonsFull));

        var profile = await sut.GetAsync(Mbid);

        Assert.NotNull(profile);
        Assert.Equal(["b", "a", "f"], profile.Discography.Select(g => g.Mbid));
    }

    [Fact]
    public async Task GetAsync_NoWikidataRelation_ReturnsNullImage()
    {
        // [AC] REQ-MSRC-08: no free image -> placeholder
        var sut = Create(Chain(MbNoWikidata, WdFull, CommonsFull));

        var profile = await sut.GetAsync(Mbid);

        Assert.NotNull(profile);
        Assert.Null(profile.ImageUrl);
        Assert.Null(profile.ImageCredit);
        Assert.Null(profile.Description);
        Assert.Single(profile.Discography);
    }

    [Fact]
    public async Task GetAsync_NoP18_ReturnsNullImage()
    {
        // [AC] REQ-MSRC-08: Wikidata without P18 -> placeholder; description still returned (REQ-MSRC-09)
        var sut = Create(Chain(MbWithWikidata, WdNoP18, CommonsFull));

        var profile = await sut.GetAsync(Mbid);

        Assert.NotNull(profile);
        Assert.Null(profile.ImageUrl);
        Assert.Equal("English band", profile.Description);
    }

    [Fact]
    public async Task GetAsync_MissingLicense_ReturnsNullImage()
    {
        // [AC] REQ-MSRC-08: image only when the Commons file carries a licence
        var sut = Create(Chain(MbWithWikidata, WdFull, CommonsNoLicense));

        var profile = await sut.GetAsync(Mbid);

        Assert.NotNull(profile);
        Assert.Null(profile.ImageUrl);
        Assert.Null(profile.ImageCredit);
    }

    [Fact]
    public async Task GetAsync_NetworkFailure_ReturnsNull()
    {
        // [AC] REQ-MSRC-10: network failure leaves local data usable (null, no throw)
        var sut = Create(_ => throw new HttpRequestException("offline"));

        Assert.Null(await sut.GetAsync(Mbid));
    }

    // ── Photo adoption (REQ-MSRC-15) ───────────────────────────────────────────

    private const string LocalRef = "local:artists/0123456789abcdef0123456789abcdef";
    private const string PhotoUrl = "https://upload.wikimedia.org/thumb/500px-x.jpg";
    private static readonly ArtistProfileDto ProfileWithPhoto = new(PhotoUrl, "Jane, CC BY", "https://cc", null, []);

    private static Func<Uri, HttpResponseMessage> PhotoServer() => uri =>
        uri.Host == "upload.wikimedia.org"
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) }
            : Json("{}", HttpStatusCode.NotFound);

    private void ArtistHas(string? imageUrl) =>
        _artists.Setup(a => a.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Artist { Id = 7, Name = "A", ImageUrl = imageUrl });

    [Fact]
    public async Task AdoptPhotoIfMissingAsync_ArtistHasNoPicture_StoresPhotoAsArtistPicture()
    {
        // [AC] REQ-MSRC-15: the licensed photo becomes the artist picture when the artist has none
        ArtistHas(null);
        _images.Setup(i => i.ImportAsync(It.IsAny<Stream>(), ImageOwnerKind.Artist, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "ok", LocalRef));
        var sut = Create(PhotoServer());

        var reference = await sut.AdoptPhotoIfMissingAsync(7, ProfileWithPhoto);

        Assert.Equal(LocalRef, reference);
        _artists.Verify(a => a.UpdateAsync(It.Is<Artist>(x => x.ImageUrl == LocalRef), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AdoptPhotoIfMissingAsync_ArtistHasPicture_NeverReplacesIt()
    {
        // [AC] REQ-MSRC-15: a picture the user chose is never replaced
        ArtistHas("img/user.jpg");
        var sut = Create(PhotoServer());

        var reference = await sut.AdoptPhotoIfMissingAsync(7, ProfileWithPhoto);

        Assert.Null(reference);
        _images.Verify(i => i.ImportAsync(It.IsAny<Stream>(), It.IsAny<ImageOwnerKind>(), It.IsAny<CancellationToken>()), Times.Never);
        _artists.Verify(a => a.UpdateAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AdoptPhotoIfMissingAsync_ProfileHasNoPhoto_DoesNothing()
    {
        // [AC] REQ-MSRC-15: no licensed photo, nothing to adopt
        ArtistHas(null);
        var sut = Create(PhotoServer());

        var reference = await sut.AdoptPhotoIfMissingAsync(7, new ArtistProfileDto(null, null, null, null, []));

        Assert.Null(reference);
        _artists.Verify(a => a.UpdateAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AdoptPhotoIfMissingAsync_DownloadFails_ReturnsNullAndStoresNothing()
    {
        // [AC] REQ-MSRC-15: offline or failure leaves the artist without a picture
        ArtistHas(null);
        var sut = Create(_ => Json("{}", HttpStatusCode.InternalServerError));

        var reference = await sut.AdoptPhotoIfMissingAsync(7, ProfileWithPhoto);

        Assert.Null(reference);
        _artists.Verify(a => a.UpdateAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
