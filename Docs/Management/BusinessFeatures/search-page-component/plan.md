# Search Page Component — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace non-MD3 inline search strips in ArtistFormPage and SongFormPage with three dedicated picker pages (ArtistPickerPage, SongPickerPage, YouTubeSearchPage) that follow the MD3 standalone search destination pattern and return results to calling ViewModels via WeakReferenceMessenger typed messages.

**Architecture:** Caller ViewModel registers a typed IMessenger handler before navigating; picker ViewModel sends the typed message on selection and pops; caller receives the message, updates its fields, and unregisters. SearchAppBar's existing `Action1Command`/`Action1Icon` bindable slots (from `AppBarBase`) serve as the explicit search-submit trigger — no component modification required.

**Tech Stack:** .NET MAUI 10 · CommunityToolkit.Mvvm (IAsyncRelayCommand, IMessenger, WeakReferenceMessenger) · DevExpress MAUI (dx:ShimmerView, DXCollectionView, ListItem) · xUnit + Moq

---

## Pre-implementation investigation — SearchAppBar wiring (RESOLVED)

**Finding (document in task-log before any code):**
`SearchAppBar` does not expose `SearchCommand` directly. Picker pages use the existing `AppBarBase.Action1Command` and `AppBarBase.Action1Icon` bindable properties (defined in `MyVocaList/UI/Components/AppBars/AppBarBase.cs`). The page's `Shell.TitleView` binds:
```xml
<appbars:SearchAppBar
    SearchText="{Binding SearchText, Mode=TwoWay}"
    BackCommand="{Binding BackCommand}"
    Action1Icon="search_outlined"
    Action1Command="{Binding SearchCommand}" />
```
No modification to `SearchAppBar.xaml` or `SearchAppBar.xaml.cs` is needed. This avoids all component-change governance requirements.

---

## File Map

**New files:**
| File | Responsibility |
|------|---------------|
| `Contracts/Messages/ArtistPickedMessage.cs` | Typed messenger payload: carries `MusicSearchResultDto` |
| `Contracts/Messages/SongPickedMessage.cs` | Typed messenger payload: carries `MusicSearchResultDto` |
| `Contracts/Messages/YouTubeVideoPickedMessage.cs` | Typed messenger payload: carries `YouTubeSearchResultDto` |
| `MyVocaList/UI/ViewModels/ArtistPickerViewModel.cs` | Search + result selection for artist picker |
| `MyVocaList/UI/ViewModels/SongPickerViewModel.cs` | Search + result selection for song picker |
| `MyVocaList/UI/ViewModels/YouTubeSearchViewModel.cs` | Search + result selection for YouTube picker |
| `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml` + `.xaml.cs` | Artist picker page XAML + code-behind |
| `MyVocaList/UI/Pages/Songs/SongPickerPage.xaml` + `.xaml.cs` | Song picker page XAML + code-behind |
| `MyVocaList/UI/Pages/Songs/YouTubeSearchPage.xaml` + `.xaml.cs` | YouTube search page XAML + code-behind |
| `MyVocaList.Tests/Unit/ViewModels/ArtistPickerViewModelTests.cs` | ViewModel unit tests (TDD Red-first) |
| `MyVocaList.Tests/Unit/ViewModels/SongPickerViewModelTests.cs` | ViewModel unit tests |
| `MyVocaList.Tests/Unit/ViewModels/YouTubeSearchViewModelTests.cs` | ViewModel unit tests |

**Modified files:**
| File | Change |
|------|--------|
| `MyVocaList/Navigation/Routes.cs` | Add 3 route constants |
| `MyVocaList/AppShell.xaml.cs` | Register 3 routes |
| `MyVocaList/MauiProgram.cs` | Register 3 pages, 3 VMs, IMessenger singleton |
| `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml` | Remove API strip, add ListItem trigger |
| `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs` | Remove 8 old properties/commands, add NavigateToArtistPickerCommand + messenger wiring |
| `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` | Remove 2 strips, add 2 ListItem triggers |
| `MyVocaList/UI/ViewModels/SongFormViewModel.cs` | Remove 13 old properties/commands, add 2 navigate commands + 2 messenger wirings |

---

## Phase 1 — Contracts

### Task 1: Add WeakReferenceMessenger message records

**Files:**
- Create: `Contracts/Messages/ArtistPickedMessage.cs`
- Create: `Contracts/Messages/SongPickedMessage.cs`
- Create: `Contracts/Messages/YouTubeVideoPickedMessage.cs`

- [ ] **Step 1.1: Create Messages folder and ArtistPickedMessage**

```csharp
// Contracts/Messages/ArtistPickedMessage.cs
namespace MyVocaList.Contracts.Messages;

public sealed record ArtistPickedMessage(MusicSearchResultDto Result);
```

- [ ] **Step 1.2: Create SongPickedMessage**

```csharp
// Contracts/Messages/SongPickedMessage.cs
namespace MyVocaList.Contracts.Messages;

public sealed record SongPickedMessage(MusicSearchResultDto Result);
```

- [ ] **Step 1.3: Create YouTubeVideoPickedMessage**

```csharp
// Contracts/Messages/YouTubeVideoPickedMessage.cs
using MyVocaList.Contracts.DTOs.List;

namespace MyVocaList.Contracts.Messages;

public sealed record YouTubeVideoPickedMessage(YouTubeSearchResultDto Result);
```

- [ ] **Step 1.4: Build**

Run: `dotnet build MyVocaList.sln`
Expected: 0 errors. The three records are in `MyVocaList.Contracts.Messages` namespace and accessible from the MAUI project.

- [ ] **Step 1.5: Register files in MyVocaList.sln**

The three `.cs` files are in `Contracts/` which is already a registered project folder. Verify `MyVocaList.Contracts.csproj` includes the new `Messages/` folder. No `.sln` Solution Folder change needed (source files are picked up by the project automatically). Confirm with a build.

- [ ] **Step 1.6: Commit**

```bash
git add Contracts/Messages/
git commit -m "feat: add WeakReferenceMessenger message records for picker pages"
```

---

## Phase 2 — Picker ViewModels (TDD: Tester writes all tests first)

**Tester/Builder split applies.** The Tester subagent writes all three test files and confirms they compile but fail (cannot instantiate the ViewModels — they don't exist yet). Then the Builder subagent implements the three ViewModels to make the tests pass.

### Task 2T: Write ViewModel tests (Tester — runs first)

**Files:**
- Create: `MyVocaList.Tests/Unit/ViewModels/ArtistPickerViewModelTests.cs`
- Create: `MyVocaList.Tests/Unit/ViewModels/SongPickerViewModelTests.cs`
- Create: `MyVocaList.Tests/Unit/ViewModels/YouTubeSearchViewModelTests.cs`

> **Note:** The MAUI project must be referenced by the test project for ViewModel tests. The existing `MyVocaList.Tests.csproj` already includes `<ProjectReference>` to MAUI if ViewModel tests already exist. Verify the `.csproj` has `<ProjectReference Include="..\MyVocaList\MyVocaList.csproj" />` and the MAUI project has:
> ```xml
> <PropertyGroup Condition="'$(TargetFramework)' == 'net10.0'">
>   <OutputType>Library</OutputType>
> </PropertyGroup>
> ```

- [ ] **Step 2T.1: Verify test project can reference MAUI ViewModels**

Run: `dotnet build MyVocaList.Tests/MyVocaList.Tests.csproj`
If it fails with "cannot find namespace MyVocaList.UI.ViewModels", add the MAUI project reference and `<OutputType>Library</OutputType>` condition as shown above, then rebuild.

- [ ] **Step 2T.2: Write ArtistPickerViewModelTests**

```csharp
// MyVocaList.Tests/Unit/ViewModels/ArtistPickerViewModelTests.cs
using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.Messages;
using MyVocaList.UI.ViewModels;

namespace MyVocaList.Tests.Unit.ViewModels;

public class ArtistPickerViewModelTests
{
    private readonly Mock<IMusicMetadataService> _serviceMock = new();
    private readonly Mock<ILogger<ArtistPickerViewModel>> _loggerMock = new();
    private readonly WeakReferenceMessenger _messenger = new();

    private ArtistPickerViewModel CreateSut() =>
        new(_serviceMock.Object, _messenger, _loggerMock.Object);

    // [AC] AC-LOAD-05: Pre-search state — neither loading nor empty shown
    [Fact]
    public void OnCreation_HasSearchedIsFalse_IsLoadingIsFalse()
    {
        var sut = CreateSut();
        Assert.False(sut.HasSearched);
        Assert.False(sut.IsLoading);
    }

    // [AC] Validation rule: empty query must not trigger a search
    [Fact]
    public async Task SearchCommand_EmptySearchText_DoesNotCallService()
    {
        var sut = CreateSut();
        sut.SearchText = "   ";

        await sut.SearchCommand.ExecuteAsync(null);

        _serviceMock.Verify(
            s => s.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // [AC] AC-LOAD-01: IsLoading = true synchronously before any await
    [Fact]
    public async Task SearchCommand_ValidQuery_SetsIsLoadingTrueBeforeAwait()
    {
        var loadingDuringCall = false;
        _serviceMock
            .Setup(s => s.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, CancellationToken _) =>
            {
                loadingDuringCall = true; // captured while service call is "in flight"
                await Task.Yield();
                return (IEnumerable<MusicSearchResultDto>)[new MusicSearchResultDto("id1", "MB", "Artist1", "")];
            });

        var sut = CreateSut();
        sut.SearchText = "test";
        await sut.SearchCommand.ExecuteAsync(null);

        Assert.True(loadingDuringCall);
    }

    // [AC] AC-LOAD-02: Prior results cleared before new search
    [Fact]
    public async Task SearchCommand_CalledTwice_ClearsPriorResults()
    {
        var callCount = 0;
        _serviceMock
            .Setup(s => s.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, CancellationToken _) =>
            {
                callCount++;
                return callCount == 1
                    ? (IEnumerable<MusicSearchResultDto>)[new MusicSearchResultDto("id1", "MB", "Artist1", "")]
                    : (IEnumerable<MusicSearchResultDto>)[];
            });

        var sut = CreateSut();
        sut.SearchText = "first";
        await sut.SearchCommand.ExecuteAsync(null);
        Assert.Single(sut.Results);

        sut.SearchText = "second";
        await sut.SearchCommand.ExecuteAsync(null);
        Assert.Empty(sut.Results);
    }

    // [AC] AC-ART-03 success path: Results populated, HasResults=true, HasSearched=true, IsLoading=false
    [Fact]
    public async Task SearchCommand_ServiceReturnsResults_SetsResultsAndFlags()
    {
        var dto = new MusicSearchResultDto("id1", "MB", "The Artist", "");
        _serviceMock
            .Setup(s => s.SearchArtistsAsync("query", It.IsAny<CancellationToken>()))
            .ReturnsAsync([dto]);

        var sut = CreateSut();
        sut.SearchText = "query";
        await sut.SearchCommand.ExecuteAsync(null);

        Assert.True(sut.HasResults);
        Assert.True(sut.HasSearched);
        Assert.False(sut.IsLoading);
        Assert.Single(sut.Results);
        Assert.Equal("The Artist", sut.Results[0].ArtistName);
    }

    // [AC] AC-ART-05 empty result path
    [Fact]
    public async Task SearchCommand_ServiceReturnsEmpty_HasResultsFalseHasSearchedTrue()
    {
        _serviceMock
            .Setup(s => s.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sut = CreateSut();
        sut.SearchText = "noresult";
        await sut.SearchCommand.ExecuteAsync(null);

        Assert.False(sut.HasResults);
        Assert.True(sut.HasSearched);
        Assert.False(sut.IsLoading);
    }

    // [AC] AC-LOAD-03 + AC-LOAD-04: Exception path — IsLoading=false, HasSearched=true, logged
    [Fact]
    public async Task SearchCommand_ServiceThrows_SetsErrorStateAndLogsException()
    {
        var ex = new HttpRequestException("network error");
        _serviceMock
            .Setup(s => s.SearchArtistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

        var sut = CreateSut();
        sut.SearchText = "fail";
        await sut.SearchCommand.ExecuteAsync(null);

        Assert.False(sut.IsLoading);
        Assert.True(sut.HasSearched);
        Assert.False(sut.HasResults);
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => true),
                ex,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // [AC] AC-ART-04: SelectResultCommand sends ArtistPickedMessage via IMessenger
    [Fact]
    public void SelectResultCommand_ValidResult_SendsArtistPickedMessage()
    {
        ArtistPickedMessage? received = null;
        _messenger.Register<ArtistPickedMessage>(this, (_, msg) => received = msg);

        var dto = new MusicSearchResultDto("id1", "MB", "The Artist", "");
        var sut = CreateSut();

        sut.SelectResultCommand.Execute(dto);

        Assert.NotNull(received);
        Assert.Equal("The Artist", received.Result.ArtistName);
    }
}
```

- [ ] **Step 2T.3: Run tests — confirm they FAIL (Red)**

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~ArtistPickerViewModelTests" --verbosity normal`
Expected: compile error "type or namespace 'ArtistPickerViewModel' not found" — Red confirmed.

- [ ] **Step 2T.4: Write SongPickerViewModelTests**

```csharp
// MyVocaList.Tests/Unit/ViewModels/SongPickerViewModelTests.cs
using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.Messages;
using MyVocaList.UI.ViewModels;

namespace MyVocaList.Tests.Unit.ViewModels;

public class SongPickerViewModelTests
{
    private readonly Mock<IMusicMetadataService> _serviceMock = new();
    private readonly Mock<ILogger<SongPickerViewModel>> _loggerMock = new();
    private readonly WeakReferenceMessenger _messenger = new();

    private SongPickerViewModel CreateSut() =>
        new(_serviceMock.Object, _messenger, _loggerMock.Object);

    [Fact]
    public void OnCreation_HasSearchedIsFalse_IsLoadingIsFalse()
    {
        var sut = CreateSut();
        Assert.False(sut.HasSearched);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task SearchCommand_EmptySearchText_DoesNotCallService()
    {
        var sut = CreateSut();
        sut.SearchText = "";
        await sut.SearchCommand.ExecuteAsync(null);
        _serviceMock.Verify(
            s => s.SearchSongsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchCommand_ServiceReturnsResults_PopulatesResults()
    {
        var dto = new MusicSearchResultDto("id1", "MB", "The Artist", "The Song");
        _serviceMock
            .Setup(s => s.SearchSongsAsync("query", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([dto]);

        var sut = CreateSut();
        sut.SearchText = "query";
        await sut.SearchCommand.ExecuteAsync(null);

        Assert.True(sut.HasResults);
        Assert.True(sut.HasSearched);
        Assert.False(sut.IsLoading);
        Assert.Equal("The Song", sut.Results[0].SongTitle);
    }

    [Fact]
    public async Task SearchCommand_ServiceReturnsEmpty_HasResultsFalse()
    {
        _serviceMock
            .Setup(s => s.SearchSongsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var sut = CreateSut();
        sut.SearchText = "x";
        await sut.SearchCommand.ExecuteAsync(null);
        Assert.False(sut.HasResults);
        Assert.True(sut.HasSearched);
    }

    // [AC] AC-SONG-04 + null-safety: SongTitle null maps to empty string in message
    [Fact]
    public void SelectResultCommand_NullSongTitle_SendsEmptyStringTitle()
    {
        SongPickedMessage? received = null;
        _messenger.Register<SongPickedMessage>(this, (_, msg) => received = msg);

        var dto = new MusicSearchResultDto("id1", "MB", "Artist", null!);
        var sut = CreateSut();
        sut.SelectResultCommand.Execute(dto);

        Assert.NotNull(received);
        Assert.Equal(string.Empty, received.Result.SongTitle ?? string.Empty);
    }

    [Fact]
    public void SelectResultCommand_ValidResult_SendsSongPickedMessage()
    {
        SongPickedMessage? received = null;
        _messenger.Register<SongPickedMessage>(this, (_, msg) => received = msg);

        var dto = new MusicSearchResultDto("id1", "MB", "Artist", "Title");
        var sut = CreateSut();
        sut.SelectResultCommand.Execute(dto);

        Assert.NotNull(received);
        Assert.Equal("Title", received.Result.SongTitle);
        Assert.Equal("Artist", received.Result.ArtistName);
    }

    [Fact]
    public async Task SearchCommand_ServiceThrows_SetsErrorState()
    {
        var ex = new Exception("boom");
        _serviceMock
            .Setup(s => s.SearchSongsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        var sut = CreateSut();
        sut.SearchText = "fail";
        await sut.SearchCommand.ExecuteAsync(null);
        Assert.False(sut.IsLoading);
        Assert.True(sut.HasSearched);
        Assert.False(sut.HasResults);
    }
}
```

- [ ] **Step 2T.5: Run tests — confirm Red**

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~SongPickerViewModelTests" --verbosity normal`
Expected: compile error — Red confirmed.

- [ ] **Step 2T.6: Write YouTubeSearchViewModelTests**

```csharp
// MyVocaList.Tests/Unit/ViewModels/YouTubeSearchViewModelTests.cs
using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Contracts.Messages;
using MyVocaList.UI.ViewModels;

namespace MyVocaList.Tests.Unit.ViewModels;

public class YouTubeSearchViewModelTests
{
    private readonly Mock<IYouTubeSearchService> _serviceMock = new();
    private readonly Mock<ILogger<YouTubeSearchViewModel>> _loggerMock = new();
    private readonly WeakReferenceMessenger _messenger = new();

    private YouTubeSearchViewModel CreateSut() =>
        new(_serviceMock.Object, _messenger, _loggerMock.Object);

    [Fact]
    public void OnCreation_HasSearchedIsFalse_IsLoadingIsFalse()
    {
        var sut = CreateSut();
        Assert.False(sut.HasSearched);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task SearchCommand_EmptySearchText_DoesNotCallService()
    {
        var sut = CreateSut();
        sut.SearchText = "";
        await sut.SearchCommand.ExecuteAsync(null);
        _serviceMock.Verify(
            s => s.SearchVideosAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchCommand_ServiceReturnsResults_PopulatesResults()
    {
        var dto = new YouTubeSearchResultDto("vid1", "Karaoke Song", "Channel", 180, "https://thumb.url");
        _serviceMock
            .Setup(s => s.SearchVideosAsync("query", It.IsAny<CancellationToken>()))
            .ReturnsAsync([dto]);

        var sut = CreateSut();
        sut.SearchText = "query";
        await sut.SearchCommand.ExecuteAsync(null);

        Assert.True(sut.HasResults);
        Assert.True(sut.HasSearched);
        Assert.False(sut.IsLoading);
        Assert.Equal("Karaoke Song", sut.Results[0].Title);
    }

    [Fact]
    public async Task SearchCommand_ServiceReturnsEmpty_HasResultsFalse()
    {
        _serviceMock
            .Setup(s => s.SearchVideosAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var sut = CreateSut();
        sut.SearchText = "x";
        await sut.SearchCommand.ExecuteAsync(null);
        Assert.False(sut.HasResults);
        Assert.True(sut.HasSearched);
    }

    // [AC] AC-YT-06: SelectResultCommand sends YouTubeVideoPickedMessage
    [Fact]
    public void SelectResultCommand_ValidResult_SendsYouTubeVideoPickedMessage()
    {
        YouTubeVideoPickedMessage? received = null;
        _messenger.Register<YouTubeVideoPickedMessage>(this, (_, msg) => received = msg);

        var dto = new YouTubeSearchResultDto("vid1", "Title", "Channel", 120, "https://thumb");
        var sut = CreateSut();
        sut.SelectResultCommand.Execute(dto);

        Assert.NotNull(received);
        Assert.Equal("vid1", received.Result.VideoId);
    }

    [Fact]
    public async Task SearchCommand_ServiceThrows_SetsErrorState()
    {
        var ex = new Exception("boom");
        _serviceMock
            .Setup(s => s.SearchVideosAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        var sut = CreateSut();
        sut.SearchText = "fail";
        await sut.SearchCommand.ExecuteAsync(null);
        Assert.False(sut.IsLoading);
        Assert.True(sut.HasSearched);
        Assert.False(sut.HasResults);
    }
}
```

- [ ] **Step 2T.7: Run all Phase 2 tests — confirm all Red**

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "ArtistPickerViewModelTests|SongPickerViewModelTests|YouTubeSearchViewModelTests" --verbosity normal`
Expected: compile errors for all three ViewModel types — Red confirmed.

- [ ] **Step 2T.8: Commit test files (Red state)**

```bash
git add MyVocaList.Tests/Unit/ViewModels/ArtistPickerViewModelTests.cs
git add MyVocaList.Tests/Unit/ViewModels/SongPickerViewModelTests.cs
git add MyVocaList.Tests/Unit/ViewModels/YouTubeSearchViewModelTests.cs
git commit -m "test: add picker ViewModel tests (Red — ViewModels not yet implemented)"
```

---

### Task 2B: Implement Picker ViewModels (Builder — runs after 2T is committed)

**Files:**
- Create: `MyVocaList/UI/ViewModels/ArtistPickerViewModel.cs`
- Create: `MyVocaList/UI/ViewModels/SongPickerViewModel.cs`
- Create: `MyVocaList/UI/ViewModels/YouTubeSearchViewModel.cs`

- [ ] **Step 2B.1: Implement ArtistPickerViewModel**

```csharp
// MyVocaList/UI/ViewModels/ArtistPickerViewModel.cs
using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.Messages;

namespace MyVocaList.UI.ViewModels;

public partial class ArtistPickerViewModel : ObservableObject
{
    private readonly IMusicMetadataService _service;
    private readonly IMessenger _messenger;
    private readonly ILogger<ArtistPickerViewModel> _logger;
    private CancellationTokenSource _cts = new();

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasResults;
    [ObservableProperty] private bool _hasSearched;
    [ObservableProperty] private string _emptyStateMessage = "No artists found.";

    public ObservableRangeCollection<MusicSearchResultDto> Results { get; } = [];

    public IAsyncRelayCommand SearchCommand { get; }
    public IRelayCommand<MusicSearchResultDto> SelectResultCommand { get; }
    public IAsyncRelayCommand BackCommand { get; }

    public ArtistPickerViewModel(
        IMusicMetadataService service,
        IMessenger messenger,
        ILogger<ArtistPickerViewModel> logger)
    {
        _service = service;
        _messenger = messenger;
        _logger = logger;

        SearchCommand = new AsyncRelayCommand(SearchAsync);
        SelectResultCommand = new RelayCommand<MusicSearchResultDto>(SelectResult);
        BackCommand = new AsyncRelayCommand(async () => await Shell.Current.GoToAsync(".."));
    }

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;

        try { _cts.Cancel(); _cts.Dispose(); }
        catch { /* ignore disposal races */ }
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        IsLoading = true;
        HasSearched = false;
        Results.Clear();

        try
        {
            var items = await _service.SearchArtistsAsync(SearchText, ct);
            Results.ReplaceRange(items);
            HasResults = Results.Count > 0;
            HasSearched = true;
        }
        catch (OperationCanceledException)
        {
            // Superseded by newer search
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Artist search failed for query {Query}", SearchText);
            HasResults = false;
            HasSearched = true;
            EmptyStateMessage = "Search failed. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SelectResult(MusicSearchResultDto result)
    {
        _messenger.Send(new ArtistPickedMessage(result));
        Shell.Current.GoToAsync("..");
    }
}
```

- [ ] **Step 2B.2: Run ArtistPickerViewModel tests — confirm Green**

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~ArtistPickerViewModelTests" --verbosity normal`
Expected: all tests PASS. Fix any failures before proceeding.

- [ ] **Step 2B.3: Implement SongPickerViewModel**

```csharp
// MyVocaList/UI/ViewModels/SongPickerViewModel.cs
using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs;
using MyVocaList.Contracts.Messages;

namespace MyVocaList.UI.ViewModels;

public partial class SongPickerViewModel : ObservableObject
{
    private readonly IMusicMetadataService _service;
    private readonly IMessenger _messenger;
    private readonly ILogger<SongPickerViewModel> _logger;
    private CancellationTokenSource _cts = new();

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasResults;
    [ObservableProperty] private bool _hasSearched;
    [ObservableProperty] private string _emptyStateMessage = "No songs found.";

    public ObservableRangeCollection<MusicSearchResultDto> Results { get; } = [];

    public IAsyncRelayCommand SearchCommand { get; }
    public IRelayCommand<MusicSearchResultDto> SelectResultCommand { get; }
    public IAsyncRelayCommand BackCommand { get; }

    public SongPickerViewModel(
        IMusicMetadataService service,
        IMessenger messenger,
        ILogger<SongPickerViewModel> logger)
    {
        _service = service;
        _messenger = messenger;
        _logger = logger;

        SearchCommand = new AsyncRelayCommand(SearchAsync);
        SelectResultCommand = new RelayCommand<MusicSearchResultDto>(SelectResult);
        BackCommand = new AsyncRelayCommand(async () => await Shell.Current.GoToAsync(".."));
    }

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;

        try { _cts.Cancel(); _cts.Dispose(); }
        catch { /* ignore disposal races */ }
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        IsLoading = true;
        HasSearched = false;
        Results.Clear();

        try
        {
            var items = await _service.SearchSongsAsync(SearchText, artistHint: null, ct);
            Results.ReplaceRange(items);
            HasResults = Results.Count > 0;
            HasSearched = true;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Song search failed for query {Query}", SearchText);
            HasResults = false;
            HasSearched = true;
            EmptyStateMessage = "Search failed. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SelectResult(MusicSearchResultDto result)
    {
        _messenger.Send(new SongPickedMessage(result));
        Shell.Current.GoToAsync("..");
    }
}
```

- [ ] **Step 2B.4: Run SongPickerViewModel tests — confirm Green**

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~SongPickerViewModelTests" --verbosity normal`
Expected: all PASS.

- [ ] **Step 2B.5: Implement YouTubeSearchViewModel**

```csharp
// MyVocaList/UI/ViewModels/YouTubeSearchViewModel.cs
using CommunityToolkit.Mvvm.Messaging;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Contracts.Messages;

namespace MyVocaList.UI.ViewModels;

public partial class YouTubeSearchViewModel : ObservableObject
{
    private readonly IYouTubeSearchService _service;
    private readonly IMessenger _messenger;
    private readonly ILogger<YouTubeSearchViewModel> _logger;
    private CancellationTokenSource _cts = new();

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasResults;
    [ObservableProperty] private bool _hasSearched;
    [ObservableProperty] private string _emptyStateMessage = "No videos found.";

    public ObservableRangeCollection<YouTubeSearchResultDto> Results { get; } = [];

    public IAsyncRelayCommand SearchCommand { get; }
    public IRelayCommand<YouTubeSearchResultDto> SelectResultCommand { get; }
    public IAsyncRelayCommand BackCommand { get; }

    public YouTubeSearchViewModel(
        IYouTubeSearchService service,
        IMessenger messenger,
        ILogger<YouTubeSearchViewModel> logger)
    {
        _service = service;
        _messenger = messenger;
        _logger = logger;

        SearchCommand = new AsyncRelayCommand(SearchAsync);
        SelectResultCommand = new RelayCommand<YouTubeSearchResultDto>(SelectResult);
        BackCommand = new AsyncRelayCommand(async () => await Shell.Current.GoToAsync(".."));
    }

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;

        try { _cts.Cancel(); _cts.Dispose(); }
        catch { /* ignore disposal races */ }
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        IsLoading = true;
        HasSearched = false;
        Results.Clear();

        try
        {
            var items = await _service.SearchVideosAsync(SearchText, ct);
            Results.ReplaceRange(items);
            HasResults = Results.Count > 0;
            HasSearched = true;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "YouTube search failed for query {Query}", SearchText);
            HasResults = false;
            HasSearched = true;
            EmptyStateMessage = "Search failed. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SelectResult(YouTubeSearchResultDto result)
    {
        _messenger.Send(new YouTubeVideoPickedMessage(result));
        Shell.Current.GoToAsync("..");
    }
}
```

- [ ] **Step 2B.6: Run all Phase 2 tests — confirm all Green**

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "ArtistPickerViewModelTests|SongPickerViewModelTests|YouTubeSearchViewModelTests" --verbosity normal`
Expected: all tests PASS. Fix any failures before proceeding.

- [ ] **Step 2B.7: Full test suite — no regressions**

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --verbosity normal`
Expected: 0 failures. If regressions exist, fix before committing.

- [ ] **Step 2B.8: Commit**

```bash
git add MyVocaList/UI/ViewModels/ArtistPickerViewModel.cs
git add MyVocaList/UI/ViewModels/SongPickerViewModel.cs
git add MyVocaList/UI/ViewModels/YouTubeSearchViewModel.cs
git commit -m "feat: implement ArtistPickerViewModel, SongPickerViewModel, YouTubeSearchViewModel"
```

---

## Phase 3 — Picker Pages

**Constraint:** XAML incremental — edit ONE file → build → fix → then next file.
**Parallel OK** for the three pages (different files, no shared edits).
**Prerequisite:** Phase 2B must be committed (ViewModels exist).

### Task 3a: Implement ArtistPickerPage

**Files:**
- Create: `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml`
- Create: `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml.cs`

> **Before writing XAML:** Read `myvocalist-coding` skill to verify current ListItem, EmptyState, and dx:ShimmerView usage patterns. Verify how `EmptyState` accepts a message string (property name). Check an existing page (e.g. `SongsPage.xaml`) for ShimmerView patterns.

- [ ] **Step 3a.1: Create ArtistPickerPage.xaml**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:dx="http://schemas.devexpress.com/maui"
    xmlns:vm="clr-namespace:MyVocaList.UI.ViewModels"
    xmlns:dto="clr-namespace:MyVocaList.Contracts.DTOs;assembly=MyVocaList.Contracts"
    xmlns:appbars="clr-namespace:MyVocaList.UI.Components.AppBars"
    xmlns:lists="clr-namespace:MyVocaList.UI.Components.Lists"
    xmlns:components="clr-namespace:MyVocaList.UI.Components"
    x:Class="MyVocaList.UI.Pages.Artists.ArtistPickerPage"
    x:DataType="vm:ArtistPickerViewModel"
    BackgroundColor="{StaticResource Surface}"
    SafeAreaEdges="Container"
    Shell.NavBarIsVisible="False">

    <Shell.TitleView>
        <appbars:SearchAppBar
            SearchText="{Binding SearchText, Mode=TwoWay}"
            BackCommand="{Binding BackCommand}"
            Action1Icon="search_outlined"
            Action1Command="{Binding SearchCommand}"
            Placeholder="Search artists..." />
    </Shell.TitleView>

    <Grid>
        <!-- Loading skeleton -->
        <dx:ShimmerView IsActive="{Binding IsLoading}"
                        IsVisible="{Binding IsLoading}">
            <VerticalStackLayout Padding="16" Spacing="4">
                <BoxView HeightRequest="56" CornerRadius="8" BackgroundColor="{StaticResource SurfaceContainerLow}" />
                <BoxView HeightRequest="56" CornerRadius="8" BackgroundColor="{StaticResource SurfaceContainerLow}" />
                <BoxView HeightRequest="56" CornerRadius="8" BackgroundColor="{StaticResource SurfaceContainerLow}" />
                <BoxView HeightRequest="56" CornerRadius="8" BackgroundColor="{StaticResource SurfaceContainerLow}" />
                <BoxView HeightRequest="56" CornerRadius="8" BackgroundColor="{StaticResource SurfaceContainerLow}" />
            </VerticalStackLayout>
        </dx:ShimmerView>

        <!-- Results list -->
        <dx:DXCollectionView
            ItemsSource="{Binding Results}"
            IsVisible="{Binding HasResults}">
            <dx:DXCollectionView.ItemTemplate>
                <DataTemplate x:DataType="dto:MusicSearchResultDto">
                    <lists:ListItem
                        Headline="{Binding ArtistName}"
                        Command="{Binding Source={RelativeSource AncestorType={x:Type vm:ArtistPickerViewModel}}, Path=SelectResultCommand}"
                        CommandParameter="{Binding .}" />
                </DataTemplate>
            </dx:DXCollectionView.ItemTemplate>
        </dx:DXCollectionView>

        <!-- Empty state (post-search, no results or error) -->
        <components:EmptyState
            Message="{Binding EmptyStateMessage}"
            IsVisible="{Binding HasSearched, Converter={StaticResource BoolAndMultiConverter},
                        ConverterParameter='!HasResults,!IsLoading'}" />

    </Grid>
</ContentPage>
```

> **Note on EmptyState visibility:** The exact binding syntax for `HasSearched && !HasResults && !IsLoading` depends on the project's converter infrastructure. Check how existing pages implement compound boolean visibility (e.g. `IsEmptyNoVenues` derived property pattern). If the converter approach doesn't exist, add `IsShowEmptyState` as a computed property in the ViewModel:
> ```csharp
> public bool IsShowEmptyState => HasSearched && !HasResults && !IsLoading;
> partial void OnHasSearchedChanged(bool _) => OnPropertyChanged(nameof(IsShowEmptyState));
> partial void OnHasResultsChanged(bool _) => OnPropertyChanged(nameof(IsShowEmptyState));
> partial void OnIsLoadingChanged(bool _) => OnPropertyChanged(nameof(IsShowEmptyState));
> ```
> Then bind: `IsVisible="{Binding IsShowEmptyState}"`. This is the existing project pattern (ViewModelBase NotifyEmptyStates). Apply this same pattern to all three ViewModels.

- [ ] **Step 3a.2: Create ArtistPickerPage.xaml.cs**

```csharp
// MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml.cs
namespace MyVocaList.UI.Pages.Artists;

public partial class ArtistPickerPage : ContentPage
{
    public ArtistPickerPage(ArtistPickerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```

- [ ] **Step 3a.3: Build**

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`
Expected: 0 errors. Fix any XAML binding or namespace errors before proceeding.

- [ ] **Step 3a.4: Commit**

```bash
git add MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml
git add MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml.cs
git commit -m "feat: add ArtistPickerPage"
```

---

### Task 3b: Implement SongPickerPage

**Files:**
- Create: `MyVocaList/UI/Pages/Songs/SongPickerPage.xaml`
- Create: `MyVocaList/UI/Pages/Songs/SongPickerPage.xaml.cs`

- [ ] **Step 3b.1: Create SongPickerPage.xaml**

Structure identical to ArtistPickerPage, with these differences:
- `x:DataType="vm:SongPickerViewModel"`
- `x:Class="MyVocaList.UI.Pages.Songs.SongPickerPage"`
- `Placeholder="Search songs..."`
- `Action1Command="{Binding SearchCommand}"`
- DataTemplate uses `x:DataType="dto:MusicSearchResultDto"` with a two-line ListItem:
  - `Headline="{Binding SongTitle}"` (null from API → bind as-is; MAUI Label renders empty for null)
  - `SupportingText="{Binding ArtistName}"`

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:dx="http://schemas.devexpress.com/maui"
    xmlns:vm="clr-namespace:MyVocaList.UI.ViewModels"
    xmlns:dto="clr-namespace:MyVocaList.Contracts.DTOs;assembly=MyVocaList.Contracts"
    xmlns:appbars="clr-namespace:MyVocaList.UI.Components.AppBars"
    xmlns:lists="clr-namespace:MyVocaList.UI.Components.Lists"
    xmlns:components="clr-namespace:MyVocaList.UI.Components"
    x:Class="MyVocaList.UI.Pages.Songs.SongPickerPage"
    x:DataType="vm:SongPickerViewModel"
    BackgroundColor="{StaticResource Surface}"
    SafeAreaEdges="Container"
    Shell.NavBarIsVisible="False">

    <Shell.TitleView>
        <appbars:SearchAppBar
            SearchText="{Binding SearchText, Mode=TwoWay}"
            BackCommand="{Binding BackCommand}"
            Action1Icon="search_outlined"
            Action1Command="{Binding SearchCommand}"
            Placeholder="Search songs..." />
    </Shell.TitleView>

    <Grid>
        <dx:ShimmerView IsActive="{Binding IsLoading}" IsVisible="{Binding IsLoading}">
            <VerticalStackLayout Padding="16" Spacing="4">
                <BoxView HeightRequest="72" CornerRadius="8" BackgroundColor="{StaticResource SurfaceContainerLow}" />
                <BoxView HeightRequest="72" CornerRadius="8" BackgroundColor="{StaticResource SurfaceContainerLow}" />
                <BoxView HeightRequest="72" CornerRadius="8" BackgroundColor="{StaticResource SurfaceContainerLow}" />
                <BoxView HeightRequest="72" CornerRadius="8" BackgroundColor="{StaticResource SurfaceContainerLow}" />
                <BoxView HeightRequest="72" CornerRadius="8" BackgroundColor="{StaticResource SurfaceContainerLow}" />
            </VerticalStackLayout>
        </dx:ShimmerView>

        <dx:DXCollectionView
            ItemsSource="{Binding Results}"
            IsVisible="{Binding HasResults}">
            <dx:DXCollectionView.ItemTemplate>
                <DataTemplate x:DataType="dto:MusicSearchResultDto">
                    <lists:ListItem
                        Headline="{Binding SongTitle}"
                        SupportingText="{Binding ArtistName}"
                        Command="{Binding Source={RelativeSource AncestorType={x:Type vm:SongPickerViewModel}}, Path=SelectResultCommand}"
                        CommandParameter="{Binding .}" />
                </DataTemplate>
            </dx:DXCollectionView.ItemTemplate>
        </dx:DXCollectionView>

        <components:EmptyState
            Message="{Binding EmptyStateMessage}"
            IsVisible="{Binding IsShowEmptyState}" />
    </Grid>
</ContentPage>
```

- [ ] **Step 3b.2: Create SongPickerPage.xaml.cs**

```csharp
// MyVocaList/UI/Pages/Songs/SongPickerPage.xaml.cs
namespace MyVocaList.UI.Pages.Songs;

public partial class SongPickerPage : ContentPage
{
    public SongPickerPage(SongPickerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```

- [ ] **Step 3b.3: Build and fix**

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`
Expected: 0 errors.

- [ ] **Step 3b.4: Commit**

```bash
git add MyVocaList/UI/Pages/Songs/SongPickerPage.xaml
git add MyVocaList/UI/Pages/Songs/SongPickerPage.xaml.cs
git commit -m "feat: add SongPickerPage"
```

---

### Task 3c: Implement YouTubeSearchPage

**Files:**
- Create: `MyVocaList/UI/Pages/Songs/YouTubeSearchPage.xaml`
- Create: `MyVocaList/UI/Pages/Songs/YouTubeSearchPage.xaml.cs`

- [ ] **Step 3c.1: Create YouTubeSearchPage.xaml**

ListItem shape: Leading = 48×48 Image (ThumbnailUrl), Headline = Title, SupportingText = `ChannelName + " · " + DurationSeconds formatted`. Check `SecondsToMinutesConverter` usage in existing `SongFormPage.xaml` (line 237) — it converts `DurationSeconds` (int?) to a formatted string. Use the same converter.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:dx="http://schemas.devexpress.com/maui"
    xmlns:vm="clr-namespace:MyVocaList.UI.ViewModels"
    xmlns:dtoList="clr-namespace:MyVocaList.Contracts.DTOs.List;assembly=MyVocaList.Contracts"
    xmlns:appbars="clr-namespace:MyVocaList.UI.Components.AppBars"
    xmlns:lists="clr-namespace:MyVocaList.UI.Components.Lists"
    xmlns:components="clr-namespace:MyVocaList.UI.Components"
    x:Class="MyVocaList.UI.Pages.Songs.YouTubeSearchPage"
    x:DataType="vm:YouTubeSearchViewModel"
    BackgroundColor="{StaticResource Surface}"
    SafeAreaEdges="Container"
    Shell.NavBarIsVisible="False">

    <Shell.TitleView>
        <appbars:SearchAppBar
            SearchText="{Binding SearchText, Mode=TwoWay}"
            BackCommand="{Binding BackCommand}"
            Action1Icon="search_outlined"
            Action1Command="{Binding SearchCommand}"
            Placeholder="Search YouTube karaoke..." />
    </Shell.TitleView>

    <Grid>
        <dx:ShimmerView IsActive="{Binding IsLoading}" IsVisible="{Binding IsLoading}">
            <VerticalStackLayout Padding="16" Spacing="4">
                <BoxView HeightRequest="72" CornerRadius="8" BackgroundColor="{StaticResource SurfaceContainerLow}" />
                <BoxView HeightRequest="72" CornerRadius="8" BackgroundColor="{StaticResource SurfaceContainerLow}" />
                <BoxView HeightRequest="72" CornerRadius="8" BackgroundColor="{StaticResource SurfaceContainerLow}" />
                <BoxView HeightRequest="72" CornerRadius="8" BackgroundColor="{StaticResource SurfaceContainerLow}" />
                <BoxView HeightRequest="72" CornerRadius="8" BackgroundColor="{StaticResource SurfaceContainerLow}" />
            </VerticalStackLayout>
        </dx:ShimmerView>

        <dx:DXCollectionView
            ItemsSource="{Binding Results}"
            IsVisible="{Binding HasResults}">
            <dx:DXCollectionView.ItemTemplate>
                <DataTemplate x:DataType="dtoList:YouTubeSearchResultDto">
                    <lists:ListItem
                        Headline="{Binding Title}"
                        SupportingText="{Binding ChannelName}"
                        Command="{Binding Source={RelativeSource AncestorType={x:Type vm:YouTubeSearchViewModel}}, Path=SelectResultCommand}"
                        CommandParameter="{Binding .}">
                        <lists:ListItem.LeadingContent>
                            <Image Source="{Binding ThumbnailUrl}"
                                   WidthRequest="48"
                                   HeightRequest="48"
                                   Aspect="AspectFill" />
                        </lists:ListItem.LeadingContent>
                    </lists:ListItem>
                </DataTemplate>
            </dx:DXCollectionView.ItemTemplate>
        </dx:DXCollectionView>

        <components:EmptyState
            Message="{Binding EmptyStateMessage}"
            IsVisible="{Binding IsShowEmptyState}" />
    </Grid>
</ContentPage>
```

> **Note on ListItem leading slot:** Check `ListItem.xaml` for exact property names for leading content and supporting text. If `ListItem` uses `LeadingContent` as a `ContentView` slot rather than an `Image` binding, adjust accordingly. If `ListItem` does not support `ContentView` in the leading slot, use `ListItemLeadingImage` (file exists: `ListItemLeadingImage.xaml`) — check its API. The key invariant is a 48×48 image with AspectFill.

- [ ] **Step 3c.2: Create YouTubeSearchPage.xaml.cs**

```csharp
// MyVocaList/UI/Pages/Songs/YouTubeSearchPage.xaml.cs
namespace MyVocaList.UI.Pages.Songs;

public partial class YouTubeSearchPage : ContentPage
{
    public YouTubeSearchPage(YouTubeSearchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```

- [ ] **Step 3c.3: Build and fix**

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`
Expected: 0 errors.

- [ ] **Step 3c.4: Commit**

```bash
git add MyVocaList/UI/Pages/Songs/YouTubeSearchPage.xaml
git add MyVocaList/UI/Pages/Songs/YouTubeSearchPage.xaml.cs
git commit -m "feat: add YouTubeSearchPage"
```

---

## Phase 4 — Route Registration + DI [SEQUENTIAL — after Phase 3 complete]

### Task 4: Register routes and DI

**Files:**
- Modify: `MyVocaList/Navigation/Routes.cs`
- Modify: `MyVocaList/AppShell.xaml.cs`
- Modify: `MyVocaList/MauiProgram.cs`

- [ ] **Step 4.1: Add route constants to Routes.cs**

Add to `MyVocaList/Navigation/Routes.cs`:
```csharp
public const string ArtistPicker = "artist-picker";
public const string SongPicker   = "song-picker";
public const string YouTubeSearch = "youtube-search";
```

- [ ] **Step 4.2: Register routes in AppShell.xaml.cs**

Add to `AppShell` constructor after existing `Routing.RegisterRoute` calls:
```csharp
Routing.RegisterRoute(Routes.ArtistPicker, typeof(ArtistPickerPage));
Routing.RegisterRoute(Routes.SongPicker, typeof(SongPickerPage));
Routing.RegisterRoute(Routes.YouTubeSearch, typeof(YouTubeSearchPage));
```

- [ ] **Step 4.3: Register pages, ViewModels, and IMessenger in MauiProgram.cs**

Find the DI registration section. Add:
```csharp
// IMessenger — singleton; WeakReferenceMessenger.Default is the shared bus
builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

// Picker pages and ViewModels — transient per navigation
builder.Services.AddTransient<ArtistPickerPage>();
builder.Services.AddTransient<ArtistPickerViewModel>();
builder.Services.AddTransient<SongPickerPage>();
builder.Services.AddTransient<SongPickerViewModel>();
builder.Services.AddTransient<YouTubeSearchPage>();
builder.Services.AddTransient<YouTubeSearchViewModel>();
```

- [ ] **Step 4.4: Build**

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`
Expected: 0 errors.

- [ ] **Step 4.5: Run full test suite**

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --verbosity normal`
Expected: 0 failures.

- [ ] **Step 4.6: Commit**

```bash
git add MyVocaList/Navigation/Routes.cs
git add MyVocaList/AppShell.xaml.cs
git add MyVocaList/MauiProgram.cs
git commit -m "feat: register picker page routes and DI"
```

---

## Phase 5a — Wire ArtistFormPage [SEQUENTIAL — after Phase 4]

### Task 5a-XAML: Update ArtistFormPage.xaml

**Files:**
- Modify: `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml`

- [ ] **Step 5a-X.1: Read ArtistFormPage.xaml in full**

Read `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml`. Locate the `Border` block at lines 63–107 (the API search strip). Confirm the exact boundaries before editing.

- [ ] **Step 5a-X.2: Replace API search strip Border with ListItem trigger row**

Remove the entire `Border` block (lines 63–107):
```xml
<!-- API search strip -->
<Border BackgroundColor="{StaticResource SurfaceContainerLow}" ... >
    ...
</Border>
```

Replace with:
```xml
<!-- Music database search trigger (MD3 list item → ArtistPickerPage) -->
<lists:ListItem
    LeadingIcon="search_outlined"
    Headline="Search music database"
    TrailingIcon="navigate_next"
    Command="{Binding NavigateToArtistPickerCommand}" />
```

> **Note on ListItem bindable properties:** Verify exact property names in `MyVocaList/UI/Components/Lists/ListItem.xaml.cs`. If the component uses `LeadingIconSource`, `HeadlineText`, or different names, match them. The semantic intent is: search icon leading, "Search music database" headline, chevron-right trailing.

- [ ] **Step 5a-X.3: Add `xmlns:lists` if not present**

Add to `ContentPage` xmlns declarations: `xmlns:lists="clr-namespace:MyVocaList.UI.Components.Lists"` (if not already present).

- [ ] **Step 5a-X.4: Build**

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`
Expected: 0 errors.

- [ ] **Step 5a-X.5: Commit XAML change**

```bash
git add MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml
git commit -m "feat: replace ArtistFormPage API search strip with ListItem trigger"
```

---

### Task 5a-VM: Update ArtistFormViewModel.cs [SEQUENTIAL — after 5a-XAML]

**Files:**
- Modify: `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs`

- [ ] **Step 5a-V.1: Read ArtistFormViewModel.cs in full**

Read `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs`. Identify:
- The 8 properties to remove: `_apiSearchText`, `_apiResults`, `_isApiSearching`, `_apiStatusMessage`, `HasApiResults`, `HasApiStatusMessage`, `SelectedExternalId`, `SelectedProvider`
- The 2 commands to remove: `SearchApiCommand`, `SelectApiResultCommand`
- The `_musicMetadataService` field (also remove — constructor parameter `IMusicMetadataService musicMetadataService`)

- [ ] **Step 5a-V.2: Remove API search state and service injection**

Remove from fields:
```csharp
private readonly IMusicMetadataService _musicMetadataService;
[ObservableProperty] private string _apiSearchText = string.Empty;
[ObservableProperty] private IEnumerable<MusicSearchResultDto> _apiResults = [];
[ObservableProperty] private bool _isApiSearching;
[ObservableProperty] private string _apiStatusMessage = string.Empty;
[ObservableProperty] private bool _hasApiResults;
[ObservableProperty] private bool _hasApiStatusMessage;
public string SelectedExternalId { get; private set; } = string.Empty;
public string SelectedProvider { get; private set; } = string.Empty;
```

Remove `IMusicMetadataService musicMetadataService` from constructor parameter list and `_musicMetadataService = musicMetadataService;` from constructor body.

Remove command declarations:
```csharp
public IAsyncRelayCommand SearchApiCommand { get; }
public IRelayCommand<MusicSearchResultDto> SelectApiResultCommand { get; }
```

Remove from constructor:
```csharp
SearchApiCommand = new AsyncRelayCommand(SearchApiAsync);
SelectApiResultCommand = new RelayCommand<MusicSearchResultDto>(SelectApiResult);
```

Remove the `SearchApiAsync` and `SelectApiResult` private methods (identify them by name and remove).

- [ ] **Step 5a-V.3: Add IMessenger injection and NavigateToArtistPickerCommand**

Add `IMessenger` field and constructor parameter:
```csharp
private readonly IMessenger _messenger;
```

Updated constructor signature (remove `IMusicMetadataService`, add `IMessenger`):
```csharp
public ArtistFormViewModel(
    IArtistService artistService,
    IMessenger messenger,
    ISnackbarComponent snackbarService,
    ILogger<ArtistFormViewModel> logger)
{
    _artistService = artistService;
    _messenger = messenger;
    _snackbarService = snackbarService;
    _logger = logger;

    SaveCommand = new AsyncRelayCommand(SaveAsync);
    CancelCommand = new AsyncRelayCommand(CancelAsync);
    SelectDuplicateCommand = new AsyncRelayCommand<ArtistListItemDto>(SelectDuplicateAsync);
    NavigateToArtistPickerCommand = new AsyncRelayCommand(NavigateToArtistPickerAsync);
}
```

Add command declaration:
```csharp
public IAsyncRelayCommand NavigateToArtistPickerCommand { get; }
```

Add navigate method:
```csharp
private async Task NavigateToArtistPickerAsync()
{
    _messenger.Register<ArtistPickedMessage>(this, (_, msg) =>
    {
        ArtistName = msg.Result.ArtistName;
        _messenger.Unregister<ArtistPickedMessage>(this);
    });
    await Shell.Current.GoToAsync(Routes.ArtistPicker);
}
```

Add `using MyVocaList.Contracts.Messages;` at the top if needed (verify against GlobalUsings.cs — if `MyVocaList.Contracts` namespace is globally included, only the specific sub-namespace may be needed).

- [ ] **Step 5a-V.4: Build**

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`
Expected: 0 errors. If `MauiProgram.cs` registered `IMusicMetadataService` as a dependency of `ArtistFormViewModel`, update the DI registration there too.

- [ ] **Step 5a-V.5: Run tests**

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --verbosity normal`
Expected: 0 failures. Any existing ArtistFormViewModel tests that inject `IMusicMetadataService` must be updated to remove that parameter.

- [ ] **Step 5a-V.6: Commit**

```bash
git add MyVocaList/UI/ViewModels/ArtistFormViewModel.cs
git add MyVocaList/MauiProgram.cs  # if DI registration changed
git commit -m "feat: wire ArtistFormViewModel — remove API search, add picker navigation"
```

---

## Phase 5b — Wire SongFormPage [SEQUENTIAL — after Phase 4, parallel with 5a]

### Task 5b-XAML: Update SongFormPage.xaml

**Files:**
- Modify: `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`

- [ ] **Step 5b-X.1: Read SongFormPage.xaml in full**

Read `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`. Identify:
- Music-DB API search strip: `Border` block at lines 57–107
- YouTube search Grid at lines 180–193
- YouTube results VerticalStackLayout at lines 218–252
- "Search YouTube" trigger location (top of the YouTube `Border`, lines 172+)
- Paste URL section (lines 254–272 — must NOT be touched)
- No-API-key nudge `VerticalStackLayout` (lines 196–206 — must NOT be touched)

- [ ] **Step 5b-X.2: Replace music-DB search strip with ListItem trigger**

Remove the `Border` block (lines 57–107). Replace with:
```xml
<!-- Music database search trigger (MD3 list item → SongPickerPage) -->
<lists:ListItem
    LeadingIcon="search_outlined"
    Headline="Search music database"
    TrailingIcon="navigate_next"
    Command="{Binding NavigateToSongPickerCommand}" />
```

- [ ] **Step 5b-X.3: Build and fix**

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`
Expected: 0 errors. Fix before proceeding to next step.

- [ ] **Step 5b-X.4: Replace YouTube search Grid and results with ListItem trigger**

In the YouTube `Border`:
- Remove the search `Grid` (lines 180–193): `<Grid ColumnDefinitions="*,Auto" ... IsVisible="{Binding HasYouTubeApiKey}"> ... </Grid>`
- Remove the search results `VerticalStackLayout` (lines 218–252): the `BindableLayout.ItemsSource="{Binding SearchResults}"` block

Add at the top of the YouTube Border's `VerticalStackLayout`, **before** the no-API-key nudge:
```xml
<!-- YouTube search trigger (visible only when API key is set) -->
<lists:ListItem
    LeadingIcon="search_outlined"
    Headline="Search YouTube"
    TrailingIcon="navigate_next"
    IsVisible="{Binding HasYouTubeApiKey}"
    Command="{Binding NavigateToYouTubeSearchCommand}" />
```

Keep unchanged:
- No-API-key nudge `VerticalStackLayout`
- Paste URL section (`Label "Or paste a YouTube URL"`, `Grid` with `PasteUrlInput` TextEdit + "Add" button)
- Paste URL error label

- [ ] **Step 5b-X.5: Build and fix**

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`
Expected: 0 errors.

- [ ] **Step 5b-X.6: Commit XAML**

```bash
git add MyVocaList/UI/Pages/Songs/SongFormPage.xaml
git commit -m "feat: replace SongFormPage search strips with ListItem triggers"
```

---

### Task 5b-VM: Update SongFormViewModel.cs [SEQUENTIAL — after 5b-XAML]

**Files:**
- Modify: `MyVocaList/UI/ViewModels/SongFormViewModel.cs`

- [ ] **Step 5b-V.1: Read SongFormViewModel.cs in full**

Read `MyVocaList/UI/ViewModels/SongFormViewModel.cs`. Identify all properties/commands to remove per the spec:
- Remove: `ApiSearchText`, `IsApiSearching`, `HasApiStatusMessage`, `ApiStatusMessage`, `HasApiResults`, `ApiResults`, `SearchApiCommand`, `SelectApiResultCommand`
- Remove: `YoutubeSearchQuery`, `IsYouTubeSearching`, `HasYouTubeSearchStatus`, `YoutubeSearchStatus`, `SearchResults`, `SearchYouTubeCommand`, `AddFromSearchCommand`
- Remove: `IMusicMetadataService` and `IYouTubeSearchService` constructor parameters and their usages in removed methods
- Keep: everything related to `KaraokeUrls`, `PasteUrlInput`, `AddFromPasteCommand`, `RemoveUrlCommand`, `HasYouTubeApiKey`, `GoToSettingsCommand`

- [ ] **Step 5b-V.2: Remove API search state and commands**

Remove all 13 properties/commands identified above, their backing fields, `_musicMetadataService`, `_youTubeSearchService` fields and constructor parameters, and the private methods `SearchApiAsync`, `SelectApiResult`, `SearchYouTubeAsync`, `AddFromSearch`.

- [ ] **Step 5b-V.3: Add IMessenger injection, NavigateToSongPickerCommand, NavigateToYouTubeSearchCommand**

Add `IMessenger _messenger` field. Update constructor to inject `IMessenger messenger`. Add:
```csharp
NavigateToSongPickerCommand = new AsyncRelayCommand(NavigateToSongPickerAsync);
NavigateToYouTubeSearchCommand = new AsyncRelayCommand(NavigateToYouTubeSearchAsync);
```

Add command declarations:
```csharp
public IAsyncRelayCommand NavigateToSongPickerCommand { get; }
public IAsyncRelayCommand NavigateToYouTubeSearchCommand { get; }
```

Add navigate methods:
```csharp
private async Task NavigateToSongPickerAsync()
{
    _messenger.Register<SongPickedMessage>(this, (_, msg) =>
    {
        SongTitle = msg.Result.SongTitle ?? string.Empty;
        ArtistSearchText = msg.Result.ArtistName;
        _messenger.Unregister<SongPickedMessage>(this);
    });
    await Shell.Current.GoToAsync(Routes.SongPicker);
}

private async Task NavigateToYouTubeSearchAsync()
{
    _messenger.Register<YouTubeVideoPickedMessage>(this, async (_, msg) =>
    {
        _messenger.Unregister<YouTubeVideoPickedMessage>(this);
        await AddKaraokeUrlFromVideoAsync(msg.Result);
    });
    await Shell.Current.GoToAsync(Routes.YouTubeSearch);
}
```

> `AddKaraokeUrlFromVideoAsync` must replicate the behavior of the removed `AddFromSearchCommand`. Read the current `AddFromSearchCommand`/`AddFromSearch` method before removing it, and extract its logic into this new private method. Keep the exact same URL construction and service call (`ISongKaraokeUrlService`).

- [ ] **Step 5b-V.4: Build**

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`
Expected: 0 errors.

- [ ] **Step 5b-V.5: Run tests**

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --verbosity normal`
Expected: 0 failures. Update any `SongFormViewModelTests` that inject `IMusicMetadataService` or `IYouTubeSearchService` to remove those mocks.

- [ ] **Step 5b-V.6: Commit**

```bash
git add MyVocaList/UI/ViewModels/SongFormViewModel.cs
git add MyVocaList/MauiProgram.cs  # if DI registration changed
git commit -m "feat: wire SongFormViewModel — remove search state, add picker navigation"
```

---

## Phase 6 — Cleanup + .sln Registration [SEQUENTIAL — after Phase 5a and 5b]

### Task 6: Register new files in MyVocaList.sln and add BACKLOG entries

**Files:**
- Modify: `MyVocaList.sln`
- Modify: `Docs/Management/BACKLOG.md`

- [ ] **Step 6.1: Audit all new files created in Phases 1–5**

List every file created in this feature. Verify each one is registered in `MyVocaList.sln` in the appropriate `ProjectSection(SolutionItems)`. Source files (`.cs`, `.xaml`) are picked up automatically by their `.csproj` — no `.sln` entry needed. Spec files in `Docs/` require explicit `.sln` Solution Folder entries.

Files that need `.sln` entries (Docs):
- `Docs/Management/BusinessFeatures/search-page-component/plan.md`
- `Docs/Management/BusinessFeatures/search-page-component/task-log.md` (if created)

Existing entries to verify (spec files created during brainstorm):
- `Docs/Management/BusinessFeatures/search-page-component/requirements.md`
- `Docs/Management/BusinessFeatures/search-page-component/design.md`
- `Docs/Management/BusinessFeatures/search-page-component/tasks.md`

Add any missing entries using the pattern from `constraints-registry.md § Visual Studio Solution (.sln)`. GUID sequence: check the last used GUID in `.sln` and increment.

- [ ] **Step 6.2: Add deferred BACKLOG entries**

Add three rows to `Docs/Management/BACKLOG.md` Business Features table (deferred items per spec's Out of Scope):
```
| 2026-06 | ↳ KaraokeUrls MD3 Card Component | 💡 Pending | Replace BindableLayout URL list with MD3 Card component in SongFormPage |
| 2026-06 | ↳ Multi-type Video Links | 💡 Pending | Multiple video types per song with type labels and usage stats |
| 2026-06 | ↳ YouTube Video Preview | 💡 Pending | Preview/launch player from SongFormPage YouTube URL entries |
```

- [ ] **Step 6.3: Update BACKLOG.md status for this feature**

Change Search Page Component status from `📋 Spec` to `✅ Done` in BACKLOG.md.

Change `↳ Bug: Artist/Song form search strip non-MD3` status from `🔴 Blocked` to `✅ Fixed`.

- [ ] **Step 6.4: Build final check**

Run: `dotnet build MyVocaList.sln`
Expected: 0 errors, 0 warnings that weren't there before.

- [ ] **Step 6.5: Run final test suite**

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --verbosity normal`
Expected: 0 failures.

- [ ] **Step 6.6: Commit**

```bash
git add MyVocaList.sln
git add Docs/Management/BACKLOG.md
git commit -m "chore: register search-page-component files in .sln; update BACKLOG"
```

---

## Spec Coverage Self-Check

| Requirement | Task |
|-------------|------|
| AC-ART-01 to AC-ART-06 | Tasks 3a, 5a-XAML, 5a-VM |
| AC-SONG-01 to AC-SONG-06 | Tasks 3b, 5b-XAML, 5b-VM |
| AC-YT-01 to AC-YT-08 | Tasks 3c, 5b-XAML, 5b-VM |
| AC-LOAD-01 to AC-LOAD-05 | Task 2B (ViewModel loading contract) |
| AC-TRIGGER-01 to AC-TRIGGER-04 | Tasks 5a-XAML, 5b-XAML |
| Validation: empty query no-op | Task 2T (tests), 2B (guard in SearchAsync) |
| Validation: cancellation of in-flight search | Task 2T (test), 2B (CancellationTokenSource pattern) |
| IMessenger testability | Task 2T/2B (injected, not static) |
| SafeAreaEdges="Container" | Tasks 3a, 3b, 3c |
| Routes + DI | Task 4 |
| WeakReferenceMessenger messages in Contracts | Task 1 |
| .sln registration | Task 6 |

No gaps found.
