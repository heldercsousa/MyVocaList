# Search Page Component — Design

**Feature:** Search Page Component  
**Status:** Spec  
**Date:** 2026-06-03  

---

## Layers Affected

- `MyVocaList` (MAUI) — 3 new pages, 3 new ViewModels, 1 `SearchAppBar` extension, 2 form page edits, AppShell route registration, DI registration
- `MyVocaList.Contracts` — 3 typed WeakReferenceMessenger message records
- `MyVocaList.Services` — no new services; `IMusicMetadataService` and `IYouTubeService` consumed as-is

---

## MD3 Basis

MD3 defines three search entry points: Search Bar, Search App Bar, and Search Icon Button. The correct pattern for form-launched search is **Search Icon Button → standalone search destination page**. The existing `SearchAppBar` component already implements the search destination header. This design uses it directly.

---

## Architecture Overview

```
ArtistFormPage          SongFormPage
 [ListItem trigger]      [ListItem trigger — music DB]
                         [ListItem trigger — YouTube]
     │                        │
     ▼                        ▼
ArtistPickerPage    SongPickerPage    YouTubeSearchPage
     │                   │                    │
     │  WeakReferenceMessenger typed message  │
     └───────────────────┴────────────────────┘
                          │
              calling ViewModel receives message,
              updates fields; page has already popped
```

---

## SearchAppBar Extension (prerequisite)

The current `SearchAppBar` has no bindable `SearchCommand` property. When the user presses the keyboard Search/Return key on the TextEdit, `ReturnType="Search"` fires the `Completed` event — but there is no way to bind an external command to it from XAML.

**Required change:** Add a `SearchCommand` bindable property to `SearchAppBar` that is invoked from `searchEdit`'s `Completed` event (keyboard submit).

```csharp
// SearchAppBar.xaml.cs — addition
public static readonly BindableProperty SearchCommandProperty =
    BindableProperty.Create(nameof(SearchCommand), typeof(ICommand), typeof(SearchAppBar));

public ICommand SearchCommand
{
    get => (ICommand)GetValue(SearchCommandProperty);
    set => SetValue(SearchCommandProperty, value);
}

// In code-behind: subscribe in constructor or OnApplyTemplate
searchEdit.Completed += (s, e) => SearchCommand?.Execute(SearchText);
```

The picker pages will bind `SearchCommand="{Binding SearchCommand}"` on the `SearchAppBar`. Users can also tap the `Action1` trailing button (magnifying glass icon) bound to the same command as an explicit tap-to-search affordance.

---

## New Pages

All three pages follow the same structure:
- `SafeAreaEdges="Container"` (mandatory — MAUI 10 breaking change)
- `Shell.TitleView`: `SearchAppBar` with `BackCommand`, `SearchCommand`, and `Action1` search button
- Body: `dx:ShimmerView` wrapping skeleton + `DXCollectionView` for results
- `EmptyState` for post-search no-results and error states

### ArtistPickerPage

**File:** `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml` + `.xaml.cs`  
**ViewModel:** `MyVocaList/UI/ViewModels/ArtistPickerViewModel.cs`  
**Route:** `artist-picker`

**ListItem shape:** Headline = `ArtistName` (single line). No leading, no supporting, no trailing.

**ViewModel interface:**
```csharp
public partial class ArtistPickerViewModel : ObservableObject
{
    [ObservableProperty] string _searchText = string.Empty;
    [ObservableProperty] bool _isLoading;
    [ObservableProperty] bool _hasResults;
    [ObservableProperty] bool _hasSearched;   // false until first search completes

    ObservableRangeCollection<MusicSearchResultDto> Results { get; } = [];

    IAsyncRelayCommand SearchCommand { get; }
    IRelayCommand<MusicSearchResultDto> SelectResultCommand { get; }
}
```

`HasResults` is set by the ViewModel: `HasResults = Results.Count > 0` after each search completes.  
`EmptyState` visibility: `HasSearched && !HasResults && !IsLoading`.

**Service call:**
```csharp
var results = await _musicMetadataService.SearchArtistsAsync(SearchText, ct);
```

---

### SongPickerPage

**File:** `MyVocaList/UI/Pages/Songs/SongPickerPage.xaml` + `.xaml.cs`  
**ViewModel:** `MyVocaList/UI/ViewModels/SongPickerViewModel.cs`  
**Route:** `song-picker`

**ListItem shape:** Headline = `SongTitle` (null displayed as empty string) · Supporting = `ArtistName`.

**ViewModel interface:** identical structure to `ArtistPickerViewModel` with `MusicSearchResultDto` results.

**Service call:**
```csharp
var results = await _musicMetadataService.SearchSongsAsync(SearchText, artistHint: null, ct);
```

---

### YouTubeSearchPage

**File:** `MyVocaList/UI/Pages/Songs/YouTubeSearchPage.xaml` + `.xaml.cs`  
**ViewModel:** `MyVocaList/UI/ViewModels/YouTubeSearchViewModel.cs`  
**Route:** `youtube-search`

**ListItem shape:** Leading slot = `Image` (48×48, `AspectFill`, `ThumbnailUrl`) · Headline = `Title` · Supporting = `ChannelName + " · " + DurationSeconds formatted via SecondsToMinutesConverter`.

**ViewModel interface:** identical structure with `YouTubeSearchResultDto` results.

---

## Result Passing — WeakReferenceMessenger

**Three message records** in `MyVocaList.Contracts/Messages/`:

```csharp
public record ArtistPickedMessage(MusicSearchResultDto Result);
public record SongPickedMessage(MusicSearchResultDto Result);
public record YouTubeVideoPickedMessage(YouTubeSearchResultDto Result);
```

**Picker side — send and pop:**
```csharp
WeakReferenceMessenger.Default.Send(new ArtistPickedMessage(result));
await Shell.Current.GoToAsync("..");
```

**Caller side — register before navigate, unregister after receive:**
```csharp
// Register in NavigateToArtistPickerCommand
WeakReferenceMessenger.Default.Register<ArtistPickedMessage>(this, (_, msg) =>
{
    ArtistName = msg.Result.ArtistName;
    WeakReferenceMessenger.Default.Unregister<ArtistPickedMessage>(this);
});
await Shell.Current.GoToAsync("artist-picker");
```

**Cancel path:** if the user navigates back without selecting, no message is sent and the handler remains registered. On the next open of the picker, the same handler fires correctly — this is intentional. No cleanup is needed on cancel because the handler is idempotent and self-removes after first use.

**Test isolation:** ViewModel unit tests must not use `WeakReferenceMessenger.Default` directly. Inject `IMessenger` via constructor and register a `TestMessenger` instance in tests. The production registration in `MauiProgram.cs` provides `WeakReferenceMessenger.Default` as the `IMessenger` singleton.

```csharp
// ViewModel constructor (testable)
public ArtistPickerViewModel(IMusicMetadataService service, IMessenger messenger, ILogger<ArtistPickerViewModel> logger)

// MauiProgram.cs
builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
```

---

## Loading State — Design Contract (UX-critical)

This contract applies to all three picker ViewModels without exception:

```csharp
private CancellationTokenSource _cts = new();

private async Task SearchAsync()
{
    if (string.IsNullOrWhiteSpace(SearchText)) return;

    // Cancel prior request
    _cts.Cancel();
    _cts.Dispose();
    _cts = new CancellationTokenSource();
    var ct = _cts.Token;

    // Synchronous — before any await
    IsLoading = true;
    HasSearched = false;
    Results.Clear();

    try
    {
        var items = await _service.SearchAsync(SearchText, ct);
        Results.ReplaceRange(items);
        HasResults = Results.Count > 0;
        HasSearched = true;
    }
    catch (OperationCanceledException)
    {
        // Silently ignored — superseded by a newer search
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Search failed for query {Query}", SearchText);
        HasResults = false;
        HasSearched = true;   // triggers EmptyState with error message
    }
    finally
    {
        IsLoading = false;
    }
}
```

`EmptyState` must distinguish between "no results found" and "search failed" — pass a message string from the ViewModel (`EmptyStateMessage` property).

---

## Form Page Trigger — ListItem Usage

The existing `ListItem` component (`UI/Components/Lists/ListItem.xaml`) is used directly as the trigger row:
- Leading slot: search icon (Material icon, `search_outlined`)
- Headline: "Search music database" (artist/song) or "Search YouTube" (YouTube)
- Trailing slot: chevron-right icon (`navigate_next`)

Wrapped in a `TapGestureRecognizer` bound to the navigate command, or via a `Command` bindable property if `ListItem` exposes one. If not, use code-behind `Tapped` event.

### ArtistFormPage changes

- **Remove:** `Border` at lines 63–107 (API search strip)
- **Add:** `ListItem` trigger row at the same vertical position, bound to `NavigateToArtistPickerCommand`
- **Add to `ArtistFormViewModel`:** `NavigateToArtistPickerCommand` (registers messenger handler, navigates)
- **Remove from `ArtistFormViewModel`:** `ApiSearchText`, `IsApiSearching`, `HasApiStatusMessage`, `ApiStatusMessage`, `HasApiResults`, `ApiResults`, `SearchApiCommand`, `SelectApiResultCommand`

### SongFormPage changes

- **Remove:** `Border` at lines 57–107 (music-DB API search strip)
- **Add:** `ListItem` trigger row at the same vertical position, bound to `NavigateToSongPickerCommand`
- Inside the YouTube `Border` (lines 172–275):
  - **Remove:** search strip `Grid` (lines 180–193) and search results `VerticalStackLayout` (lines 218–252)
  - **Add:** `ListItem` "Search YouTube" trigger row at top of Border, visible only when `HasYouTubeApiKey`
  - **Keep unchanged:** no-API-key nudge, `ActivityIndicator` (if any), Paste URL section
- **Add to `SongFormViewModel`:** `NavigateToSongPickerCommand`, `NavigateToYouTubeSearchCommand`
- **Remove from `SongFormViewModel`:** `ApiSearchText`, `IsApiSearching`, `HasApiStatusMessage`, `ApiStatusMessage`, `HasApiResults`, `ApiResults`, `SearchApiCommand`, `SelectApiResultCommand`, `YoutubeSearchQuery`, `IsYouTubeSearching`, `HasYouTubeSearchStatus`, `YoutubeSearchStatus`, `SearchResults`, `SearchYouTubeCommand`, `AddFromSearchCommand`

---

## Route Registration

```csharp
// AppShell.xaml.cs
Routing.RegisterRoute("artist-picker", typeof(ArtistPickerPage));
Routing.RegisterRoute("song-picker", typeof(SongPickerPage));
Routing.RegisterRoute("youtube-search", typeof(YouTubeSearchPage));
```

---

## DI Registration

| Type | Lifetime |
|---|---|
| `ArtistPickerPage`, `SongPickerPage`, `YouTubeSearchPage` | `AddTransient` |
| `ArtistPickerViewModel`, `SongPickerViewModel`, `YouTubeSearchViewModel` | `AddTransient` |
| `IMessenger` → `WeakReferenceMessenger.Default` | `AddSingleton` (if not already registered) |

---

## Invariants & Postconditions

- After `SelectResultCommand` executes: the picker page is no longer on the navigation stack.
- After navigating back without selecting: the calling ViewModel's fields are identical to their pre-navigation state.
- `IsLoading` is never `true` when the page is idle (pre-search) or displaying results/empty state.
- `Results` never contains stale data from a prior search when a new search begins.
- `HasSearched` is `false` on page open; it becomes `true` after the first search completes (success, empty, or error).
- `SongTitle` null from `MusicSearchResultDto` is treated as empty string when pre-filling — never crash on null.
