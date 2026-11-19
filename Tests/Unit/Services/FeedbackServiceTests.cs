using Microsoft.Extensions.Configuration;
using Moq.Protected;
using MyVocaList.Contracts.DTOs;
using System.Net;

namespace MyVocaList.Tests.Unit.Services;

public class FeedbackServiceTests
{
    private readonly Mock<IHttpClientFactory> _factoryMock = new();
    private readonly Mock<IAppInfo> _appInfoMock = new();
    private readonly Mock<IDeviceInfo> _deviceInfoMock = new();
    private readonly Mock<ILogger<FeedbackService>> _loggerMock = new();

    private static Microsoft.Extensions.Configuration.IConfiguration ValidConfig()
    {
        var data = new Dictionary<string, string?>
        {
            ["GitHub:FeedbackPat"] = "github_pat_test",
            ["GitHub:FeedbackRepo"] = "heldercsousa/MyVocaList"
        };
        return new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
    }

    private static Microsoft.Extensions.Configuration.IConfiguration MissingPatConfig()
    {
        var data = new Dictionary<string, string?>
        {
            ["GitHub:FeedbackPat"] = "",
            ["GitHub:FeedbackRepo"] = "heldercsousa/MyVocaList"
        };
        return new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
    }

    private void SetupHttpResponse(HttpStatusCode statusCode)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("{\"number\": 1}")
            });

        var client = new HttpClient(handlerMock.Object);
        _factoryMock.Setup(f => f.CreateClient("feedback")).Returns(client);
    }

    private FeedbackService CreateSut(Microsoft.Extensions.Configuration.IConfiguration? config = null)
    {
        _appInfoMock.Setup(a => a.VersionString).Returns("1.0.0");
        _deviceInfoMock.Setup(d => d.Platform).Returns(DevicePlatform.Android);
        _deviceInfoMock.Setup(d => d.Model).Returns("TestDevice");

        return new FeedbackService(
            _factoryMock.Object,
            config ?? ValidConfig(),
            _appInfoMock.Object,
            _deviceInfoMock.Object,
            _loggerMock.Object);
    }

    // [AC] AC-FB-01: Successful submission creates GitHub Issue
    [Fact]
    public async Task SubmitAsync_ValidSubmission_ReturnsSuccess()
    {
        SetupHttpResponse(HttpStatusCode.Created);
        var sut = CreateSut();
        var submission = new FeedbackSubmission(FeedbackCategory.BugReport, "The app crashes on startup", null);

        var (success, error) = await sut.SubmitAsync(submission);

        Assert.True(success);
        Assert.Null(error);
    }

    // [AC] AC-FB-06: API error returns failure
    [Fact]
    public async Task SubmitAsync_HttpError_ReturnsFailure()
    {
        SetupHttpResponse(HttpStatusCode.UnprocessableEntity);
        var sut = CreateSut();
        var submission = new FeedbackSubmission(FeedbackCategory.BugReport, "Test message", null);

        var (success, error) = await sut.SubmitAsync(submission);

        Assert.False(success);
        Assert.NotNull(error);
    }

    // [AC] AC-FB-06: Network exception returns failure
    [Fact]
    public async Task SubmitAsync_NetworkException_ReturnsFailure()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network unreachable"));
        _factoryMock.Setup(f => f.CreateClient("feedback"))
            .Returns(new HttpClient(handlerMock.Object));

        var sut = CreateSut();
        var submission = new FeedbackSubmission(FeedbackCategory.FeatureRequest, "Add dark mode", null);

        var (success, error) = await sut.SubmitAsync(submission);

        Assert.False(success);
        Assert.NotNull(error);
    }

    // Validation rule: missing PAT -> failure without HTTP call
    [Fact]
    public async Task SubmitAsync_MissingPat_ReturnsFailureWithoutHttpCall()
    {
        var sut = CreateSut(MissingPatConfig());
        var submission = new FeedbackSubmission(FeedbackCategory.Other, "Some feedback", null);

        var (success, error) = await sut.SubmitAsync(submission);

        Assert.False(success);
        Assert.NotNull(error);
        _factoryMock.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
    }

    // [AC] AC-FB-02: Issue body includes version metadata
    [Fact]
    public async Task SubmitAsync_ValidSubmission_RequestBodyContainsMetadata()
    {
        HttpRequestMessage? capturedRequest = null;
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("{\"number\": 1}")
            });
        _factoryMock.Setup(f => f.CreateClient("feedback"))
            .Returns(new HttpClient(handlerMock.Object));

        var sut = CreateSut();
        var submission = new FeedbackSubmission(FeedbackCategory.BugReport, "Crash on startup", "user@test.com");

        await sut.SubmitAsync(submission);

        Assert.NotNull(capturedRequest);
        var body = await capturedRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("1.0.0", body);
        Assert.Contains("Android", body);
        Assert.Contains("TestDevice", body);
        Assert.Contains("user@test.com", body);
    }

    // [AC] AC-FB-01: Title format is [Category] first 60 chars
    [Fact]
    public async Task SubmitAsync_ValidSubmission_IssueTitleFormattedCorrectly()
    {
        HttpRequestMessage? capturedRequest = null;
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("{\"number\": 1}")
            });
        _factoryMock.Setup(f => f.CreateClient("feedback"))
            .Returns(new HttpClient(handlerMock.Object));

        var sut = CreateSut();
        var submission = new FeedbackSubmission(FeedbackCategory.FeatureRequest, "Add export to PDF functionality", null);

        await sut.SubmitAsync(submission);

        var body = await capturedRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("[Feature Request] Add export to PDF functionality", body);
    }
}
