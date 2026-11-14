using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using MyVocaList.Contracts.DTOs.Venues;
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Services.Places;

namespace MyVocaList.Tests.Unit.Services.Places;

/// <summary>
/// V7c — <see cref="PlaceLookupService"/>: offline skip, 5,000 ms bound, failure mapping, search guard and
/// the usage rules of the gate (REQ-VEN-25..28, 34). The provider is mocked; the gate is real and uses the
/// manual clock from <c>PlaceLookupGateTests.cs</c>.
/// </summary>
public class PlaceLookupServiceTests
{
    private const double TappedLatitude = 38.71234;
    private const double TappedLongitude = -9.13987;

    private static readonly IReadOnlyDictionary<string, string> RoadAddress = new Dictionary<string, string>
    {
        ["road"] = "Rua Augusta",
        ["house_number"] = "10",
        ["city"] = "Lisboa",
        ["country"] = "Portugal",
        ["country_code"] = "pt",
    };

    private sealed class Harness
    {
        public required Mock<IGeocodingProvider> Provider { get; init; }
        public required Mock<INetworkStatus> Network { get; init; }
        public required ManualTimeProvider Time { get; init; }
        public required PlaceLookupService Service { get; init; }
    }

    private static Harness Build(bool online = true)
    {
        var provider = new Mock<IGeocodingProvider>();
        var network = new Mock<INetworkStatus>();
        network.SetupGet(n => n.HasInternetAccess).Returns(online);
        var time = new ManualTimeProvider();
        var gate = new PlaceLookupGate(time);
        var service = new PlaceLookupService(provider.Object, gate, network.Object, NullLogger<PlaceLookupService>.Instance);
        return new Harness { Provider = provider, Network = network, Time = time, Service = service };
    }

    private static GeocodingRecord RoadRecord(double lat = 40.0, double lon = -8.0) =>
        new(lat, lon, null, null, "Rua Augusta 10, Lisboa, Portugal", RoadAddress);

    [Fact]
    // [AC] REQ-VEN-26: a record with an address gives Found, mapped fields and the TAPPED coordinates
    public async Task ReverseAsync_RecordWithAddress_ReturnsFoundWithTappedCoordinates()
    {
        var h = Build();
        h.Provider.Setup(p => p.ReverseAsync(TappedLatitude, TappedLongitude, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RoadRecord());

        var result = await h.Service.ReverseAsync(TappedLatitude, TappedLongitude);

        Assert.Equal(PlaceLookupStatus.Found, result.Status);
        Assert.NotNull(result.Place);
        Assert.Equal(TappedLatitude, result.Place.Latitude);
        Assert.Equal(TappedLongitude, result.Place.Longitude);
        Assert.Equal("Rua Augusta, 10", result.Place.Address.Line1);   // country_code "pt" puts the number after the road (V7b mapper)
        Assert.Equal("Lisboa", result.Place.Address.City);
        Assert.Equal("Portugal", result.Place.Address.Country);
    }

    [Fact]
    // [AC] REQ-VEN-27: no record from the provider gives NoAddress with the tapped coordinates and an empty address
    public async Task ReverseAsync_ProviderReturnsNull_ReturnsNoAddressWithTappedCoordinates()
    {
        var h = Build();
        h.Provider.Setup(p => p.ReverseAsync(TappedLatitude, TappedLongitude, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GeocodingRecord?)null);

        var result = await h.Service.ReverseAsync(TappedLatitude, TappedLongitude);

        Assert.Equal(PlaceLookupStatus.NoAddress, result.Status);
        Assert.NotNull(result.Place);
        Assert.Equal(TappedLatitude, result.Place.Latitude);
        Assert.Equal(TappedLongitude, result.Place.Longitude);
        Assert.Equal(VenueAddress.Empty, result.Place.Address);
        Assert.Equal(string.Empty, result.Place.AddressLine);
    }

    [Fact]
    // [AC] REQ-VEN-27: a record whose address has no usable field gives NoAddress
    public async Task ReverseAsync_RecordWithEmptyAddress_ReturnsNoAddress()
    {
        var h = Build();
        var empty = new GeocodingRecord(40.0, -8.0, null, null, "Somewhere", new Dictionary<string, string>());
        h.Provider.Setup(p => p.ReverseAsync(TappedLatitude, TappedLongitude, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(empty);

        var result = await h.Service.ReverseAsync(TappedLatitude, TappedLongitude);

        Assert.Equal(PlaceLookupStatus.NoAddress, result.Status);
        Assert.Equal(TappedLatitude, result.Place!.Latitude);
    }

    [Fact]
    // [AC] REQ-VEN-28: a provider transport failure gives Unavailable and no exception reaches the caller
    public async Task ReverseAsync_ProviderThrowsHttpRequestException_ReturnsUnavailable()
    {
        var h = Build();
        h.Provider.Setup(p => p.ReverseAsync(TappedLatitude, TappedLongitude, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("network down"));

        var result = await h.Service.ReverseAsync(TappedLatitude, TappedLongitude);

        Assert.Equal(PlaceLookupStatus.Unavailable, result.Status);
        Assert.Null(result.Place);
    }

    [Fact]
    // [AC] REQ-VEN-28: offline gives Unavailable without calling the provider
    public async Task ReverseAsync_Offline_ReturnsUnavailableWithoutProviderCall()
    {
        var h = Build(online: false);

        var result = await h.Service.ReverseAsync(TappedLatitude, TappedLongitude);

        Assert.Equal(PlaceLookupStatus.Unavailable, result.Status);
        h.Provider.Verify(p => p.ReverseAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-28: a provider that never answers is abandoned after the bound and gives Unavailable
    public async Task ReverseAsync_ProviderNeverAnswers_ReturnsUnavailableAfterBound()
    {
        var h = Build();
        h.Provider.Setup(p => p.ReverseAsync(TappedLatitude, TappedLongitude, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<double, double, string, CancellationToken>(async (_, _, _, ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return null;
            });

        var result = await h.Service.ReverseAsync(TappedLatitude, TappedLongitude);

        Assert.Equal(PlaceLookupStatus.Unavailable, result.Status);
    }

    [Fact]
    // [AC] REQ-VEN-28: the caller's own cancellation propagates as OperationCanceledException
    public async Task ReverseAsync_CallerCancelled_ThrowsOperationCanceled()
    {
        var h = Build();
        using var cts = new CancellationTokenSource();
        h.Provider.Setup(p => p.ReverseAsync(TappedLatitude, TappedLongitude, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<double, double, string, CancellationToken>(async (_, _, _, ct) =>
            {
                cts.Cancel();
                await Task.Delay(Timeout.Infinite, ct);
                return null;
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => h.Service.ReverseAsync(TappedLatitude, TappedLongitude, cts.Token));
    }

    [Fact]
    // [AC] REQ-VEN-34: the same point twice reaches the provider once
    public async Task ReverseAsync_SamePointTwice_CallsProviderOnce()
    {
        var h = Build();
        h.Provider.Setup(p => p.ReverseAsync(TappedLatitude, TappedLongitude, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RoadRecord());

        await h.Service.ReverseAsync(TappedLatitude, TappedLongitude);
        var second = await h.Service.ReverseAsync(TappedLatitude, TappedLongitude);

        Assert.Equal(PlaceLookupStatus.Found, second.Status);
        h.Provider.Verify(p => p.ReverseAsync(TappedLatitude, TappedLongitude, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-VEN-34: a failed reverse is not remembered, so the same reverse is sent again
    public async Task ReverseAsync_FailedThenSameReverse_CallsProviderTwice()
    {
        var h = Build();
        h.Provider.SetupSequence(p => p.ReverseAsync(TappedLatitude, TappedLongitude, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("first fails"))
            .ReturnsAsync(RoadRecord());

        var first = await h.Service.ReverseAsync(TappedLatitude, TappedLongitude);
        h.Time.Advance(TimeSpan.FromSeconds(1));
        var second = await h.Service.ReverseAsync(TappedLatitude, TappedLongitude);

        Assert.Equal(PlaceLookupStatus.Unavailable, first.Status);
        Assert.Equal(PlaceLookupStatus.Found, second.Status);
        h.Provider.Verify(p => p.ReverseAsync(TappedLatitude, TappedLongitude, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    // [AC] REQ-VEN-34: the provider is asked in the language of CurrentUICulture
    public async Task ReverseAsync_PassesCurrentUICultureAsLanguage()
    {
        var h = Build();
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("pt-PT");
            string? language = null;
            h.Provider.Setup(p => p.ReverseAsync(TappedLatitude, TappedLongitude, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<double, double, string, CancellationToken>((_, _, lang, _) => language = lang)
                .ReturnsAsync(RoadRecord());

            await h.Service.ReverseAsync(TappedLatitude, TappedLongitude);

            Assert.Equal(CultureInfo.CurrentUICulture.Name, language);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    // [AC] REQ-VEN-25: search text shorter than three characters gives Found with no places and no request
    public async Task SearchAsync_TwoCharacters_ReturnsEmptyFoundWithoutProviderCall()
    {
        var h = Build();

        var result = await h.Service.SearchAsync("  ab ");

        Assert.Equal(PlaceLookupStatus.Found, result.Status);
        Assert.Empty(result.Places);
        h.Provider.Verify(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-25: three characters are searched with the trimmed text and a limit of five
    public async Task SearchAsync_ThreeCharacters_SearchesTrimmedTextWithLimitFive()
    {
        var h = Build();
        h.Provider.Setup(p => p.SearchAsync("abc", It.IsAny<string>(), 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GeocodingRecord>());

        var result = await h.Service.SearchAsync("  abc  ");

        Assert.Equal(PlaceLookupStatus.Found, result.Status);
        h.Provider.Verify(p => p.SearchAsync("abc", It.IsAny<string>(), 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // [AC] REQ-VEN-25: each search record becomes one place, in order, with its own coordinates
    public async Task SearchAsync_TwoRecords_ReturnsTwoPlacesInOrderWithTheirOwnCoordinates()
    {
        var h = Build();
        var first = new GeocodingRecord(41.1, -8.6, null, null, "First", RoadAddress);
        var second = new GeocodingRecord(42.2, -8.7, null, null, "Second", RoadAddress);
        h.Provider.Setup(p => p.SearchAsync("porto", It.IsAny<string>(), 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GeocodingRecord> { first, second });

        var result = await h.Service.SearchAsync("porto");

        Assert.Equal(PlaceLookupStatus.Found, result.Status);
        Assert.Equal(2, result.Places.Count);
        Assert.Equal(41.1, result.Places[0].Latitude);
        Assert.Equal(-8.6, result.Places[0].Longitude);
        Assert.Equal(42.2, result.Places[1].Latitude);
        Assert.Equal(-8.7, result.Places[1].Longitude);
    }

    [Fact]
    // [AC] REQ-VEN-25: an empty answer is a valid Found result
    public async Task SearchAsync_EmptyAnswer_ReturnsFoundWithNoPlaces()
    {
        var h = Build();
        h.Provider.Setup(p => p.SearchAsync("nowhere", It.IsAny<string>(), 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GeocodingRecord>());

        var result = await h.Service.SearchAsync("nowhere");

        Assert.Equal(PlaceLookupStatus.Found, result.Status);
        Assert.Empty(result.Places);
    }

    [Fact]
    // [AC] REQ-VEN-28: a provider failure during search gives Unavailable with no places
    public async Task SearchAsync_ProviderThrows_ReturnsUnavailable()
    {
        var h = Build();
        h.Provider.Setup(p => p.SearchAsync("porto", It.IsAny<string>(), 5, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("network down"));

        var result = await h.Service.SearchAsync("porto");

        Assert.Equal(PlaceLookupStatus.Unavailable, result.Status);
        Assert.Empty(result.Places);
    }

    [Fact]
    // [AC] REQ-VEN-28: offline search gives Unavailable without a request
    public async Task SearchAsync_Offline_ReturnsUnavailableWithoutProviderCall()
    {
        var h = Build(online: false);

        var result = await h.Service.SearchAsync("porto");

        Assert.Equal(PlaceLookupStatus.Unavailable, result.Status);
        Assert.Empty(result.Places);
        h.Provider.Verify(p => p.SearchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] REQ-VEN-34: the same search text twice reaches the provider once
    public async Task SearchAsync_SameTextTwice_CallsProviderOnce()
    {
        var h = Build();
        h.Provider.Setup(p => p.SearchAsync("porto", It.IsAny<string>(), 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GeocodingRecord> { RoadRecord() });

        await h.Service.SearchAsync("porto");
        var second = await h.Service.SearchAsync("porto");

        Assert.Single(second.Places);
        h.Provider.Verify(p => p.SearchAsync("porto", It.IsAny<string>(), 5, It.IsAny<CancellationToken>()), Times.Once);
    }
}
