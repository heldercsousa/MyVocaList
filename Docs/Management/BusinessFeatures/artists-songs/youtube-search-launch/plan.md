# YouTube Search Launch Button — Implementation Plan

> **Approved for implementation** — specification complete and ready for development.

---

## Context: Why This Feature?

MyVocaList provides two pathways for admins to find YouTube karaoke videos for songs:

1. **YouTube Karaoke URLs feature** (completed): In-app YouTube search (with API key) or paste URL into the song form, stores URLs in the database, plays videos via queue.
2. **YouTube Search Launch Button** (this feature): Quick external search without leaving the app — opens YouTube app or browser with a pre-filled search for `karaoke <title> <artist>`.

The second pathway is **lightweight and complementary** — no API key required, no database changes, just a one-tap shortcut to explore videos before committing to storing them. It's available on three pages where users interact with songs: the song form (editing), the songs list (browsing), and the song picker (selecting).

---

## Overview of Changes

### Files Modified

- **ViewModels:** 3 files
  - `MyVocaList/UI/ViewModels/Songs/SongFormViewModel.cs` — add `LaunchYouTubeSearchCommand` + `CanLaunchYouTubeSearch` property
  - `MyVocaList/UI/ViewModels/Songs/SongsViewModel.cs` — add `LaunchYouTubeSearchCommand(SongListItemDto)` command
  - `MyVocaList/UI/ViewModels/Songs/SongPickerViewModel.cs` — add `LaunchYouTubeSearchCommand(MusicSearchResultDto)` command

- **Views:** 3 files
  - `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` — add "🔍 Search YouTube" button in YouTube URLs section
  - `MyVocaList/UI/Pages/Songs/SongsPage.xaml` — add trailing action button to list item template
  - `MyVocaList/UI/Pages/Songs/SongPickerPage.xaml` — add trailing action button to list item template

- **Tests:** 1 file (optional)
  - `MyVocaList.Tests/Unit/ViewModels/LaunchYouTubeSearchCommandTests.cs` — verify URL construction and encoding

- **Documentation:** Updated via session-end ritual
  - This `plan.md` file

### No Database Changes

No migrations, no entity additions, no schema updates.

### No Service Layer Changes

All logic lives in ViewModels. No need for new services or repository methods.

---

## Implementation Approach

### Step 1: ViewModel Commands (Sequential)

Implement the `LaunchYouTubeSearchCommand` in each ViewModel:

```csharp
// SongFormViewModel
[RelayCommand]
private async Task LaunchYouTubeSearch()
{
    var query = $"karaoke {SongTitle} {ArtistName}";
    var encodedQuery = Uri.EscapeDataString(query);
    var uri = new Uri($"https://youtu.be/?search_query={encodedQuery}");
    
    bool opened = await Launcher.TryOpenAsync(uri);
    if (!opened)
    {
        await Browser.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
    }
}

// Add the CanLaunchYouTubeSearch computed property
[ObservableProperty]
private bool _canLaunchYouTubeSearch;

partial void OnSongTitleChanged(string value) => UpdateCanLaunchYouTubeSearch();
partial void OnArtistNameChanged(string value) => UpdateCanLaunchYouTubeSearch();

private void UpdateCanLaunchYouTubeSearch()
{
    CanLaunchYouTubeSearch = !string.IsNullOrWhiteSpace(SongTitle) 
                          && !string.IsNullOrWhiteSpace(ArtistName);
}
```

Similar implementations for `SongsViewModel` and `SongPickerViewModel` (with parameter overloads).

**Required imports:**
- `using Microsoft.Maui.Controls;` (for `Launcher`, `Browser`)

### Step 2: XAML Buttons (Parallel after Step 1)

#### SongFormPage

Add a button in the YouTube URLs section (between the header and the "Search YouTube" navigation row):

```xaml
<Grid ColumnDefinitions="*,Auto" ColumnSpacing="8" Margin="0,8,0,0">
    <dx:DXButton Grid.Column="0"
                 Content="🔍 Search YouTube"
                 Style="{StaticResource OutlinedButton}"
                 Command="{Binding LaunchYouTubeSearchCommand}"
                 IsEnabled="{Binding CanLaunchYouTubeSearch}" />
</Grid>
```

Placement: After the YouTube URLs section header, before the saved URL list.

#### SongsPage

Extend the list item template's trailing content to include a menu button:

```xaml
<lists:ListItem.TrailingContent>
    <HorizontalStackLayout Spacing="8" VerticalOptions="Center">
        <dx:CheckEdit IsChecked="True" ... />
        <dx:DXButton Icon="more_vert"
                     Style="{StaticResource StandardIconButton}"
                     Command="{Binding Source={RelativeSource AncestorType={x:Type vm:SongsViewModel}}, Path=LaunchYouTubeSearchCommand}"
                     CommandParameter="{Binding .}" />
    </HorizontalStackLayout>
</lists:ListItem.TrailingContent>
```

#### SongPickerPage

Similar trailing button in the list item template:

```xaml
<lists:ListItem.TrailingContent>
    <dx:DXButton Icon="more_vert"
                 Style="{StaticResource StandardIconButton}"
                 Command="{Binding Source={RelativeSource AncestorType={x:Type vm:SongPickerViewModel}}, Path=LaunchYouTubeSearchCommand}"
                 CommandParameter="{Binding .}" />
</lists:ListItem.TrailingContent>
```

### Step 3: Build & Verify

1. **Build:** `dotnet build` — confirm no compiler errors
2. **Smoke test each page:**
   - SongFormPage: Edit a song with title + artist; tap button; verify YouTube opens
   - SongsPage: Tap trailing button on a song; verify YouTube opens
   - SongPickerPage: Search for a song; tap trailing button; verify YouTube opens
3. **Edge cases:**
   - Tap button when title is empty → button should be disabled
   - On a device without YouTube app installed → browser should open instead

### Step 4: Unit Tests (Optional, Recommended)

Write tests to verify:
- URL construction: `karaoke Title Artist` → correctly formatted
- URL encoding: spaces → `%20`, special chars → percent-encoded
- Button disabled state: empty title or artist → `CanLaunchYouTubeSearch = false`

Example test:

```csharp
[Fact]
public void LaunchYouTubeSearch_ConstructsEncodedQuery()
{
    var vm = new SongFormViewModel(/* deps */);
    vm.SongTitle = "Despacito";
    vm.ArtistName = "Luis Fonsi";
    
    // Command execution would build:
    // "karaoke Despacito Luis Fonsi" → 
    // "karaoke%20Despacito%20Luis%20Fonsi"
    
    var query = $"karaoke {vm.SongTitle} {vm.ArtistName}";
    var encoded = Uri.EscapeDataString(query);
    
    Assert.Equal("karaoke%20Despacito%20Luis%20Fonsi", encoded);
}

[Fact]
public void CanLaunchYouTubeSearch_EmptyTitle_ReturnsFalse()
{
    var vm = new SongFormViewModel(/* deps */);
    vm.SongTitle = "";
    vm.ArtistName = "Artist";
    
    Assert.False(vm.CanLaunchYouTubeSearch);
}
```

---

## Verification Strategy

### Manual Smoke Test Checklist

- [ ] SongFormPage: Button visible with title + artist; disabled when either is empty
- [ ] SongFormPage: Tapping button opens YouTube with `karaoke <title> <artist>` search
- [ ] SongsPage: Trailing menu button visible on each song row
- [ ] SongsPage: Tapping button opens YouTube for that song; list unchanged
- [ ] SongPickerPage: Trailing button visible on search results
- [ ] SongPickerPage: Tapping button opens YouTube; picker remains open
- [ ] URL encoding test: Special characters (!, &, ?, #) are percent-encoded correctly

### Unit Test Verification

- [ ] Run `dotnet test --filter "LaunchYouTube"` — all tests pass
- [ ] URL construction test passes
- [ ] URL encoding test passes
- [ ] Button disabled state test passes

### Build Verification

- [ ] `dotnet build` → 0 errors, 0 warnings
- [ ] No new compiler suppressions needed

---

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| Button disabled logic breaks binding | Low | Medium | Unit test the `CanLaunchYouTubeSearch` computed property |
| URL encoding produces invalid YouTube search | Low | Medium | Test with special characters (spaces, !, ?) |
| YouTube app launch fails silently | Low | Low | Fallback to browser is automatic |
| XAML button placement breaks list layout | Low | Low | Incremental XAML edit per constitutional constraint |

---

## Schedule

- **Phase 1 (ViewModel Commands):** 1 session, 1–2 hours
- **Phase 2 (XAML Buttons):** 1 session, 1–2 hours (parallel after Phase 1)
- **Phase 3 (Unit Tests):** 30–45 minutes (optional, concurrent with Phase 2)
- **Total:** 2–3 hours for full feature including tests

---

## Acceptance

This plan is approved for implementation once Helder confirms:
1. The three pages and ViewModels identified are correct
2. The URL format (`https://youtu.be/?search_query=...`) is the intended YouTube search endpoint
3. No component governance review is needed for the trailing buttons on SongsPage and SongPickerPage

---

## Follow-Up Tasks

1. **Component Governance:** Task 2.2 and 2.3 add trailing actions directly to list item templates. A future refactoring may extract a reusable `CrudListView.TrailingActions` component to consolidate this pattern across the app (see BACKLOG.md § Component Change Governance Rule).

2. **Error State UX:** If YouTube launch fails on both Launcher and Browser, the current behavior shows a snackbar. Consider standardizing error states across all search surfaces (see BACKLOG.md § Search Error State UX Standardization).

3. **Share Intent Integration:** A future feature (YouTube Share Intent) may provide an alternative to this button — allowing users to share a YouTube video directly into the app to add karaoke URLs. When that feature ships, this button may become a secondary pathway.
