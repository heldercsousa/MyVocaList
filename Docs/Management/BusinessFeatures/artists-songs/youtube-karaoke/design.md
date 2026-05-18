# YouTube Karaoke — Technical Design

> **Status:** Spec in progress — brainstorm complete 2026-05-17

---

## Architecture

| Layer | Artefacts |
|-------|-----------|
| Domain | `SongKaraokeUrl` · `ISongKaraokeUrlRepository` |
| Contracts | `SongKaraokeUrlDto` · `YouTubeSearchResultDto` |
| Infra | `SongKaraokeUrlRepository` · `SongKaraokeUrlConfiguration` · `YouTubeSearchService` · `YouTubeOEmbedService` |
| Services | `ISongKaraokeUrlService` · `SongKaraokeUrlService` · `INextSingerAlertService` · `NextSingerAlertService` |
| MAUI | `SongFormViewModel` (extended) · `SongFormPage` (extended) · `OverlayService` (Android only) · `SettingsPage` (extended) |

---

## Domain Layer

### SongKaraokeUrl entity

```csharp
public class SongKaraokeUrl
{
    public string VideoId { get; set; }      // PK — 11-char YouTube video ID
    public int SongId { get; set; }          // FK → Song.Id
    public int PlayCount { get; set; }
    public int? DurationSeconds { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime AddedAt { get; set; }
    public string? Label { get; set; }

    public Song Song { get; set; }
}
```

### ISongKaraokeUrlRepository

```csharp
public interface ISongKaraokeUrlRepository
{
    Task<List<SongKaraokeUrl>> GetBySongIdAsync(int songId, CancellationToken ct = default);
    Task<SongKaraokeUrl?> GetSuggestedAsync(int songId, CancellationToken ct = default);
    Task<bool> ExistsAsync(int songId, string videoId, CancellationToken ct = default);
    Task AddAsync(SongKaraokeUrl url, CancellationToken ct = default);
    Task RemoveAsync(int songId, string videoId, CancellationToken ct = default);
    Task IncrementPlayCountAsync(int songId, string videoId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

---

## Contracts Layer

```csharp
public record SongKaraokeUrlDto(
    string VideoId,
    int SongId,
    int PlayCount,
    int? DurationSeconds,
    DateTime? LastUsedAt,
    DateTime AddedAt,
    string? Label,
    bool IsSuggested);   // computed — true when this is the top-ranked URL for the song

public record YouTubeSearchResultDto(
    string VideoId,
    string Title,
    string ChannelName,
    int? DurationSeconds,
    string ThumbnailUrl);  // https://img.youtube.com/vi/{videoId}/mqdefault.jpg
```

---

## Infrastructure Layer

### SongKaraokeUrlConfiguration

```csharp
public class SongKaraokeUrlConfiguration : IEntityTypeConfiguration<SongKaraokeUrl>
{
    public void Configure(EntityTypeBuilder<SongKaraokeUrl> builder)
    {
        builder.HasKey(u => new { u.SongId, u.VideoId });

        builder.Property(u => u.VideoId).HasColumnType("TEXT").IsRequired().HasMaxLength(11);
        builder.Property(u => u.SongId).IsRequired();
        builder.Property(u => u.PlayCount).IsRequired().HasDefaultValue(0);
        builder.Property(u => u.DurationSeconds).IsRequired(false);
        builder.Property(u => u.LastUsedAt).IsRequired(false);
        builder.Property(u => u.AddedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(u => u.Label).HasColumnType("TEXT").IsRequired(false).HasMaxLength(100);

        builder.HasOne(u => u.Song)
               .WithMany()
               .HasForeignKey(u => u.SongId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("SongKaraokeUrls");
    }
}
```

### YouTubeOEmbedService

Fetches video duration via the free oEmbed endpoint — no API key required.

```csharp
public interface IYouTubeOEmbedService
{
    Task<int?> GetDurationSecondsAsync(string videoId, CancellationToken ct = default);
}

// Implementation: GET https://www.youtube.com/oembed?url=https://youtu.be/{videoId}&format=json
// Parses response JSON — note: oEmbed does NOT return duration directly.
// Falls back to YouTube Data API if key is available, otherwise returns null.
```

> **Note:** YouTube oEmbed does not reliably return duration. `DurationSeconds` will be populated
> via YouTube Data API v3 when a key is available, or remain null otherwise.

### YouTubeSearchService

```csharp
public interface IYouTubeSearchService
{
    /// <summary>Returns up to 5 results. Returns empty if no API key is configured.</summary>
    Task<IEnumerable<YouTubeSearchResultDto>> SearchAsync(string query, CancellationToken ct = default);

    Task<bool> ValidateApiKeyAsync(string apiKey, CancellationToken ct = default);
}
// GET https://www.googleapis.com/youtube/v3/search?part=snippet&type=video&maxResults=5&q={query}&key={apiKey}
// Duration fetched via a second call: videos?part=contentDetails&id={ids}&key={apiKey}
```

---

## Services Layer

### ISongKaraokeUrlService

```csharp
public interface ISongKaraokeUrlService
{
    Task<List<SongKaraokeUrlDto>> GetUrlsForSongAsync(int songId, CancellationToken ct = default);

    /// <summary>Normalises any YouTube URL format to an 11-char video ID before saving.</summary>
    Task<(bool success, string message, SongKaraokeUrlDto? url)> AddUrlAsync(
        int songId, string rawUrl, string? label = null, CancellationToken ct = default);

    Task<(bool success, string message)> RemoveUrlAsync(
        int songId, string videoId, CancellationToken ct = default);

    /// <summary>Increments PlayCount and sets LastUsedAt. Called by queue flow on confirmed launch.</summary>
    Task RecordPlayAsync(int songId, string videoId, CancellationToken ct = default);

    Task<SongKaraokeUrlDto?> GetSuggestedUrlAsync(int songId, CancellationToken ct = default);

    /// <summary>Returns null if input cannot be parsed as a YouTube URL.</summary>
    string? ExtractVideoId(string rawUrl);
}
```

### INextSingerAlertService

Cross-platform service. On Android, also manages the overlay via `IOverlayService`.

```csharp
public interface INextSingerAlertService
{
    /// <summary>
    /// Schedules Stage 1 (T-45s) and Stage 2 (T-15s) local notifications.
    /// No-op if durationSeconds is null.
    /// </summary>
    Task ScheduleAlertsAsync(
        string singerName,
        string songTitle,
        int? durationSeconds,
        CancellationToken ct = default);

    Task CancelAlertsAsync(CancellationToken ct = default);
}
```

### URL normalisation — ExtractVideoId

Handles all YouTube URL formats:

| Input | Extracted ID |
|-------|-------------|
| `https://www.youtube.com/watch?v=dQw4w9WgXcQ` | `dQw4w9WgXcQ` |
| `https://youtu.be/dQw4w9WgXcQ` | `dQw4w9WgXcQ` |
| `https://www.youtube.com/embed/dQw4w9WgXcQ` | `dQw4w9WgXcQ` |
| `https://youtube.com/shorts/dQw4w9WgXcQ` | `dQw4w9WgXcQ` |
| Anything else | `null` |

Regex: `[A-Za-z0-9_-]{11}` extracted from the appropriate URL segment.

---

## MAUI Layer

### SongFormViewModel (additions)

```csharp
// YouTube URLs section
[ObservableProperty] ObservableRangeCollection<SongKaraokeUrlDto> _karaokeUrls;
[ObservableProperty] string _youtubeSearchQuery;
[ObservableProperty] ObservableRangeCollection<YouTubeSearchResultDto> _searchResults;
[ObservableProperty] bool _isSearching;
[ObservableProperty] string _searchStatusMessage;
[ObservableProperty] bool _hasApiKey;           // drives search strip vs nudge message
[ObservableProperty] string _pasteUrlInput;
[ObservableProperty] string _pasteUrlError;

// Commands
SearchYouTubeCommand       // fires YouTubeSearchService.SearchAsync
AddFromSearchCommand(YouTubeSearchResultDto)
AddFromPasteCommand
RemoveUrlCommand(SongKaraokeUrlDto)  // with undo snackbar
```

### OverlayService (Android only — platform project)

```csharp
// MyVocaList.Platforms.Android/Services/OverlayService.cs
public interface IOverlayService
{
    bool IsPermissionGranted { get; }
    void RequestPermission();           // opens Settings → Manage display over other apps
    void Show(string singerName, string songTitle, OverlayStage stage);
    void UpdateStage(OverlayStage stage);
    void Dismiss();
}

public enum OverlayStage { Stage1, Stage2 }
```

**Implementation constraints:**
- Uses `WindowManager.AddView` with `TYPE_APPLICATION_OVERLAY`
- View contains a single `TextView` — no background, text shadow for legibility
- Animation via `ObjectAnimator.ofFloat(view, "alpha", ...)` — platform-native, GPU-accelerated
- Runs as `ForegroundService` with a persistent low-priority notification (required by Android 8.0+)
- Touch events pass through (`FLAG_NOT_TOUCHABLE` except for the drag handle)
- Tapping the label calls `StartActivity` to bring MyVocaList to the foreground

### SongFormPage (additions)

New section appended after the Lyrics field:

```xml
<!-- YouTube URLs section header — no button -->
<HStack>
    <Image Source="youtube_icon" ... />
    <Label Text="YouTube URLs" />
    <Label Text="optional" Opacity="0.4" />
</HStack>

<!-- Saved URL rows (DXCollectionView) -->
<dx:DXCollectionView ItemsSource="{Binding KaraokeUrls}" ...>
    <!-- Row: VideoId, PlayCount, DurationSeconds, Label, IsSuggested badge, trailing ✕ button -->
</dx:DXCollectionView>

<!-- Search strip -->
<dx:DXBorder ...>
    <!-- Query input + search button (hidden/replaced by nudge when no API key) -->
    <!-- Search results (DXCollectionView, max 5 rows, trailing + button) -->
    <!-- Paste field (always visible) -->
</dx:DXBorder>
```

---

## Interaction Flows

### Admin adds a URL via search

1. Admin opens Song edit form
2. Scrolls to YouTube URLs section
3. Taps search input — pre-filled with "{Artist} {Title} karaoke"
4. Taps ▶ → `SearchYouTubeCommand` → results appear inline
5. Taps `+` on a result → `AddFromSearchCommand` → URL saved, row gains ✓, URL appears in saved list above
6. `DurationSeconds` auto-populated from API response
7. Admin saves song → `SongKaraokeUrlService.AddUrlAsync` called for each pending URL

### Admin adds a URL via paste

1. Admin pastes any YouTube URL into paste field
2. `AddFromPasteCommand` → `ExtractVideoId` → validates format
3. If valid: URL saved, appears in saved list; `DurationSeconds` attempted via oEmbed/API
4. If invalid: inline error "Not a valid YouTube URL."

### Video launched from queue (future spec integration point)

1. Queue page calls `ISongKaraokeUrlService.GetSuggestedUrlAsync(songId)`
2. If single URL or no choice needed: `Launcher.OpenAsync($"https://youtu.be/{videoId}")`
3. If multiple URLs: bottom sheet lists them; admin picks
4. `RecordPlayAsync(songId, videoId)` increments `PlayCount` + sets `LastUsedAt`
5. `INextSingerAlertService.ScheduleAlertsAsync(nextSingerName, nextSongTitle, durationSeconds)`
6. On Android (permission granted): `IOverlayService.Show(nextSingerName, nextSongTitle, Stage1)`

---

## Alert Timing Logic

```
songStartTime = DateTime.UtcNow  (recorded when Launcher.OpenAsync is called)

stage1FireAt = songStartTime + (durationSeconds - 45) seconds
stage2FireAt = songStartTime + (durationSeconds - 15) seconds

If durationSeconds <= 45 → skip Stage 1, fire Stage 2 only at T-15s
If durationSeconds <= 15 → skip both; log warning
```

---

## Navigation & DI Registration

### New routes

None. YouTube URL management lives within the existing `SongFormPage`.
The Settings page extension uses the existing Settings route.

### MauiProgram.cs additions

```csharp
builder.Services.AddScoped<ISongKaraokeUrlService, SongKaraokeUrlService>();
builder.Services.AddScoped<ISongKaraokeUrlRepository, SongKaraokeUrlRepository>();
builder.Services.AddScoped<IYouTubeSearchService, YouTubeSearchService>();
builder.Services.AddScoped<INextSingerAlertService, NextSingerAlertService>();

#if ANDROID
builder.Services.AddSingleton<IOverlayService, OverlayService>();
#else
builder.Services.AddSingleton<IOverlayService, NoOpOverlayService>();
#endif
```

---

## Key Decisions

| Decision | Choice | Reason |
|----------|--------|--------|
| `VideoId` as PK | Composite `(SongId, VideoId)` | Natural key; prevents duplicate per song without surrogate |
| Store video ID only | `dQw4w9WgXcQ` not full URL | Compact; reconstruct URL at runtime; format-agnostic |
| oEmbed for duration | Falls back to null | Free, no key — but doesn't reliably return duration; Data API used when key available |
| Overlay animation | Android `ObjectAnimator` | Platform-native GPU animation; does not compete with MAUI render thread |
| iOS alert | Local notifications only | iOS sandbox forbids overlay over other apps; notifications are the universal baseline |
| `NoOpOverlayService` on iOS | Registered in DI, does nothing | Allows `IOverlayService` to be injected without platform `#if` in ViewModels |
| API key in SecureStorage | `SecureStorage.SetAsync` | Prevents key exposure in app data backups |
| YouTube search optional | User supplies own key | Avoids quota dependency; paste always works without any key |
