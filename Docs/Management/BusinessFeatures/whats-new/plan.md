# What's New / Release Notes — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Show a one-time `dx:BottomSheet` modal on app upgrade listing what changed, sourced from a bundled `releases.json` MauiAsset — never on fresh install, never shown twice for the same version.

**Architecture:** `IWhatsNewService` (Domain interface) reads `releases.json` from app package and checks `IPreferences` for the last-seen version. `AppShellViewModel.InitializeAsync()` calls `GetPendingReleaseAsync()`; if non-null, sends a `WeakReferenceMessenger` message that `AppShell.xaml.cs` handles to show `WhatsNewBottomSheet`. `AboutViewModel.GetCurrentReleaseAsync()` (existing) shows current notes in the About page regardless of seen status.

**Tech Stack:** .NET MAUI 10 · CommunityToolkit.Mvvm (WeakReferenceMessenger) · DevExpress MAUI `dx:BottomSheet` · `Microsoft.Maui.Storage.IPreferences` · `Microsoft.Maui.Storage.IFileSystem` · `Microsoft.Maui.ApplicationModel.IAppInfo` · xUnit + Moq

---

## Existing Assets (do NOT recreate)

- `MyVocaList.Contracts/DTOs/ReleaseEntry.cs` — DTO already exists ✅
- `MyVocaList.Services/NullWhatsNewService.cs` — stub already exists; update it in Task 1
- `MauiProgram.cs` line: `builder.Services.AddSingleton<IWhatsNewService, NullWhatsNewService>()` — swap in Task 6

---

## File Map

| File | Action | Responsibility |
|------|--------|----------------|
| `MyVocaList.Domain/ServicesInterfaces/IWhatsNewService.cs` | **Modify** | Add `GetPendingReleaseAsync` + `MarkCurrentVersionSeen` |
| `MyVocaList.Services/NullWhatsNewService.cs` | **Modify** | Stub new interface methods |
| `MyVocaList.Tests/Unit/Services/WhatsNewServiceTests.cs` | **Create** | Unit tests for `WhatsNewService` |
| `MyVocaList.Services/WhatsNewService.cs` | **Create** | Real implementation |
| `MyVocaList/Resources/Raw/releases.json` | **Create** | Bundled release notes asset |
| `MyVocaList/UI/Components/Sheets/WhatsNewBottomSheet.xaml` | **Create** | BottomSheet UI |
| `MyVocaList/UI/Components/Sheets/WhatsNewBottomSheet.xaml.cs` | **Create** | Code-behind + dismiss handler |
| `MyVocaList/UI/ViewModels/AppShellViewModel.cs` | **Modify** | Add `InitializeAsync`, inject `IWhatsNewService` |
| `MyVocaList/AppShell.xaml.cs` | **Modify** | Subscribe to `ShowWhatsNewMessage`, show sheet |
| `MyVocaList/MauiProgram.cs` | **Modify** | Swap `NullWhatsNewService` → `WhatsNewService` |
| `MyVocaList.sln` | **Modify** | Register new files in Solution Explorer |

---

## Task 1 — Update `IWhatsNewService` interface + `NullWhatsNewService` stub

**Files:**
- Modify: `MyVocaList.Domain/ServicesInterfaces/IWhatsNewService.cs`
- Modify: `MyVocaList.Services/NullWhatsNewService.cs`

- [ ] **Step 1.1 — Read existing IWhatsNewService**

  ```bash
  # read MyVocaList.Domain/ServicesInterfaces/IWhatsNewService.cs
  ```
  Confirm it contains only `GetCurrentReleaseAsync`. You will add two methods.

- [ ] **Step 1.2 — Update the interface**

  Final content of `MyVocaList.Domain/ServicesInterfaces/IWhatsNewService.cs`:

  ```csharp
  namespace MyVocaList.Domain.ServicesInterfaces;

  public interface IWhatsNewService
  {
      /// <summary>Returns the current version's release entry for display (e.g. About page). Never checks seen status.</summary>
      Task<ReleaseEntry?> GetCurrentReleaseAsync(CancellationToken ct = default);

      /// <summary>Returns the current version's release entry only if the user has not seen it yet. Returns null on fresh install, same version, or no matching entry.</summary>
      Task<ReleaseEntry?> GetPendingReleaseAsync(CancellationToken ct = default);

      /// <summary>Persists the current version as seen so GetPendingReleaseAsync returns null on subsequent launches.</summary>
      void MarkCurrentVersionSeen();
  }
  ```

- [ ] **Step 1.3 — Update NullWhatsNewService to implement new methods**

  Final content of `MyVocaList.Services/NullWhatsNewService.cs`:

  ```csharp
  namespace MyVocaList.Services;

  /// <summary>
  /// Temporary stub — always returns null. Replaced by WhatsNewService in MauiProgram.cs.
  /// </summary>
  public sealed class NullWhatsNewService : IWhatsNewService
  {
      /// <inheritdoc />
      public Task<ReleaseEntry?> GetCurrentReleaseAsync(CancellationToken ct = default)
          => Task.FromResult<ReleaseEntry?>(null);

      /// <inheritdoc />
      public Task<ReleaseEntry?> GetPendingReleaseAsync(CancellationToken ct = default)
          => Task.FromResult<ReleaseEntry?>(null);

      /// <inheritdoc />
      public void MarkCurrentVersionSeen() { }
  }
  ```

- [ ] **Step 1.4 — Build**

  ```bash
  dotnet build MyVocaList.sln
  ```
  Expected: 0 errors. The interface change is additive; the null stub already satisfies it.

- [ ] **Step 1.5 — Commit**

  ```bash
  git add MyVocaList.Domain/ServicesInterfaces/IWhatsNewService.cs
  git add MyVocaList.Services/NullWhatsNewService.cs
  git commit -m "feat(whats-new): extend IWhatsNewService with GetPendingReleaseAsync + MarkCurrentVersionSeen"
  ```

---

## Task 2 — Unit tests for `WhatsNewService` (Red)

**Files:**
- Create: `MyVocaList.Tests/Unit/Services/WhatsNewServiceTests.cs`

> **AC coverage:** AC-WN-01, AC-WN-02, AC-WN-03, AC-WN-04, AC-WN-05, AC-WN-06

- [ ] **Step 2.1 — Create test file**

  `MyVocaList.Tests/Unit/Services/WhatsNewServiceTests.cs`:

  ```csharp
  using Microsoft.Maui.ApplicationModel;
  using Microsoft.Maui.Storage;

  namespace MyVocaList.Tests.Unit.Services;

  public class WhatsNewServiceTests
  {
      private readonly Mock<IPreferences> _prefsMock = new();
      private readonly Mock<IAppInfo> _appInfoMock = new();
      private readonly Mock<IFileSystem> _fsMock = new();
      private readonly Mock<ILogger<WhatsNewService>> _loggerMock = new();

      // Minimal valid releases.json with one entry for version "1.2.0"
      private const string ValidJson = """
          [
            {
              "version": "1.2.0",
              "date": "2026-06-01",
              "highlights": ["New queue management"],
              "fixes": ["Fixed crash on empty list"]
            }
          ]
          """;

      private WhatsNewService CreateSut() =>
          new(_prefsMock.Object, _appInfoMock.Object, _fsMock.Object, _loggerMock.Object);

      private void SetupVersion(string version) =>
          _appInfoMock.Setup(a => a.VersionString).Returns(version);

      private void SetupLastSeen(string? value) =>
          _prefsMock.Setup(p => p.Get("last_seen_version", null as string, null))
                    .Returns(value);

      private void SetupReleasesJson(string json)
      {
          var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
          _fsMock.Setup(f => f.OpenAppPackageFileAsync("releases.json"))
                 .ReturnsAsync(stream);
      }

      // ── GetPendingReleaseAsync ────────────────────────────────────────────

      // [AC] AC-WN-02: No modal on fresh install
      [Fact]
      public async Task GetPendingReleaseAsync_FreshInstall_ReturnsNullAndStoresVersion()
      {
          SetupVersion("1.2.0");
          SetupLastSeen(null); // no last-seen → fresh install
          var sut = CreateSut();

          var result = await sut.GetPendingReleaseAsync();

          Assert.Null(result);
          _prefsMock.Verify(p => p.Set("last_seen_version", "1.2.0", null), Times.Once);
      }

      // [AC] AC-WN-01: Modal not shown again for same version
      [Fact]
      public async Task GetPendingReleaseAsync_SameVersion_ReturnsNull()
      {
          SetupVersion("1.2.0");
          SetupLastSeen("1.2.0"); // already seen
          var sut = CreateSut();

          var result = await sut.GetPendingReleaseAsync();

          Assert.Null(result);
      }

      // [AC] AC-WN-01: Modal shown on version upgrade with matching entry
      [Fact]
      public async Task GetPendingReleaseAsync_VersionUpgradeWithEntry_ReturnsEntry()
      {
          SetupVersion("1.2.0");
          SetupLastSeen("1.1.0"); // older version seen
          SetupReleasesJson(ValidJson);
          var sut = CreateSut();

          var result = await sut.GetPendingReleaseAsync();

          Assert.NotNull(result);
          Assert.Equal("1.2.0", result.Version);
          Assert.Equal("2026-06-01", result.Date);
          Assert.Single(result.Highlights);
          Assert.Single(result.Fixes);
      }

      // [AC] AC-WN-03: No modal when no entry for current version
      [Fact]
      public async Task GetPendingReleaseAsync_VersionUpgradeNoMatchingEntry_ReturnsNull()
      {
          SetupVersion("9.9.9"); // not in json
          SetupLastSeen("1.1.0");
          SetupReleasesJson(ValidJson);
          var sut = CreateSut();

          var result = await sut.GetPendingReleaseAsync();

          Assert.Null(result);
      }

      // Validation rule: malformed JSON → null (no crash)
      [Fact]
      public async Task GetPendingReleaseAsync_MalformedJson_ReturnsNull()
      {
          SetupVersion("1.2.0");
          SetupLastSeen("1.1.0");
          SetupReleasesJson("not valid json {{{{");
          var sut = CreateSut();

          var result = await sut.GetPendingReleaseAsync();

          Assert.Null(result);
      }

      // Validation rule: missing releases.json → null (no crash)
      [Fact]
      public async Task GetPendingReleaseAsync_MissingFile_ReturnsNull()
      {
          SetupVersion("1.2.0");
          SetupLastSeen("1.1.0");
          _fsMock.Setup(f => f.OpenAppPackageFileAsync("releases.json"))
                 .ThrowsAsync(new FileNotFoundException());
          var sut = CreateSut();

          var result = await sut.GetPendingReleaseAsync();

          Assert.Null(result);
      }

      // ── GetCurrentReleaseAsync ────────────────────────────────────────────

      // GetCurrentReleaseAsync ignores seen status — always returns entry if it exists
      [Fact]
      public async Task GetCurrentReleaseAsync_AlreadySeen_StillReturnsEntry()
      {
          SetupVersion("1.2.0");
          SetupLastSeen("1.2.0"); // already seen — should NOT block GetCurrentReleaseAsync
          SetupReleasesJson(ValidJson);
          var sut = CreateSut();

          var result = await sut.GetCurrentReleaseAsync();

          Assert.NotNull(result);
          Assert.Equal("1.2.0", result.Version);
      }

      // ── MarkCurrentVersionSeen ────────────────────────────────────────────

      // [AC] AC-WN-05: Dismiss persists version
      [Fact]
      public void MarkCurrentVersionSeen_StoresCurrentVersion()
      {
          SetupVersion("1.2.0");
          var sut = CreateSut();

          sut.MarkCurrentVersionSeen();

          _prefsMock.Verify(p => p.Set("last_seen_version", "1.2.0", null), Times.Once);
      }
  }
  ```

- [ ] **Step 2.2 — Run tests to confirm Red**

  ```bash
  dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~WhatsNewServiceTests" --verbosity normal
  ```
  Expected: Build error (`WhatsNewService` not found) or all FAIL. That is correct — proceed to Task 3.

- [ ] **Step 2.3 — Commit failing tests**

  ```bash
  git add MyVocaList.Tests/Unit/Services/WhatsNewServiceTests.cs
  git commit -m "test(whats-new): add WhatsNewService unit tests (Red)"
  ```

---

## Task 3 — Implement `WhatsNewService` (Green)

**Files:**
- Create: `MyVocaList.Services/WhatsNewService.cs`

- [ ] **Step 3.1 — Create WhatsNewService**

  `MyVocaList.Services/WhatsNewService.cs`:

  ```csharp
  using System.Text.Json;
  using Microsoft.Maui.ApplicationModel;
  using Microsoft.Maui.Storage;

  namespace MyVocaList.Services;

  public sealed class WhatsNewService : IWhatsNewService
  {
      private const string LastSeenKey = "last_seen_version";
      private const string ReleasesFileName = "releases.json";

      private readonly IPreferences _preferences;
      private readonly IAppInfo _appInfo;
      private readonly IFileSystem _fileSystem;
      private readonly ILogger<WhatsNewService> _logger;

      public WhatsNewService(
          IPreferences preferences,
          IAppInfo appInfo,
          IFileSystem fileSystem,
          ILogger<WhatsNewService> logger)
      {
          _preferences = preferences;
          _appInfo = appInfo;
          _fileSystem = fileSystem;
          _logger = logger;
      }

      /// <inheritdoc />
      public async Task<ReleaseEntry?> GetCurrentReleaseAsync(CancellationToken ct = default)
      {
          var entries = await LoadEntriesAsync(ct);
          return entries?.FirstOrDefault(e => e.Version == _appInfo.VersionString);
      }

      /// <inheritdoc />
      public async Task<ReleaseEntry?> GetPendingReleaseAsync(CancellationToken ct = default)
      {
          var current = _appInfo.VersionString;
          var lastSeen = _preferences.Get(LastSeenKey, (string?)null);

          if (lastSeen is null)
          {
              // Fresh install — store version, skip modal
              MarkCurrentVersionSeen();
              return null;
          }

          if (lastSeen == current)
              return null;

          var entries = await LoadEntriesAsync(ct);
          return entries?.FirstOrDefault(e => e.Version == current);
      }

      /// <inheritdoc />
      public void MarkCurrentVersionSeen()
          => _preferences.Set(LastSeenKey, _appInfo.VersionString);

      private async Task<IReadOnlyList<ReleaseEntry>?> LoadEntriesAsync(CancellationToken ct)
      {
          try
          {
              await using var stream = await _fileSystem.OpenAppPackageFileAsync(ReleasesFileName);
              var entries = await JsonSerializer.DeserializeAsync<List<ReleaseEntryJson>>(stream,
                  new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);
              return entries?
                  .Select(e => new ReleaseEntry(e.Version, e.Date,
                      (e.Highlights ?? []).AsReadOnly(),
                      (e.Fixes ?? []).AsReadOnly()))
                  .ToList()
                  .AsReadOnly();
          }
          catch (Exception ex)
          {
              _logger.LogWarning(ex, "Failed to load {File} — skipping What's New", ReleasesFileName);
              return null;
          }
      }

      // Local deserialization model (snake_case / camelCase JSON → C# properties)
      private sealed class ReleaseEntryJson
      {
          public string Version { get; set; } = string.Empty;
          public string Date { get; set; } = string.Empty;
          public List<string>? Highlights { get; set; }
          public List<string>? Fixes { get; set; }
      }
  }
  ```

- [ ] **Step 3.2 — Run tests to confirm Green**

  ```bash
  dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~WhatsNewServiceTests" --verbosity normal
  ```
  Expected: All 9 tests PASS.

- [ ] **Step 3.3 — Full build**

  ```bash
  dotnet build MyVocaList.sln
  ```
  Expected: 0 errors.

- [ ] **Step 3.4 — Commit**

  ```bash
  git add MyVocaList.Services/WhatsNewService.cs
  git commit -m "feat(whats-new): implement WhatsNewService with Preferences-backed seen tracking"
  ```

---

## Task 4 — Create `releases.json` MauiAsset

**Files:**
- Create: `MyVocaList/Resources/Raw/releases.json`
- Modify: `MyVocaList/MyVocaList.csproj` — confirm MauiAsset glob includes it

- [ ] **Step 4.1 — Create releases.json**

  `MyVocaList/Resources/Raw/releases.json`:

  ```json
  [
    {
      "version": "0.1.0",
      "date": "2026-06-01",
      "highlights": [
        "Artists & Songs catalog with API search",
        "YouTube karaoke URL management per song",
        "Crash reporting with Sentry integration",
        "Local database backup and restore"
      ],
      "fixes": []
    }
  ]
  ```

  > Update the version string to match the actual current `AppInfo.VersionString` before release.

- [ ] **Step 4.2 — Confirm MauiAsset build action**

  Open `MyVocaList/MyVocaList.csproj` and confirm there is a glob that covers `Resources/Raw/**`:

  ```xml
  <MauiAsset Include="Resources/Raw/**" LogicalName="%(RecursiveDir)%(Filename)%(Extension)" />
  ```

  If not present, add it. `releases.json` must have build action `MauiAsset` so `FileSystem.OpenAppPackageFileAsync("releases.json")` finds it.

- [ ] **Step 4.3 — Build**

  ```bash
  dotnet build MyVocaList.sln
  ```
  Expected: 0 errors.

- [ ] **Step 4.4 — Commit**

  ```bash
  git add MyVocaList/Resources/Raw/releases.json
  git commit -m "feat(whats-new): add releases.json MauiAsset with v0.1.0 entry"
  ```

---

## Task 5 — Create `WhatsNewBottomSheet` component

**Files:**
- Create: `MyVocaList/UI/Components/Sheets/WhatsNewBottomSheet.xaml`
- Create: `MyVocaList/UI/Components/Sheets/WhatsNewBottomSheet.xaml.cs`

> **Pattern reference:** See `MyVocaList/UI/Components/Sheets/ConfirmSheet.xaml` for existing sheet structure.

- [ ] **Step 5.1 — Read ConfirmSheet.xaml for pattern**

  ```bash
  # read MyVocaList/UI/Components/Sheets/ConfirmSheet.xaml
  # read MyVocaList/UI/Components/Sheets/ConfirmSheet.xaml.cs
  ```
  Note the `dx:BottomSheet` usage, binding approach, and dismiss pattern.

- [ ] **Step 5.2 — Create WhatsNewBottomSheet.xaml**

  `MyVocaList/UI/Components/Sheets/WhatsNewBottomSheet.xaml`:

  ```xml
  <?xml version="1.0" encoding="utf-8" ?>
  <ContentView
      x:Class="MyVocaList.UI.Components.Sheets.WhatsNewBottomSheet"
      xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
      xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
      xmlns:dx="clr-namespace:DevExpress.Maui.Controls;assembly=DevExpress.Maui.Controls"
      xmlns:dxe="clr-namespace:DevExpress.Maui.Editors;assembly=DevExpress.Maui.Editors"
      xmlns:dto="clr-namespace:MyVocaList.Contracts.DTOs;assembly=MyVocaList.Contracts">

      <dx:BottomSheet
          x:Name="Sheet"
          AllowDismiss="True"
          IsCancelable="True"
          HalfExpandedRatio="0.6"
          ExpandedRatio="0.85">

          <dx:BottomSheet.Content>
              <ScrollView>
                  <VerticalStackLayout Padding="24,16,24,32" Spacing="0">

                      <!-- Header -->
                      <Label
                          x:Name="TitleLabel"
                          Style="{DynamicResource TitleLargeStyle}"
                          Margin="0,0,0,4" />

                      <Label
                          x:Name="DateLabel"
                          Style="{DynamicResource BodySmallStyle}"
                          TextColor="{DynamicResource SecondaryTextColor}"
                          Margin="0,0,0,24" />

                      <!-- Highlights section -->
                      <Label
                          x:Name="HighlightsTitleLabel"
                          Text="What's New"
                          Style="{DynamicResource TitleMediumStyle}"
                          Margin="0,0,0,8"
                          IsVisible="{Binding HasHighlights, Source={RelativeSource AncestorType={x:Type ContentView}}}" />

                      <VerticalStackLayout
                          x:Name="HighlightsList"
                          Spacing="6"
                          Margin="0,0,0,16"
                          IsVisible="{Binding HasHighlights, Source={RelativeSource AncestorType={x:Type ContentView}}}">
                          <BindableLayout.ItemsSource>
                              <x:Reference Name="Sheet" />
                          </BindableLayout.ItemsSource>
                          <!-- Items bound via code-behind -->
                      </VerticalStackLayout>

                      <!-- Fixes section -->
                      <Label
                          x:Name="FixesTitleLabel"
                          Text="Bug Fixes"
                          Style="{DynamicResource TitleMediumStyle}"
                          Margin="0,0,0,8"
                          IsVisible="{Binding HasFixes, Source={RelativeSource AncestorType={x:Type ContentView}}}" />

                      <VerticalStackLayout
                          x:Name="FixesList"
                          Spacing="6"
                          Margin="0,0,0,24"
                          IsVisible="{Binding HasFixes, Source={RelativeSource AncestorType={x:Type ContentView}}}">
                          <!-- Items bound via code-behind -->
                      </VerticalStackLayout>

                      <!-- Got it button -->
                      <dx:DXButton
                          Content="Got it"
                          ButtonType="Filled"
                          HorizontalOptions="Fill"
                          Clicked="OnGotItClicked" />

                  </VerticalStackLayout>
              </ScrollView>
          </dx:BottomSheet.Content>
      </dx:BottomSheet>
  </ContentView>
  ```

  > Note: The bullet lists are built programmatically in code-behind to avoid complex XAML data templates on a static content view. This is simpler and correct for this use case.

- [ ] **Step 5.3 — Create WhatsNewBottomSheet.xaml.cs**

  `MyVocaList/UI/Components/Sheets/WhatsNewBottomSheet.xaml.cs`:

  ```csharp
  namespace MyVocaList.UI.Components.Sheets;

  public partial class WhatsNewBottomSheet : ContentView
  {
      private IWhatsNewService? _whatsNewService;

      public static readonly BindableProperty HasHighlightsProperty =
          BindableProperty.Create(nameof(HasHighlights), typeof(bool), typeof(WhatsNewBottomSheet), false);

      public static readonly BindableProperty HasFixesProperty =
          BindableProperty.Create(nameof(HasFixes), typeof(bool), typeof(WhatsNewBottomSheet), false);

      public bool HasHighlights
      {
          get => (bool)GetValue(HasHighlightsProperty);
          private set => SetValue(HasHighlightsProperty, value);
      }

      public bool HasFixes
      {
          get => (bool)GetValue(HasFixesProperty);
          private set => SetValue(HasFixesProperty, value);
      }

      public WhatsNewBottomSheet()
      {
          InitializeComponent();
      }

      public void Show(ReleaseEntry entry, IWhatsNewService whatsNewService)
      {
          _whatsNewService = whatsNewService;

          TitleLabel.Text = $"What's New in {entry.Version}";
          DateLabel.Text = entry.Date;

          HasHighlights = entry.Highlights.Count > 0;
          HasFixes = entry.Fixes.Count > 0;

          PopulateBulletList(HighlightsList, entry.Highlights);
          PopulateBulletList(FixesList, entry.Fixes);

          Sheet.State = DevExpress.Maui.Controls.BottomSheetState.HalfExpanded;
      }

      private static void PopulateBulletList(VerticalStackLayout container, IReadOnlyList<string> items)
      {
          container.Children.Clear();
          foreach (var item in items)
          {
              container.Children.Add(new Label
              {
                  Text = $"• {item}",
                  Style = (Style)Application.Current!.Resources["BodyMediumStyle"]
              });
          }
      }

      private void OnGotItClicked(object sender, EventArgs e)
      {
          _whatsNewService?.MarkCurrentVersionSeen();
          Sheet.State = DevExpress.Maui.Controls.BottomSheetState.Hidden;
      }
  }
  ```

- [ ] **Step 5.4 — Build (MAUI head — net10.0-android)**

  ```bash
  dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
  ```
  Expected: 0 errors. Fix any XAML or binding errors before proceeding.

- [ ] **Step 5.5 — Commit**

  ```bash
  git add "MyVocaList/UI/Components/Sheets/WhatsNewBottomSheet.xaml"
  git add "MyVocaList/UI/Components/Sheets/WhatsNewBottomSheet.xaml.cs"
  git commit -m "feat(whats-new): add WhatsNewBottomSheet component"
  ```

---

## Task 6 — Wire `AppShellViewModel` + `AppShell.xaml.cs`

**Files:**
- Modify: `MyVocaList/UI/ViewModels/AppShellViewModel.cs`
- Modify: `MyVocaList/AppShell.xaml.cs`

- [ ] **Step 6.1 — Define the messenger message record**

  Add to `MyVocaList/UI/Messages/ShowWhatsNewMessage.cs` (create the file):

  ```csharp
  namespace MyVocaList.UI.Messages;

  public sealed record ShowWhatsNewMessage(ReleaseEntry Entry);
  ```

- [ ] **Step 6.2 — Update AppShellViewModel**

  Add `IWhatsNewService` injection and `InitializeAsync` method. Read the current file first, then apply these changes:

  - Add `IWhatsNewService _whatsNewService` field
  - Add it to the constructor signature
  - Add `InitializeAsync` method

  Modified constructor + new method (add to existing class body):

  ```csharp
  // Add field:
  private readonly IWhatsNewService _whatsNewService;

  // Update constructor signature and body (keep existing parameters, add new one):
  public AppShellViewModel(IServiceProvider serviceProvider, IWhatsNewService whatsNewService)
  {
      _serviceProvider = serviceProvider;
      _whatsNewService = whatsNewService;
      NavigateCommand = new AsyncRelayCommand<string>(route => NavigateAsync(route!));
      MenuGroups = NavigationConfig.BuildMenuGroups(NavigateCommand);
  }

  // Add new method:
  public async Task InitializeAsync(CancellationToken ct = default)
  {
      var entry = await _whatsNewService.GetPendingReleaseAsync(ct);
      if (entry is not null)
          WeakReferenceMessenger.Default.Send(new ShowWhatsNewMessage(entry));
  }
  ```

- [ ] **Step 6.3 — Update AppShell.xaml.cs**

  Subscribe to `ShowWhatsNewMessage` and show the sheet. Final content:

  ```csharp
  using CommunityToolkit.Mvvm.Messaging;
  using MyVocaList.UI.Messages;
  using MyVocaList.UI.Components.Sheets;

  namespace MyVocaList;

  public partial class AppShell : Shell
  {
      private readonly AppShellViewModel _viewModel;
      private readonly IWhatsNewService _whatsNewService;
      private WhatsNewBottomSheet? _whatsNewSheet;

      public AppShell(AppShellViewModel viewModel, IWhatsNewService whatsNewService)
      {
          _viewModel = viewModel;
          _whatsNewService = whatsNewService;
          BindingContext = viewModel;
          InitializeComponent();

          viewModel.ExitRequested += OnExitRequested;

          Routing.RegisterRoute(Routes.VenueForm, typeof(VenueFormPage));
          Routing.RegisterRoute(Routes.PersonForm, typeof(PersonFormPage));
          Routing.RegisterRoute(Routes.ArtistForm, typeof(ArtistFormPage));
          Routing.RegisterRoute(Routes.SongForm, typeof(SongFormPage));

          WeakReferenceMessenger.Default.Register<ShowWhatsNewMessage>(this, OnShowWhatsNew);

          // Fire-and-forget startup check — does not block Shell initialization
          _ = viewModel.InitializeAsync();
      }

      private void OnShowWhatsNew(object recipient, ShowWhatsNewMessage message)
      {
          MainThread.BeginInvokeOnMainThread(() =>
          {
              _whatsNewSheet ??= new WhatsNewBottomSheet();
              // Add sheet to the visual tree if not already there
              if (CurrentPage is Page page && _whatsNewSheet.Parent is null)
              {
                  // Attach to the Shell's content overlay
                  if (page.Content is Layout layout)
                      layout.Children.Add(_whatsNewSheet);
              }
              _whatsNewSheet.Show(message.Entry, _whatsNewService);
          });
      }

      protected override bool OnBackButtonPressed()
      {
          if (Navigation.NavigationStack.Count == 0 && CurrentPage is QueuePage queuePage)
          {
              queuePage.ShowExitConfirmation();
              return true;
          }
          return base.OnBackButtonPressed();
      }

      private void OnExitRequested()
      {
          if (CurrentPage is QueuePage queuePage)
              queuePage.ShowExitConfirmation();
      }
  }
  ```

  > **Note on sheet attachment:** The `OnShowWhatsNew` approach above attaches the sheet to the current page layout. If this proves fragile, an alternative is to attach it directly to the `AppShell`'s `Content` root overlay. Investigate in the emulator and adjust — the `dx:BottomSheet` documentation shows multiple attachment patterns.

- [ ] **Step 6.4 — Build**

  ```bash
  dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android
  ```
  Expected: 0 errors.

- [ ] **Step 6.5 — Commit**

  ```bash
  git add MyVocaList/UI/Messages/ShowWhatsNewMessage.cs
  git add MyVocaList/UI/ViewModels/AppShellViewModel.cs
  git add MyVocaList/AppShell.xaml.cs
  git commit -m "feat(whats-new): wire AppShellViewModel startup check and AppShell sheet trigger"
  ```

---

## Task 7 — DI registration + `.sln` registration

**Files:**
- Modify: `MyVocaList/MauiProgram.cs`
- Modify: `MyVocaList.sln`

- [ ] **Step 7.1 — Swap NullWhatsNewService → WhatsNewService in MauiProgram.cs**

  Find this line:
  ```csharp
  builder.Services.AddSingleton<IWhatsNewService, NullWhatsNewService>();
  ```

  Replace with:
  ```csharp
  builder.Services.AddSingleton<IWhatsNewService, WhatsNewService>();
  ```

  Also confirm `AppShell` constructor gets `IWhatsNewService` resolved. Since both `AppShell` and `AppShellViewModel` are registered as `AddSingleton`, the DI container will inject `IWhatsNewService` automatically.

  Update the `AppShell` registration if it uses a factory — it should just be:
  ```csharp
  builder.Services.AddSingleton<AppShell>();
  ```
  The DI container resolves all constructor parameters automatically.

- [ ] **Step 7.2 — Register MAUI injectable interfaces if not already present**

  MAUI registers `IPreferences`, `IAppInfo`, and `IFileSystem` automatically via `UseMauiApp<App>()`. No manual registration needed — verify by checking `builder.Services` registrations or MAUI source docs. If the build fails with "cannot resolve IPreferences", add:

  ```csharp
  builder.Services.AddSingleton<IPreferences>(Preferences.Default);
  builder.Services.AddSingleton<IAppInfo>(AppInfo.Current);
  builder.Services.AddSingleton<IFileSystem>(FileSystem.Current);
  ```

- [ ] **Step 7.3 — Register new files in MyVocaList.sln**

  Open `MyVocaList.sln` and find the `BusinessFeatures` or `whats-new` solution folder (or create one). Add entries for all new files under the appropriate `ProjectSection(SolutionItems)`:

  ```
  Docs\Management\BusinessFeatures\whats-new\plan.md = Docs\Management\BusinessFeatures\whats-new\plan.md
  ```

  Per `constraints-registry.md § Visual Studio Solution (.sln)`: new docs files must be registered before commit.

- [ ] **Step 7.4 — Full build + test**

  ```bash
  dotnet build MyVocaList.sln
  dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj
  ```
  Expected: 0 errors, 0 test failures.

- [ ] **Step 7.5 — Commit**

  ```bash
  git add MyVocaList/MauiProgram.cs
  git add MyVocaList.sln
  git commit -m "feat(whats-new): register WhatsNewService in DI; register new files in .sln"
  ```

---

## Self-Review

### Spec coverage check

| AC | Task that covers it |
|----|---------------------|
| AC-WN-01 Modal shown once per upgrade | Task 2 test + Task 3 `GetPendingReleaseAsync` |
| AC-WN-02 Hidden on fresh install | Task 2 test + Task 3 fresh-install path |
| AC-WN-03 Hidden when no entry | Task 2 test + Task 3 `FirstOrDefault` returning null |
| AC-WN-04 Modal content correct | Task 5 `WhatsNewBottomSheet.Show()` sets all fields |
| AC-WN-05 Dismiss persists version | Task 5 `OnGotItClicked` + Task 2 `MarkCurrentVersionSeen` test |
| AC-WN-06 No network call | Architecture: bundled JSON, no HTTP; Task 3 uses `IFileSystem` only |

### No placeholder scan
All tasks contain complete code. No TBD/TODO/placeholder items remain.

### Type consistency
- `ReleaseEntry` — existing DTO, used consistently in Tasks 1, 2, 3, 5, 6
- `IWhatsNewService.GetPendingReleaseAsync` — defined Task 1, tested Task 2, implemented Task 3, called Task 6
- `IWhatsNewService.MarkCurrentVersionSeen` — defined Task 1, tested Task 2, implemented Task 3, called Task 5
- `ShowWhatsNewMessage` — defined Task 6.1, sent Task 6.2, received Task 6.3

---

## Verification

After all tasks complete:
1. `dotnet test` — all `WhatsNewServiceTests` pass
2. `dotnet build MyVocaList.sln` — 0 errors
3. Emulator smoke test:
   - Fresh install → no modal appears
   - Downgrade `last_seen_version` in Preferences to an older value → modal appears
   - Dismiss "Got it" → relaunch → modal does NOT reappear
   - About page → "What's New" section shows current release notes
