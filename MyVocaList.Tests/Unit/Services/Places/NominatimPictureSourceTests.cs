using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using MyVocaList.Contracts.DTOs.Venues;
using MyVocaList.Services.Places;

namespace MyVocaList.Tests.Unit.Services.Places;

/// <summary>
/// REQ-VEN-37 — the picture source a looked-up place carries: extra-tag request, extra-tag parsing (provider) and the
/// acceptance rules of the tags (mapper). Uses a fake <see cref="HttpMessageHandler"/>; no network.
/// </summary>
public class NominatimPictureSourceTests
{
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

    private static (NominatimGeocodingProvider Provider, StubHandler Handler) CreateProvider(string body)
    {
        var handler = new StubHandler(_ => Json(body));
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://nominatim.openstreetmap.org/") };
        return (new NominatimGeocodingProvider(client, NullLogger<NominatimGeocodingProvider>.Instance), handler);
    }

    private static VenuePictureSource MapTags(IReadOnlyDictionary<string, string>? tags)
    {
        var record = new GeocodingRecord(
            -23.5614,
            -46.6559,
            "Bar Brahma",
            "amenity",
            "Bar Brahma, São Paulo",
            new Dictionary<string, string>(),
            tags);
        var source = NominatimAddressMapper.ToPlace(record).PictureSource;
        Assert.NotNull(source);
        return source;
    }

    private static IReadOnlyDictionary<string, string> Tags(params (string Key, string Value)[] pairs) =>
        pairs.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

    [Fact]
    public async Task ReverseAsync_SendsExtratagsParameter()
    {
        // [AC] REQ-VEN-37: every place lookup asks the provider for the extra tags (reverse)
        var (provider, handler) = CreateProvider("{\"error\":\"Unable to geocode\"}");

        await provider.ReverseAsync(-23.5614, -46.6559, "pt-BR");

        var uri = Assert.Single(handler.Requests);
        Assert.Contains("&extratags=1", uri.PathAndQuery, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SearchAsync_SendsExtratagsParameter()
    {
        // [AC] REQ-VEN-37: every place lookup asks the provider for the extra tags (search)
        var (provider, handler) = CreateProvider("[]");

        await provider.SearchAsync("Bar Brahma", "en-US", 5);

        var uri = Assert.Single(handler.Requests);
        Assert.Contains("&extratags=1", uri.PathAndQuery, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReverseAsync_ExtratagsObject_ParsesStringValuesOnly()
    {
        // [AC] REQ-VEN-37: the extratags object is parsed; non-string values are ignored
        const string body =
            "{\"lat\":\"1.5\",\"lon\":\"2.5\",\"extratags\":{\"wikidata\":\"Q1\",\"image\":\"https://example.org/a.jpg\",\"capacity\":40}}";
        var (provider, _) = CreateProvider(body);

        var record = await provider.ReverseAsync(1.5, 2.5, "en-US");

        Assert.NotNull(record);
        Assert.Equal("Q1", record.ExtraTags!["wikidata"]);
        Assert.Equal("https://example.org/a.jpg", record.ExtraTags["image"]);
        Assert.False(record.ExtraTags.ContainsKey("capacity"));
    }

    [Fact]
    public async Task ReverseAsync_NoExtratagsMember_GivesEmptyExtraTags()
    {
        // [AC] REQ-VEN-37: an answer without an extratags member maps to empty extra tags, never a parse failure
        const string body = "{\"lat\":\"1.5\",\"lon\":\"2.5\",\"name\":\"Bar\"}";
        var (provider, _) = CreateProvider(body);

        var record = await provider.ReverseAsync(1.5, 2.5, "en-US");

        Assert.NotNull(record);
        Assert.NotNull(record.ExtraTags);
        Assert.Empty(record.ExtraTags);
    }

    [Fact]
    public void ToPlace_BrandWikidataAndWikidata_UsesBrandWikidata()
    {
        // [AC] REQ-VEN-37: given brand:wikidata = Q918151 and wikidata = Q1, the Wikidata id is Q918151
        var source = MapTags(Tags(("brand:wikidata", "Q918151"), ("wikidata", "Q1")));

        Assert.Equal("Q918151", source.WikidataId);
    }

    [Fact]
    public void ToPlace_OnlyWikidataTag_UsesWikidata()
    {
        // [AC] REQ-VEN-37: the wikidata tag is used when brand:wikidata is absent
        var source = MapTags(Tags(("wikidata", "Q1")));

        Assert.Equal("Q1", source.WikidataId);
    }

    [Theory]
    [InlineData("918151")]
    [InlineData("Q")]
    [InlineData("Qabc")]
    [InlineData("q1")]
    [InlineData("Q1 x")]
    [InlineData("https://www.wikidata.org/wiki/Q1")]
    public void ToPlace_InvalidWikidataId_IsRejected(string invalid)
    {
        // [AC] REQ-VEN-37: a Wikidata id is accepted only when it matches Q followed by digits
        var source = MapTags(Tags(("wikidata", invalid)));

        Assert.Null(source.WikidataId);
    }

    [Fact]
    public void ToPlace_CommonsFileWithFilePrefix_KeepsNameWithoutPrefix()
    {
        // [AC] REQ-VEN-37: a wikimedia_commons tag starting with File: gives the Commons file name
        var source = MapTags(Tags(("wikimedia_commons", "File:Bar Logo.png")));

        Assert.Equal("Bar Logo.png", source.CommonsFile);
    }

    [Theory]
    [InlineData("Category:Bars")]
    [InlineData("Bar Logo.png")]
    [InlineData("File:   ")]
    public void ToPlace_CommonsValueWithoutFilePrefix_IsRejected(string invalid)
    {
        // [AC] REQ-VEN-37: a wikimedia_commons value is accepted only when it starts with File:
        var source = MapTags(Tags(("wikimedia_commons", invalid)));

        Assert.Null(source.CommonsFile);
    }

    [Fact]
    public void ToPlace_HttpsImageTag_IsKept()
    {
        // [AC] REQ-VEN-37: an image tag that is an absolute https address is kept
        var source = MapTags(Tags(("image", "https://example.org/bar.jpg")));

        Assert.Equal("https://example.org/bar.jpg", source.ImageUrl);
    }

    [Theory]
    [InlineData("http://example.org/bar.jpg")]
    [InlineData("ftp://example.org/bar.jpg")]
    [InlineData("/bar.jpg")]
    [InlineData("not a url")]
    public void ToPlace_ImageTagNotHttpsAbsolute_IsRejected(string invalid)
    {
        // [AC] REQ-VEN-37: an image tag is accepted only when it is an absolute https address
        var source = MapTags(Tags(("image", invalid)));

        Assert.Null(source.ImageUrl);
    }

    [Fact]
    public void ToPlace_NoPictureTags_GivesEmptyPictureSource()
    {
        // [AC] REQ-VEN-37: given a place with no such tags, the picture source is empty
        var source = MapTags(Tags(("amenity", "bar")));

        Assert.Null(source.WikidataId);
        Assert.Null(source.CommonsFile);
        Assert.Null(source.ImageUrl);
    }
}
