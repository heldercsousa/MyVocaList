# YouTube Search Launch Button — Technical Design

> **Status:** Spec ready for implementation
> **Architecture:** View-Model level only (UI commands, no services, no database)

---

## Architecture Overview

This feature adds command methods to three ViewModels and XAML buttons to three pages. There are **no domain entities, no repository changes, and no service layer** — the entire feature is UI-level navigation logic using the MAUI `Launcher` and `Browser` APIs.

| Layer | Artifacts |
|-------|-----------|
| MAUI (ViewModel) | `SongFormViewModel.LaunchYouTubeSearchCommand` · `SongsViewModel.LaunchYouTubeSearchCommand` · `SongPickerViewModel.LaunchYouTubeSearchCommand` |
| MAUI (View) | `SongFormPage` (button) · `SongsPage` (trailing action) · `SongPickerPage` (trailing action) |
| Domain/Infra | (none) |
| Services | (none) |

---

## Data Model

**No data model changes.** The feature does not create, read, or persist any data.

---

## Page Architecture

### SongFormPage

**Button placement:** Inside the YouTube URLs section, above the current "Search YouTube" navigation row (which navigates to YouTubeSearchPage for API-powered search).

**XAML structure:**
```xaml
<!-- Two ways to search: button for immediate web search, or navigate to YouTubeSearchPage for API search -->
<Grid ColumnDefinitions="*,Auto" ColumnSpacing="8" Margin="0,8,0,8">
    <dx:DXButton Grid.Column="0"
                 Content="🔍 Search YouTube"
                 Style="{StaticResource OutlinedButton}"
                 Command="{Binding LaunchYouTubeSearchCommand}"
                 IsEnabled="{Binding CanLaunchYouTubeSearch}" />
    <Label Grid.Column="1" Text="or" VerticalOptions="Center" />
</Grid>

<!-- Existing navigation row -->
<lists:ListItem
    Headline="Search YouTube (API)"
    IsVisible="{Binding HasYouTubeApiKey}">
    <!-- ... -->
</lists:ListItem>
```

**ViewModel contract:**
- `[RelayCommand] async Task LaunchYouTubeSearch()` — command decorated with `[INotifyPropertyChanged]` source generator
- `[ObservableProperty] bool _canLaunchYouTubeSearch` — computed from `SongTitle` and `ArtistName` (non-empty)

**Bindings:**
- `Command="{Binding LaunchYouTubeSearchCommand}"`
- `IsEnabled="{Binding CanLaunchYouTubeSearch}"`

---

### SongsPage

**Button placement:** Trailing action in each list item (within the `CrudListView` template).

**XAML integration:** Since `CrudListView` currently has no built-in trailing action slot beyond the checkbox, either:

**Option A (preferred):** Add a `TrailingActions` BindableProperty to `CrudListView` and extend the template with a context menu or icon button. This requires a component change governed by the Component Change Governance Rule (BACKLOG.md § Pending).

**Option B (inline for now):** Replace the trailing checkbox with a horizontal stack containing the checkbox + an overflow menu button. The menu includes "Search YouTube" action.

For this spec, **assume Option B (inline)** to avoid blocking on component governance. Future refactoring can extract trailing actions into a reusable `CrudListView` feature.

**Modified item template in SongsPage.xaml:**
```xaml
<lists:ListItem Headline="{Binding Title}" SupportingText="{Binding FeaturedArtists}" IsSelected="True">
    <lists:ListItem.TrailingContent>
        <HorizontalStackLayout Spacing="8" VerticalOptions="Center">
            <dx:CheckEdit IsChecked="True" ... />
            <dx:DXButton Icon="more_vert"
                         Style="{StaticResource StandardIconButton}"
                         Clicked="OnSongActionMenuClicked"
                         CommandParameter="{Binding .}" />
        </HorizontalStackLayout>
    </lists:ListItem.TrailingContent>
</lists:ListItem>
```

**Code-behind or ViewModel:**
- Option 1: Inline `Clicked` handler in code-behind that constructs a `SearchYouTubeCommand` and invokes it via the ViewModel
- Option 2: Add `SearchYouTubeCommand` and `LaunchYouTubeSearchCommand` to `SongsViewModel`; pass the song as parameter

**Recommendation:** Use **Option 2** — add a `LaunchYouTubeSearchCommand(SongListItemDto song)` that takes the song as a parameter. This keeps logic in the ViewModel.

```csharp
[RelayCommand]
private async Task LaunchYouTubeSearch(SongListItemDto song)
{
    // Construct query and launch
}
```

---

### SongPickerPage

**Button placement:** Trailing action in each search result item.

**XAML integration:** Similar to SongsPage — extend the list item template with a trailing menu button or action.

**Modified item template in SongPickerPage.xaml:**
```xaml
<lists:ListItem Headline="{Binding SongTitle}" SupportingText="{Binding ArtistName}">
    <lists:ListItem.TrailingContent>
        <dx:DXButton Icon="more_vert"
                     Style="{StaticResource StandardIconButton}"
                     Command="{Binding Source={RelativeSource AncestorType={x:Type ContentPage}}, Path=BindingContext.LaunchYouTubeSearchCommand}"
                     CommandParameter="{Binding .}" />
    </lists:ListItem.TrailingContent>
    <lists:ListItem.GestureRecognizers>
        <!-- Existing tap to select -->
    </lists:ListItem.GestureRecognizers>
</lists:ListItem>
```

**ViewModel contract:**
- `[RelayCommand] async Task LaunchYouTubeSearch(MusicSearchResultDto result)` — takes the search result as parameter

---

## ViewModel Contract

### SongFormViewModel

```csharp
// Observable property — true if both SongTitle and ArtistName are non-empty
[ObservableProperty]
private bool _canLaunchYouTubeSearch;

// Called when SongTitle or ArtistName changes (via partial method)
partial void OnSongTitleChanged(string value) => UpdateCanLaunchYouTubeSearch();
partial void OnArtistNameChanged(string value) => UpdateCanLaunchYouTubeSearch();

private void UpdateCanLaunchYouTubeSearch()
{
    CanLaunchYouTubeSearch = !string.IsNullOrWhiteSpace(SongTitle) 
                          && !string.IsNullOrWhiteSpace(ArtistName);
}

// Command
[RelayCommand]
private async Task LaunchYouTubeSearch()
{
    var query = $"karaoke {SongTitle} {ArtistName}";
    var encodedQuery = Uri.EscapeDataString(query);
    var youtubeUri = new Uri($"https://youtu.be/?search_query={encodedQuery}");
    
    bool opened = await Launcher.TryOpenAsync(youtubeUri);
    
    if (!opened)
    {
        // Fallback to browser
        await Browser.OpenAsync(youtubeUri, BrowserLaunchMode.SystemPreferred);
    }
}
```

### SongsViewModel

```csharp
[RelayCommand]
private async Task LaunchYouTubeSearch(SongListItemDto song)
{
    if (song == null || string.IsNullOrWhiteSpace(song.Title) || string.IsNullOrWhiteSpace(song.ArtistName))
    {
        _snackbarService.Show("Song title or artist missing");
        return;
    }
    
    var query = $"karaoke {song.Title} {song.ArtistName}";
    var encodedQuery = Uri.EscapeDataString(query);
    var youtubeUri = new Uri($"https://youtu.be/?search_query={encodedQuery}");
    
    bool opened = await Launcher.TryOpenAsync(youtubeUri);
    
    if (!opened)
    {
        await Browser.OpenAsync(youtubeUri, BrowserLaunchMode.SystemPreferred);
    }
}
```

### SongPickerViewModel

```csharp
[RelayCommand]
private async Task LaunchYouTubeSearch(MusicSearchResultDto result)
{
    if (result == null || string.IsNullOrWhiteSpace(result.SongTitle) || string.IsNullOrWhiteSpace(result.ArtistName))
    {
        _snackbarService.Show("Song title or artist missing");
        return;
    }
    
    var query = $"karaoke {result.SongTitle} {result.ArtistName}";
    var encodedQuery = Uri.EscapeDataString(query);
    var youtubeUri = new Uri($"https://youtu.be/?search_query={encodedQuery}");
    
    bool opened = await Launcher.TryOpenAsync(youtubeUri);
    
    if (!opened)
    {
        await Browser.OpenAsync(youtubeUri, BrowserLaunchMode.SystemPreferred);
    }
}
```

---

## Navigation & Platform APIs

### Launcher & Browser

**Using .NET MAUI built-ins:**
- `Microsoft.Maui.Controls.Launcher.TryOpenAsync(Uri)` — attempts to open a URI using the default app (YouTube)
- `Microsoft.Maui.Controls.Browser.OpenAsync(Uri, BrowserLaunchMode)` — opens a URI in the system web browser
- Both are async and return a `Task<bool>` or `Task` respectively

**URL format for YouTube search:**
- YouTube search URL: `https://youtu.be/?search_query={query}`
- Alternative: `https://www.youtube.com/results?search_query={query}`

**Query parameter encoding:**
- Use `Uri.EscapeDataString(query)` to RFC-encode the query string
- Example: `"karaoke Despacito Luis Fonsi"` → `"karaoke%20Despacito%20Luis%20Fonsi"`

### Exception Handling

- **No exceptions expected.** `Launcher.TryOpenAsync` returns `false` if the app is not installed or launch fails.
- `Browser.OpenAsync` may throw in rare cases; catch and log as a warning.
- Do NOT show error dialogs for failed YouTube launches — show a quiet snackbar or log only.

---

## Interaction Flows

### Flow 1: SongFormPage — Admin launches YouTube search

1. Admin opens or creates a song form
2. Fills in Artist Name and Song Title
3. Taps "🔍 Search YouTube" button
4. `LaunchYouTubeSearchCommand` executes:
   - Constructs query: `karaoke {title} {artist}`
   - URL-encodes the query
   - Calls `Launcher.TryOpenAsync(youtubeUri)`
5. YouTube app opens (or browser if app not installed) with search results
6. Admin previews videos and returns to the app
7. Form is unchanged; admin can continue editing or save

### Flow 2: SongsPage — Admin searches from list

1. Admin views the Songs CRUD list
2. Sees each song row with a trailing menu icon
3. Taps the menu icon on a song row
4. A context menu appears with "Search YouTube" option
5. Tapping "Search YouTube" executes `LaunchYouTubeSearchCommand(song)`
6. YouTube app/browser opens with the search
7. Admin returns to the list; list state is unchanged

### Flow 3: SongPickerPage — Admin searches before selecting

1. Admin uses SongPickerPage to add a song (e.g., to a queue)
2. Enters a search query to find a song
3. Results appear with each song having a trailing menu
4. Tapping the menu on a song shows "Search YouTube"
5. Tapping "Search YouTube" executes `LaunchYouTubeSearchCommand(result)`
6. YouTube opens with the search
7. Admin returns to the picker; it remains open and unsaved

---

## Timing & Performance

- **Instant.** No network calls, no database queries, no async waiting (other than the platform launch).
- `Launcher.TryOpenAsync` is a platform-native call; latency depends on OS (typically <100ms).
- `Uri.EscapeDataString` is synchronous and O(n) in query length (negligible).

---

## Error Handling

| Scenario | Behavior |
|----------|----------|
| YouTube app installed | Launcher succeeds; YouTube opens |
| YouTube app NOT installed | Launcher fails (returns false); fallback to Browser → browser opens with search |
| Both launcher and browser fail | Log warning; show snackbar "Could not open YouTube" |
| Title or artist is empty | Button is disabled (`IsEnabled=false`); command cannot execute |
| Network unavailable (on browser fallback) | Browser shows "no internet" message (handled by OS) |

---

## Key Decisions

| Decision | Rationale |
|----------|-----------|
| No services, no DI | This is pure UI-level navigation — no business logic, no data persistence. Keeping it simple. |
| `Launcher` first, `Browser` fallback | YouTube app provides native UX; browser fallback ensures feature works everywhere. |
| RFC 3986 encoding via `Uri.EscapeDataString()` | Standard .NET utility; handles all special characters correctly. |
| "karaoke" prefix is hardcoded | Non-configurable by design; users can customize search in YouTube app after launch. |
| Snackbar on failure | Quiet notification; does not interrupt the user with an error dialog. |
| No navigation stack change | Button tap does not change app navigation; user returns via OS back button. |
