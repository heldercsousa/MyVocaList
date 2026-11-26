using MyVocaList.Contracts.DTOs;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;

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

    private static ArtistProfileService Create(Func<Uri, HttpResponseMessage> respond) =>
        new(new HttpClient(new StubHandler(respond)), new MusicBrainzRateLimiter(), NullLogger<ArtistProfileService>.Instance);

    private const string MbWithWikidata = """
        {"relations":[{"type":"wikidata","url":{"resource":"https://www.wikidata.org/wiki/Q1299"}}],
         "release-groups":[{"id":"rg1","title":"Help!","first-release-date":"1965-08-06"},{"id":"rg2","title":"Untitled","first-release-date":""}]}
        """;
    private const string MbNoWikidata = """
        {"relations":[{"type":"official homepage","url":{"resource":"https://example.org"}}],
         "release-groups":[{"id":"rg1","title":"Help!","first-release-date":"1965"}]}
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
}
