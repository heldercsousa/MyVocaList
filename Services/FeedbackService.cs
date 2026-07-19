using Microsoft.Extensions.Configuration;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Domain.ServicesInterfaces;
using System.Net.Http.Json;

namespace MyVocaList.Services;

public sealed class FeedbackService : IFeedbackService
{
    private const string ClientName = "feedback";
    private static readonly Dictionary<FeedbackCategory, (string label, string githubLabel)> CategoryMap = new()
    {
        [FeedbackCategory.BugReport] = ("Bug Report", "bug"),
        [FeedbackCategory.FeatureRequest] = ("Feature Request", "enhancement"),
        [FeedbackCategory.Other] = ("Other", "question"),
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IAppInfo _appInfo;
    private readonly IDeviceInfo _deviceInfo;
    private readonly ILogger<FeedbackService> _logger;

    public FeedbackService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IAppInfo appInfo,
        IDeviceInfo deviceInfo,
        ILogger<FeedbackService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _appInfo = appInfo;
        _deviceInfo = deviceInfo;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(bool success, string? error)> SubmitAsync(
        FeedbackSubmission submission, CancellationToken ct = default)
    {
        var pat = _configuration["GitHub:FeedbackPat"];
        var repo = _configuration["GitHub:FeedbackRepo"] ?? "heldercsousa/MyVocaList";

        if (string.IsNullOrWhiteSpace(pat))
        {
            _logger.LogWarning("GitHub:FeedbackPat is not configured — feedback submission skipped");
            return (false, "Could not send — please try again");
        }

        var (displayLabel, githubLabel) = CategoryMap[submission.Category];
        var truncatedMessage = submission.Message.Length > 60
            ? submission.Message[..60]
            : submission.Message;

        var title = $"[{displayLabel}] {truncatedMessage}";
        var body = BuildIssueBody(submission, displayLabel);
        var labels = new[] { "user-feedback", githubLabel };

        var payload = new { title, body, labels };

        try
        {
            var client = _httpClientFactory.CreateClient(ClientName);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {pat}");
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
            client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            client.DefaultRequestHeaders.Add("User-Agent", "MyVocaList-App");

            var response = await client.PostAsJsonAsync(
                $"https://api.github.com/repos/{repo}/issues", payload, ct);

            if (response.IsSuccessStatusCode)
                return (true, null);

            _logger.LogWarning("GitHub Issues API returned {StatusCode}", response.StatusCode);
            return (false, "Could not send — please try again");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Feedback submission failed");
            return (false, "Could not send — please try again");
        }
    }

    private string BuildIssueBody(FeedbackSubmission submission, string displayLabel)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(submission.Message.Trim());
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine($"**App version:** {_appInfo.VersionString}");
        sb.AppendLine($"**OS:** {_deviceInfo.Platform} {_deviceInfo.VersionString}");
        sb.AppendLine($"**Device:** {_deviceInfo.Model}");
        sb.AppendLine($"**Submitted:** {DateTime.UtcNow:O}");

        if (!string.IsNullOrWhiteSpace(submission.Email))
            sb.AppendLine($"**Contact:** {submission.Email}");

        return sb.ToString();
    }
}
