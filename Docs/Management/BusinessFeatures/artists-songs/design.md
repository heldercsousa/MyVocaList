# Artists & Songs — Technical Design

> **Status:** Spec approved — implementation in progress (phases 1–7 complete)
> **Last updated:** 2026-04-12
> **Spec updated 2026-05-15:** Unified Artist model clarified: Artist serves dual roles (copyright
> owner + performer); Song.ArtistId is and must remain int NOT NULL; Catalog join entity added;
> Songs added as top-level menu item; navigation model revised; Lyrics field added; ILyricsProvider
> placeholder added. Phases 9–14 added to tasks.md.
> **Spec updated 2026-05-15b:** Role filter added: `ArtistRoleFilter` enum; `_roleFilter` observable
> on ArtistsViewModel; `IArtistRepository.GetPagedAsync` updated with `roleFilter` parameter; Role
> filter row added to ArtistsPage Page Structure table.
> **Spec updated 2026-06-20:** Phase 2 reconciliation — FilterChipGroup (two chips) replaces top tab bar; single "Artists" menu entry replaces Authors/Performers split; Song.Version added; IX_Songs_ArtistId_Title_Version replaces 2-col index; ArtistsViewModel and AppShell blocks updated.

---

## Architecture

| Layer | Artefacts |
|-------|-----------|
| Domain | `Artist` · `Song` · `Catalog` · `IArtistRepository` · `ISongRepository` · `ICatalogRepository` |
| Contracts | `ArtistListItemDto` · `SongListItemDto` · `MusicSearchResultDto` |
| Infra | `ArtistRepository` · `SongRepository` · `CatalogRepository` · `ArtistConfiguration` · `SongConfiguration` · `CatalogConfiguration` |
| Services | `ArtistService` · `SongService` · `CatalogService` · `MusicMetadataService` · `MusicBrainzProvider` · `DeezerProvider` · `ILyricsProvider` (placeholder) |
| MAUI | `ArtistsPage` · `ArtistsViewModel` · `ArtistFormPage` · `ArtistFormViewModel` · `SongsPage` · `SongsViewModel` · `SongFormPage` · `SongFormViewModel` |

---

## Artist Roles

The Artist entity is **unified** — one table, one set of fields. Role is determined by usage
context, not a flag or type column.

| Role | How it is expressed | Constraint |
|------|---------------------|------------|
| **Author** | `Song.ArtistId` references an Artist | `int NOT NULL` — every song has exactly one Author Artist |
| **Performer** | Artist has one or more `Catalog` entries | Optional — Catalog is empty by default |
| **Both** | Artist has songs AND Catalog entries | Fully supported — same Artist record serves both roles |

A future `ArtistMember` join table will link Artist records to `Person` records, but the core
Artist entity is not split into sub-types regardless of that addition.

---

## Domain Layer

### Artist entity

```csharp
public class Artist
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? ExternalId { get; set; }
    public string? ExternalProvider { get; set; }
    public bool HasManualEdits { get; set; }

    public ICollection<Catalog> CatalogEntries { get; set; }  // Performer role: songs this artist can perform live
    public ICollection<Song> OriginalSongs { get; set; }      // Author role: songs this artist created/owns

    public Artist() { }
}
```

### Song entity

```csharp
public class Song
{
    public int Id { get; set; }
    public string Title { get; set; }
    public int ArtistId { get; set; }              // original/copyright artist — mandatory
    public string? FeaturedArtists { get; set; }   // Free text: "feat. Ivete Sangalo"
    public string? Version { get; set; }           // Added by Song Import — version variant label (live, acoustic, remix) within the same artist-title pair
    public string? Lyrics { get; set; }            // plain text, max 10 000 chars
    public string? ExternalId { get; set; }
    public string? ExternalProvider { get; set; }
    public bool HasManualEdits { get; set; }

    public Artist OriginalArtist { get; set; }     // required nav property
    public ICollection<Catalog> CatalogEntries { get; set; }  // artists who include this song in their repertoire

    public Song() { }
}
```

### Catalog entity (join table)

```csharp
/// <summary>
/// Represents an artist's performance repertoire entry — a song the artist performs (may be a cover).
/// "Catálogo" in Portuguese.
/// </summary>
public class Catalog
{
    public int ArtistId { get; set; }
    public int SongId { get; set; }

    public Artist Artist { get; set; }
    public Song Song { get; set; }
}
```

### Repository interfaces

```csharp
public enum ArtistRoleFilter { All, AuthorsOnly, PerformersOnly }

public interface IArtistRepository : IBaseRepository<Artist>
{
    Task<(IEnumerable<Artist> items, int totalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string? query = null,
        ArtistRoleFilter roleFilter = ArtistRoleFilter.All, CancellationToken ct = default);

    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default);

    Task<List<Artist>> SearchByNameAsync(string term, int maxResults = 5, CancellationToken ct = default);

    Task<Artist?> GetByExternalIdAsync(string externalId, CancellationToken ct = default);
}

public interface ISongRepository : IBaseRepository<Song>
{
    /// <summary>All songs — global, not scoped to any artist.</summary>
    Task<(IEnumerable<Song> items, int totalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string? query = null, CancellationToken ct = default);

    Task<bool> ExistsByTitleForArtistAsync(string title, int artistId, int? excludeId = null, CancellationToken ct = default);

    Task<Song?> GetByExternalIdAsync(string externalId, CancellationToken ct = default);
}

public interface ICatalogRepository
{
    /// <summary>Songs in a specific artist's Catalog (their performance repertoire).</summary>
    Task<(IEnumerable<Song> items, int totalCount)> GetPagedByArtistAsync(
        int artistId, int pageNumber, int pageSize, string? query = null, CancellationToken ct = default);

    Task<int> CountByArtistAsync(int artistId, CancellationToken ct = default);

    Task<bool> ExistsAsync(int artistId, int songId, CancellationToken ct = default);

    Task AddAsync(Catalog entry, CancellationToken ct = default);

    Task RemoveAsync(int artistId, int songId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
```

---

## Contracts Layer

```csharp
// Artists list page row
public record ArtistListItemDto(int Id, string Name, string? ExternalProvider, bool HasManualEdits, int CatalogCount);

// Songs list page row (global + catalog mode)
public record SongListItemDto(
    int Id,
    string Title,
    int OriginalArtistId,        // NOT nullable — Song.ArtistId is mandatory (int NOT NULL)
    string? OriginalArtistName,
    string? FeaturedArtists,
    string? ExternalProvider,
    bool HasManualEdits);

// Returned by IMusicMetadataService to the ViewModel
public record MusicSearchResultDto(
    string ExternalId,
    string Provider,
    string ArtistName,
    string? SongTitle,
    string? FeaturedArtists);
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
        builder.Property(a => a.Name).HasColumnType("TEXT").IsRequired().HasMaxLength(250);
        builder.Property(a => a.ExternalId).HasColumnType("TEXT").IsRequired(false).HasMaxLength(100);
        builder.Property(a => a.ExternalProvider).HasColumnType("TEXT").IsRequired(false).HasMaxLength(50);
        builder.Property(a => a.HasManualEdits).IsRequired().HasDefaultValue(false);
        builder.HasIndex(a => a.Name).IsUnique().HasDatabaseName("IX_Artists_Name");
        builder.HasIndex(a => a.ExternalId).IsUnique().HasDatabaseName("IX_Artists_ExternalId");

        // Songs where this artist is the copyright owner
        builder.HasMany(a => a.OriginalSongs)
               .WithOne(s => s.OriginalArtist)
               .HasForeignKey(s => s.ArtistId)
               .OnDelete(DeleteBehavior.Restrict);  // Cannot delete an Artist who owns songs — user must delete their songs first.
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
        builder.Property(s => s.Title).HasColumnType("TEXT").IsRequired().HasMaxLength(250);
        builder.Property(s => s.FeaturedArtists).HasColumnType("TEXT").IsRequired(false).HasMaxLength(200);
        builder.Property(s => s.Lyrics).HasColumnType("TEXT").IsRequired(false).HasMaxLength(10000);
        builder.Property(s => s.ExternalId).HasColumnType("TEXT").IsRequired(false).HasMaxLength(100);
        builder.Property(s => s.ExternalProvider).HasColumnType("TEXT").IsRequired(false).HasMaxLength(50);
        builder.Property(s => s.HasManualEdits).IsRequired().HasDefaultValue(false);
        builder.HasIndex(s => new { s.ArtistId, s.Title, s.Version }).IsUnique().HasDatabaseName("IX_Songs_ArtistId_Title_Version"); // 3-col index added by migration AddSongVersion (Song Import Wave 2.2)
        builder.HasIndex(s => s.ArtistId).HasDatabaseName("IX_Songs_ArtistId");
        builder.HasIndex(s => s.ExternalId).IsUnique().HasDatabaseName("IX_Songs_ExternalId");
    }
}
```

### CatalogConfiguration

```csharp
public class CatalogConfiguration : IEntityTypeConfiguration<Catalog>
{
    public void Configure(EntityTypeBuilder<Catalog> builder)
    {
        builder.HasKey(c => new { c.ArtistId, c.SongId });

        builder.HasOne(c => c.Artist)
               .WithMany(a => a.CatalogEntries)
               .HasForeignKey(c => c.ArtistId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Song)
               .WithMany(s => s.CatalogEntries)
               .HasForeignKey(c => c.SongId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("Catalog");
    }
}
```

**Note on collation:** No normalized columns. All text searches use `EF.Functions.Like` +
`EF.Functions.Collate` on both operands relying on the globally applied `CollationInterceptor`.

---

## Services Layer

### IMusicMetadataProvider

```csharp
public interface IMusicMetadataProvider
{
    string ProviderName { get; }
    Task<IEnumerable<MusicSearchResultDto>> SearchArtistsAsync(string term, CancellationToken ct = default);
    Task<IEnumerable<MusicSearchResultDto>> SearchSongsAsync(string term, string? artistHint = null, CancellationToken ct = default);
}
```

**MusicBrainzProvider:** Base URL `https://musicbrainz.org/ws/2/` · Rate limit 1 req/sec ·
Required `User-Agent` header.

**DeezerProvider:** Base URL `https://api.deezer.com/` · No API key required.

### IMusicMetadataService

```csharp
public interface IMusicMetadataService
{
    Task<IEnumerable<MusicSearchResultDto>> SearchArtistsAsync(string term, CancellationToken ct = default);
    Task<IEnumerable<MusicSearchResultDto>> SearchSongsAsync(string term, string? artistHint = null, CancellationToken ct = default);
}
```

### ILyricsProvider (placeholder — not yet implemented)

```csharp
/// <summary>
/// Placeholder interface for future lyrics API integration.
/// No implementation is registered in DI until a provider is selected via spike task.
/// </summary>
public interface ILyricsProvider
{
    Task<string?> FetchLyricsAsync(string title, string? artistName, CancellationToken ct = default);
}
```

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
    Task<(int catalogCount, string confirmMessage)> GetDeleteConfirmationAsync(IEnumerable<int> artistIds, CancellationToken ct = default);
}
```

### ISongService

```csharp
public interface ISongService
{
    (bool isValid, string message) ValidateTitleInput(string title);
    Task<(bool success, string message, Song? song)> CreateSongAsync(int artistId, string title, string? featuredArtists = null, string? lyrics = null, string? externalId = null, string? externalProvider = null, CancellationToken ct = default);
    Task<(bool success, string message)> UpdateSongAsync(int id, string title, string? featuredArtists, string? lyrics, bool hasManualEdits, CancellationToken ct = default);
    Task<bool> ExistsByTitleForArtistAsync(string title, int artistId, int? excludeId = null, CancellationToken ct = default);
    Task<(bool success, string message)> DeleteSongsAsync(IEnumerable<int> ids, CancellationToken ct = default);
    Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedSongsForListAsync(int pageNumber, int pageSize, string? query = null, CancellationToken ct = default);
}
```

### ICatalogService

```csharp
public interface ICatalogService
{
    Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedCatalogForArtistAsync(int artistId, int pageNumber, int pageSize, string? query = null, CancellationToken ct = default);
    Task<(bool success, string message)> AddSongToCatalogAsync(int artistId, int songId, CancellationToken ct = default);
    Task<(bool success, string message)> RemoveSongFromCatalogAsync(int artistId, int songId, CancellationToken ct = default);
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
| List | `DXCollectionView` | `SelectionMode="Multiple"` hardcoded; row tap = selection toggle only |
| Item row | `ListItem` | Headline=`Name`; SupportingText=`CatalogCountText`; LeadingContent=`CheckEdit` (MD3 multi-action rule — trailing button present, so checkbox moves LEFT; person icon dropped); TrailingContent=`DXButton` (catalog navigation, own touch target) |
| Empty states | Two `EmptyState` components | `IsEmptyNoArtists` / `IsEmptyNoResults` |
| Role filter | FilterChipGroup | Authors / Performers chips; deselect both = All; drives `ArtistRoleFilter` observable on ViewModel |
| Actions | `FloatingToolbar` + FAB in `HorizontalStackLayout` | |
| Confirm delete | Inline `dx:BottomSheet` | |

**MD3 multi-action artist row layout (checkbox LEFT, catalog button RIGHT):**
```xml
<lists:ListItem.LeadingContent>
    <dx:CheckEdit IsChecked="False" InputTransparent="True" VerticalOptions="Center" />
</lists:ListItem.LeadingContent>
<lists:ListItem.TrailingContent>
    <dx:DXButton Style="{StaticResource IconButton}"
                 Icon="queue_music_outlined"
                 Command="{Binding Source={RelativeSource AncestorType={x:Type vm:ArtistsViewModel}},
                                   Path=ViewCatalogCommand}"
                 CommandParameter="{Binding .}"
                 InputTransparent="False" />
</lists:ListItem.TrailingContent>
```

**Row tap behavior:** Row tap always toggles selection via `DXCollectionView` native behavior. The
`OnItemTapped` code-behind is removed (or made a no-op). Navigation to Catalog happens only via
`ViewCatalogCommand` triggered by the icon button.

### Page Structure — SongsPage (dual-mode)

SongsPage operates in two modes determined by the `ArtistId` query parameter:

| Mode | Entry point | ArtistId | AppBar title | Data source |
|------|-------------|----------|--------------|-------------|
| Global | Main menu | `null` / absent | "Songs" | All songs via `ISongService.GetPagedSongsForListAsync` |
| Catalog | Artist trailing button | artist's Id | Artist name | `ICatalogService.GetPagedCatalogForArtistAsync` |

```csharp
// SongsViewModel: ArtistId is optional
[QueryProperty(nameof(ArtistIdRaw), "artistId")]
[QueryProperty(nameof(ArtistNameRaw), "artistName")]

public string AppBarTitle => string.IsNullOrEmpty(_artistName) ? "Songs" : _artistName;
public bool IsCatalogMode => _artistId > 0;
```

In Catalog mode, the FAB opens a song picker (search from global songs) to add to the Catalog.
In Global mode, the FAB opens `SongFormPage` to create a new song.

### Page Structure — SongFormPage (add / edit)

| Slot | Component | Notes |
|------|-----------|-------|
| Shell title | `PageTitle` binding | "New Song" / "Edit Song" |
| Artist autocomplete field | `AutocompleteField` component (`MyVocaList/UI/Components/AutocompleteField/`) | Required; `Text` two-way bound to `ArtistSearchText`; `Suggestions` bound to `ArtistSuggestions` (mapped to `AutocompleteSuggestion` with `Headline=Name`); `SearchRequestedCommand`→`SearchArtistsCommand`; `SuggestionSelectedCommand`→`SelectArtistCommand`; `IsEnabled` = `!IsArtistLocked`; `HasError`/`ErrorText` bound to `ArtistHasError`/`ArtistErrorText` |
| Title field | `TextEdit` | `HasError` / `ErrorText` binding |
| Character counter | `Label` | Visible when title > 180 chars |
| FeaturedArtists field | `TextEdit` | Optional |
| Lyrics field | `Editor` (multi-line) | Optional, max 10 000 chars |
| API search strip | `DXBorder` row | Search term + "Search" button; uses artist name as hint when artist is selected |
| API results list | `DXCollectionView` (compact) | Max 5 rows |
| API status label | `Label` | Error / no-results |
| Overwrite warning | Inline `dx:BottomSheet` | `HasManualEdits` guard |
| Action buttons | Cancel + Save | |

**Artist field behavior:**
- Manual entry: user types ≥ 2 chars → autocomplete dropdown shows matching artists → user taps to select → field shows artist name (editable until saved)
- API import: artist is resolved from the API result → field pre-filled with matched artist name → field disabled (read-only, locked to preserve external attribution)

### AppShell additions

```xml
<!-- AppShell.xaml -->
<FlyoutItem Route="songs" FlyoutItemIsVisible="False">
    <ShellContent ContentTemplate="{DataTemplate songs:SongsPage}" />
</FlyoutItem>
```

```csharp
// AppShell.xaml.cs — Songs is now a FlyoutItem root route, not a pushed route
// Remove: Routing.RegisterRoute(Routes.Songs, typeof(SongsPage));
// Keep SongForm and ArtistForm as pushed routes
```

```csharp
// AppShellViewModel — Catalog group
// Phase 16A.2: simplified from two-entry Authors/Performers navigation to a single "Artists" entry.
// Role filtering is handled on-page via FilterChipGroup chips.
new MenuGroup("Catalog", [
    new MenuItemDescription("Artists", "person_outlined",     Routes.Artists),
    new MenuItemDescription("Songs",   "music_note_outlined", Routes.Songs),
])
```

**ArtistsPage filter behavior:**

The page opens with no chip selected (all artists shown, AppBar title = "Artists"). Selecting a chip activates the role filter. All CRUD operations (register, edit, delete, catalog navigation) are available regardless of the active filter.

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
[ObservableProperty] ArtistRoleFilter _roleFilter; // All | AuthorsOnly | PerformersOnly

// Derived
string AppBarTitle           // "Artists" | "N selected"
bool CanEditSelected         // SelectedCount == 1
bool CanDeleteSelected       // SelectedCount > 0
bool IsAllSelected
bool IsEmptyNoArtists
bool IsEmptyNoResults

// Commands
RefreshCommand, LoadMoreCommand, AddArtistCommand
EditSelectedCommand, DeleteSelectedCommand, SelectAllCommand
ConfirmActionCommand, DismissConfirmCommand
OpenSearchCommand, CloseSearchCommand
ViewCatalogCommand(ArtistListItemDto)  // navigates to Songs page in Catalog mode
```

The `_roleFilter` defaults to `All` on page arrival. A FilterChipGroup renders two chips (Authors, Performers); deselecting both reverts to All; the active chip state drives `_roleFilter`. The filter is applied server-side in `IArtistRepository.GetPagedAsync` via an optional `roleFilter` parameter; search and pagination operate within the filtered set.

### SongsViewModel

```csharp
[QueryProperty(nameof(ArtistIdRaw), "artistId")]   // optional — absent = global mode
[QueryProperty(nameof(ArtistNameRaw), "artistName")]

[ObservableProperty] int _artistId;               // 0 = global mode
[ObservableProperty] string _artistName;           // empty = global mode

// Derived
bool IsCatalogMode => _artistId > 0;
string AppBarTitle => IsCatalogMode ? _artistName : "Songs";
string AppBarSubtitle => _selectedCount > 0 ? $"{_selectedCount} selected" : "";

// Commands (same as ArtistsViewModel pattern, plus:)
AddToCatalogCommand   // Catalog mode only — opens song picker
RemoveFromCatalogCommand(SongListItemDto)  // Catalog mode only
```

### SongFormViewModel

```csharp
// Artist autocomplete
[ObservableProperty] string _artistSearchText;     // user types here
[ObservableProperty] int? _selectedArtistId;       // set when user picks from list or API resolves
[ObservableProperty] string? _selectedArtistName;  // display label
[ObservableProperty] bool _isArtistLocked;         // true when set from API — disables the field
[ObservableProperty] IEnumerable<ArtistListItemDto> _artistSuggestions;
[ObservableProperty] bool _artistHasError;
[ObservableProperty] string _artistErrorText;

[ObservableProperty] string _title;
[ObservableProperty] string? _featuredArtists;
[ObservableProperty] string? _lyrics;

// Validation
[ObservableProperty] bool _titleHasError;
[ObservableProperty] string _titleErrorText;

// API search
[ObservableProperty] string _apiSearchTerm;
[ObservableProperty] IEnumerable<MusicSearchResultDto> _apiResults;
[ObservableProperty] bool _isApiSearching;
[ObservableProperty] string _apiStatusMessage;

// Overwrite warning sheet
[ObservableProperty] BottomSheetState _overwriteWarningState;
[ObservableProperty] MusicSearchResultDto _pendingImport;

bool IsEditMode => _songId.HasValue;
string PageTitle => IsEditMode ? "Edit Song" : "New Song";

// Commands
SaveCommand, CancelCommand
SearchArtistsCommand      // triggered by AutocompleteField.SearchRequestedCommand; queries IArtistService.SearchArtistsByNameAsync
SelectArtistCommand       // triggered by AutocompleteField.SuggestionSelectedCommand; sets SelectedArtistId + SelectedArtistName
SearchApiCommand
ImportFromApiCommand(MusicSearchResultDto)
ConfirmOverwriteCommand, DismissOverwriteCommand
```

---

## Interaction Flows

### Song registration (global mode)

1. Admin opens Songs page from menu
2. Admin taps FAB → `SongFormPage` (add mode)
3. Admin types title; optionally fills FeaturedArtists and Lyrics
4. Admin optionally uses API strip to enrich data
5. Admin saves → song created (ArtistId = selected artist's Id — mandatory)

### Artist Catalog management

1. Admin opens Artists page
2. Admin taps catalog icon button on an artist row → `Songs?artistId={id}&artistName={name}`
3. SongsPage opens in Catalog mode showing that artist's repertoire
4. Admin taps FAB → song picker opens (search from global songs)
5. Admin selects a song → `CatalogService.AddSongToCatalogAsync(artistId, songId)`
6. Song appears in the Catalog list

### Artist deletion (revised)

1. Admin selects artist(s) and taps Delete
2. Service checks: does any selected artist own songs (`Song.ArtistId` references)?
   - If yes → deletion blocked with message: "Cannot delete — this artist owns N song(s). Delete their songs first."
   - If no → proceed to confirmation
3. Confirmation message: "Delete N artist(s)? Their Catalog entries will also be removed." (Catalog links gone; no songs affected since check above ensures none)
4. Admin confirms → Catalog rows cascade-deleted; Artist rows deleted

---

## Error Handling

| Scenario | Behavior |
|----------|----------|
| Song title empty / too short / too long | Inline `HasError` / `ErrorText` |
| Song title duplicate globally | Inline error: "A song with this title is already registered." |
| Song already in artist's Catalog | `AddSongToCatalogAsync` returns `(false, "This song is already in the catalog.")` |
| API search — all providers fail | Inline: "Could not reach music catalog. Check your connection." |
| API search — no results | Inline: "No results found. You can register manually." |
| API import on `HasManualEdits = true` | Warning BottomSheet before overwrite |
| Load failure (list page) | Logged; `IsRefreshing = false`; list stays as-is |
| Delete failure | Snackbar: "Could not delete. Try again." |
| Unexpected exceptions | Bubble to `GlobalExceptionHandler` |

---

## Navigation & DI Registration

### Routes additions (`Navigation/Routes.cs`)

```csharp
public const string Artists    = "artists";  // top-level FlyoutItem
public const string Songs      = "songs";    // top-level FlyoutItem (was pushed route — now promoted)
public const string ArtistForm = "artist-form";
public const string SongForm   = "song-form";
```

### AppShell registration

```csharp
// Songs is now a FlyoutItem root, not a pushed route
// Keep as pushed route only if Shell navigation requires it for Catalog mode deep-link
Routing.RegisterRoute(Routes.ArtistForm, typeof(ArtistFormPage));
Routing.RegisterRoute(Routes.SongForm,   typeof(SongFormPage));
```

### MauiProgram.cs additions

```csharp
// New: Catalog service + repository
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<ICatalogRepository, CatalogRepository>();

// Updated: ISongService no longer takes artistId parameter on create
// ILyricsProvider: NOT registered — deferred until spike selects provider
```

---

## Key Decisions

| Decision | Choice | Reason |
|----------|--------|--------|
| Song.ArtistId mandatory | `int` NOT NULL | Every song has one original/copyright artist; ownership is non-optional |
| OnDelete for Song.ArtistId | `Restrict` | Cannot delete an artist who owns songs — user must delete their songs first or reassign them |
| Catalog join table | Separate entity named `Catalog` | Reflects ubiquitous language ("catálogo" in Portuguese); clear domain concept |
| OnDelete for Catalog | `Cascade` on both sides | Removing artist or song removes the link only, not the other entity |
| Song title uniqueness | Global unique index on `Title` | Simplest deduplication; avoids cross-artist duplicate management |
| SongsPage dual-mode | Single page, `IsCatalogMode` flag | Avoids a separate CatalogPage; reuses all list/search/selection infrastructure |
| TrailingContent in artist row | `HorizontalStackLayout` inside existing `TrailingContent` slot | ListItem's `TrailingContent` accepts any `View` — no component change needed |
| Artist autocomplete in SongFormPage | Searchable dropdown from registered artists | Song.ArtistId is mandatory; user must select or confirm the original artist before saving |
| Artist field locked after API import | `IsArtistLocked = true` when set from API | Preserves external attribution; prevents accidental mis-attribution after API enrichment |
| ILyricsProvider | Placeholder interface, no DI registration | Provider selection deferred to spike task; interface defines contract now |
| Row tap = selection only | Remove `OnItemTapped` navigation logic | Navigation via dedicated catalog icon button; consistent with MD3 list selection pattern |

---

## Future Architecture

Brief notes for future specs. Do not implement any of these in this spec.

### ArtistMember join table

`ArtistMember(artistId int FK, personId int FK, composite PK)` — links an Artist to one or more
`Person` records. Enables singer-as-performer identity sharing and future device P2P catalog sync
keyed on Person identity. Will require a new migration and updates to `AppDbContext`,
`ArtistConfiguration`, and `IArtistRepository`.

### YouTube integration

`Song` will need a `YouTubeUrl string?` column (or a separate `SongMedia` entity if multiple media
sources per song are required). The app will reference the URL to launch or project the karaoke
video. The existing `Lyrics` field is Bandokê-mode-specific; YouTube mode does not use app-side
lyrics.

### AI catalog import pipeline

Requires: (1) a file upload service accepting TXT/XLS/DOC/PDF; (2) an AI parsing agent that
extracts artist names and song titles from the file content; (3) a batch-create service method
that resolves or creates Author and Song records and adds them to the Performer's Catalog;
(4) a review/confirmation UI step before records are committed. The AI agent is embedded — no
external SaaS dependency at this stage.
