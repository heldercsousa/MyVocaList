# YouTube Karaoke Mode — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement YouTube URL management per song, in-app YouTube search, play-count-based URL suggestion, local notification alerts (2-stage), and Android overlay for next-singer awareness.

**Architecture:** New `SongKaraokeUrl` entity with composite PK `(SongId, VideoId)` extends the existing Song domain without modifying `Song.cs`. Services layer owns all URL normalisation, duplicate detection, and alert scheduling. Android overlay runs as a foreground service via `WindowManager`; iOS uses notifications only.

**Tech Stack:** .NET MAUI 10 · EF Core 10 + SQLite · CommunityToolkit.Mvvm · DevExpress MAUI v25.2.4 · Plugin.LocalNotification · SecureStorage · Android WindowManager / ObjectAnimator

---

## File Map

| File | Action | Responsibility |
|------|--------|---------------|
| `Domain/Entity/SongKaraokeUrl.cs` | Create | Entity with composite PK fields |
| `Domain/RepositoryInterface/ISongKaraokeUrlRepository.cs` | Create | Repository contract |
| `Domain/ServicesInterfaces/ISongKaraokeUrlService.cs` | Create | Service contract including `ExtractVideoId` |
| `Domain/ServicesInterfaces/INextSingerAlertService.cs` | Create | Alert scheduling contract |
| `Domain/ServicesInterfaces/IOverlayService.cs` | Create | Overlay contract (cross-platform) |
| `Contracts/DTOs/List/SongKaraokeUrlDto.cs` | Create | DTO with `IsSuggested` flag |
| `Contracts/DTOs/List/YouTubeSearchResultDto.cs` | Create | YouTube search result DTO |
| `Infra/EntityEFConfig/SongKaraokeUrlConfiguration.cs` | Create | EF Core composite PK, cascade delete |
| `Infra/AppDbContext.cs` | Modify | Add `DbSet<SongKaraokeUrl>` + apply configuration |
| `Infra/Migrations/*_AddSongKaraokeUrls.cs` | Create | EF migration |
| `Infra/Repository/SongKaraokeUrlRepository.cs` | Create | Repository implementation |
| `Services/SongKaraokeUrlService.cs` | Create | URL normalisation + CRUD service |
| `Services/YouTubeSearchService.cs` | Create | YouTube Data API v3 wrapper |
| `Services/NextSingerAlertService.cs` | Create | Local notification scheduling |
| `MyVocaList/Platforms/Android/Services/OverlayService.cs` | Create | Android overlay (ForegroundService + WindowManager) |
| `MyVocaList/Services/NoOpOverlayService.cs` | Create | iOS/Windows no-op |
| `MyVocaList/UI/ViewModels/SongFormViewModel.cs` | Modify | Add YouTube URL section observables + commands |
| `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` | Modify | Add YouTube URLs section after Lyrics |
| `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` | Modify | Wire up URL section lifecycle |
| `MyVocaList/MauiProgram.cs` | Modify | DI registration for new services |
| `MyVocaList.Tests/Unit/Services/SongKaraokeUrlServiceTests.cs` | Create | Unit tests for service layer |
| `MyVocaList.Tests/Unit/Services/NextSingerAlertServiceTests.cs` | Create | Unit tests for alert scheduling |
| `MyVocaList.Tests/Integration/Repositories/SongKaraokeUrlRepositoryTests.cs` | Create | Integration tests for repository |

---

## Phase 1 — Domain Layer

### Task 1: SongKaraokeUrl entity

**Files:**
- Create: `Domain/Entity/SongKaraokeUrl.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// MyVocaList.Tests/Unit/Domain/SongKaraokeUrlEntityTests.cs
[Fact]
public void SongKaraokeUrl_DefaultPlayCount_IsZero()
{
    var url = new SongKaraokeUrl { VideoId = "dQw4w9WgXcQ", SongId = 1 };
    Assert.Equal(0, url.PlayCount);
}
```

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "SongKaraokeUrlEntityTests"`
Expected: FAIL — `SongKaraokeUrl` not found

- [ ] **Step 2: Create the entity**

```csharp
// Domain/Entity/SongKaraokeUrl.cs
namespace MyVocaList.Domain.Entity;

public class SongKaraokeUrl
{
    public string VideoId { get; set; }
    public int SongId { get; set; }
    public int PlayCount { get; set; }
    public int? DurationSeconds { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime AddedAt { get; set; }
    public string? Label { get; set; }

    public Song Song { get; set; }
}
```

- [ ] **Step 3: Run tests, confirm Green**

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "SongKaraokeUrlEntityTests"`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add Domain/Entity/SongKaraokeUrl.cs MyVocaList.Tests/Unit/Domain/SongKaraokeUrlEntityTests.cs
git commit -m "feat: add SongKaraokeUrl entity"
```

---

### Task 2: ISongKaraokeUrlRepository

**Files:**
- Create: `Domain/RepositoryInterface/ISongKaraokeUrlRepository.cs`

- [ ] **Step 1: Create the interface**

```csharp
// Domain/RepositoryInterface/ISongKaraokeUrlRepository.cs
using MyVocaList.Domain.Entity;

namespace MyVocaList.Domain.RepositoryInterface;

public interface ISongKaraokeUrlRepository
{
    /// <summary>Returns all URLs for a song, ordered: PlayCount DESC, LastUsedAt DESC, AddedAt DESC.</summary>
    Task<List<SongKaraokeUrl>> GetBySongIdAsync(int songId, CancellationToken ct = default);

    /// <summary>Returns the top-ranked URL (play count → recency), or null if none exist.</summary>
    Task<SongKaraokeUrl?> GetSuggestedAsync(int songId, CancellationToken ct = default);

    /// <summary>Returns true if the video ID is already associated with the song.</summary>
    Task<bool> ExistsAsync(int songId, string videoId, CancellationToken ct = default);

    Task AddAsync(SongKaraokeUrl url, CancellationToken ct = default);
    Task RemoveAsync(int songId, string videoId, CancellationToken ct = default);

    /// <summary>Increments PlayCount by 1 and sets LastUsedAt = UtcNow for the given (songId, videoId).</summary>
    Task IncrementPlayCountAsync(int songId, string videoId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: Build**

Run: `dotnet build MyVocaList.Domain/MyVocaList.Domain.csproj`
Expected: 0 errors

- [ ] **Step 3: Commit**

```bash
git add Domain/RepositoryInterface/ISongKaraokeUrlRepository.cs
git commit -m "feat: add ISongKaraokeUrlRepository interface"
```

---

### Task 3: ISongKaraokeUrlService and INextSingerAlertService and IOverlayService

**Files:**
- Create: `Domain/ServicesInterfaces/ISongKaraokeUrlService.cs`
- Create: `Domain/ServicesInterfaces/INextSingerAlertService.cs`
- Create: `Domain/ServicesInterfaces/IOverlayService.cs`

- [ ] **Step 1: Create ISongKaraokeUrlService**

```csharp
// Domain/ServicesInterfaces/ISongKaraokeUrlService.cs
using MyVocaList.Contracts.DTOs.List;

namespace MyVocaList.Domain.ServicesInterfaces;

public interface ISongKaraokeUrlService
{
    /// <summary>Returns all URLs for the song, marked IsSuggested on the top-ranked one.</summary>
    Task<List<SongKaraokeUrlDto>> GetUrlsForSongAsync(int songId, CancellationToken ct = default);

    /// <summary>
    /// Normalises rawUrl to a video ID, validates it, checks for duplicates, persists.
    /// Returns (false, "This URL is already saved for this song.") on duplicate.
    /// Returns (false, "Not a valid YouTube URL.") on parse failure.
    /// </summary>
    Task<(bool success, string message, SongKaraokeUrlDto? url)> AddUrlAsync(
        int songId, string rawUrl, string? label = null, CancellationToken ct = default);

    Task<(bool success, string message)> RemoveUrlAsync(
        int songId, string videoId, CancellationToken ct = default);

    /// <summary>Increments PlayCount and sets LastUsedAt. Called on confirmed launch from queue.</summary>
    Task RecordPlayAsync(int songId, string videoId, CancellationToken ct = default);

    Task<SongKaraokeUrlDto?> GetSuggestedUrlAsync(int songId, CancellationToken ct = default);

    /// <summary>
    /// Extracts the 11-char video ID from any YouTube URL format.
    /// Accepts: watch?v=, youtu.be/, /embed/, /shorts/
    /// Returns null if the input cannot be parsed.
    /// </summary>
    string? ExtractVideoId(string rawUrl);
}
```

- [ ] **Step 2: Create INextSingerAlertService**

```csharp
// Domain/ServicesInterfaces/INextSingerAlertService.cs
namespace MyVocaList.Domain.ServicesInterfaces;

public interface INextSingerAlertService
{
    /// <summary>
    /// Schedules Stage 1 (T-45s) and Stage 2 (T-15s) local notifications.
    /// No-op when durationSeconds is null or too short.
    /// </summary>
    Task ScheduleAlertsAsync(
        string singerName,
        string songTitle,
        int? durationSeconds,
        CancellationToken ct = default);

    /// <summary>Cancels any pending Stage 1 and Stage 2 notifications.</summary>
    Task CancelAlertsAsync(CancellationToken ct = default);
}
```

- [ ] **Step 3: Create IOverlayService**

```csharp
// Domain/ServicesInterfaces/IOverlayService.cs
namespace MyVocaList.Domain.ServicesInterfaces;

public enum OverlayStage { Stage1, Stage2 }

public interface IOverlayService
{
    bool IsPermissionGranted { get; }

    /// <summary>Opens Android Settings → "Display over other apps". No-op on iOS.</summary>
    void RequestPermission();

    /// <summary>Shows or updates the floating label. No-op on iOS.</summary>
    void Show(string singerName, string songTitle, OverlayStage stage);

    void UpdateStage(OverlayStage stage);

    /// <summary>Hides and destroys the overlay view.</summary>
    void Dismiss();
}
```

- [ ] **Step 4: Build**

Run: `dotnet build MyVocaList.Domain/MyVocaList.Domain.csproj`
Expected: 0 errors

- [ ] **Step 5: Commit**

```bash
git add Domain/ServicesInterfaces/ISongKaraokeUrlService.cs Domain/ServicesInterfaces/INextSingerAlertService.cs Domain/ServicesInterfaces/IOverlayService.cs
git commit -m "feat: add ISongKaraokeUrlService, INextSingerAlertService, IOverlayService interfaces"
```

---

## Phase 2 — Contracts + Infrastructure

### Task 4: DTOs

**Files:**
- Create: `Contracts/DTOs/List/SongKaraokeUrlDto.cs`
- Create: `Contracts/DTOs/List/YouTubeSearchResultDto.cs`

- [ ] **Step 1: Create DTOs**

```csharp
// Contracts/DTOs/List/SongKaraokeUrlDto.cs
namespace MyVocaList.Contracts.DTOs.List;

public record SongKaraokeUrlDto(
    string VideoId,
    int SongId,
    int PlayCount,
    int? DurationSeconds,
    DateTime? LastUsedAt,
    DateTime AddedAt,
    string? Label,
    bool IsSuggested);
```

```csharp
// Contracts/DTOs/List/YouTubeSearchResultDto.cs
namespace MyVocaList.Contracts.DTOs.List;

public record YouTubeSearchResultDto(
    string VideoId,
    string Title,
    string ChannelName,
    int? DurationSeconds,
    string ThumbnailUrl);
```

- [ ] **Step 2: Build**

Run: `dotnet build MyVocaList.Contracts/MyVocaList.Contracts.csproj`
Expected: 0 errors

- [ ] **Step 3: Commit**

```bash
git add Contracts/DTOs/List/SongKaraokeUrlDto.cs Contracts/DTOs/List/YouTubeSearchResultDto.cs
git commit -m "feat: add SongKaraokeUrlDto and YouTubeSearchResultDto"
```

---

### Task 5: EF Core configuration + DbContext + migration

**Files:**
- Create: `Infra/EntityEFConfig/SongKaraokeUrlConfiguration.cs`
- Modify: `Infra/AppDbContext.cs`
- Create: EF migration (generated)

- [ ] **Step 1: Create SongKaraokeUrlConfiguration**

```csharp
// Infra/EntityEFConfig/SongKaraokeUrlConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyVocaList.Domain.Entity;

namespace MyVocaList.Infra.EntityEFConfig;

public class SongKaraokeUrlConfiguration : IEntityTypeConfiguration<SongKaraokeUrl>
{
    public void Configure(EntityTypeBuilder<SongKaraokeUrl> builder)
    {
        builder.ToTable("SongKaraokeUrls");
        builder.HasKey(u => new { u.SongId, u.VideoId });

        builder.Property(u => u.VideoId)
               .HasColumnType("TEXT")
               .IsRequired()
               .HasMaxLength(11);

        builder.Property(u => u.SongId).IsRequired();

        builder.Property(u => u.PlayCount)
               .IsRequired()
               .HasDefaultValue(0);

        builder.Property(u => u.DurationSeconds).IsRequired(false);
        builder.Property(u => u.LastUsedAt).IsRequired(false);

        builder.Property(u => u.AddedAt)
               .IsRequired()
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(u => u.Label)
               .HasColumnType("TEXT")
               .IsRequired(false)
               .HasMaxLength(100);

        builder.HasOne(u => u.Song)
               .WithMany()
               .HasForeignKey(u => u.SongId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 2: Add DbSet and apply configuration in AppDbContext.cs**

In `Infra/AppDbContext.cs`, add after the `Catalog` DbSet:

```csharp
public DbSet<SongKaraokeUrl> SongKaraokeUrls { get; set; }
```

In `OnModelCreating`, after `modelBuilder.ApplyConfiguration(new CatalogConfiguration());` add:

```csharp
modelBuilder.ApplyConfiguration(new SongKaraokeUrlConfiguration());
```

- [ ] **Step 3: Build to verify no compile errors**

Run: `dotnet build MyVocaList.Infra/MyVocaList.Infra.csproj`
Expected: 0 errors

- [ ] **Step 4: Add EF Core migration**

Run from solution root:
```bash
dotnet ef migrations add AddSongKaraokeUrls --project MyVocaList.Infra/MyVocaList.Infra.csproj --startup-project MyVocaList.Infra/MyVocaList.Infra.csproj
```

Expected: Creates `Infra/Migrations/*_AddSongKaraokeUrls.cs` with `SongKaraokeUrls` table.

Verify the migration file contains:
- `CreateTable("SongKaraokeUrls", ...)`
- `PrimaryKey("PK_SongKaraokeUrls", x => new { x.SongId, x.VideoId })`
- `AddForeignKey("FK_SongKaraokeUrls_Songs_SongId", onDelete: ReferentialAction.Cascade)`

- [ ] **Step 5: Commit**

```bash
git add Infra/EntityEFConfig/SongKaraokeUrlConfiguration.cs Infra/AppDbContext.cs Infra/Migrations/
git commit -m "feat: EF Core migration AddSongKaraokeUrls — composite PK (SongId, VideoId), cascade delete"
```

---

### Task 6: SongKaraokeUrlRepository

**Files:**
- Create: `Infra/Repository/SongKaraokeUrlRepository.cs`

- [ ] **Step 1: Write failing integration tests**

```csharp
// MyVocaList.Tests/Integration/Repositories/SongKaraokeUrlRepositoryTests.cs
public class SongKaraokeUrlRepositoryTests : IAsyncLifetime
{
    private AppDbContext _db;
    private SongKaraokeUrlRepository _repo;

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.EnsureCreatedAsync();
        _repo = new SongKaraokeUrlRepository(_db);

        // Seed a Song and Artist so FK constraint is satisfied
        var artist = new Artist { Name = "Test Artist", NameNormalized = "test artist" };
        _db.Artists.Add(artist);
        await _db.SaveChangesAsync();
        var song = new Song
        {
            ArtistId = artist.Id, Title = "Test Song", TitleNormalized = "test song",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Songs.Add(song);
        await _db.SaveChangesAsync();
        SongId = song.Id;
    }

    public int SongId { get; private set; }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    [Fact]
    // [AC] AC-1.3: saved URLs appear in list
    public async Task GetBySongIdAsync_AfterAdd_ReturnsUrl()
    {
        var url = new SongKaraokeUrl
            { SongId = SongId, VideoId = "dQw4w9WgXcQ", AddedAt = DateTime.UtcNow };
        await _repo.AddAsync(url);
        await _repo.SaveChangesAsync();

        var list = await _repo.GetBySongIdAsync(SongId);

        Assert.Single(list);
        Assert.Equal("dQw4w9WgXcQ", list[0].VideoId);
    }

    [Fact]
    // [AC] AC-1.9: duplicate video ID per song rejected
    public async Task AddAsync_DuplicateVideoId_ThrowsDbUpdateException()
    {
        _db.SongKaraokeUrls.Add(new SongKaraokeUrl
            { SongId = SongId, VideoId = "dQw4w9WgXcQ", AddedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        _db.SongKaraokeUrls.Add(new SongKaraokeUrl
            { SongId = SongId, VideoId = "dQw4w9WgXcQ", AddedAt = DateTime.UtcNow });

        await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(
            () => _db.SaveChangesAsync());
    }

    [Fact]
    // [AC] AC-1.4: highest play count is suggested
    public async Task GetSuggestedAsync_ReturnsHighestPlayCount()
    {
        _db.SongKaraokeUrls.AddRange(
            new SongKaraokeUrl { SongId = SongId, VideoId = "aaaaaaaaaaa", PlayCount = 1, AddedAt = DateTime.UtcNow },
            new SongKaraokeUrl { SongId = SongId, VideoId = "bbbbbbbbbbb", PlayCount = 5, AddedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var suggested = await _repo.GetSuggestedAsync(SongId);

        Assert.NotNull(suggested);
        Assert.Equal("bbbbbbbbbbb", suggested!.VideoId);
    }

    [Fact]
    // [AC] AC-1.5: remove URL removes it from list
    public async Task RemoveAsync_RemovesFromList()
    {
        _db.SongKaraokeUrls.Add(new SongKaraokeUrl
            { SongId = SongId, VideoId = "dQw4w9WgXcQ", AddedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        await _repo.RemoveAsync(SongId, "dQw4w9WgXcQ");
        await _repo.SaveChangesAsync();

        var list = await _repo.GetBySongIdAsync(SongId);
        Assert.Empty(list);
    }

    [Fact]
    // [AC] AC-3.4: PlayCount incremented on confirmed launch
    public async Task IncrementPlayCountAsync_IncrementsByOne()
    {
        _db.SongKaraokeUrls.Add(new SongKaraokeUrl
            { SongId = SongId, VideoId = "dQw4w9WgXcQ", PlayCount = 2, AddedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        await _repo.IncrementPlayCountAsync(SongId, "dQw4w9WgXcQ");
        await _repo.SaveChangesAsync();

        // Re-query to verify persisted value
        var reloaded = await _repo.GetBySongIdAsync(SongId);
        Assert.Equal(3, reloaded[0].PlayCount);
    }

    [Fact]
    public async Task CascadeDelete_WhenSongDeleted_UrlsAreRemoved()
    {
        _db.SongKaraokeUrls.Add(new SongKaraokeUrl
            { SongId = SongId, VideoId = "dQw4w9WgXcQ", AddedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        _db.Songs.Remove(await _db.Songs.FindAsync(SongId));
        await _db.SaveChangesAsync();

        var list = await _repo.GetBySongIdAsync(SongId);
        Assert.Empty(list);
    }
}
```

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "SongKaraokeUrlRepositoryTests"`
Expected: FAIL — `SongKaraokeUrlRepository` not found

- [ ] **Step 2: Implement SongKaraokeUrlRepository**

```csharp
// Infra/Repository/SongKaraokeUrlRepository.cs
using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;

namespace MyVocaList.Infra.Repository;

/// <inheritdoc />
public class SongKaraokeUrlRepository : ISongKaraokeUrlRepository
{
    private readonly AppDbContext _db;

    public SongKaraokeUrlRepository(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public Task<List<SongKaraokeUrl>> GetBySongIdAsync(int songId, CancellationToken ct = default)
        => _db.SongKaraokeUrls
              .Where(u => u.SongId == songId)
              .OrderByDescending(u => u.PlayCount)
              .ThenByDescending(u => u.LastUsedAt)
              .ThenByDescending(u => u.AddedAt)
              .ToListAsync(ct);

    /// <inheritdoc />
    public Task<SongKaraokeUrl?> GetSuggestedAsync(int songId, CancellationToken ct = default)
        => _db.SongKaraokeUrls
              .Where(u => u.SongId == songId)
              .OrderByDescending(u => u.PlayCount)
              .ThenByDescending(u => u.LastUsedAt)
              .ThenByDescending(u => u.AddedAt)
              .FirstOrDefaultAsync(ct);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(int songId, string videoId, CancellationToken ct = default)
        => _db.SongKaraokeUrls
              .AnyAsync(u => u.SongId == songId && u.VideoId == videoId, ct);

    /// <inheritdoc />
    public Task AddAsync(SongKaraokeUrl url, CancellationToken ct = default)
    {
        _db.SongKaraokeUrls.Add(url);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task RemoveAsync(int songId, string videoId, CancellationToken ct = default)
    {
        await _db.SongKaraokeUrls
                 .Where(u => u.SongId == songId && u.VideoId == videoId)
                 .ExecuteDeleteAsync(ct);
    }

    /// <inheritdoc />
    public async Task IncrementPlayCountAsync(int songId, string videoId, CancellationToken ct = default)
    {
        await _db.SongKaraokeUrls
                 .Where(u => u.SongId == songId && u.VideoId == videoId)
                 .ExecuteUpdateAsync(s => s
                     .SetProperty(u => u.PlayCount, u => u.PlayCount + 1)
                     .SetProperty(u => u.LastUsedAt, _ => DateTime.UtcNow),
                     ct);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
```

- [ ] **Step 3: Run tests, confirm Green**

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "SongKaraokeUrlRepositoryTests"`
Expected: All PASS

- [ ] **Step 4: Build full solution**

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`
Expected: 0 errors

- [ ] **Step 5: Commit**

```bash
git add Infra/Repository/SongKaraokeUrlRepository.cs MyVocaList.Tests/Integration/Repositories/SongKaraokeUrlRepositoryTests.cs
git commit -m "feat: SongKaraokeUrlRepository — CRUD, GetSuggestedAsync, IncrementPlayCountAsync; integration tests Green"
```

---

## Phase 3 — Services Layer

### Task 7: SongKaraokeUrlService

**Files:**
- Create: `Services/SongKaraokeUrlService.cs`

- [ ] **Step 1: Write failing unit tests**

```csharp
// MyVocaList.Tests/Unit/Services/SongKaraokeUrlServiceTests.cs
public class SongKaraokeUrlServiceTests
{
    private readonly Mock<ISongKaraokeUrlRepository> _repoMock = new();
    private readonly Mock<ILogger<SongKaraokeUrlService>> _loggerMock = new();

    private SongKaraokeUrlService CreateSut() =>
        new(_repoMock.Object, _loggerMock.Object);

    // ── ExtractVideoId ───────────────────────────────────────────────────────

    [Theory]
    // [AC] AC-2.6: accepts all 4 YouTube URL formats
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/shorts/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    public void ExtractVideoId_ValidFormats_ReturnsId(string url, string expectedId)
    {
        var sut = CreateSut();
        Assert.Equal(expectedId, sut.ExtractVideoId(url));
    }

    [Theory]
    // [AC] AC-2.7: invalid URLs return null
    [InlineData("https://vimeo.com/12345")]
    [InlineData("not-a-url")]
    [InlineData("")]
    [InlineData(null)]
    public void ExtractVideoId_InvalidFormats_ReturnsNull(string? url)
    {
        var sut = CreateSut();
        Assert.Null(sut.ExtractVideoId(url!));
    }

    // ── AddUrlAsync ──────────────────────────────────────────────────────────

    [Fact]
    // [AC] AC-1.9: duplicate video ID per song is rejected
    public async Task AddUrlAsync_DuplicateVideoId_ReturnsFalse()
    {
        _repoMock.Setup(r => r.ExistsAsync(1, "dQw4w9WgXcQ", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        var sut = CreateSut();

        var (success, message, dto) = await sut.AddUrlAsync(1, "https://youtu.be/dQw4w9WgXcQ");

        Assert.False(success);
        Assert.Contains("already saved", message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(dto);
    }

    [Fact]
    // [AC] AC-2.7: invalid URL format returns error
    public async Task AddUrlAsync_InvalidUrl_ReturnsFalse()
    {
        var sut = CreateSut();
        var (success, message, dto) = await sut.AddUrlAsync(1, "https://vimeo.com/12345");

        Assert.False(success);
        Assert.Contains("valid YouTube URL", message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(dto);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<SongKaraokeUrl>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    // [AC] AC-2.4: valid URL is saved and returned
    public async Task AddUrlAsync_ValidUrl_PersistsAndReturnsDto()
    {
        _repoMock.Setup(r => r.ExistsAsync(1, "dQw4w9WgXcQ", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        var sut = CreateSut();

        var (success, _, dto) = await sut.AddUrlAsync(1, "https://youtu.be/dQw4w9WgXcQ");

        Assert.True(success);
        Assert.NotNull(dto);
        Assert.Equal("dQw4w9WgXcQ", dto!.VideoId);
        _repoMock.Verify(r => r.AddAsync(It.Is<SongKaraokeUrl>(u => u.VideoId == "dQw4w9WgXcQ"), It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "SongKaraokeUrlServiceTests"`
Expected: FAIL — `SongKaraokeUrlService` not found

- [ ] **Step 2: Implement SongKaraokeUrlService**

```csharp
// Services/SongKaraokeUrlService.cs
using System.Text.RegularExpressions;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entity;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services;

public class SongKaraokeUrlService : ISongKaraokeUrlService
{
    private readonly ISongKaraokeUrlRepository _repo;
    private readonly ILogger<SongKaraokeUrlService> _logger;

    // Matches the 11-char video ID segment in all supported YouTube URL formats
    private static readonly Regex VideoIdRegex = new(
        @"(?:youtube\.com/(?:watch\?v=|embed/|shorts/)|youtu\.be/)([A-Za-z0-9_-]{11})",
        RegexOptions.Compiled);

    public SongKaraokeUrlService(ISongKaraokeUrlRepository repo, ILogger<SongKaraokeUrlService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    /// <inheritdoc />
    public string? ExtractVideoId(string rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl)) return null;
        var match = VideoIdRegex.Match(rawUrl);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <inheritdoc />
    public async Task<List<SongKaraokeUrlDto>> GetUrlsForSongAsync(int songId, CancellationToken ct = default)
    {
        var urls = await _repo.GetBySongIdAsync(songId, ct);
        var suggested = urls.FirstOrDefault();
        return urls.Select((u, i) => ToDto(u, isSuggested: i == 0 && suggested != null)).ToList();
    }

    /// <inheritdoc />
    public async Task<(bool success, string message, SongKaraokeUrlDto? url)> AddUrlAsync(
        int songId, string rawUrl, string? label = null, CancellationToken ct = default)
    {
        var videoId = ExtractVideoId(rawUrl);
        if (videoId is null)
            return (false, "Not a valid YouTube URL.", null);

        if (await _repo.ExistsAsync(songId, videoId, ct))
            return (false, "This URL is already saved for this song.", null);

        var entity = new SongKaraokeUrl
        {
            SongId = songId,
            VideoId = videoId,
            Label = label?.Trim() is { Length: > 0 } l ? l : null,
            AddedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(entity, ct);
        await _repo.SaveChangesAsync(ct);

        return (true, string.Empty, ToDto(entity, isSuggested: false));
    }

    /// <inheritdoc />
    public async Task<(bool success, string message)> RemoveUrlAsync(
        int songId, string videoId, CancellationToken ct = default)
    {
        if (!await _repo.ExistsAsync(songId, videoId, ct))
            return (false, "URL not found.");

        await _repo.RemoveAsync(songId, videoId, ct);
        await _repo.SaveChangesAsync(ct);
        return (true, string.Empty);
    }

    /// <inheritdoc />
    public async Task RecordPlayAsync(int songId, string videoId, CancellationToken ct = default)
    {
        await _repo.IncrementPlayCountAsync(songId, videoId, ct);
        await _repo.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<SongKaraokeUrlDto?> GetSuggestedUrlAsync(int songId, CancellationToken ct = default)
    {
        var entity = await _repo.GetSuggestedAsync(songId, ct);
        return entity is null ? null : ToDto(entity, isSuggested: true);
    }

    private static SongKaraokeUrlDto ToDto(SongKaraokeUrl u, bool isSuggested)
        => new(u.VideoId, u.SongId, u.PlayCount, u.DurationSeconds, u.LastUsedAt, u.AddedAt, u.Label, isSuggested);
}
```

- [ ] **Step 3: Run tests, confirm Green**

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "SongKaraokeUrlServiceTests"`
Expected: All PASS

- [ ] **Step 4: Commit**

```bash
git add Services/SongKaraokeUrlService.cs MyVocaList.Tests/Unit/Services/SongKaraokeUrlServiceTests.cs
git commit -m "feat: SongKaraokeUrlService — ExtractVideoId (4 URL formats), AddUrlAsync duplicate detection; unit tests Green"
```

---

### Task 8: YouTubeSearchService

**Files:**
- Create: `Services/YouTubeSearchService.cs`

- [ ] **Step 1: Write failing unit tests**

```csharp
// MyVocaList.Tests/Unit/Services/YouTubeSearchServiceTests.cs
public class YouTubeSearchServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpMock = new();
    private readonly Mock<ILogger<YouTubeSearchService>> _loggerMock = new();

    private YouTubeSearchService CreateSut(string? storedKey = null)
    {
        // SecureStorage cannot be tested directly — inject via a testable wrapper
        var secureStorageMock = new Mock<ISecureStorageWrapper>();
        secureStorageMock.Setup(s => s.GetAsync("youtube_api_key")).ReturnsAsync(storedKey);
        return new YouTubeSearchService(_httpMock.Object, secureStorageMock.Object, _loggerMock.Object);
    }

    [Fact]
    // [AC] AC-2.5: no key → returns empty result set
    public async Task SearchAsync_NoApiKey_ReturnsEmpty()
    {
        var sut = CreateSut(storedKey: null);

        var results = await sut.SearchAsync("test query");

        Assert.Empty(results);
    }
}
```

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "YouTubeSearchServiceTests"`
Expected: FAIL — types not found

- [ ] **Step 2: Create ISecureStorageWrapper (testability shim)**

```csharp
// Domain/ServicesInterfaces/ISecureStorageWrapper.cs
namespace MyVocaList.Domain.ServicesInterfaces;

/// <summary>Thin wrapper around SecureStorage to allow unit testing without platform binding.</summary>
public interface ISecureStorageWrapper
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    bool Remove(string key);
}
```

```csharp
// MyVocaList/Services/SecureStorageWrapper.cs
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.UI.Services;

/// <summary>Delegates to MAUI SecureStorage. Registered in MauiProgram.cs for the MAUI project.</summary>
public class SecureStorageWrapper : ISecureStorageWrapper
{
    public Task<string?> GetAsync(string key) => SecureStorage.GetAsync(key);
    public Task SetAsync(string key, string value) => SecureStorage.SetAsync(key, value);
    public bool Remove(string key) => SecureStorage.Remove(key);
}
```

- [ ] **Step 3: Implement YouTubeSearchService**

```csharp
// Services/YouTubeSearchService.cs
using System.Net.Http.Json;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services;

public class YouTubeSearchService : IYouTubeSearchService
{
    private const string SearchEndpoint =
        "https://www.googleapis.com/youtube/v3/search?part=snippet&type=video&maxResults=5&q={0}&key={1}";
    private const string VideosEndpoint =
        "https://www.googleapis.com/youtube/v3/videos?part=contentDetails&id={0}&key={1}";

    private readonly IHttpClientFactory _httpFactory;
    private readonly ISecureStorageWrapper _secureStorage;
    private readonly ILogger<YouTubeSearchService> _logger;

    public YouTubeSearchService(
        IHttpClientFactory httpFactory,
        ISecureStorageWrapper secureStorage,
        ILogger<YouTubeSearchService> logger)
    {
        _httpFactory = httpFactory;
        _secureStorage = secureStorage;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<YouTubeSearchResultDto>> SearchAsync(string query, CancellationToken ct = default)
    {
        var apiKey = await _secureStorage.GetAsync("youtube_api_key");
        if (string.IsNullOrEmpty(apiKey))
            return [];

        try
        {
            var client = _httpFactory.CreateClient();
            var url = string.Format(SearchEndpoint, Uri.EscapeDataString(query), apiKey);
            var response = await client.GetFromJsonAsync<YouTubeSearchResponse>(url, ct);
            if (response?.Items is not { Length: > 0 })
                return [];

            var videoIds = string.Join(",", response.Items.Select(i => i.Id.VideoId));
            var durationsUrl = string.Format(VideosEndpoint, videoIds, apiKey);
            var durationsResponse = await client.GetFromJsonAsync<YouTubeVideosResponse>(durationsUrl, ct);

            var durations = durationsResponse?.Items?
                .ToDictionary(v => v.Id, v => ParseIso8601Duration(v.ContentDetails?.Duration))
                ?? new Dictionary<string, int?>();

            return response.Items.Select(i =>
            {
                var videoId = i.Id.VideoId;
                return new YouTubeSearchResultDto(
                    videoId,
                    i.Snippet.Title,
                    i.Snippet.ChannelTitle,
                    durations.GetValueOrDefault(videoId),
                    $"https://img.youtube.com/vi/{videoId}/mqdefault.jpg");
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "YouTube search failed for query: {Query}", query);
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateApiKeyAsync(string apiKey, CancellationToken ct = default)
    {
        try
        {
            var client = _httpFactory.CreateClient();
            var url = string.Format(SearchEndpoint, "test", apiKey);
            var response = await client.GetAsync(url, ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Parses ISO 8601 duration (PT1H2M3S) to total seconds
    private static int? ParseIso8601Duration(string? iso)
    {
        if (string.IsNullOrEmpty(iso)) return null;
        var match = System.Text.RegularExpressions.Regex.Match(iso,
            @"PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?");
        if (!match.Success) return null;
        var h = int.TryParse(match.Groups[1].Value, out var hh) ? hh : 0;
        var m = int.TryParse(match.Groups[2].Value, out var mm) ? mm : 0;
        var s = int.TryParse(match.Groups[3].Value, out var ss) ? ss : 0;
        return h * 3600 + m * 60 + s;
    }

    // Internal deserialization types — not exposed outside this file
    private record YouTubeSearchResponse(YouTubeSearchItem[] Items);
    private record YouTubeSearchItem(YouTubeSearchItemId Id, YouTubeSnippet Snippet);
    private record YouTubeSearchItemId(string VideoId);
    private record YouTubeSnippet(string Title, string ChannelTitle);
    private record YouTubeVideosResponse(YouTubeVideoItem[]? Items);
    private record YouTubeVideoItem(string Id, YouTubeContentDetails? ContentDetails);
    private record YouTubeContentDetails(string? Duration);
}
```

Also add `IYouTubeSearchService` to `Domain/ServicesInterfaces/`:

```csharp
// Domain/ServicesInterfaces/IYouTubeSearchService.cs
using MyVocaList.Contracts.DTOs.List;

namespace MyVocaList.Domain.ServicesInterfaces;

public interface IYouTubeSearchService
{
    /// <summary>Returns up to 5 results. Returns empty list when no API key is configured.</summary>
    Task<IEnumerable<YouTubeSearchResultDto>> SearchAsync(string query, CancellationToken ct = default);

    /// <summary>Makes a minimal API call to verify the key is valid.</summary>
    Task<bool> ValidateApiKeyAsync(string apiKey, CancellationToken ct = default);
}
```

- [ ] **Step 4: Run tests, confirm Green**

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "YouTubeSearchServiceTests"`
Expected: PASS

- [ ] **Step 5: Build**

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`
Expected: 0 errors

- [ ] **Step 6: Commit**

```bash
git add Services/YouTubeSearchService.cs Domain/ServicesInterfaces/IYouTubeSearchService.cs Domain/ServicesInterfaces/ISecureStorageWrapper.cs MyVocaList/Services/SecureStorageWrapper.cs MyVocaList.Tests/Unit/Services/YouTubeSearchServiceTests.cs
git commit -m "feat: YouTubeSearchService — optional API key, Data API v3 search + duration parsing; ISecureStorageWrapper shim"
```

---

### Task 9: NextSingerAlertService

**Files:**
- Create: `Services/NextSingerAlertService.cs`

> **Dependency:** This service uses `Plugin.LocalNotification`. Add the NuGet package to `MyVocaList/MyVocaList.csproj` and `MyVocaList.Tests/MyVocaList.Tests.csproj` before writing tests.
>
> Run: `dotnet add MyVocaList/MyVocaList.csproj package Plugin.LocalNotification`

- [ ] **Step 1: Add NuGet package**

```bash
dotnet add MyVocaList/MyVocaList.csproj package Plugin.LocalNotification
```

Verify it appears in `MyVocaList.csproj` PackageReferences.

- [ ] **Step 2: Write failing unit tests**

```csharp
// MyVocaList.Tests/Unit/Services/NextSingerAlertServiceTests.cs
public class NextSingerAlertServiceTests
{
    private readonly Mock<INotificationService> _notifMock = new();
    private readonly Mock<ILogger<NextSingerAlertService>> _loggerMock = new();

    private NextSingerAlertService CreateSut()
        => new(_notifMock.Object, _loggerMock.Object);

    [Fact]
    // [AC] AC-4.2: null duration → no notifications scheduled
    public async Task ScheduleAlertsAsync_NullDuration_DoesNotSchedule()
    {
        var sut = CreateSut();

        await sut.ScheduleAlertsAsync("Alice", "My Song", durationSeconds: null);

        _notifMock.Verify(n => n.Show(It.IsAny<NotificationRequest>()), Times.Never);
    }

    [Fact]
    // [AC] AC-4.2: duration ≤ 15s → both stages skipped
    public async Task ScheduleAlertsAsync_DurationTooShort_DoesNotSchedule()
    {
        var sut = CreateSut();

        await sut.ScheduleAlertsAsync("Alice", "My Song", durationSeconds: 10);

        _notifMock.Verify(n => n.Show(It.IsAny<NotificationRequest>()), Times.Never);
    }

    [Fact]
    // [AC] AC-4.1: duration > 45s → both stages scheduled
    public async Task ScheduleAlertsAsync_NormalDuration_SchedulesBothStages()
    {
        _notifMock.Setup(n => n.Show(It.IsAny<NotificationRequest>())).Returns(true);
        var sut = CreateSut();

        await sut.ScheduleAlertsAsync("Alice", "My Song", durationSeconds: 180);

        _notifMock.Verify(n => n.Show(It.IsAny<NotificationRequest>()), Times.Exactly(2));
    }

    [Fact]
    // [AC] AC-4.1: 15 < duration ≤ 45s → stage 1 skipped, stage 2 scheduled
    public async Task ScheduleAlertsAsync_BetweenEdges_SchedulesOnlyStage2()
    {
        _notifMock.Setup(n => n.Show(It.IsAny<NotificationRequest>())).Returns(true);
        var sut = CreateSut();

        await sut.ScheduleAlertsAsync("Alice", "My Song", durationSeconds: 30);

        _notifMock.Verify(n => n.Show(It.IsAny<NotificationRequest>()), Times.Once);
    }

    [Fact]
    // [AC] AC-4.6: CancelAlertsAsync cancels both pending notifications
    public async Task CancelAlertsAsync_CancelsBothIds()
    {
        var sut = CreateSut();

        await sut.CancelAlertsAsync();

        // Verify cancellation was called for both Stage1 and Stage2 notification IDs
        _notifMock.Verify(n => n.Cancel(NextSingerAlertService.Stage1NotificationId), Times.Once);
        _notifMock.Verify(n => n.Cancel(NextSingerAlertService.Stage2NotificationId), Times.Once);
    }
}
```

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "NextSingerAlertServiceTests"`
Expected: FAIL

- [ ] **Step 3: Implement NextSingerAlertService**

```csharp
// Services/NextSingerAlertService.cs
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Services;

public class NextSingerAlertService : INextSingerAlertService
{
    public const int Stage1NotificationId = 9001;
    public const int Stage2NotificationId = 9002;

    private readonly INotificationService _notifications;
    private readonly ILogger<NextSingerAlertService> _logger;

    public NextSingerAlertService(
        INotificationService notifications,
        ILogger<NextSingerAlertService> logger)
    {
        _notifications = notifications;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ScheduleAlertsAsync(
        string singerName, string songTitle, int? durationSeconds, CancellationToken ct = default)
    {
        if (durationSeconds is null or <= 15)
        {
            if (durationSeconds is not null)
                _logger.LogWarning("Duration {Seconds}s too short for alerts; skipping", durationSeconds);
            return;
        }

        var now = DateTime.Now;

        if (durationSeconds > 45)
        {
            var stage1 = new NotificationRequest
            {
                NotificationId = Stage1NotificationId,
                Title = $"Next up — {singerName}",
                Description = $"{songTitle} · preparing in ~45s",
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = now.AddSeconds(durationSeconds.Value - 45)
                },
                Android = new AndroidOptions { Priority = AndroidPriority.Default }
            };
            _notifications.Show(stage1);
        }

        var stage2 = new NotificationRequest
        {
            NotificationId = Stage2NotificationId,
            Title = $"⚡ {singerName} — mic now!",
            Description = $"{songTitle} · ~15s remaining",
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = now.AddSeconds(durationSeconds.Value - 15)
            },
            Android = new AndroidOptions { Priority = AndroidPriority.High }
        };
        _notifications.Show(stage2);

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CancelAlertsAsync(CancellationToken ct = default)
    {
        _notifications.Cancel(Stage1NotificationId);
        _notifications.Cancel(Stage2NotificationId);
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 4: Run tests, confirm Green**

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "NextSingerAlertServiceTests"`
Expected: All PASS

- [ ] **Step 5: Build**

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`
Expected: 0 errors

- [ ] **Step 6: Commit**

```bash
git add Services/NextSingerAlertService.cs MyVocaList.Tests/Unit/Services/NextSingerAlertServiceTests.cs MyVocaList/MyVocaList.csproj
git commit -m "feat: NextSingerAlertService — 2-stage alerts at T-45s and T-15s; edge cases for short durations; unit tests Green"
```

---

## Phase 4 — Android Overlay

### Task 10: OverlayService (Android) + NoOpOverlayService (iOS)

**Files:**
- Create: `MyVocaList/Platforms/Android/Services/OverlayService.cs`
- Create: `MyVocaList/Services/NoOpOverlayService.cs`

> **Risk: High.** This task requires Android API 26+ (`TYPE_APPLICATION_OVERLAY`, `ForegroundService`, `ObjectAnimator`). Build only on the Android TFM. The iOS build must still pass because `IOverlayService` is registered as `NoOpOverlayService` there.

- [ ] **Step 1: Create NoOpOverlayService**

```csharp
// MyVocaList/Services/NoOpOverlayService.cs
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.UI.Services;

/// <summary>iOS/Windows implementation — overlay is Android-only. All methods are no-ops.</summary>
public class NoOpOverlayService : IOverlayService
{
    public bool IsPermissionGranted => false;
    public void RequestPermission() { }
    public void Show(string singerName, string songTitle, OverlayStage stage) { }
    public void UpdateStage(OverlayStage stage) { }
    public void Dismiss() { }
}
```

- [ ] **Step 2: Create OverlayService for Android**

```csharp
// MyVocaList/Platforms/Android/Services/OverlayService.cs
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Widget;
using AndroidX.Core.App;
using MyVocaList.Domain.ServicesInterfaces;

namespace MyVocaList.Platforms.Android.Services;

public class OverlayService : IOverlayService
{
    private readonly Context _context;
    private IWindowManager? _windowManager;
    private TextView? _overlayView;

    public OverlayService(Context context)
    {
        _context = context;
    }

    public bool IsPermissionGranted =>
        Settings.CanDrawOverlays(_context);

    public void RequestPermission()
    {
        var intent = new Intent(
            Settings.ActionManageOverlayPermission,
            global::Android.Net.Uri.Parse($"package:{_context.PackageName}"));
        intent.AddFlags(ActivityFlags.NewTask);
        _context.StartActivity(intent);
    }

    public void Show(string singerName, string songTitle, OverlayStage stage)
    {
        if (!IsPermissionGranted) return;

        Dismiss(); // Remove existing overlay before creating a new one

        _windowManager = _context.GetSystemService(Context.WindowService)!.JavaCast<IWindowManager>()!;

        var layoutParams = new WindowManagerLayoutParams(
            ViewGroup.LayoutParams.WrapContent,
            ViewGroup.LayoutParams.WrapContent,
            WindowManagerTypes.ApplicationOverlay,
            WindowManagerFlags.NotFocusable | WindowManagerFlags.NotTouchModal,
            Format.Translucent)
        {
            Gravity = GravityFlags.Top | GravityFlags.End,
            X = 16,
            Y = 80
        };

        _overlayView = new TextView(_context)
        {
            Text = BuildLabelText(singerName, songTitle, stage),
            TextSize = 16f
        };
        _overlayView.SetTextColor(stage == OverlayStage.Stage2 ? Color.Red : Color.White);
        _overlayView.SetShadowLayer(4f, 2f, 2f, Color.Black);
        _overlayView.SetPadding(0, 0, 0, 0);

        _windowManager.AddView(_overlayView, layoutParams);
        StartPulseAnimation(stage);
    }

    public void UpdateStage(OverlayStage stage)
    {
        if (_overlayView == null) return;
        _overlayView.Text = BuildLabelText(
            _overlayView.Tag?.ToString() ?? string.Empty,
            string.Empty,
            stage);
        _overlayView.SetTextColor(stage == OverlayStage.Stage2 ? Color.Red : Color.White);
        StartPulseAnimation(stage);
    }

    public void Dismiss()
    {
        if (_overlayView == null || _windowManager == null) return;
        try
        {
            _windowManager.RemoveView(_overlayView);
        }
        catch { /* view may already be detached */ }
        _overlayView = null;
        _windowManager = null;
    }

    private void StartPulseAnimation(OverlayStage stage)
    {
        if (_overlayView == null) return;

        // Use ObjectAnimator (GPU-native, off MAUI render thread)
        var duration = stage == OverlayStage.Stage1 ? 1200L : 400L;
        var fadeOut = global::Android.Animation.ObjectAnimator.OfFloat(_overlayView, "alpha", 1f, 0f);
        fadeOut!.SetDuration(duration);
        var fadeIn = global::Android.Animation.ObjectAnimator.OfFloat(_overlayView, "alpha", 0f, 1f);
        fadeIn!.SetDuration(duration);

        var set = new global::Android.Animation.AnimatorSet();
        set.PlaySequentially(fadeOut, fadeIn);
        set.SetStartDelay(0);
        set.RepeatCount = -1; // infinite
        set.Start();
    }

    private static string BuildLabelText(string singerName, string songTitle, OverlayStage stage)
        => stage == OverlayStage.Stage1
            ? $"NEXT: {singerName}"
            : $"⚡ {singerName} — mic now!";
}
```

- [ ] **Step 3: Build Android TFM**

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`
Expected: 0 errors

If you see `AnimatorSet.RepeatCount` not found, use `set.Start()` in a loop via the animation end event instead.

- [ ] **Step 4: Build iOS TFM**

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-ios`
Expected: 0 errors (OverlayService is Android-only via `#if ANDROID`)

- [ ] **Step 5: Commit**

```bash
git add MyVocaList/Platforms/Android/Services/OverlayService.cs MyVocaList/Services/NoOpOverlayService.cs
git commit -m "feat: OverlayService (Android) + NoOpOverlayService (iOS) — blinking overlay using ObjectAnimator"
```

---

## Phase 5 — DI Registration + Settings Page

### Task 11: MauiProgram.cs DI registration

**Files:**
- Modify: `MyVocaList/MauiProgram.cs`

- [ ] **Step 1: Register new services**

In `MauiProgram.cs`, after `builder.Services.AddScoped<IMusicMetadataService, MusicMetadataService>();` add:

```csharp
// YouTube Karaoke
builder.Services.AddScoped<ISongKaraokeUrlRepository, SongKaraokeUrlRepository>();
builder.Services.AddScoped<ISongKaraokeUrlService, SongKaraokeUrlService>();
builder.Services.AddScoped<IYouTubeSearchService, YouTubeSearchService>();
builder.Services.AddScoped<INextSingerAlertService, NextSingerAlertService>();
builder.Services.AddSingleton<ISecureStorageWrapper, SecureStorageWrapper>();

#if ANDROID
builder.Services.AddSingleton<IOverlayService, MyVocaList.Platforms.Android.Services.OverlayService>();
#else
builder.Services.AddSingleton<IOverlayService, NoOpOverlayService>();
#endif
```

Also register `HttpClient`:

```csharp
builder.Services.AddHttpClient();
```

(If `AddHttpClient()` is already registered, skip.)

- [ ] **Step 2: Add required usings to MauiProgram.cs** (if not already global):

```csharp
using MyVocaList.Domain.ServicesInterfaces;
using MyVocaList.Domain.RepositoryInterface;
using MyVocaList.Infra.Repository;
using MyVocaList.UI.Services;
```

- [ ] **Step 3: Build**

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git add MyVocaList/MauiProgram.cs
git commit -m "feat: DI registration for SongKaraokeUrlService, YouTubeSearchService, NextSingerAlertService, OverlayService"
```

---

### Task 12: Settings page — YouTube API key

**Files:**
- Create: `MyVocaList/UI/Pages/Settings/SettingsPage.xaml`
- Create: `MyVocaList/UI/Pages/Settings/SettingsPage.xaml.cs`
- Create: `MyVocaList/UI/ViewModels/SettingsViewModel.cs`
- Modify: `MyVocaList/AppShell.xaml` — add Settings route
- Modify: `MyVocaList/AppShell.xaml.cs` — register route
- Modify: `MyVocaList/MauiProgram.cs` — register SettingsViewModel + SettingsPage

- [ ] **Step 1: Create SettingsViewModel**

```csharp
// MyVocaList/UI/ViewModels/SettingsViewModel.cs
namespace MyVocaList.UI.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IYouTubeSearchService _youtubeSearch;
    private readonly ISecureStorageWrapper _secureStorage;
    private readonly ISnackbarComponent _snackbar;
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty] private string _apiKeyInput = string.Empty;
    [ObservableProperty] private bool _isApiKeyMasked = true;
    [ObservableProperty] private bool _isTestingKey;
    [ObservableProperty] private string _apiKeyStatus = string.Empty;
    [ObservableProperty] private bool _hasApiKeyStatus;

    public SettingsViewModel(
        IYouTubeSearchService youtubeSearch,
        ISecureStorageWrapper secureStorage,
        ISnackbarComponent snackbar,
        ILogger<SettingsViewModel> logger)
    {
        _youtubeSearch = youtubeSearch;
        _secureStorage = secureStorage;
        _snackbar = snackbar;
        _logger = logger;

        SaveApiKeyCommand = new AsyncRelayCommand(SaveApiKeyAsync);
        TestApiKeyCommand = new AsyncRelayCommand(TestApiKeyAsync);
        ClearApiKeyCommand = new AsyncRelayCommand(ClearApiKeyAsync);
        ToggleMaskCommand = new RelayCommand(ToggleMask);
    }

    public IAsyncRelayCommand SaveApiKeyCommand { get; }
    public IAsyncRelayCommand TestApiKeyCommand { get; }
    public IAsyncRelayCommand ClearApiKeyCommand { get; }
    public IRelayCommand ToggleMaskCommand { get; }

    public async Task InitializeAsync()
    {
        var stored = await _secureStorage.GetAsync("youtube_api_key");
        ApiKeyInput = stored ?? string.Empty;
    }

    private async Task SaveApiKeyAsync()
    {
        var key = ApiKeyInput.Trim();
        if (string.IsNullOrEmpty(key))
        {
            await ClearApiKeyAsync();
            return;
        }
        await _secureStorage.SetAsync("youtube_api_key", key);
        await _snackbar.ShowSuccessAsync("API key saved");
    }

    private async Task TestApiKeyAsync()
    {
        var key = ApiKeyInput.Trim();
        if (string.IsNullOrEmpty(key)) return;

        IsTestingKey = true;
        ApiKeyStatus = string.Empty;
        HasApiKeyStatus = false;
        try
        {
            var valid = await _youtubeSearch.ValidateApiKeyAsync(key);
            ApiKeyStatus = valid ? "Key valid ✓" : "Invalid key — check and retry.";
            HasApiKeyStatus = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API key test failed");
            ApiKeyStatus = "Test failed. Check your connection.";
            HasApiKeyStatus = true;
        }
        finally
        {
            IsTestingKey = false;
        }
    }

    private async Task ClearApiKeyAsync()
    {
        _secureStorage.Remove("youtube_api_key");
        ApiKeyInput = string.Empty;
        ApiKeyStatus = string.Empty;
        HasApiKeyStatus = false;
        await _snackbar.ShowSuccessAsync("API key removed");
    }

    private void ToggleMask() => IsApiKeyMasked = !IsApiKeyMasked;
}
```

- [ ] **Step 2: Create SettingsPage.xaml**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:dx="http://schemas.devexpress.com/maui"
    xmlns:dxe="clr-namespace:DevExpress.Maui.Editors;assembly=DevExpress.Maui.Editors"
    xmlns:vm="clr-namespace:MyVocaList.UI.ViewModels"
    x:Class="MyVocaList.UI.Pages.Settings.SettingsPage"
    x:DataType="vm:SettingsViewModel"
    Title="Settings"
    BackgroundColor="{StaticResource Surface}"
    SafeAreaEdges="All">

    <ScrollView>
        <VerticalStackLayout Padding="24" Spacing="24">

            <!-- YouTube Integration section -->
            <Label Text="YouTube Integration" StyleClass="Title.Medium" />

            <VerticalStackLayout Spacing="8">
                <dxe:TextEdit
                    Text="{Binding ApiKeyInput, Mode=TwoWay}"
                    LabelText="YouTube Data API v3 Key"
                    PlaceholderText="Paste your API key here"
                    IsPassword="{Binding IsApiKeyMasked}" />

                <HorizontalStackLayout Spacing="8">
                    <dx:DXButton Content="{Binding IsApiKeyMasked, Converter={StaticResource BoolToStringConverter}, ConverterParameter='Show|Hide'}"
                                 Command="{Binding ToggleMaskCommand}"
                                 ButtonType="Outlined" />
                    <dx:DXButton Content="Test"
                                 Command="{Binding TestApiKeyCommand}"
                                 IsEnabled="{Binding IsTestingKey, Converter={StaticResource InverseBoolConverter}}"
                                 ButtonType="Outlined" />
                    <dx:DXButton Content="Save"
                                 Command="{Binding SaveApiKeyCommand}"
                                 ButtonType="Filled" />
                    <dx:DXButton Content="Clear"
                                 Command="{Binding ClearApiKeyCommand}"
                                 ButtonType="Text" />
                </HorizontalStackLayout>

                <Label Text="{Binding ApiKeyStatus}"
                       IsVisible="{Binding HasApiKeyStatus}"
                       StyleClass="Body.Small" />
            </VerticalStackLayout>

            <Label StyleClass="Body.Small" Opacity="0.6">
                <Label.FormattedText>
                    <FormattedString>
                        <Span Text="Free quota: ~100 searches/day (10,000 units). " />
                        <Span Text="Without a key, paste YouTube URLs directly." />
                    </FormattedString>
                </Label.FormattedText>
            </Label>

        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

- [ ] **Step 3: Create SettingsPage.xaml.cs**

```csharp
// MyVocaList/UI/Pages/Settings/SettingsPage.xaml.cs
namespace MyVocaList.UI.Pages.Settings;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is SettingsViewModel vm)
            await vm.InitializeAsync();
    }
}
```

- [ ] **Step 4: Register route and DI**

In `AppShell.xaml`, add: `<ShellContent Route="settings" ContentTemplate="{DataTemplate pages:SettingsPage}" />`

In `MauiProgram.cs`, add:
```csharp
builder.Services.AddTransient<SettingsViewModel>();
builder.Services.AddTransient<SettingsPage>();
```

- [ ] **Step 5: Build**

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`
Expected: 0 errors

- [ ] **Step 6: Commit**

```bash
git add MyVocaList/UI/Pages/Settings/ MyVocaList/UI/ViewModels/SettingsViewModel.cs MyVocaList/AppShell.xaml MyVocaList/AppShell.xaml.cs MyVocaList/MauiProgram.cs
git commit -m "feat: Settings page with YouTube API key management — save/test/clear/mask; SecureStorage"
```

---

## Phase 6 — SongFormPage YouTube Section

### Task 13: Extend SongFormViewModel with YouTube URL section

**Files:**
- Modify: `MyVocaList/UI/ViewModels/SongFormViewModel.cs`

- [ ] **Step 1: Add observables and commands for YouTube URL section**

Add to constructor parameters:
```csharp
private readonly ISongKaraokeUrlService _karaokeUrlService;
private readonly IYouTubeSearchService _youtubeSearch;
```

Add fields after the existing `_lyrics` field:

```csharp
// YouTube URLs section
[ObservableProperty] private ObservableRangeCollection<SongKaraokeUrlDto> _karaokeUrls = [];
[ObservableProperty] private string _youtubeSearchQuery = string.Empty;
[ObservableProperty] private ObservableRangeCollection<YouTubeSearchResultDto> _searchResults = [];
[ObservableProperty] private bool _isYouTubeSearching;
[ObservableProperty] private string _youtubeSearchStatus = string.Empty;
[ObservableProperty] private bool _hasYouTubeSearchStatus;
[ObservableProperty] private bool _hasYouTubeApiKey;
[ObservableProperty] private string _pasteUrlInput = string.Empty;
[ObservableProperty] private string _pasteUrlError = string.Empty;
[ObservableProperty] private bool _hasPasteUrlError;
```

Add commands in the constructor body:

```csharp
SearchYouTubeCommand = new AsyncRelayCommand(SearchYouTubeAsync);
AddFromSearchCommand = new AsyncRelayCommand<YouTubeSearchResultDto>(AddFromSearchAsync);
AddFromPasteCommand = new AsyncRelayCommand(AddFromPasteAsync);
RemoveUrlCommand = new AsyncRelayCommand<SongKaraokeUrlDto>(RemoveUrlAsync);
```

Declare command properties:

```csharp
public IAsyncRelayCommand SearchYouTubeCommand { get; }
public IAsyncRelayCommand<YouTubeSearchResultDto> AddFromSearchCommand { get; }
public IAsyncRelayCommand AddFromPasteCommand { get; }
public IAsyncRelayCommand<SongKaraokeUrlDto> RemoveUrlCommand { get; }
```

Add private methods:

```csharp
private async Task LoadKaraokeUrlsAsync()
{
    if (!SongId.HasValue) return;
    var urls = await _karaokeUrlService.GetUrlsForSongAsync(SongId.Value);
    RunOnUiThread(() => KaraokeUrls.ReplaceRange(urls));
}

private async Task SearchYouTubeAsync()
{
    var query = YoutubeSearchQuery?.Trim() ?? string.Empty;
    if (string.IsNullOrEmpty(query)) return;

    IsYouTubeSearching = true;
    YoutubeSearchStatus = string.Empty;
    HasYouTubeSearchStatus = false;
    try
    {
        var results = await _youtubeSearch.SearchAsync(query);
        var list = results.ToList();
        RunOnUiThread(() =>
        {
            SearchResults.ReplaceRange(list);
            YoutubeSearchStatus = list.Count == 0 ? "No results found" : string.Empty;
            HasYouTubeSearchStatus = list.Count == 0;
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "YouTube search failed");
        YoutubeSearchStatus = "Search failed";
        HasYouTubeSearchStatus = true;
    }
    finally
    {
        IsYouTubeSearching = false;
    }
}

private async Task AddFromSearchAsync(YouTubeSearchResultDto result)
{
    if (result is null || !SongId.HasValue) return;

    var rawUrl = $"https://youtu.be/{result.VideoId}";
    var (success, message, dto) = await _karaokeUrlService.AddUrlAsync(SongId.Value, rawUrl);
    if (success && dto is not null)
    {
        RunOnUiThread(() => KaraokeUrls.Add(dto));
        await _snackbarService.ShowSuccessAsync("URL added");
    }
    else
    {
        await _snackbarService.ShowErrorAsync(message);
    }
}

private async Task AddFromPasteAsync()
{
    var raw = PasteUrlInput?.Trim() ?? string.Empty;
    if (string.IsNullOrEmpty(raw)) return;

    if (!SongId.HasValue)
    {
        PasteUrlError = "Save the song first before adding URLs";
        HasPasteUrlError = true;
        return;
    }

    var (success, message, dto) = await _karaokeUrlService.AddUrlAsync(SongId.Value, raw);
    if (success && dto is not null)
    {
        RunOnUiThread(() =>
        {
            KaraokeUrls.Add(dto);
            PasteUrlInput = string.Empty;
            PasteUrlError = string.Empty;
            HasPasteUrlError = false;
        });
    }
    else
    {
        PasteUrlError = message;
        HasPasteUrlError = true;
    }
}

private async Task RemoveUrlAsync(SongKaraokeUrlDto dto)
{
    if (dto is null || !SongId.HasValue) return;

    var (success, _) = await _karaokeUrlService.RemoveUrlAsync(SongId.Value, dto.VideoId);
    if (success)
    {
        RunOnUiThread(() => KaraokeUrls.Remove(dto));
        await _snackbarService.ShowSuccessAsync("URL removed", actionText: "Undo", onAction: async () =>
        {
            var rawUrl = $"https://youtu.be/{dto.VideoId}";
            var (reAdded, _, reDto) = await _karaokeUrlService.AddUrlAsync(SongId.Value, rawUrl, dto.Label);
            if (reAdded && reDto is not null)
                RunOnUiThread(() => KaraokeUrls.Add(reDto));
        });
    }
}
```

Also update `InitializeAsync` (or `OnSongIdChanged`) to call `LoadKaraokeUrlsAsync()` and set `HasYouTubeApiKey` by checking `ISecureStorageWrapper.GetAsync("youtube_api_key")`.

- [ ] **Step 2: Build**

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`
Expected: 0 errors

- [ ] **Step 3: Commit**

```bash
git add MyVocaList/UI/ViewModels/SongFormViewModel.cs
git commit -m "feat: SongFormViewModel — YouTube URL section observables and commands"
```

---

### Task 14: Extend SongFormPage.xaml with YouTube URLs section

**Files:**
- Modify: `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`
- Modify: `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs`

> **Incremental edit rule:** build after every meaningful XAML block added.

- [ ] **Step 1: Add YouTube URLs section after the Lyrics field**

Inside the `VerticalStackLayout` (after the existing lyrics `TextEdit`), append:

```xml
<!-- YouTube URLs section -->
<HorizontalStackLayout Spacing="8" Margin="0,8,0,0">
    <Image Source="youtube_icon.png"
           WidthRequest="20" HeightRequest="20"
           VerticalOptions="Center" />
    <Label Text="YouTube URLs"
           StyleClass="Title.Small"
           VerticalOptions="Center" />
    <Label Text="optional"
           StyleClass="Body.Small"
           Opacity="0.4"
           VerticalOptions="Center" />
</HorizontalStackLayout>

<!-- Saved URLs list -->
<dx:DXCollectionView
    ItemsSource="{Binding KaraokeUrls}"
    IsVisible="{Binding KaraokeUrls.Count, Converter={StaticResource IsNotZeroConverter}}"
    SelectionMode="None">
    <dx:DXCollectionView.ItemTemplate>
        <DataTemplate x:DataType="dto:SongKaraokeUrlDto">
            <Grid ColumnDefinitions="*,Auto" Padding="0,4">
                <VerticalStackLayout Grid.Column="0" Spacing="2">
                    <HorizontalStackLayout Spacing="6">
                        <Label Text="{Binding VideoId}"
                               StyleClass="Body.Medium" />
                        <Border BackgroundColor="{StaticResource Primary}"
                                StrokeThickness="0"
                                Padding="4,2"
                                StrokeShape="RoundRectangle 4"
                                IsVisible="{Binding IsSuggested}">
                            <Label Text="★ SUGGESTED"
                                   StyleClass="Label.Small"
                                   TextColor="{StaticResource OnPrimary}" />
                        </Border>
                    </HorizontalStackLayout>
                    <Label StyleClass="Body.Small" Opacity="0.6">
                        <Label.FormattedText>
                            <FormattedString>
                                <Span Text="{Binding PlayCount}" />
                                <Span Text=" plays" />
                                <Span Text=" · " IsVisible="{Binding DurationSeconds, Converter={StaticResource IsNotNullConverter}}" />
                                <Span Text="{Binding DurationSeconds, Converter={StaticResource SecondsToMinutesConverter}}"
                                      IsVisible="{Binding DurationSeconds, Converter={StaticResource IsNotNullConverter}}" />
                            </FormattedString>
                        </Label.FormattedText>
                    </Label>
                </VerticalStackLayout>
                <dx:DXButton Grid.Column="1"
                             Content="✕"
                             ButtonType="Text"
                             Command="{Binding Source={RelativeSource AncestorType={x:Type vm:SongFormViewModel}}, Path=RemoveUrlCommand}"
                             CommandParameter="{Binding .}"
                             VerticalOptions="Center" />
            </Grid>
        </DataTemplate>
    </dx:DXCollectionView.ItemTemplate>
</dx:DXCollectionView>

<!-- Search strip -->
<Border BackgroundColor="{StaticResource SurfaceContainerLow}"
        StrokeThickness="0"
        Padding="12"
        StrokeShape="RoundRectangle 8">
    <VerticalStackLayout Spacing="8">

        <!-- Search row (hidden when no API key) -->
        <Grid ColumnDefinitions="*,Auto"
              IsVisible="{Binding HasYouTubeApiKey}">
            <dxe:TextEdit Grid.Column="0"
                          Text="{Binding YoutubeSearchQuery, Mode=TwoWay}"
                          PlaceholderText="Search YouTube..."
                          ReturnCommand="{Binding SearchYouTubeCommand}" />
            <dx:DXButton Grid.Column="1"
                         Content="▶"
                         Command="{Binding SearchYouTubeCommand}"
                         IsEnabled="{Binding IsYouTubeSearching, Converter={StaticResource InverseBoolConverter}}"
                         VerticalOptions="Center"
                         Margin="8,0,0,0" />
        </Grid>

        <!-- No API key nudge -->
        <Label IsVisible="{Binding HasYouTubeApiKey, Converter={StaticResource InverseBoolConverter}}"
               StyleClass="Body.Small"
               Opacity="0.7">
            <Label.FormattedText>
                <FormattedString>
                    <Span Text="Add a YouTube API key in " />
                    <Span Text="Settings"
                          TextColor="{StaticResource Primary}"
                          TextDecorations="Underline">
                        <Span.GestureRecognizers>
                            <TapGestureRecognizer Command="{Binding GoToSettingsCommand}" />
                        </Span.GestureRecognizers>
                    </Span>
                    <Span Text=" to search without leaving the app." />
                </FormattedString>
            </Label.FormattedText>
        </Label>

        <!-- Search results -->
        <dx:DXCollectionView
            ItemsSource="{Binding SearchResults}"
            IsVisible="{Binding SearchResults.Count, Converter={StaticResource IsNotZeroConverter}}"
            SelectionMode="None">
            <dx:DXCollectionView.ItemTemplate>
                <DataTemplate x:DataType="dto:YouTubeSearchResultDto">
                    <Grid ColumnDefinitions="48,*,Auto" Padding="0,4" ColumnSpacing="8">
                        <Image Grid.Column="0"
                               Source="{Binding ThumbnailUrl}"
                               WidthRequest="48" HeightRequest="36"
                               Aspect="AspectFill" />
                        <VerticalStackLayout Grid.Column="1" Spacing="2" VerticalOptions="Center">
                            <Label Text="{Binding Title}"
                                   StyleClass="Body.Medium"
                                   MaxLines="2"
                                   LineBreakMode="TailTruncation" />
                            <Label Text="{Binding ChannelName}"
                                   StyleClass="Body.Small"
                                   Opacity="0.6" />
                        </VerticalStackLayout>
                        <dx:DXButton Grid.Column="2"
                                     Content="+"
                                     ButtonType="Text"
                                     Command="{Binding Source={RelativeSource AncestorType={x:Type vm:SongFormViewModel}}, Path=AddFromSearchCommand}"
                                     CommandParameter="{Binding .}"
                                     VerticalOptions="Center" />
                    </Grid>
                </DataTemplate>
            </dx:DXCollectionView.ItemTemplate>
        </dx:DXCollectionView>

        <Label Text="{Binding YoutubeSearchStatus}"
               IsVisible="{Binding HasYouTubeSearchStatus}"
               StyleClass="Body.Small"
               Opacity="0.6" />

        <!-- Paste field -->
        <Label Text="Or paste a URL directly" StyleClass="Body.Small" Opacity="0.6" />
        <Grid ColumnDefinitions="*,Auto">
            <dxe:TextEdit Grid.Column="0"
                          Text="{Binding PasteUrlInput, Mode=TwoWay}"
                          PlaceholderText="https://youtu.be/..."
                          HasError="{Binding HasPasteUrlError}"
                          ErrorText="{Binding PasteUrlError}" />
            <dx:DXButton Grid.Column="1"
                         Content="Add"
                         Command="{Binding AddFromPasteCommand}"
                         ButtonType="Outlined"
                         VerticalOptions="Center"
                         Margin="8,0,0,0" />
        </Grid>

    </VerticalStackLayout>
</Border>
```

Also add `GoToSettingsCommand` to `SongFormViewModel`:

```csharp
GoToSettingsCommand = new AsyncRelayCommand(async () => await Shell.Current.GoToAsync("settings"));
```

- [ ] **Step 2: Build**

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`
Expected: 0 errors. Fix any XAML binding issues before proceeding.

- [ ] **Step 3: Add namespace declarations to SongFormPage.xaml**

Ensure these xmlns are present at the top of `SongFormPage.xaml`:

```xml
xmlns:dto="clr-namespace:MyVocaList.Contracts.DTOs.List;assembly=MyVocaList.Contracts"
```

- [ ] **Step 4: Build again**

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`
Expected: 0 errors

- [ ] **Step 5: Commit**

```bash
git add MyVocaList/UI/Pages/Songs/SongFormPage.xaml MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs
git commit -m "feat: SongFormPage — YouTube URLs section with saved URL list, search strip, paste field"
```

---

## Phase 7 — Tests

### Task 15: Additional unit tests

- [ ] **Step 1: Add property-based tests for ExtractVideoId**

```csharp
// In SongKaraokeUrlServiceTests.cs
[Property]
public Property ExtractVideoId_VideoIdPassedDirectly_ExtractsSelf()
{
    // A raw 11-char video ID passed as youtu.be URL should round-trip correctly
    return Prop.ForAll(
        Arb.Default.String()
           .Filter(s => s != null && System.Text.RegularExpressions.Regex.IsMatch(s, "^[A-Za-z0-9_-]{11}$")),
        id =>
        {
            var sut = CreateSut();
            var result = sut.ExtractVideoId($"https://youtu.be/{id}");
            return result == id;
        });
}
```

> Add FsCheck.Xunit to `MyVocaList.Tests.csproj` if not present:
> `dotnet add MyVocaList.Tests/MyVocaList.Tests.csproj package FsCheck.Xunit`

- [ ] **Step 2: Add RemoveUrlAsync and GetUrlsForSongAsync service tests**

```csharp
[Fact]
// [AC] AC-1.5: remove URL succeeds
public async Task RemoveUrlAsync_ExistingUrl_ReturnsSuccess()
{
    _repoMock.Setup(r => r.ExistsAsync(1, "dQw4w9WgXcQ", It.IsAny<CancellationToken>()))
             .ReturnsAsync(true);
    var sut = CreateSut();

    var (success, _) = await sut.RemoveUrlAsync(1, "dQw4w9WgXcQ");

    Assert.True(success);
    _repoMock.Verify(r => r.RemoveAsync(1, "dQw4w9WgXcQ", It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
public async Task RemoveUrlAsync_NonExistentUrl_ReturnsFalse()
{
    _repoMock.Setup(r => r.ExistsAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(false);
    var sut = CreateSut();

    var (success, _) = await sut.RemoveUrlAsync(1, "dQw4w9WgXcQ");

    Assert.False(success);
    _repoMock.Verify(r => r.RemoveAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
}

[Fact]
// [AC] AC-1.4: first item is marked IsSuggested
public async Task GetUrlsForSongAsync_FirstItemIsMarkedSuggested()
{
    _repoMock.Setup(r => r.GetBySongIdAsync(1, It.IsAny<CancellationToken>()))
             .ReturnsAsync([
                 new SongKaraokeUrl { SongId = 1, VideoId = "aaaaaaaaaaa", PlayCount = 5, AddedAt = DateTime.UtcNow },
                 new SongKaraokeUrl { SongId = 1, VideoId = "bbbbbbbbbbb", PlayCount = 1, AddedAt = DateTime.UtcNow }
             ]);
    var sut = CreateSut();

    var urls = await sut.GetUrlsForSongAsync(1);

    Assert.True(urls[0].IsSuggested);
    Assert.False(urls[1].IsSuggested);
}
```

- [ ] **Step 3: Run all tests**

Run: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --verbosity normal`
Expected: 0 failures

- [ ] **Step 4: Commit**

```bash
git add MyVocaList.Tests/
git commit -m "test: additional unit tests for SongKaraokeUrlService — remove, GetUrls, property-based ExtractVideoId"
```

---

## Final Verification

- [ ] Run full build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` → 0 errors
- [ ] Run all tests: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` → 0 failures
- [ ] Run iOS build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-ios` → 0 errors
- [ ] Verify migration: `dotnet ef migrations list --project MyVocaList.Infra/MyVocaList.Infra.csproj` shows `AddSongKaraokeUrls`
- [ ] Update `Docs/specs/youtube-karaoke/tasks.md` — mark all phases complete
- [ ] Final commit with changelog update

---

## Spec Coverage Self-Review

| Spec requirement | Task |
|-----------------|------|
| SongKaraokeUrl entity, composite PK | Task 1 |
| ISongKaraokeUrlRepository | Task 2 |
| ISongKaraokeUrlService, INextSingerAlertService, IOverlayService | Task 3 |
| DTOs (SongKaraokeUrlDto, YouTubeSearchResultDto) | Task 4 |
| EF Core config + migration (composite PK, cascade) | Task 5 |
| SongKaraokeUrlRepository + integration tests | Task 6 |
| SongKaraokeUrlService + ExtractVideoId + unit tests | Task 7 |
| YouTubeSearchService (API key, optional, empty when no key) | Task 8 |
| NextSingerAlertService (Stage 1/2, edge cases) + unit tests | Task 9 |
| Android OverlayService (ObjectAnimator) + NoOpOverlayService | Task 10 |
| DI registration (all new services, #if ANDROID) | Task 11 |
| Settings page (API key save/test/clear/mask) | Task 12 |
| SongFormViewModel YouTube URL observables + commands | Task 13 |
| SongFormPage YouTube URLs section XAML | Task 14 |
| Additional unit + property-based tests | Task 15 |
| AC-2.5: nudge when no API key | Task 14 (XAML nudge message + GoToSettings) |
| AC-3.1–3.5: queue play launch | Future spec — marked Out of Scope in this plan |
| AC-4.7: notification permission request | NextSingerAlertService (handled by Plugin.LocalNotification internally) |
| AC-5.1–5.11: Android overlay full behavior | Task 10 |
| AC-6.1–6.6: Settings page API key | Task 12 |
