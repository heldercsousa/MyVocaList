# Artists & Songs — Technical Design

> **Status:** Spec approved — pending implementation
> **Last updated:** 2026-04-12

---

## Architecture

| Layer | Artefacts |
|-------|-----------|
| Domain | `Artist` entity · `Song` entity · `IArtistRepository` · `ISongRepository` — **all new** |
| Contracts | `ArtistListItemDto` · `SongListItemDto` · `MusicSearchResultDto` — **all new** |
| Infra | `ArtistRepository` · `SongRepository` · `ArtistConfiguration` · `SongConfiguration` — **all new** |
| Services | `ArtistService` · `SongService` · `MusicMetadataService` · `MusicBrainzProvider` · `DeezerProvider` — **all new** |
| MAUI | `ArtistsPage` · `ArtistsViewModel` · `ArtistFormPage` · `ArtistFormViewModel` · `SongsPage` · `SongsViewModel` · `SongFormPage` · `SongFormViewModel` — **all new** (`ArtistsPage` stub exists) |

---

## Domain Layer

### Artist entity

```csharp
public class Artist
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? ExternalId { get; set; }       // Provider's own ID (e.g. MusicBrainz MBID)
    public string? ExternalProvider { get; set; } // "MusicBrainz", "Deezer", or null (manual)
    public bool HasManualEdits { get; set; }      // True if any field changed after API import

    public ICollection<Song> Songs { get; set; }  // Navigation property

    public Artist() { }
}
```

### Song entity

```csharp
public class Song
{
    public int Id { get; set; }
    public string Title { get; set; }
    public int ArtistId { get; set; }
    public string? FeaturedArtists { get; set; }  // Free text: "feat. Ivete Sangalo"
    public string? ExternalId { get; set; }
    public string? ExternalProvider { get; set; }
    public bool HasManualEdits { get; set; }

    public Artist Artist { get; set; }            // Navigation property

    public Song() { }
}
```

### Repository interfaces

```csharp
public interface IArtistRepository : IBaseRepository<Artist>
{
    Task<(IEnumerable<Artist> items, int totalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string? query = null, CancellationToken ct = default);

    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default);

    Task<List<Artist>> SearchByNameAsync(string term, int maxResults = 5, CancellationToken ct = default);

    Task<Artist?> GetByExternalIdAsync(string externalId, CancellationToken ct = default);
}

public interface ISongRepository : IBaseRepository<Song>
{
    Task<(IEnumerable<Song> items, int totalCount)> GetPagedByArtistAsync(
        int artistId, int pageNumber, int pageSize, string? query = null, CancellationToken ct = default);

    Task<bool> ExistsByTitleForArtistAsync(string title, int artistId, int? excludeId = null, CancellationToken ct = default);

    Task<List<Song>> SearchByTitleForArtistAsync(string term, int artistId, int maxResults = 5, CancellationToken ct = default);

    Task<Song?> GetByExternalIdAsync(string externalId, CancellationToken ct = default);

    Task<int> CountByArtistAsync(int artistId, CancellationToken ct = default);
}
```

---

## Contracts Layer

```csharp
// List page row
public record ArtistListItemDto(int Id, string Name, string? ExternalProvider, bool HasManualEdits, int SongCount);

// List page row (scoped to artist)
public record SongListItemDto(int Id, string Title, int ArtistId, string ArtistName, string? FeaturedArtists, string? ExternalProvider, bool HasManualEdits);

// Returned by IMusicMetadataService to the ViewModel
public record MusicSearchResultDto(
    string ExternalId,
    string Provider,
    string ArtistName,
    string? SongTitle,           // null when searching artists only
    string? FeaturedArtists);    // null when searching artists only
```

---

## Infrastructure Layer

### ArtistConfiguration

```csharp
public class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
    public void Configure(EntityTypeBuilder<Artist> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
               .HasColumnType("TEXT").IsRequired().HasMaxLength(250);

        builder.Property(a => a.ExternalId)
               .HasColumnType("TEXT").IsRequired(false).HasMaxLength(100);

        builder.Property(a => a.ExternalProvider)
               .HasColumnType("TEXT").IsRequired(false).HasMaxLength(50);

        builder.Property(a => a.HasManualEdits)
               .IsRequired().HasDefaultValue(false);

        builder.HasIndex(a => a.Name)
               .IsUnique()
               .HasDatabaseName("IX_Artists_Name");

        builder.HasIndex(a => a.ExternalId)
               .IsUnique()
               .HasDatabaseName("IX_Artists_ExternalId");

        builder.HasMany(a => a.Songs)
               .WithOne(s => s.Artist)
               .HasForeignKey(s => s.ArtistId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### SongConfiguration

```csharp
public class SongConfiguration : IEntityTypeConfiguration<Song>
{
    public void Configure(EntityTypeBuilder<Song> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title)
               .HasColumnType("TEXT").IsRequired().HasMaxLength(250);

        builder.Property(s => s.FeaturedArtists)
               .HasColumnType("TEXT").IsRequired(false).HasMaxLength(200);

        builder.Property(s => s.ExternalId)
               .HasColumnType("TEXT").IsRequired(false).HasMaxLength(100);

        builder.Property(s => s.ExternalProvider)
               .HasColumnType("TEXT").IsRequired(false).HasMaxLength(50);

        builder.Property(s => s.HasManualEdits)
               .IsRequired().HasDefaultValue(false);

        builder.HasIndex(s => s.ArtistId)
               .HasDatabaseName("IX_Songs_ArtistId");

        builder.HasIndex(s => new { s.ArtistId, s.Title })
               .IsUnique()
               .HasDatabaseName("IX_Songs_ArtistId_Title");

        builder.HasIndex(s => s.ExternalId)
               .IsUnique()
               .HasDatabaseName("IX_Songs_ExternalId");
    }
}
```

**Note on collation:** No normalized columns. All text searches use `EF.Functions.Like` + `EF.Functions.Collate` on both operands in the repository `WHERE` clauses, relying on the globally applied `CollationInterceptor`. This pattern is portable: for MSSQL, remove the explicit `Collate` calls and rely on DB-level collation cascade.

---

## Services Layer

### IMusicMetadataProvider (provider abstraction)

```csharp
public interface IMusicMetadataProvider
{
    string ProviderName { get; }
    Task<IEnumerable<MusicSearchResultDto>> SearchArtistsAsync(string term, CancellationToken ct = default);
    Task<IEnumerable<MusicSearchResultDto>> SearchSongsAsync(string term, string? artistHint = null, CancellationToken ct = default);
}
```

**MusicBrainzProvider:**
- Base URL: `https://musicbrainz.org/ws/2/`
- Endpoints: `artist?query={term}&fmt=json`, `recording?query=recording:{term} AND artist:{artistHint}&fmt=json`
- Required header: `User-Agent: MyVocaList/1.0 (contact@myvocalist.app)` (MusicBrainz policy)
- Rate limit: 1 req/sec — respected via `Polly` delay policy or `Task.Delay(1100)` between calls
- Returns: mapped to `MusicSearchResultDto`; max 5 results

**DeezerProvider:**
- Base URL: `https://api.deezer.com/`
- Endpoints: `search/artist?q={term}`, `search/track?q=track:"{term}" artist:"{artistHint}"`
- No API key required
- Returns: mapped to `MusicSearchResultDto`; max 5 results

### IMusicMetadataService

```csharp
public interface IMusicMetadataService
{
    Task<IEnumerable<MusicSearchResultDto>> SearchArtistsAsync(string term, CancellationToken ct = default);
    Task<IEnumerable<MusicSearchResultDto>> SearchSongsAsync(string term, string? artistHint = null, CancellationToken ct = default);
}
```

`MusicMetadataService` receives `IEnumerable<IMusicMetadataProvider>` ordered by registration (MusicBrainz first). It tries each provider in turn: on empty result or transient network error, it falls through to the next. If all providers fail, it returns an empty collection (ViewModel surfaces the error message).

### IArtistService

```csharp
public interface IArtistService
{
    (bool isValid, string message) ValidateNameInput(string name);
    Task<(bool success, string message, Artist? artist)> CreateArtistAsync(string name, string? externalId = null, string? externalProvider = null, CancellationToken ct = default);
    Task<(bool success, string message)> UpdateArtistAsync(int id, string name, bool hasManualEdits, CancellationToken ct = default);
    Task<(bool success, string message)> DeleteArtistsAsync(IEnumerable<int> ids, CancellationToken ct = default);
    Task<(IEnumerable<ArtistListItemDto> items, int totalCount)> GetPagedArtistsForListAsync(int pageNumber, int pageSize, string? query = null, CancellationToken ct = default);
    Task<List<ArtistListItemDto>> SearchArtistsByNameAsync(string term, int maxResults = 5, CancellationToken ct = default);
    Task<(int songCount, string confirmMessage)> GetDeleteConfirmationAsync(IEnumerable<int> artistIds, CancellationToken ct = default);
}
```

### ISongService

```csharp
public interface ISongService
{
    (bool isValid, string message) ValidateTitleInput(string title);
    Task<(bool success, string message, Song? song)> CreateSongAsync(int artistId, string title, string? featuredArtists = null, string? externalId = null, string? externalProvider = null, CancellationToken ct = default);
    Task<(bool success, string message)> UpdateSongAsync(int id, string title, string? featuredArtists, bool hasManualEdits, CancellationToken ct = default);
    Task<(bool success, string message)> DeleteSongsAsync(IEnumerable<int> ids, CancellationToken ct = default);
    Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedSongsForListAsync(int artistId, int pageNumber, int pageSize, string? query = null, CancellationToken ct = default);
    Task<List<SongListItemDto>> SearchSongsByTitleAsync(string term, int artistId, int maxResults = 5, CancellationToken ct = default);
}
```

---

## MAUI Layer

### Page Structure — ArtistsPage (list)

| Slot | Component | Notes |
|------|-----------|-------|
| `Shell.TitleView` | `SmallAppBar` + `SearchAppBar` | Swapped via `InverseBoolConverter` on `IsSearchMode` |
| Content root | Single-cell `Grid` | Overlay pattern |
| Loading | `ShimmerView` wrapping `DXCollectionView` | `IsInitialLoading` drives shimmer |
| List | `DXCollectionView` | `SelectionMode="Multiple"` hardcoded; `Margin="0,0,0,88"` |
| Item row | `ListItem` | Headline=`Name`; SupportingText=`SongCountText`; LeadingContent=`ListItemLeadingIcon` (music icon); TrailingContent=`CheckEdit` |
| Empty states | Two `EmptyState` components | `IsEmptyNoArtists` / `IsEmptyNoResults` |
| Actions | `FloatingToolbar` + FAB in shared `HorizontalStackLayout` | |
| Confirm delete | Inline `dx:BottomSheet` | `ConfirmSheet` component not used (known ANR limitation) |

**FloatingToolbar slots:**

| Slot | Icon | Action | CanExecute |
|------|------|--------|-----------|
| Action1 | `checklist_outlined` | Select All toggle | Always |
| Action2 | `edit_outlined` | Edit selected | `SelectedCount == 1` |
| Action3 | `delete_outlined` | Delete selected | `SelectedCount > 0` |

**Row tap behavior:** When `SelectedCount == 0`, a row tap navigates to `SongsPage` for that artist. When `SelectedCount > 0`, row tap toggles selection (DXCollectionView native behavior). This is handled in `OnItemTapped` code-behind.

### Page Structure — ArtistFormPage (add / edit)

Shell navigation page, `SafeAreaEdges="All"` + `ScrollView`.

| Slot | Component | Notes |
|------|-----------|-------|
| Shell title | `PageTitle` binding | "New Artist" / "Edit Artist" |
| Name field | `TextEdit` | `HasError` / `ErrorText` binding |
| Character counter | `Label` | Visible when name > 180 chars |
| Suggestion list | `DXCollectionView` (compact) | Max 5 rows; hidden when empty |
| API search strip | `DXBorder` row | `TextEdit` (search term) + "Search" `DXButton` |
| API results list | `DXCollectionView` (compact) | Max 5 rows; hidden when empty |
| API status label | `Label` | Error / no-results messages; hidden otherwise |
| Overwrite warning | Inline `dx:BottomSheet` | Shown when `HasManualEdits = true` and API import triggered |
| Action buttons | `OutlinedButton("Cancel")` + `FilledButton("Save")` | `HorizontalOptions=End` |

### Page Structure — SongsPage (list, scoped to artist)

Mirrors ArtistsPage structure. Differences:

- `SmallAppBar` title = artist name; `NavigationIcon="arrow_back_outlined"`
- Row tap always navigates to Edit Song form (no artist-level sub-navigation)
- `ListItem` SupportingText = `FeaturedArtists` (if present, else empty)
- `ArtistId` is passed via Shell query parameter on navigation

### Page Structure — SongFormPage (add / edit)

Mirrors ArtistFormPage structure. Differences:

- Artist shown as read-only `Label` (not editable — always inherited from parent Songs page)
- Additional `FeaturedArtists` field (`TextEdit`, optional)
- API search uses `SearchSongsAsync(term, artistHint: artistName)`

---

## ViewModel Design

### ArtistsViewModel

```csharp
[ObservableProperty] bool _isRefreshing;
[ObservableProperty] bool _isInitialLoading;
[ObservableProperty] bool _hasMoreItems;
[ObservableProperty] string _searchText;
[ObservableProperty] bool _isSearchMode;
[ObservableProperty] bool _isScrolled;
[ObservableProperty] int _selectedCount;
[ObservableProperty] BottomSheetState _confirmSheetState;
[ObservableProperty] string _confirmMessage;
[ObservableProperty] string _confirmActionText;

// Derived
string AppBarTitle           // "Artists" | "N selected"
bool CanEditSelected         // SelectedCount == 1
bool CanDeleteSelected       // SelectedCount > 0
bool IsAllSelected
bool IsEmptyNoArtists        // no items, no active search
bool IsEmptyNoResults        // no items, active search

// Commands
RefreshCommand, LoadMoreCommand, AddArtistCommand
EditSelectedCommand, DeleteSelectedCommand, SelectAllCommand
ConfirmActionCommand, DismissConfirmCommand
OpenSearchCommand, CloseSearchCommand
TapArtistCommand(ArtistListItemDto)  // navigates to Songs page when nothing selected
```

### ArtistFormViewModel

```csharp
[QueryProperty(nameof(ArtistIdRaw), "artistId")]
[QueryProperty(nameof(ArtistName), "artistName")]
[QueryProperty(nameof(ArtistExternalId), "artistExternalId")]
[QueryProperty(nameof(ArtistExternalProvider), "artistExternalProvider")]
[QueryProperty(nameof(ArtistHasManualEdits), "artistHasManualEdits")]

[ObservableProperty] int? _artistId;
[ObservableProperty] string _artistName;
[ObservableProperty] string? _artistExternalId;
[ObservableProperty] string? _artistExternalProvider;
[ObservableProperty] bool _artistHasManualEdits;

// Validation
[ObservableProperty] bool _nameHasError;
[ObservableProperty] string _nameErrorText;

// API search
[ObservableProperty] string _apiSearchTerm;
[ObservableProperty] IEnumerable<MusicSearchResultDto> _apiResults;
[ObservableProperty] bool _isApiSearching;
[ObservableProperty] string _apiStatusMessage;  // error or no-results text

// Duplicate suggestions (local DB)
[ObservableProperty] IEnumerable<ArtistListItemDto> _suggestions;

// Overwrite warning sheet
[ObservableProperty] BottomSheetState _overwriteWarningState;
[ObservableProperty] MusicSearchResultDto _pendingImport;  // held until admin confirms

// Character counter
bool ShowCharacterCounter
string CharacterCounterText
bool IsCharacterCounterWarning
bool IsCharacterCounterError

bool IsEditMode => ArtistId.HasValue;
string PageTitle => IsEditMode ? "Edit Artist" : "New Artist";

// HasManualEdits tracking
private bool _importedFromApi;  // set true after ImportFromApiCommand populates fields
// Any subsequent OnArtistNameChanged (when _importedFromApi is true) marks _pendingHasManualEdits = true

// Commands
SaveCommand, CancelCommand
SearchApiCommand        // fires MusicMetadataService.SearchArtistsAsync
ImportFromApiCommand(MusicSearchResultDto)  // populates form; checks HasManualEdits first
ConfirmOverwriteCommand, DismissOverwriteCommand
SearchSuggestionsCommand(string term)  // local DB dedup
SelectSuggestionCommand(ArtistListItemDto)  // navigate to edit that artist
```

### SongsViewModel

Same structure as ArtistsViewModel. Additional:
- `ArtistId` and `ArtistName` received via query parameter
- `AppBarTitle` always = artist name (no selection-count variant — the SmallAppBar Subtitle shows selection count instead, or title stays fixed; pick in implementation)

### SongFormViewModel

Same structure as ArtistFormViewModel. Additional:
- `ArtistId` and `ArtistName` received via query parameter (read-only — not editable)
- `FeaturedArtists` field with its own `[ObservableProperty]`
- API search uses `SearchSongsAsync(term, artistHint: ArtistName)`

---

## Code-Behind Responsibilities

### ArtistsPage.xaml.cs

| Event | Handler | Purpose |
|-------|---------|---------|
| `OnAppearing` | Assigns `SelectedItems`; calls `InitializeAsync` | DXCollectionView `IList` requirement |
| `SelectionChanged` | `OnSelectionChanged` → `_viewModel.OnSelectionChanged(count)` | `SelectedCount` sync |
| `Scrolled` | `OnCollectionViewScrolled` → `_viewModel.IsScrolled = e.Offset > 0` | App bar elevation |
| `Tap` | `OnItemTapped` | Navigate to Songs if `SelectedCount == 0`; else let DX handle selection |
| `StateChanged` (BottomSheet) | `OnConfirmSheetStateChanged` | User-dismiss → VM sync |
| `PropertyChanged` (VM) | `OnViewModelPropertyChanged` | Opens/closes confirm sheet |
| `OnBackButtonPressed` | Sheet → search → default | Android back priority |

---

## Interaction Flows

### Artist-first registration with API enrichment

1. Admin taps FAB → `ArtistFormPage` (add mode)
2. Admin types artist name → local suggestions appear (≥ 2 chars, 400ms debounce)
3. Admin taps "Search" in API strip → `SearchArtistsAsync(term)` → results appear
4. Admin taps an API result → form populated; `_importedFromApi = true`
5. Admin adjusts name → `HasManualEdits` flag set for save
6. Admin taps Save → artist created with `ExternalId`, `ExternalProvider`, `HasManualEdits`

### Song-first registration with API enrichment

1. Admin navigates to any artist's Songs page
2. Admin taps FAB → `SongFormPage` (add mode, artist pre-set)
3. Admin types song title → local suggestions for this artist appear
4. Admin taps "Search" → `SearchSongsAsync(term, artistHint)` → results
5. Admin selects result → `Title` and `FeaturedArtists` populated
6. Admin saves → song created under that artist

### Overwrite warning flow (edit mode, HasManualEdits = true)

1. Admin opens Edit Artist form for an artist with `HasManualEdits = true`
2. Admin taps "Search" in API strip and selects a result
3. Warning BottomSheet appears: "This artist has been manually edited. Importing will overwrite your changes."
4. Admin confirms → fields populated; `HasManualEdits` remains `true` (admin just accepted the overwrite)
5. Admin cancels → form unchanged; `pendingImport` discarded

### Artist row tap (no selection active)

1. `SelectedCount == 0` and admin taps an artist row
2. `OnItemTapped` in code-behind → `_viewModel.TapArtistCommand.Execute(item)`
3. Navigate to `Songs?artistId={id}&artistName={name}`

---

## Error Handling

| Scenario | Behavior |
|----------|----------|
| Artist name empty / too short / too long | Inline `HasError` / `ErrorText` — no navigation |
| Artist name duplicate (local) | Suggestion list appears — non-blocking |
| Artist name unique violation on save | Inline error: "An artist with this name is already registered." |
| Artist delete with songs | Confirmation message includes song count; cascade handled at DB level |
| Song title duplicate for same artist | Inline error: "This artist already has a song with this title." |
| API search — all providers fail | Inline label below search strip: "Could not reach music catalog. Check your connection." |
| API search — no results | Inline label: "No results found. You can register manually." |
| API import on `HasManualEdits = true` record | Warning BottomSheet before overwrite |
| Load failure (list page) | Logged; `IsRefreshing = false`; list stays as-is |
| Delete failure (unexpected) | Snackbar: "Could not delete. Try again." |
| Unexpected exceptions | Bubble to `GlobalExceptionHandler` |

---

## Navigation & DI Registration

### Routes additions (`Navigation/Routes.cs`)

```csharp
public const string ArtistForm = "artist-form";
public const string Songs      = "songs";
public const string SongForm   = "song-form";
```

### AppShell registration

```csharp
Routing.RegisterRoute(Routes.ArtistForm, typeof(ArtistFormPage));
Routing.RegisterRoute(Routes.Songs,      typeof(SongsPage));
Routing.RegisterRoute(Routes.SongForm,   typeof(SongFormPage));
```

### MauiProgram.cs

```csharp
// Pages + ViewModels
builder.Services.AddTransient<ArtistsPage>();
builder.Services.AddTransient<ArtistsViewModel>();
builder.Services.AddTransient<ArtistFormPage>();
builder.Services.AddTransient<ArtistFormViewModel>();
builder.Services.AddTransient<SongsPage>();
builder.Services.AddTransient<SongsViewModel>();
builder.Services.AddTransient<SongFormPage>();
builder.Services.AddTransient<SongFormViewModel>();

// Services
builder.Services.AddScoped<IArtistService, ArtistService>();
builder.Services.AddScoped<ISongService, SongService>();
builder.Services.AddScoped<IMusicMetadataService, MusicMetadataService>();

// Providers (order determines chain: MusicBrainz first, Deezer fallback)
builder.Services.AddScoped<IMusicMetadataProvider, MusicBrainzProvider>();
builder.Services.AddScoped<IMusicMetadataProvider, DeezerProvider>();

// HttpClient — MusicBrainz requires User-Agent header
builder.Services.AddHttpClient<MusicBrainzProvider>(client =>
{
    client.BaseAddress = new Uri("https://musicbrainz.org/ws/2/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("MyVocaList/1.0 (contact@myvocalist.app)");
});
builder.Services.AddHttpClient<DeezerProvider>(client =>
{
    client.BaseAddress = new Uri("https://api.deezer.com/");
});

// Repositories
builder.Services.AddScoped<IArtistRepository, ArtistRepository>();
builder.Services.AddScoped<ISongRepository, SongRepository>();
```

---

## Key Decisions

| Decision | Choice | Reason |
|----------|--------|--------|
| No normalized columns | Rely on DB collation + `EF.Functions.Collate` | Portable; `Person.FullNameNormalized` is acknowledged technical debt |
| API abstraction | `IMusicMetadataProvider` chain | Pluggable without over-engineering; MusicBrainz + Deezer as first two adapters |
| Provider fallback | Silent; ViewModel surfaces only final outcome | Admin doesn't need to know which provider responded |
| `HasManualEdits` tracking | Private `_importedFromApi` flag in ViewModel; any post-import field change sets flag on save | Lightweight; coarse-grained is sufficient for v1 |
| Artist-scoped song list | `SongsPage` always scoped to one artist | Simpler paging and search; cross-artist song search deferred |
| Row tap = navigate (when no selection) | `OnItemTapped` code-behind checks `SelectedCount` | Follows MD3 single-action list pattern |
| Featured artists | Free text `string?` on Song | No join table; display reality captured without relational complexity |
| Cascade delete | DB-level via `OnDelete(DeleteBehavior.Cascade)` | Consistent with EF Core patterns; service surfaces count in confirmation |
| Form page (not BottomSheet) | Shell navigation page | Keyboard safety on Android |
| `ConfirmSheet` component | Not used — inline `dx:BottomSheet` | Known ANR limitation when `BottomSheet` is inside `ContentView` |
| `HttpClient` via `AddHttpClient<T>` | Typed client per provider | Manages socket lifetime; avoids `HttpClient` instantiation anti-pattern |
