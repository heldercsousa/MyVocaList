using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using MyVocaList.Services.Places;

namespace MyVocaList.Tests.Unit.Services.Places;

/// <summary>
/// V7a — <see cref="NominatimGeocodingProvider"/> request shape, reverse and search mapping, nothing-found and
/// failure propagation (REQ-VEN-25..28, 34). Uses a fake <see cref="HttpMessageHandler"/>; no network.
/// </summary>
public class NominatimGeocodingProviderTests
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

    private const string BrahmaReverseBody =
        "{\"place_id\":1,\"osm_type\":\"node\",\"osm_id\":1,\"lat\":\"-23.5614\",\"lon\":\"-46.6559\"," +
        "\"name\":\"Bar Brahma\",\"category\":\"amenity\",\"type\":\"bar\"," +
        "\"display_name\":\"Bar Brahma, Rua Augusta, 100, Consolação, São Paulo, Brasil\"," +
        "\"address\":{\"amenity\":\"Bar Brahma\",\"house_number\":\"100\",\"road\":\"Rua Augusta\"," +
        "\"suburb\":\"Consolação\",\"city\":\"São Paulo\",\"state\":\"São Paulo\",\"postcode\":\"01305-000\"," +
        "\"country\":\"Brasil\",\"country_code\":\"br\"}}";

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static (NominatimGeocodingProvider Provider, StubHandler Handler) Create(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new StubHandler(respond);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://nominatim.openstreetmap.org/") };
        return (new NominatimGeocodingProvider(client, NullLogger<NominatimGeocodingProvider>.Instance), handler);
    }

    [Fact]
    public async Task ReverseAsync_SendsInvariantCoordinatesWithSixDecimals_UnderDecimalCommaCulture()
    {
        // [AC] REQ-VEN-34: reverse request is reverse?format=jsonv2&lat&lon (invariant, 6 decimals)&zoom=18&addressdetails=1&accept-language
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
        try
        {
            var (provider, handler) = Create(_ => Json("{\"error\":\"Unable to geocode\"}"));

            await provider.ReverseAsync(-23.5614, -46.6559, "pt-BR");

            var uri = Assert.Single(handler.Requests);
            Assert.Equal("/reverse?format=jsonv2&lat=-23.561400&lon=-46.655900&zoom=18&addressdetails=1&accept-language=pt-BR", uri.PathAndQuery);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public async Task SearchAsync_SendsEscapedTextAndLimit()
    {
        // [AC] REQ-VEN-34: search request is search?format=jsonv2&q (escaped)&limit&addressdetails=1&accept-language
        var (provider, handler) = Create(_ => Json("[]"));

        await provider.SearchAsync("Bar Brahma São Paulo", "en-US", 5);

        var uri = Assert.Single(handler.Requests);
        Assert.Equal("/search?format=jsonv2&q=Bar%20Brahma%20S%C3%A3o%20Paulo&limit=5&addressdetails=1&accept-language=en-US", uri.PathAndQuery);
    }

    [Fact]
    public async Task ReverseAsync_Http200Realistic_MapsEveryField()
    {
        // [AC] REQ-VEN-26: a reverse answer maps name, category, display_name, address keys and coordinates parsed from the string values
        var (provider, _) = Create(_ => Json(BrahmaReverseBody));

        var record = await provider.ReverseAsync(-23.5614, -46.6559, "pt-BR");

        Assert.NotNull(record);
        Assert.Equal(-23.5614, record.Latitude);
        Assert.Equal(-46.6559, record.Longitude);
        Assert.Equal("Bar Brahma", record.Name);
        Assert.Equal("amenity", record.Category);
        Assert.Equal("Bar Brahma, Rua Augusta, 100, Consolação, São Paulo, Brasil", record.DisplayName);
        Assert.Equal("Rua Augusta", record.Address["road"]);
        Assert.Equal("100", record.Address["house_number"]);
        Assert.Equal("Consolação", record.Address["suburb"]);
        Assert.Equal("São Paulo", record.Address["city"]);
        Assert.Equal("São Paulo", record.Address["state"]);
        Assert.Equal("01305-000", record.Address["postcode"]);
        Assert.Equal("Brasil", record.Address["country"]);
        Assert.Equal("br", record.Address["country_code"]);
        Assert.Equal(9, record.Address.Count);
    }

    [Fact]
    public async Task ReverseAsync_AddressMemberWithNonStringValue_IsIgnored()
    {
        // [AC] REQ-VEN-26: address values that are not JSON strings are ignored; string values are kept
        const string body = "{\"lat\":\"1.5\",\"lon\":\"2.5\",\"address\":{\"road\":\"Rua A\",\"house_number\":100,\"city\":null}}";
        var (provider, _) = Create(_ => Json(body));

        var record = await provider.ReverseAsync(1.5, 2.5, "en-US");

        Assert.NotNull(record);
        var entry = Assert.Single(record.Address);
        Assert.Equal("road", entry.Key);
        Assert.Equal("Rua A", entry.Value);
    }

    [Fact]
    public async Task ReverseAsync_Http200WithoutAddressObject_ReturnsEmptyAddress()
    {
        // [AC] REQ-VEN-26: a reverse answer without an address object maps to a record with an empty Address, never null
        const string body = "{\"lat\":\"48.8584\",\"lon\":\"2.2945\",\"name\":\"Tour Eiffel\",\"category\":\"tourism\",\"display_name\":\"Tour Eiffel, Paris\"}";
        var (provider, _) = Create(_ => Json(body));

        var record = await provider.ReverseAsync(48.8584, 2.2945, "fr");

        Assert.NotNull(record);
        Assert.NotNull(record.Address);
        Assert.Empty(record.Address);
        Assert.Equal("Tour Eiffel", record.Name);
    }

    [Fact]
    public async Task ReverseAsync_Http200ErrorObject_ReturnsNull()
    {
        // [AC] REQ-VEN-27: a reverse answer {"error":...} with HTTP 200 means nothing is there and returns null
        var (provider, _) = Create(_ => Json("{\"error\":\"Unable to geocode\"}"));

        var record = await provider.ReverseAsync(0.0, 0.0, "en-US");

        Assert.Null(record);
    }

    [Theory]
    [InlineData("{\"lat\":\"abc\",\"lon\":\"2.0\",\"display_name\":\"x\"}")]
    [InlineData("{\"lon\":\"2.0\",\"display_name\":\"x\"}")]
    public async Task ReverseAsync_Http200WithoutUsableCoordinates_ReturnsNull(string body)
    {
        // [AC] REQ-VEN-27: a reverse answer whose lat or lon is missing or unparsable yields no record (null)
        var (provider, _) = Create(_ => Json(body));

        var record = await provider.ReverseAsync(2.0, 2.0, "en-US");

        Assert.Null(record);
    }

    [Fact]
    public async Task SearchAsync_Http200TwoElements_ReturnsRecordsInOrder()
    {
        // [AC] REQ-VEN-25: a search answer lists its records in the service's order, mapped like reverse answers
        const string body =
            "[{\"lat\":\"-23.5614\",\"lon\":\"-46.6559\",\"name\":\"Bar Brahma\",\"category\":\"amenity\",\"display_name\":\"Bar Brahma, São Paulo\",\"address\":{\"city\":\"São Paulo\"}}," +
            "{\"lat\":\"-23.5500\",\"lon\":\"-46.6333\",\"name\":\"Praça da Sé\",\"category\":\"place\",\"display_name\":\"Praça da Sé, São Paulo\"}]";
        var (provider, _) = Create(_ => Json(body));

        var result = await provider.SearchAsync("Bar Brahma", "pt-BR", 5);

        Assert.Equal(2, result.Count);
        Assert.Equal("Bar Brahma", result[0].Name);
        Assert.Equal(-23.5614, result[0].Latitude);
        Assert.Equal("São Paulo", result[0].Address["city"]);
        Assert.Equal("Praça da Sé", result[1].Name);
        Assert.Equal(-23.55, result[1].Latitude);
        Assert.Empty(result[1].Address);
    }

    [Fact]
    public async Task SearchAsync_ElementWithUnparsableCoordinates_IsSkipped()
    {
        // [AC] REQ-VEN-25: a search element whose lat or lon cannot be parsed is skipped, the others are kept
        const string body = "[{\"lat\":\"x\",\"lon\":\"1\",\"name\":\"Bad\"},{\"lat\":\"1\",\"lon\":\"2\",\"name\":\"Good\"}]";
        var (provider, _) = Create(_ => Json(body));

        var result = await provider.SearchAsync("place", "en-US", 5);

        var record = Assert.Single(result);
        Assert.Equal("Good", record.Name);
    }

    [Fact]
    public async Task SearchAsync_EmptyArray_ReturnsEmptyList()
    {
        // [AC] REQ-VEN-27: a search with no match answers an empty list, not null and not an error
        var (provider, _) = Create(_ => Json("[]"));

        var result = await provider.SearchAsync("zzzzzz", "en-US", 5);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task ReverseAsync_NonSuccessStatus_ThrowsHttpRequestException(HttpStatusCode status)
    {
        // [AC] REQ-VEN-28: a reverse lookup with a non-200 status throws HttpRequestException so the service can show "unavailable"
        var (provider, _) = Create(_ => Json("{}", status));

        await Assert.ThrowsAsync<HttpRequestException>(() => provider.ReverseAsync(1.0, 2.0, "en-US"));
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task SearchAsync_NonSuccessStatus_ThrowsHttpRequestException(HttpStatusCode status)
    {
        // [AC] REQ-VEN-28: a search lookup with a non-200 status throws HttpRequestException so the service can show "unavailable"
        var (provider, _) = Create(_ => Json("[]", status));

        await Assert.ThrowsAsync<HttpRequestException>(() => provider.SearchAsync("bar", "en-US", 5));
    }

    [Fact]
    public async Task ReverseAsync_CallerCancelled_PropagatesOperationCanceled()
    {
        // [AC] REQ-VEN-28: caller cancellation propagates as OperationCanceledException and is not reported as a failure
        var (provider, _) = Create(_ => Json(BrahmaReverseBody));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.ReverseAsync(1.0, 2.0, "en-US", cts.Token));
    }
}
