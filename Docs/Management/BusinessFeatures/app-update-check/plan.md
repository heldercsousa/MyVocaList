# App Update Check — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** On every startup, fetch a remote `version-manifest.json` and show a dismissible soft-nudge sheet (update available) or a non-dismissible hard-block sheet (update required) — fail silently on network errors.

**Architecture:** `IVersionCheckService` (Domain) calls a named `HttpClient` to fetch the manifest from GitHub raw, compares versions using `NuGet.Versioning.NuGetVersion`, and returns an `UpdateCheckResult`. `AppShellViewModel.InitializeAsync()` (added by the What's New feature) sends `WeakReferenceMessenger` messages that `AppShell.xaml.cs` handles to show the appropriate `dx:BottomSheet` component.

**Tech Stack:** .NET MAUI 10 · `NuGet.Versioning` (new dependency) · `IHttpClientFactory` · DevExpress MAUI `dx:BottomSheet` (`IsCancelable="True/False"`) · `WeakReferenceMessenger` · xUnit + Moq

> **Dependency on What's New feature:** This plan assumes `AppShellViewModel.InitializeAsync()` already exists and `AppShell.xaml.cs` already subscribes to messages. Both were added by the What's New feature. Implement that feature first and merge before starting this one.

---

## Existing Assets (do NOT recreate)

- `MyVocaList/UI/Messages/` — directory created by What's New; add new message records here
- `AppShellViewModel.InitializeAsync()` — already exists; extend it (add version check call after What's New call)
- `AppShell.xaml.cs` — `WeakReferenceMessenger` subscription pattern already established

---

## File Map

| File | Action | Responsibility |
|------|--------|----------------|
| `MyVocaList.Services/MyVocaList.Services.csproj` | **Modify** | Add `NuGet.Versioning` package reference |
| `MyVocaList.Contracts/DTOs/VersionManifest.cs` | **Create** | Deserialization DTO |
| `MyVocaList.Contracts/DTOs/UpdateCheckResult.cs` | **Create** | Result DTO |
| `MyVocaList.Domain/ServicesInterfaces/IVersionCheckService.cs` | **Create** | Service interface |
| `MyVocaList.Tests/Unit/Services/VersionCheckServiceTests.cs` | **Create** | Unit tests |
| `MyVocaList.Services/VersionCheckService.cs` | **Create** | Implementation |
| `MyVocaList/UI/Messages/ShowUpdateAvailableMessage.cs` | **Create** | Messenger message record |
| `MyVocaList/UI/Messages/ShowUpdateRequiredMessage.cs` | **Create** | Messenger message record |
| `MyVocaList/UI/Components/Sheets/UpdateAvailableBottomSheet.xaml` | **Create** | Soft-nudge sheet |
| `MyVocaList/UI/Components/Sheets/UpdateAvailableBottomSheet.xaml.cs` | **Create** | Code-behind |
| `MyVocaList/UI/Components/Sheets/UpdateRequiredBottomSheet.xaml` | **Create** | Hard-block sheet |
| `MyVocaList/UI/Components/Sheets/UpdateRequiredBottomSheet.xaml.cs` | **Create** | Code-behind |
| `MyVocaList/UI/ViewModels/AppShellViewModel.cs` | **Modify** | Add `IVersionCheckService` injection + version check in `InitializeAsync` |
| `MyVocaList/AppShell.xaml.cs` | **Modify** | Subscribe to update messages, show sheets |
| `MyVocaList/MauiProgram.cs` | **Modify** | Register `IVersionCheckService`, named HttpClient |
| `version-manifest.json` (repo root) | **Create** | Hosted manifest file |
| `MyVocaList.sln` | **Modify** | Register new doc files |

---

## Task 1 — Add `NuGet.Versioning` package + DTOs + Interface

**Files:**
- Modify: `MyVocaList.Services/MyVocaList.Services.csproj`
- Create: `MyVocaList.Contracts/DTOs/VersionManifest.cs`
- Create: `MyVocaList.Contracts/DTOs/UpdateCheckResult.cs`
- Create: `MyVocaList.Domain/ServicesInterfaces/IVersionCheckService.cs`

- [ ] **Step 1.1 — Add NuGet.Versioning to Services project**

  In `MyVocaList.Services/MyVocaList.Services.csproj`, add inside `<ItemGroup>`:

  ```xml
  <PackageReference Include="NuGet.Versioning" Version="6.*" />
  ```

- [ ] **Step 1.2 — Create VersionManifest DTO**

  `MyVocaList.Contracts/DTOs/VersionManifest.cs`:

  ```csharp
  namespace MyVocaList.Contracts.DTOs;

  public record VersionManifest(
      string LatestVersion,
      string MinRequiredVersion,
      Dictionary<string, string> StoreUrls,
      string UpdateMessage);
  ```

- [ ] **Step 1.3 — Create UpdateCheckResult DTO**

  `MyVocaList.Contracts/DTOs/UpdateCheckResult.cs`:

  ```csharp
  namespace MyVocaList.Contracts.DTOs;

  public record UpdateCheckResult(
      bool IsUpToDate,
      bool IsUpdateAvailable,
      bool IsUpdateRequired,
      string StoreUrl,
      string LatestVersion,
      string UpdateMessage)
  {
      /// <summary>Returned when the manifest could not be fetched (fail-open) or the app is up to date.</summary>
      public static readonly UpdateCheckResult UpToDate =
          new(true, false, false, string.Empty, string.Empty, string.Empty);
  }
  ```

- [ ] **Step 1.4 — Create IVersionCheckService interface**

  `MyVocaList.Domain/ServicesInterfaces/IVersionCheckService.cs`:

  ```csharp
  namespace MyVocaList.Domain.ServicesInterfaces;

  public interface IVersionCheckService
  {
      /// <summary>Fetches the version manifest and determines if the current app version requires action. Never throws — returns UpToDate on any error.</summary>
      Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct = default);
  }
  ```

- [ ] **Step 1.5 — Build**

  ```bash
  dotnet build MyVocaList.sln
  ```
  Expected: 0 errors (NuGet.Versioning will be restored automatically).

- [ ] **Step 1.6 — Commit**

  ```bash
  git add MyVocaList.Services/MyVocaList.Services.csproj
  git add MyVocaList.Contracts/DTOs/VersionManifest.cs
  git add MyVocaList.Contracts/DTOs/UpdateCheckResult.cs
  git add MyVocaList.Domain/ServicesInterfaces/IVersionCheckService.cs
  git commit -m "feat(update-check): add NuGet.Versioning, DTOs, and IVersionCheckService interface"
  ```

---

## Task 2 — Unit tests for `VersionCheckService` (Red)

**Files:**
- Create: `MyVocaList.Tests/Unit/Services/VersionCheckServiceTests.cs`

- [ ] **Step 2.1 — Create test file**

  `MyVocaList.Tests/Unit/Services/VersionCheckServiceTests.cs`:

  ```csharp
  using System.Net;
  using System.Net.Http;
  using System.Text.Json;
  using Microsoft.Maui.ApplicationModel;
  using Moq;
  using Moq.Protected;

  namespace MyVocaList.Tests.Unit.Services;

  public class VersionCheckServiceTests
  {
      private readonly Mock<IHttpClientFactory> _factoryMock = new();
      private readonly Mock<IAppInfo> _appInfoMock = new();
      private readonly Mock<IDeviceInfo> _deviceInfoMock = new();
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

      private VersionCheckService CreateSut(string currentVersion, DevicePlatform platform)
      {
          _appInfoMock.Setup(a => a.VersionString).Returns(currentVersion);
          _deviceInfoMock.Setup(d => d.Platform).Returns(platform);
          return new VersionCheckService(_factoryMock.Object, _appInfoMock.Object, _deviceInfoMock.Object, _loggerMock.Object);
      }

      // [AC] AC-UC-03: App proceeds when up to date
      [Fact]
      public async Task CheckForUpdatesAsync_UpToDate_ReturnsUpToDate()
      {
          SetupHttpResponse(ValidManifestJson);
          var sut = CreateSut("2.0.0", DevicePlatform.Android);

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
          var sut = CreateSut("1.8.0", DevicePlatform.Android); // 1.8 >= 1.5 (min), < 2.0 (latest)

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
          var sut = CreateSut("1.0.0", DevicePlatform.Android); // 1.0 < 1.5 (min)

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
          var sut = CreateSut("1.8.0", DevicePlatform.iOS);

          var result = await sut.CheckForUpdatesAsync();

          Assert.Contains("apps.apple.com", result.StoreUrl);
      }

      // [AC] AC-UC-04: Fail-open on network error
      [Fact]
      public async Task CheckForUpdatesAsync_NetworkFailure_ReturnsUpToDate()
      {
          SetupNetworkFailure();
          var sut = CreateSut("1.0.0", DevicePlatform.Android);

          var result = await sut.CheckForUpdatesAsync();

          Assert.True(result.IsUpToDate);
          Assert.False(result.IsUpdateRequired);
      }

      // Validation rule: malformed JSON → fail-open
      [Fact]
      public async Task CheckForUpdatesAsync_MalformedJson_ReturnsUpToDate()
      {
          SetupHttpResponse("{ not valid json {{");
          var sut = CreateSut("1.0.0", DevicePlatform.Android);

          var result = await sut.CheckForUpdatesAsync();

          Assert.True(result.IsUpToDate);
      }

      // [AC] AC-UC-06: SemVer pre-release — 0.3.0-alpha.5 < 0.3.0
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
          var sut = CreateSut("0.3.0-alpha.5", DevicePlatform.Android); // pre-release < release

          var result = await sut.CheckForUpdatesAsync();

          Assert.True(result.IsUpdateAvailable); // 0.3.0-alpha.5 < 0.3.0 (release)
      }

      // Validation rule: missing storeUrls key → empty string, no crash
      [Fact]
      public async Task CheckForUpdatesAsync_MissingStoreUrlKey_ReturnsEmptyStoreUrl()
      {
          var manifest = JsonSerializer.Serialize(new
          {
              latestVersion = "2.0.0",
              minRequiredVersion = "1.5.0",
              storeUrls = new { android = "https://play.google.com" },
              // no "ios" key
              updateMessage = ""
          });
          SetupHttpResponse(manifest);
          var sut = CreateSut("1.8.0", DevicePlatform.iOS);

          var result = await sut.CheckForUpdatesAsync();

          Assert.True(result.IsUpdateAvailable);
          Assert.Equal(string.Empty, result.StoreUrl); // missing key → empty, no crash
      }
  }
  ```

- [ ] **Step 2.2 — Run tests to confirm Red**

  ```bash
  dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~VersionCheckServiceTests" --verbosity normal
  ```
  Expected: Build error (`VersionCheckService` not found). Correct — proceed to Task 3.

- [ ] **Step 2.3 — Commit**

  ```bash
  git add MyVocaList.Tests/Unit/Services/VersionCheckServiceTests.cs
  git commit -m "test(update-check): add VersionCheckService unit tests (Red)"
  ```

---

## Task 3 — Implement `VersionCheckService` (Green)

**Files:**
- Create: `MyVocaList.Services/VersionCheckService.cs`

- [ ] **Step 3.1 — Create VersionCheckService**

  `MyVocaList.Services/VersionCheckService.cs`:

  ```csharp
  using System.Net.Http.Json;
  using System.Text.Json;
  using Microsoft.Maui.ApplicationModel;
  using NuGet.Versioning;

  namespace MyVocaList.Services;

  public sealed class VersionCheckService : IVersionCheckService
  {
      private const string ClientName = "version-check";
      private const string ManifestUrl =
          "https://raw.githubusercontent.com/heldercsousa/MyVocaList/main/version-manifest.json";

      private readonly IHttpClientFactory _httpClientFactory;
      private readonly IAppInfo _appInfo;
      private readonly IDeviceInfo _deviceInfo;
      private readonly ILogger<VersionCheckService> _logger;

      public VersionCheckService(
          IHttpClientFactory httpClientFactory,
          IAppInfo appInfo,
          IDeviceInfo deviceInfo,
          ILogger<VersionCheckService> logger)
      {
          _httpClientFactory = httpClientFactory;
          _appInfo = appInfo;
          _deviceInfo = deviceInfo;
          _logger = logger;
      }

      /// <inheritdoc />
      public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct = default)
      {
          try
          {
              using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
              cts.CancelAfter(TimeSpan.FromSeconds(5));

              var client = _httpClientFactory.CreateClient(ClientName);
              var manifest = await client.GetFromJsonAsync<VersionManifestJson>(
                  ManifestUrl,
                  new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                  cts.Token);

              if (manifest is null)
              {
                  _logger.LogWarning("Version manifest returned null — fail-open");
                  return UpdateCheckResult.UpToDate;
              }

              if (!NuGetVersion.TryParse(_appInfo.VersionString, out var current) ||
                  !NuGetVersion.TryParse(manifest.LatestVersion, out var latest) ||
                  !NuGetVersion.TryParse(manifest.MinRequiredVersion, out var minRequired))
              {
                  _logger.LogWarning("Version parse failed (current={Current}, latest={Latest}, min={Min}) — fail-open",
                      _appInfo.VersionString, manifest.LatestVersion, manifest.MinRequiredVersion);
                  return UpdateCheckResult.UpToDate;
              }

              var platformKey = _deviceInfo.Platform == DevicePlatform.iOS ? "ios" : "android";
              var storeUrl = manifest.StoreUrls?.GetValueOrDefault(platformKey, string.Empty) ?? string.Empty;

              if (current < minRequired)
                  return new UpdateCheckResult(false, false, true, storeUrl, manifest.LatestVersion, manifest.UpdateMessage ?? string.Empty);

              if (current < latest)
                  return new UpdateCheckResult(false, true, false, storeUrl, manifest.LatestVersion, manifest.UpdateMessage ?? string.Empty);

              return UpdateCheckResult.UpToDate;
          }
          catch (Exception ex)
          {
              _logger.LogWarning(ex, "Version manifest fetch failed — fail-open");
              return UpdateCheckResult.UpToDate;
          }
      }

      // Local deserialization model
      private sealed class VersionManifestJson
      {
          public string LatestVersion { get; set; } = string.Empty;
          public string MinRequiredVersion { get; set; } = string.Empty;
          public Dictionary<string, string>? StoreUrls { get; set; }
          public string? UpdateMessage { get; set; }
      }
  }
  ```

- [ ] **Step 3.2 — Run tests to confirm Green**

  ```bash
  dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~VersionCheckServiceTests" --verbosity normal
  ```
  Expected: All 8 tests PASS.

- [ ] **Step 3.3 — Full build**

  ```bash
  dotnet build MyVocaList.sln
  ```
  Expected: 0 errors.

- [ ] **Step 3.4 — Commit**

  ```bash
  git add MyVocaList.Services/VersionCheckService.cs
  git commit -m "feat(update-check): implement VersionCheckService with SemVer comparison and fail-open"
  ```

---

## Task 4 — Create bottom sheet components

**Files:**
- Create: `MyVocaList/UI/Components/Sheets/UpdateAvailableBottomSheet.xaml`
- Create: `MyVocaList/UI/Components/Sheets/UpdateAvailableBottomSheet.xaml.cs`
- Create: `MyVocaList/UI/Components/Sheets/UpdateRequiredBottomSheet.xaml`
- Create: `MyVocaList/UI/Components/Sheets/UpdateRequiredBottomSheet.xaml.cs`

> **Pattern reference:** See `MyVocaList/UI/Components/Sheets/ConfirmSheet.xaml` — same `dx:BottomSheet` structure with dividers and DXButton styles.

- [ ] **Step 4.1 — Create UpdateAvailableBottomSheet.xaml (soft nudge — dismissible)**

  `MyVocaList/UI/Components/Sheets/UpdateAvailableBottomSheet.xaml`:

  ```xml
  <?xml version="1.0" encoding="utf-8" ?>
  <ContentView
      x:Class="MyVocaList.UI.Components.Sheets.UpdateAvailableBottomSheet"
      xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
      xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
      xmlns:dx="http://schemas.devexpress.com/maui">

      <dx:BottomSheet
          x:Name="Sheet"
          AllowDismiss="True"
          IsCancelable="True"
          IsModal="True"
          ShowGrabber="True"
          HalfExpandedRatio="0.38"
          AllowedState="HalfExpanded"
          BackgroundColor="{StaticResource Surface}"
          CornerRadius="28">

          <VerticalStackLayout>
              <Label
                  x:Name="TitleLabel"
                  StyleClass="Title.Large"
                  TextColor="{StaticResource OnSurface}"
                  HorizontalTextAlignment="Center"
                  Margin="24,20,24,4" />

              <Label
                  x:Name="BodyLabel"
                  StyleClass="Body.Medium"
                  TextColor="{StaticResource OnSurfaceVariant}"
                  HorizontalTextAlignment="Center"
                  Margin="24,0,24,16" />

              <BoxView Style="{StaticResource Divider}" />

              <dx:DXButton
                  Content="Update Now"
                  Style="{StaticResource BottomSheetDestructiveAction}"
                  Clicked="OnUpdateNowClicked" />

              <BoxView Style="{StaticResource Divider}" />

              <dx:DXButton
                  Content="Later"
                  Style="{StaticResource BottomSheetCancelAction}"
                  Clicked="OnLaterClicked" />
          </VerticalStackLayout>
      </dx:BottomSheet>
  </ContentView>
  ```

- [ ] **Step 4.2 — Create UpdateAvailableBottomSheet.xaml.cs**

  `MyVocaList/UI/Components/Sheets/UpdateAvailableBottomSheet.xaml.cs`:

  ```csharp
  namespace MyVocaList.UI.Components.Sheets;

  public partial class UpdateAvailableBottomSheet : ContentView
  {
      private string _storeUrl = string.Empty;

      public UpdateAvailableBottomSheet()
      {
          InitializeComponent();
      }

      public void Show(UpdateCheckResult result)
      {
          _storeUrl = result.StoreUrl;
          TitleLabel.Text = "Update Available";
          BodyLabel.Text = $"Version {result.LatestVersion} is ready. Update for the latest features and fixes.";
          Sheet.State = DevExpress.Maui.Controls.BottomSheetState.HalfExpanded;
      }

      private async void OnUpdateNowClicked(object sender, EventArgs e)
      {
          Sheet.State = DevExpress.Maui.Controls.BottomSheetState.Hidden;
          if (!string.IsNullOrEmpty(_storeUrl))
              await Launcher.OpenAsync(_storeUrl);
      }

      private void OnLaterClicked(object sender, EventArgs e)
          => Sheet.State = DevExpress.Maui.Controls.BottomSheetState.Hidden;
  }
  ```

- [ ] **Step 4.3 — Create UpdateRequiredBottomSheet.xaml (hard block — non-dismissible)**

  `MyVocaList/UI/Components/Sheets/UpdateRequiredBottomSheet.xaml`:

  ```xml
  <?xml version="1.0" encoding="utf-8" ?>
  <ContentView
      x:Class="MyVocaList.UI.Components.Sheets.UpdateRequiredBottomSheet"
      xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
      xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
      xmlns:dx="http://schemas.devexpress.com/maui">

      <!--
          IsCancelable="False" + AllowDismiss="False": back gesture and swipe-down are both swallowed.
          User cannot proceed without tapping "Update Now".
      -->
      <dx:BottomSheet
          x:Name="Sheet"
          AllowDismiss="False"
          IsCancelable="False"
          IsModal="True"
          ShowGrabber="False"
          HalfExpandedRatio="0.38"
          AllowedState="HalfExpanded"
          BackgroundColor="{StaticResource Surface}"
          CornerRadius="28">

          <VerticalStackLayout>
              <Label
                  Text="Update Required"
                  StyleClass="Title.Large"
                  TextColor="{StaticResource OnSurface}"
                  HorizontalTextAlignment="Center"
                  Margin="24,20,24,4" />

              <Label
                  x:Name="MessageLabel"
                  StyleClass="Body.Medium"
                  TextColor="{StaticResource OnSurfaceVariant}"
                  HorizontalTextAlignment="Center"
                  Margin="24,0,24,16" />

              <BoxView Style="{StaticResource Divider}" />

              <dx:DXButton
                  Content="Update Now"
                  Style="{StaticResource BottomSheetDestructiveAction}"
                  Clicked="OnUpdateNowClicked" />
          </VerticalStackLayout>
      </dx:BottomSheet>
  </ContentView>
  ```

- [ ] **Step 4.4 — Create UpdateRequiredBottomSheet.xaml.cs**

  `MyVocaList/UI/Components/Sheets/UpdateRequiredBottomSheet.xaml.cs`:

  ```csharp
  namespace MyVocaList.UI.Components.Sheets;

  public partial class UpdateRequiredBottomSheet : ContentView
  {
      private string _storeUrl = string.Empty;

      public UpdateRequiredBottomSheet()
      {
          InitializeComponent();
      }

      public void Show(UpdateCheckResult result)
      {
          _storeUrl = result.StoreUrl;
          MessageLabel.Text = string.IsNullOrWhiteSpace(result.UpdateMessage)
              ? "This version is no longer supported. Please update to continue."
              : result.UpdateMessage;
          Sheet.State = DevExpress.Maui.Controls.BottomSheetState.HalfExpanded;
      }

      private async void OnUpdateNowClicked(object sender, EventArgs e)
      {
          if (!string.IsNullOrEmpty(_storeUrl))
              await Launcher.OpenAsync(_storeUrl);
          // Sheet stays open — user must update before continuing.
      }
  }
  ```

- [ ] **Step 4.5 — Build**

  ```bash
  dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
  ```
  Expected: 0 errors. Fix any XAML style reference errors (e.g. if `BottomSheetDestructiveAction` style key differs — check `ConfirmSheet.xaml` for the exact style name used in the project).

- [ ] **Step 4.6 — Commit**

  ```bash
  git add "MyVocaList/UI/Components/Sheets/UpdateAvailableBottomSheet.xaml"
  git add "MyVocaList/UI/Components/Sheets/UpdateAvailableBottomSheet.xaml.cs"
  git add "MyVocaList/UI/Components/Sheets/UpdateRequiredBottomSheet.xaml"
  git add "MyVocaList/UI/Components/Sheets/UpdateRequiredBottomSheet.xaml.cs"
  git commit -m "feat(update-check): add UpdateAvailableBottomSheet and UpdateRequiredBottomSheet components"
  ```

---

## Task 5 — Message records + AppShellViewModel + AppShell wiring

**Files:**
- Create: `MyVocaList/UI/Messages/ShowUpdateAvailableMessage.cs`
- Create: `MyVocaList/UI/Messages/ShowUpdateRequiredMessage.cs`
- Modify: `MyVocaList/UI/ViewModels/AppShellViewModel.cs`
- Modify: `MyVocaList/AppShell.xaml.cs`

- [ ] **Step 5.1 — Create message records**

  `MyVocaList/UI/Messages/ShowUpdateAvailableMessage.cs`:
  ```csharp
  namespace MyVocaList.UI.Messages;

  public sealed record ShowUpdateAvailableMessage(UpdateCheckResult Result);
  ```

  `MyVocaList/UI/Messages/ShowUpdateRequiredMessage.cs`:
  ```csharp
  namespace MyVocaList.UI.Messages;

  public sealed record ShowUpdateRequiredMessage(UpdateCheckResult Result);
  ```

- [ ] **Step 5.2 — Extend AppShellViewModel.InitializeAsync**

  Read `MyVocaList/UI/ViewModels/AppShellViewModel.cs`. Add `IVersionCheckService` to constructor and version check to `InitializeAsync`:

  ```csharp
  // Add field:
  private readonly IVersionCheckService _versionCheckService;

  // Updated constructor (add parameter alongside existing ones):
  public AppShellViewModel(
      IServiceProvider serviceProvider,
      IWhatsNewService whatsNewService,
      IVersionCheckService versionCheckService)
  {
      _serviceProvider = serviceProvider;
      _whatsNewService = whatsNewService;
      _versionCheckService = versionCheckService;
      NavigateCommand = new AsyncRelayCommand<string>(route => NavigateAsync(route!));
      MenuGroups = NavigationConfig.BuildMenuGroups(NavigateCommand);
  }

  // Updated InitializeAsync (add version check after What's New check):
  public async Task InitializeAsync(CancellationToken ct = default)
  {
      var entry = await _whatsNewService.GetPendingReleaseAsync(ct);
      if (entry is not null)
          WeakReferenceMessenger.Default.Send(new ShowWhatsNewMessage(entry));

      var updateResult = await _versionCheckService.CheckForUpdatesAsync(ct);
      if (updateResult.IsUpdateRequired)
          WeakReferenceMessenger.Default.Send(new ShowUpdateRequiredMessage(updateResult));
      else if (updateResult.IsUpdateAvailable)
          WeakReferenceMessenger.Default.Send(new ShowUpdateAvailableMessage(updateResult));
  }
  ```

- [ ] **Step 5.3 — Add subscriptions and sheet instances to AppShell.xaml.cs**

  Read `MyVocaList/AppShell.xaml.cs` (updated by What's New feature). Add:

  ```csharp
  // Add fields:
  private UpdateAvailableBottomSheet? _updateAvailableSheet;
  private UpdateRequiredBottomSheet? _updateRequiredSheet;

  // Add to constructor (after existing WeakReferenceMessenger.Register calls):
  WeakReferenceMessenger.Default.Register<ShowUpdateAvailableMessage>(this, OnShowUpdateAvailable);
  WeakReferenceMessenger.Default.Register<ShowUpdateRequiredMessage>(this, OnShowUpdateRequired);

  // Add also IVersionCheckService injection to AppShell constructor if needed:
  // (AppShell constructor receives IWhatsNewService from What's New — same pattern here,
  //  but IVersionCheckService is only used by AppShellViewModel; AppShell does not need it directly)

  // Add handler methods:
  private void OnShowUpdateAvailable(object recipient, ShowUpdateAvailableMessage message)
  {
      MainThread.BeginInvokeOnMainThread(() =>
      {
          _updateAvailableSheet ??= new UpdateAvailableBottomSheet();
          AttachSheetToCurrentPage(_updateAvailableSheet);
          _updateAvailableSheet.Show(message.Result);
      });
  }

  private void OnShowUpdateRequired(object recipient, ShowUpdateRequiredMessage message)
  {
      MainThread.BeginInvokeOnMainThread(() =>
      {
          _updateRequiredSheet ??= new UpdateRequiredBottomSheet();
          AttachSheetToCurrentPage(_updateRequiredSheet);
          _updateRequiredSheet.Show(message.Result);
      });
  }

  // Shared helper (add once; also used by What's New sheet if that was wired similarly):
  private void AttachSheetToCurrentPage(ContentView sheet)
  {
      if (sheet.Parent is not null) return;
      if (CurrentPage?.Content is Layout layout)
          layout.Children.Add(sheet);
  }
  ```

  > **Note:** The `AttachSheetToCurrentPage` helper should be consolidated with the What's New attachment code — if both follow the same pattern, extract once and reuse.

- [ ] **Step 5.4 — Build**

  ```bash
  dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
  ```
  Expected: 0 errors.

- [ ] **Step 5.5 — Commit**

  ```bash
  git add MyVocaList/UI/Messages/ShowUpdateAvailableMessage.cs
  git add MyVocaList/UI/Messages/ShowUpdateRequiredMessage.cs
  git add MyVocaList/UI/ViewModels/AppShellViewModel.cs
  git add MyVocaList/AppShell.xaml.cs
  git commit -m "feat(update-check): wire version check to AppShellViewModel and AppShell message handlers"
  ```

---

## Task 6 — Create `version-manifest.json` + DI + `.sln`

**Files:**
- Create: `version-manifest.json` (repo root)
- Modify: `MyVocaList/MauiProgram.cs`
- Modify: `MyVocaList.sln`

- [ ] **Step 6.1 — Create version-manifest.json at repo root**

  `version-manifest.json`:

  ```json
  {
    "latestVersion": "0.1.0",
    "minRequiredVersion": "0.1.0",
    "storeUrls": {
      "android": "https://play.google.com/store/apps/details?id=com.myvocalist",
      "ios": "https://apps.apple.com/app/myvocalist/idXXXXXXX"
    },
    "updateMessage": "This version is no longer supported. Please update to continue."
  }
  ```

  > Update `latestVersion` and `minRequiredVersion` to match the actual published version when shipping. `idXXXXXXX` must be replaced with the real App Store ID when the app is published.

- [ ] **Step 6.2 — Register IVersionCheckService in MauiProgram.cs**

  Add after the `IWhatsNewService` registration:

  ```csharp
  builder.Services.AddSingleton<IVersionCheckService, VersionCheckService>();
  builder.Services.AddHttpClient("version-check");
  ```

  > `AddHttpClient("feedback")` was added by the User Suggestions feature. `AddHttpClient("version-check")` is a separate named client with its own lifecycle. Both can coexist.

  Also confirm `IDeviceInfo` is registered (added by User Suggestions feature):
  ```csharp
  builder.Services.AddSingleton<IDeviceInfo>(DeviceInfo.Current);
  ```

- [ ] **Step 6.3 — Register new files in MyVocaList.sln**

  Add to the appropriate solution folder:
  ```
  version-manifest.json = version-manifest.json
  Docs\Management\BusinessFeatures\app-update-check\plan.md = Docs\Management\BusinessFeatures\app-update-check\plan.md
  ```

- [ ] **Step 6.4 — Full build + tests**

  ```bash
  dotnet build MyVocaList.sln
  dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj
  ```
  Expected: 0 errors, 0 test failures.

- [ ] **Step 6.5 — Commit**

  ```bash
  git add version-manifest.json
  git add MyVocaList/MauiProgram.cs
  git add MyVocaList.sln
  git commit -m "feat(update-check): register VersionCheckService in DI; add version-manifest.json"
  ```

---

## Self-Review

### Spec coverage check

| AC | Task |
|----|------|
| AC-UC-01 Soft nudge when update available | Task 2 test + Task 3 `current < latest` branch + Task 4 `UpdateAvailableBottomSheet` |
| AC-UC-02 Hard block when below minimum | Task 2 test + Task 3 `current < minRequired` branch + Task 4 `UpdateRequiredBottomSheet` (`IsCancelable="False"`) |
| AC-UC-03 App proceeds when up to date | Task 2 test + Task 3 `UpToDate` return |
| AC-UC-04 Fail-open on network error | Task 2 test + Task 3 catch-all exception handling |
| AC-UC-05 Correct store URL per platform | Task 2 iOS/Android tests + Task 3 `platformKey` lookup |
| AC-UC-06 SemVer-aware comparison | Task 2 pre-release test + Task 3 `NuGetVersion.TryParse` |

### No placeholder scan
All tasks contain complete code. `idXXXXXXX` in `version-manifest.json` is a real placeholder that requires Helder's action (App Store ID is not yet known) — documented as a manual step.

### Type consistency
- `UpdateCheckResult` — defined Task 1, used in Tasks 2, 3, 4, 5
- `IVersionCheckService.CheckForUpdatesAsync` — defined Task 1, tested Task 2, implemented Task 3, called Task 5
- `ShowUpdateAvailableMessage` / `ShowUpdateRequiredMessage` — defined Task 5.1, sent Task 5.2, received Task 5.3
- `VersionCheckService` constructor params — consistent across Tasks 2, 3

---

## Verification

1. `dotnet test` — all `VersionCheckServiceTests` pass
2. `dotnet build MyVocaList.sln` — 0 errors
3. Emulator smoke test:
   - Normal launch (version matches manifest) → no sheet
   - Temporarily lower `latestVersion` in manifest to trigger soft nudge → `UpdateAvailableBottomSheet` appears, "Later" dismisses, "Update Now" opens Play Store
   - Lower `minRequiredVersion` above current version → `UpdateRequiredBottomSheet` appears, cannot be dismissed, "Update Now" opens store
   - Disconnect device from internet → launch → no sheet (fail-open)
