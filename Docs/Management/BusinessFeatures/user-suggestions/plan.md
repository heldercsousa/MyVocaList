# User Suggestions — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an in-app feedback form that submits user suggestions as GitHub Issues in the MyVocaList repo, with auto-captured device/version metadata and graceful failure handling.

**Architecture:** `IFeedbackService` (Domain) → `FeedbackService` (Services, uses `IHttpClientFactory`) → `FeedbackViewModel` + `FeedbackPage` (MAUI). Entry point is a "Send Feedback" item on `SettingsPage`. PAT stored in `appsettings.json` (gitignored). No local persistence — form clears on success, retains content on failure.

**Tech Stack:** .NET MAUI 10 · DevExpress MAUI (`dxe:ComboBoxEdit`, `dxe:MultilineEdit`, `dxe:TextEdit`, `dx:DXButton`) · `IHttpClientFactory` · GitHub Issues REST API v3 · xUnit + Moq

---

## Existing Assets (do NOT recreate)

- `MyVocaList/Navigation/Routes.cs` — add `Feedback = "feedback"` constant
- `MyVocaList/appsettings.template.json` — add `GitHub` section
- `MyVocaList/UI/Pages/Settings/SettingsPage.xaml` — add "Send Feedback" entry at bottom

---

## File Map

| File | Action | Responsibility |
|------|--------|----------------|
| `MyVocaList.Contracts/DTOs/FeedbackSubmission.cs` | **Create** | DTO + `FeedbackCategory` enum |
| `MyVocaList.Domain/ServicesInterfaces/IFeedbackService.cs` | **Create** | Service interface |
| `MyVocaList.Tests/Unit/Services/FeedbackServiceTests.cs` | **Create** | Unit tests |
| `MyVocaList.Services/FeedbackService.cs` | **Create** | GitHub Issues HTTP implementation |
| `MyVocaList/UI/ViewModels/FeedbackViewModel.cs` | **Create** | Form state + submit command |
| `MyVocaList/UI/Pages/Feedback/FeedbackPage.xaml` | **Create** | Form UI |
| `MyVocaList/UI/Pages/Feedback/FeedbackPage.xaml.cs` | **Create** | Code-behind |
| `MyVocaList/Navigation/Routes.cs` | **Modify** | Add `Feedback` route constant |
| `MyVocaList/AppShell.xaml.cs` | **Modify** | Register `feedback` route |
| `MyVocaList/UI/Pages/Settings/SettingsPage.xaml` | **Modify** | Add "Send Feedback" list item |
| `MyVocaList/UI/Pages/Settings/SettingsViewModel.cs` | **Modify** | Add `NavigateToFeedbackCommand` |
| `MyVocaList/appsettings.json` | **Modify** | Add `GitHub` section (gitignored — create if absent) |
| `MyVocaList/appsettings.template.json` | **Modify** | Add `GitHub` section with empty values |
| `MyVocaList/MauiProgram.cs` | **Modify** | Register `IFeedbackService`, `FeedbackViewModel`, `FeedbackPage`; configure `HttpClient` |
| `MyVocaList.sln` | **Modify** | Register new doc files |

---

## Task 1 — DTOs + Interface

**Files:**
- Create: `MyVocaList.Contracts/DTOs/FeedbackSubmission.cs`
- Create: `MyVocaList.Domain/ServicesInterfaces/IFeedbackService.cs`

- [ ] **Step 1.1 — Create FeedbackSubmission DTO and FeedbackCategory enum**

  `MyVocaList.Contracts/DTOs/FeedbackSubmission.cs`:

  ```csharp
  namespace MyVocaList.Contracts.DTOs;

  public enum FeedbackCategory { BugReport, FeatureRequest, Other }

  public record FeedbackSubmission(
      FeedbackCategory Category,
      string Message,
      string? Email);
  ```

- [ ] **Step 1.2 — Create IFeedbackService interface**

  `MyVocaList.Domain/ServicesInterfaces/IFeedbackService.cs`:

  ```csharp
  namespace MyVocaList.Domain.ServicesInterfaces;

  public interface IFeedbackService
  {
      /// <summary>Submits a user suggestion as a GitHub Issue.</summary>
      /// <returns>(true, null) on success; (false, errorMessage) on failure.</returns>
      Task<(bool success, string? error)> SubmitAsync(FeedbackSubmission submission, CancellationToken ct = default);
  }
  ```

- [ ] **Step 1.3 — Build**

  ```bash
  dotnet build MyVocaList.sln
  ```
  Expected: 0 errors.

- [ ] **Step 1.4 — Commit**

  ```bash
  git add MyVocaList.Contracts/DTOs/FeedbackSubmission.cs
  git add MyVocaList.Domain/ServicesInterfaces/IFeedbackService.cs
  git commit -m "feat(feedback): add FeedbackSubmission DTO and IFeedbackService interface"
  ```

---

## Task 2 — Unit tests for `FeedbackService` (Red)

**Files:**
- Create: `MyVocaList.Tests/Unit/Services/FeedbackServiceTests.cs`

- [ ] **Step 2.1 — Create test file**

  `MyVocaList.Tests/Unit/Services/FeedbackServiceTests.cs`:

  ```csharp
  using System.Net;
  using System.Net.Http;
  using Microsoft.Maui.ApplicationModel;
  using Microsoft.Maui.ApplicationModel.DataTransfer;
  using Moq;
  using Moq.Protected;

  namespace MyVocaList.Tests.Unit.Services;

  public class FeedbackServiceTests
  {
      private readonly Mock<IHttpClientFactory> _factoryMock = new();
      private readonly Mock<IAppInfo> _appInfoMock = new();
      private readonly Mock<IDeviceInfo> _deviceInfoMock = new();
      private readonly Mock<ILogger<FeedbackService>> _loggerMock = new();

      // Configuration with a valid PAT and repo
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

      // [AC] AC-FB-06: API error returns failure with message preserved (service returns error string)
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

      // Validation rule: missing PAT → failure (no crash)
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
          Assert.Contains("1.0.0", body);          // version
          Assert.Contains("Android", body);         // OS
          Assert.Contains("TestDevice", body);      // device
          Assert.Contains("user@test.com", body);   // email when provided
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
  ```

- [ ] **Step 2.2 — Run tests to confirm Red**

  ```bash
  dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~FeedbackServiceTests" --verbosity normal
  ```
  Expected: Build error (`FeedbackService` not found). Correct — proceed to Task 3.

- [ ] **Step 2.3 — Commit**

  ```bash
  git add MyVocaList.Tests/Unit/Services/FeedbackServiceTests.cs
  git commit -m "test(feedback): add FeedbackService unit tests (Red)"
  ```

---

## Task 3 — Implement `FeedbackService` (Green)

**Files:**
- Create: `MyVocaList.Services/FeedbackService.cs`

- [ ] **Step 3.1 — Create FeedbackService**

  `MyVocaList.Services/FeedbackService.cs`:

  ```csharp
  using System.Net.Http.Json;
  using System.Text.Json;
  using Microsoft.Extensions.Configuration;
  using Microsoft.Maui.ApplicationModel;

  namespace MyVocaList.Services;

  public sealed class FeedbackService : IFeedbackService
  {
      private const string ClientName = "feedback";
      private static readonly Dictionary<FeedbackCategory, (string label, string githubLabel)> CategoryMap = new()
      {
          [FeedbackCategory.BugReport]      = ("Bug Report",      "bug"),
          [FeedbackCategory.FeatureRequest] = ("Feature Request", "enhancement"),
          [FeedbackCategory.Other]          = ("Other",           "question"),
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
          var pat  = _configuration["GitHub:FeedbackPat"];
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
          var body  = BuildIssueBody(submission, displayLabel);
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
          sb.AppendLine($"**OS:** {_deviceInfo.Platform} {DeviceInfo.Current.VersionString}");
          sb.AppendLine($"**Device:** {_deviceInfo.Model}");
          sb.AppendLine($"**Submitted:** {DateTime.UtcNow:O}");

          if (!string.IsNullOrWhiteSpace(submission.Email))
              sb.AppendLine($"**Contact:** {submission.Email}");

          return sb.ToString();
      }
  }
  ```

- [ ] **Step 3.2 — Run tests to confirm Green**

  ```bash
  dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~FeedbackServiceTests" --verbosity normal
  ```
  Expected: All 6 tests PASS.

- [ ] **Step 3.3 — Full build**

  ```bash
  dotnet build MyVocaList.sln
  ```
  Expected: 0 errors.

- [ ] **Step 3.4 — Commit**

  ```bash
  git add MyVocaList.Services/FeedbackService.cs
  git commit -m "feat(feedback): implement FeedbackService using GitHub Issues REST API"
  ```

---

## Task 4 — `FeedbackViewModel`

**Files:**
- Create: `MyVocaList/UI/ViewModels/FeedbackViewModel.cs`

- [ ] **Step 4.1 — Create FeedbackViewModel**

  `MyVocaList/UI/ViewModels/FeedbackViewModel.cs`:

  ```csharp
  namespace MyVocaList.UI.ViewModels;

  public sealed partial class FeedbackViewModel : ViewModelBase
  {
      private readonly IFeedbackService _feedbackService;
      private readonly ISnackbarService _snackbar;

      [ObservableProperty]
      [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
      private string _message = string.Empty;

      [ObservableProperty]
      private string _email = string.Empty;

      [ObservableProperty]
      [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
      private bool _isSubmitting;

      [ObservableProperty]
      private FeedbackCategory _selectedCategory = FeedbackCategory.BugReport;

      public IReadOnlyList<FeedbackCategory> Categories { get; } =
          Enum.GetValues<FeedbackCategory>().ToList().AsReadOnly();

      public FeedbackViewModel(IFeedbackService feedbackService, ISnackbarService snackbar)
      {
          _feedbackService = feedbackService;
          _snackbar = snackbar;
      }

      private bool CanSubmit => !IsSubmitting && !string.IsNullOrWhiteSpace(Message);

      [RelayCommand(CanExecute = nameof(CanSubmit))]
      private async Task SubmitAsync()
      {
          IsSubmitting = true;
          try
          {
              var submission = new FeedbackSubmission(
                  SelectedCategory,
                  Message.Trim(),
                  string.IsNullOrWhiteSpace(Email) ? null : Email.Trim());

              var (success, error) = await _feedbackService.SubmitAsync(submission);

              if (success)
              {
                  Message = string.Empty;
                  Email   = string.Empty;
                  SelectedCategory = FeedbackCategory.BugReport;
                  _snackbar.Show("Feedback sent — thank you!");
              }
              else
              {
                  _snackbar.Show(error ?? "Could not send — please try again");
              }
          }
          finally
          {
              IsSubmitting = false;
          }
      }
  }
  ```

- [ ] **Step 4.2 — Build**

  ```bash
  dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
  ```
  Expected: 0 errors.

- [ ] **Step 4.3 — Commit**

  ```bash
  git add MyVocaList/UI/ViewModels/FeedbackViewModel.cs
  git commit -m "feat(feedback): add FeedbackViewModel with submit command and snackbar feedback"
  ```

---

## Task 5 — `FeedbackPage` XAML + code-behind

**Files:**
- Create: `MyVocaList/UI/Pages/Feedback/FeedbackPage.xaml`
- Create: `MyVocaList/UI/Pages/Feedback/FeedbackPage.xaml.cs`

> **Pattern reference:** See `MyVocaList/UI/Pages/Settings/SettingsPage.xaml` for ScrollView + VerticalStackLayout form structure and DevExpress edit controls.

- [ ] **Step 5.1 — Create FeedbackPage.xaml**

  `MyVocaList/UI/Pages/Feedback/FeedbackPage.xaml`:

  ```xml
  <?xml version="1.0" encoding="utf-8" ?>
  <ContentPage
      xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
      xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
      xmlns:dx="http://schemas.devexpress.com/maui"
      xmlns:dxe="clr-namespace:DevExpress.Maui.Editors;assembly=DevExpress.Maui.Editors"
      xmlns:vm="clr-namespace:MyVocaList.UI.ViewModels"
      x:Class="MyVocaList.UI.Pages.Feedback.FeedbackPage"
      x:DataType="vm:FeedbackViewModel"
      Title="Send Feedback"
      BackgroundColor="{StaticResource Surface}"
      SafeAreaEdges="Container">

      <ScrollView>
          <VerticalStackLayout Padding="24" Spacing="16">

              <Label
                  Text="What's on your mind?"
                  StyleClass="Title.Medium"
                  TextColor="{StaticResource OnSurface}" />

              <Label
                  Text="Your feedback helps improve MyVocaList. Bug reports include device info automatically."
                  StyleClass="Body.Small"
                  TextColor="{StaticResource OnSurfaceVariant}" />

              <!-- Category -->
              <dxe:ComboBoxEdit
                  LabelText="Category"
                  ItemsSource="{Binding Categories}"
                  SelectedItem="{Binding SelectedCategory, Mode=TwoWay}"
                  IsReadOnly="True" />

              <!-- Message -->
              <dxe:MultilineEdit
                  LabelText="Message"
                  PlaceholderText="Describe the issue or feature request..."
                  Text="{Binding Message, Mode=TwoWay}"
                  MaxCharacterCount="1000"
                  CharacterCounterVisibility="Visible"
                  MinLines="5"
                  MaxLines="10" />

              <!-- Email (optional) -->
              <dxe:TextEdit
                  LabelText="Contact email (optional)"
                  PlaceholderText="your@email.com"
                  Text="{Binding Email, Mode=TwoWay}"
                  Keyboard="Email" />

              <!-- Submit -->
              <Grid RowDefinitions="Auto,Auto" RowSpacing="8">
                  <dx:DXButton
                      Grid.Row="0"
                      Content="Send Feedback"
                      ButtonType="Filled"
                      HorizontalOptions="Fill"
                      Command="{Binding SubmitCommand}"
                      IsEnabled="{Binding IsSubmitting, Converter={StaticResource InverseBoolConverter}}" />

                  <ActivityIndicator
                      Grid.Row="1"
                      IsRunning="{Binding IsSubmitting}"
                      IsVisible="{Binding IsSubmitting}"
                      Color="{StaticResource Primary}"
                      HorizontalOptions="Center" />
              </Grid>

          </VerticalStackLayout>
      </ScrollView>

  </ContentPage>
  ```

- [ ] **Step 5.2 — Create FeedbackPage.xaml.cs**

  `MyVocaList/UI/Pages/Feedback/FeedbackPage.xaml.cs`:

  ```csharp
  namespace MyVocaList.UI.Pages.Feedback;

  public partial class FeedbackPage : ContentPage
  {
      public FeedbackPage(FeedbackViewModel vm)
      {
          InitializeComponent();
          BindingContext = vm;
      }
  }
  ```

- [ ] **Step 5.3 — Build**

  ```bash
  dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
  ```
  Expected: 0 errors. Fix any XAML binding or style reference errors before proceeding.

- [ ] **Step 5.4 — Commit**

  ```bash
  git add "MyVocaList/UI/Pages/Feedback/FeedbackPage.xaml"
  git add "MyVocaList/UI/Pages/Feedback/FeedbackPage.xaml.cs"
  git commit -m "feat(feedback): add FeedbackPage form UI"
  ```

---

## Task 6 — Route + Settings entry + appsettings

**Files:**
- Modify: `MyVocaList/Navigation/Routes.cs`
- Modify: `MyVocaList/AppShell.xaml.cs`
- Modify: `MyVocaList/UI/Pages/Settings/SettingsPage.xaml`
- Modify: `MyVocaList/UI/ViewModels/SettingsViewModel.cs`
- Modify: `MyVocaList/appsettings.template.json`
- Modify: `MyVocaList/appsettings.json` (gitignored — create if absent)

- [ ] **Step 6.1 — Add Feedback route constant**

  In `MyVocaList/Navigation/Routes.cs`, add:
  ```csharp
  public const string Feedback = "feedback";
  ```

- [ ] **Step 6.2 — Register route in AppShell.xaml.cs**

  In `AppShell.xaml.cs` constructor, after existing `Routing.RegisterRoute` calls:
  ```csharp
  Routing.RegisterRoute(Routes.Feedback, typeof(FeedbackPage));
  ```

- [ ] **Step 6.3 — Add NavigateToFeedbackCommand to SettingsViewModel**

  Read `MyVocaList/UI/ViewModels/SettingsViewModel.cs`, then add:

  ```csharp
  [RelayCommand]
  private async Task NavigateToFeedbackAsync()
      => await Shell.Current.GoToAsync(Routes.Feedback);
  ```

- [ ] **Step 6.4 — Add "Send Feedback" entry to SettingsPage.xaml**

  In `MyVocaList/UI/Pages/Settings/SettingsPage.xaml`, after the YouTube Integration section's closing `</VerticalStackLayout>` and before `</VerticalStackLayout>` of the outer stack, add:

  ```xml
  <!-- Feedback section -->
  <Label Text="Help &amp; Feedback"
         StyleClass="Title.Medium"
         TextColor="{StaticResource OnSurface}" />

  <dx:DXButton
      Content="Send Feedback"
      ButtonType="Outlined"
      HorizontalOptions="Fill"
      Command="{Binding NavigateToFeedbackCommand}" />
  ```

- [ ] **Step 6.5 — Update appsettings.template.json**

  Final content:
  ```json
  {
    "Sentry": {
      "Dsn": ""
    },
    "GitHub": {
      "FeedbackPat": "",
      "FeedbackRepo": "heldercsousa/MyVocaList"
    }
  }
  ```

- [ ] **Step 6.6 — Update appsettings.json (gitignored)**

  Add the `GitHub` section to `MyVocaList/appsettings.json`:
  ```json
  {
    "Sentry": {
      "Dsn": "<existing-value-if-any>"
    },
    "GitHub": {
      "FeedbackPat": "<your-fine-grained-PAT-here>",
      "FeedbackRepo": "heldercsousa/MyVocaList"
    }
  }
  ```
  > **Manual step for Helder:** Generate a fine-grained PAT at github.com/settings/personal-access-tokens/new scoped to `heldercsousa/MyVocaList` with Issues (Read & Write) permission only. Paste the token as `FeedbackPat`.

- [ ] **Step 6.7 — Build**

  ```bash
  dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
  ```
  Expected: 0 errors.

- [ ] **Step 6.8 — Commit**

  ```bash
  git add MyVocaList/Navigation/Routes.cs
  git add MyVocaList/AppShell.xaml.cs
  git add "MyVocaList/UI/Pages/Settings/SettingsPage.xaml"
  git add "MyVocaList/UI/ViewModels/SettingsViewModel.cs"
  git add MyVocaList/appsettings.template.json
  git commit -m "feat(feedback): add feedback route, Settings entry, and appsettings template"
  ```

---

## Task 7 — DI registration + `.sln` registration

**Files:**
- Modify: `MyVocaList/MauiProgram.cs`
- Modify: `MyVocaList.sln`

- [ ] **Step 7.1 — Register in MauiProgram.cs**

  Add after existing service registrations:

  ```csharp
  // Feedback
  builder.Services.AddTransient<IFeedbackService, FeedbackService>();
  builder.Services.AddTransient<FeedbackViewModel>();
  builder.Services.AddTransient<FeedbackPage>();
  ```

  Also confirm `IHttpClientFactory` is available. Add if not already registered:
  ```csharp
  builder.Services.AddHttpClient("feedback");
  ```

  > If `AddHttpClient` is not available, add `using Microsoft.Extensions.DependencyInjection;` and verify `Microsoft.Extensions.Http` is referenced (it is included with MAUI).

  Also register MAUI device info if not already present:
  ```csharp
  builder.Services.AddSingleton<IDeviceInfo>(DeviceInfo.Current);
  ```

- [ ] **Step 7.2 — Register new files in MyVocaList.sln**

  Find the `whats-new` (or nearest Business Features) solution folder in `MyVocaList.sln`. Add:
  ```
  Docs\Management\BusinessFeatures\user-suggestions\plan.md = Docs\Management\BusinessFeatures\user-suggestions\plan.md
  ```

- [ ] **Step 7.3 — Full build + tests**

  ```bash
  dotnet build MyVocaList.sln
  dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj
  ```
  Expected: 0 errors, 0 failures.

- [ ] **Step 7.4 — Commit**

  ```bash
  git add MyVocaList/MauiProgram.cs
  git add MyVocaList.sln
  git commit -m "feat(feedback): register FeedbackService, FeedbackViewModel, FeedbackPage in DI"
  ```

---

## Self-Review

### Spec coverage check

| AC | Task |
|----|------|
| AC-FB-01 GitHub Issue created with title format | Task 2 title-format test + Task 3 `SubmitAsync` |
| AC-FB-02 Metadata in body | Task 2 metadata test + Task 3 `BuildIssueBody` |
| AC-FB-03 Optional email appended | Task 2 metadata test + Task 3 `BuildIssueBody` email line |
| AC-FB-04 Empty message disables Send | Task 4 `CanSubmit` depends on `!IsNullOrWhiteSpace(Message)` |
| AC-FB-05 Success clears form + snackbar | Task 4 success branch in `SubmitAsync` |
| AC-FB-06 Failure shows snackbar, preserves content | Task 4 failure branch (fields not cleared) |
| AC-FB-07 Button disabled during in-flight request | Task 4 `IsSubmitting` + `[NotifyCanExecuteChangedFor]` |

### No placeholder scan
All tasks contain complete code. No TBD/TODO items remain.

### Type consistency
- `FeedbackSubmission` — defined Task 1, used in Tasks 2, 3, 4
- `FeedbackCategory` — defined Task 1, used throughout
- `IFeedbackService.SubmitAsync` — defined Task 1, tested Task 2, implemented Task 3, called Task 4
- `FeedbackViewModel.SubmitCommand` — defined Task 4, bound in Task 5 XAML

---

## Verification

1. `dotnet test` — all `FeedbackServiceTests` pass
2. `dotnet build MyVocaList.sln` — 0 errors
3. **Manual step (Helder):** Create GitHub PAT and add to `appsettings.json`
4. Emulator smoke test:
   - Settings page → "Send Feedback" button visible
   - Tap "Send Feedback" → navigates to FeedbackPage
   - Empty message → Send button disabled
   - Fill message → Send button enabled
   - Submit with valid PAT → snackbar "Feedback sent", form clears
   - Check github.com/heldercsousa/MyVocaList/issues → new issue with metadata
   - Disconnect network → submit → snackbar "Could not send", message preserved
