using System.Net;
using System.Net.Http;
using System.Text.Json;
using Moq;
using Moq.Protected;
using MyVocaList.Contracts.DTOs;

namespace MyVocaList.Tests.Unit.Services;

public class VersionCheckServiceTests
{
    private readonly Mock<IHttpClientFactory> _factoryMock = new();
    private readonly Mock<ILogger<VersionCheckService>> _loggerMock = new();

    private static readonly string ValidManifestJson = JsonSerializer.Serialize(new
    {
        latestVersion = "2.0.0",
        minRequiredVersion = "1.5.0",
        storeUrls = new { android = "https://play.google.com/store/apps/details?id=com.myvocalist", ios = "https://apps.apple.com/app/id123" },
        updateMessage = "Please update to continue."
    });

    private void SetupHttpResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        _factoryMock.Setup(f => f.CreateClient("version-check"))
            .Returns(new HttpClient(handler.Object));
    }

    private void SetupNetworkFailure()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network unavailable"));
        _factoryMock.Setup(f => f.CreateClient("version-check"))
            .Returns(new HttpClient(handler.Object));
    }

    private VersionCheckService CreateSut(string currentVersion, string platformKey)
        => new(_factoryMock.Object, currentVersion, platformKey, _loggerMock.Object);

    // [AC] AC-UC-03: App proceeds when up to date
    [Fact]
    public async Task CheckForUpdatesAsync_UpToDate_ReturnsUpToDate()
    {
        SetupHttpResponse(ValidManifestJson);
        var sut = CreateSut("2.0.0", "android");

        var result = await sut.CheckForUpdatesAsync();

        Assert.True(result.IsUpToDate);
        Assert.False(result.IsUpdateAvailable);
        Assert.False(result.IsUpdateRequired);
    }

    // [AC] AC-UC-01: Soft nudge when update available but above minimum
    [Fact]
    public async Task CheckForUpdatesAsync_UpdateAvailable_ReturnsIsUpdateAvailable()
    {
        SetupHttpResponse(ValidManifestJson);
        var sut = CreateSut("1.8.0", "android");

        var result = await sut.CheckForUpdatesAsync();

        Assert.False(result.IsUpToDate);
        Assert.True(result.IsUpdateAvailable);
        Assert.False(result.IsUpdateRequired);
        Assert.Equal("2.0.0", result.LatestVersion);
        Assert.Contains("play.google.com", result.StoreUrl);
    }

    // [AC] AC-UC-02: Hard block when below minimum
    [Fact]
    public async Task CheckForUpdatesAsync_BelowMinimum_ReturnsIsUpdateRequired()
    {
        SetupHttpResponse(ValidManifestJson);
        var sut = CreateSut("1.0.0", "android");

        var result = await sut.CheckForUpdatesAsync();

        Assert.False(result.IsUpToDate);
        Assert.False(result.IsUpdateAvailable);
        Assert.True(result.IsUpdateRequired);
        Assert.Equal("Please update to continue.", result.UpdateMessage);
    }

    // [AC] AC-UC-05: iOS URL returned for iOS platform
    [Fact]
    public async Task CheckForUpdatesAsync_UpdateAvailableOnIos_ReturnsIosStoreUrl()
    {
        SetupHttpResponse(ValidManifestJson);
        var sut = CreateSut("1.8.0", "ios");

        var result = await sut.CheckForUpdatesAsync();

        Assert.Contains("apps.apple.com", result.StoreUrl);
    }

    // [AC] AC-UC-04: Fail-open on network error
    [Fact]
    public async Task CheckForUpdatesAsync_NetworkFailure_ReturnsUpToDate()
    {
        SetupNetworkFailure();
        var sut = CreateSut("1.0.0", "android");

        var result = await sut.CheckForUpdatesAsync();

        Assert.True(result.IsUpToDate);
        Assert.False(result.IsUpdateRequired);
    }

    // Validation rule: malformed JSON -> fail-open
    [Fact]
    public async Task CheckForUpdatesAsync_MalformedJson_ReturnsUpToDate()
    {
        SetupHttpResponse("{ not valid json {{");
        var sut = CreateSut("1.0.0", "android");

        var result = await sut.CheckForUpdatesAsync();

        Assert.True(result.IsUpToDate);
    }

    // [AC] AC-UC-06: SemVer pre-release -- 0.3.0-alpha.5 < 0.3.0
    [Fact]
    public async Task CheckForUpdatesAsync_PreReleaseVersion_ComparedCorrectly()
    {
        var manifest = JsonSerializer.Serialize(new
        {
            latestVersion = "0.3.0",
            minRequiredVersion = "0.1.0",
            storeUrls = new { android = "https://play.google.com/store" },
            updateMessage = ""
        });
        SetupHttpResponse(manifest);
        var sut = CreateSut("0.3.0-alpha.5", "android");

        var result = await sut.CheckForUpdatesAsync();

        Assert.True(result.IsUpdateAvailable);
    }

    // Validation rule: missing storeUrls key -> empty string, no crash
    [Fact]
    public async Task CheckForUpdatesAsync_MissingStoreUrlKey_ReturnsEmptyStoreUrl()
    {
        var manifest = JsonSerializer.Serialize(new
        {
            latestVersion = "2.0.0",
            minRequiredVersion = "1.5.0",
            storeUrls = new { android = "https://play.google.com" },
            updateMessage = ""
        });
        SetupHttpResponse(manifest);
        var sut = CreateSut("1.8.0", "ios");

        var result = await sut.CheckForUpdatesAsync();

        Assert.True(result.IsUpdateAvailable);
        Assert.Equal(string.Empty, result.StoreUrl);
    }
}
