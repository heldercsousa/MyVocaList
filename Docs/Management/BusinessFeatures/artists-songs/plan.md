# Implementation Plan: Artists & Songs Catalog
**Date:** 2026-04-23
**Spec:** `Docs/specs/artists-songs/`
**Branch:** develop

---

## Overview

16 tasks across 8 phases. TDD workflow is mandatory for Tasks 3–8 (repos and services): write failing test → implement → green. UI tasks (9–16) follow spec-first XAML → build → fix cycle.

**Architectural decisions captured here (deviations from spec noted):**

- `IArtistRepository.GetPagedAsync` returns `(IEnumerable<(Artist artist, int songCount)>, int totalCount)` — deviation from spec's `IEnumerable<Artist>` to avoid N+1 query for SongCount in list
- `ArtistService` injects both `IArtistRepository` + `ISongRepository` (for song-count queries in suggestions and delete confirmation)
- Collation: `NOCASE_NOACCENT` on both operands (matches PersonRepository pattern)
- Confirm delete: inline `dx:BottomSheet` only — NOT `ConfirmSheet` component (ANR bug when wrapped in ContentView)
- `SelectedItems` assigned in `OnAppearing` code-behind only, never in XAML
- `SafeAreaEdges="Container"` on list pages, `SafeAreaEdges="All"` on form pages

---

## Task 1 — Domain Entities

**Files:**
- `MyVocaList.Domain/Entities/Artist.cs` (new)
- `MyVocaList.Domain/Entities/Song.cs` (new)

**Artist.cs**
```csharp
namespace MyVocaList.Domain.Entities;

public class Artist
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string NameNormalized { get; set; }
    public string? ExternalProvider { get; set; }
    public string? ExternalId { get; set; }
    public bool HasManualEdits { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Song> Songs { get; set; } = [];
}
```

**Song.cs**
```csharp
namespace MyVocaList.Domain.Entities;

public class Song
{
    public int Id { get; set; }
    public int ArtistId { get; set; }
    public string Title { get; set; }
    public string TitleNormalized { get; set; }
    public string? ExternalProvider { get; set; }
    public string? ExternalId { get; set; }
    public bool HasManualEdits { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Artist Artist { get; set; }
}
```

**Build:** `dotnet build` — confirm zero errors.

---

## Task 2 — Contracts (DTOs)

**Files:**
- `MyVocaList.Contracts/DTOs/List/ArtistListItemDto.cs` (new)
- `MyVocaList.Contracts/DTOs/List/SongListItemDto.cs` (new)
- `MyVocaList.Contracts/DTOs/MusicSearchResultDto.cs` (new)

**ArtistListItemDto.cs**
```csharp
namespace MyVocaList.Contracts.DTOs.List;

public record ArtistListItemDto(
    int Id,
    string Name,
    string? ExternalProvider,
    bool HasManualEdits,
    int SongCount)
{
    public string SongCountText => SongCount == 1 ? "1 song" : $"{SongCount} songs";
    public string ProviderBadgeText => ExternalProvider switch
    {
        "musicbrainz" => "MB",
        "deezer" => "DZ",
        _ => string.Empty
    };
}
```

**SongListItemDto.cs**
```csharp
namespace MyVocaList.Contracts.DTOs.List;

public record SongListItemDto(
    int Id,
    int ArtistId,
    string Title,
    string ArtistName,
    string? ExternalProvider,
    bool HasManualEdits);
```

**MusicSearchResultDto.cs**
```csharp
namespace MyVocaList.Contracts.DTOs;

public record MusicSearchResultDto(
    string ExternalId,
    string Provider,
    string ArtistName,
    string SongTitle);
```

**Build:** `dotnet build` — confirm zero errors.

---

## Task 3 — Domain Repository Interfaces

**Files:**
- `MyVocaList.Domain/Interfaces/IArtistRepository.cs` (new)
- `MyVocaList.Domain/Interfaces/ISongRepository.cs` (new)

**IArtistRepository.cs**
```csharp
namespace MyVocaList.Domain.Interfaces;

public interface IArtistRepository
{
    /// <summary>Returns a paged list of artists with their song counts.</summary>
    Task<(IEnumerable<(Artist artist, int songCount)> items, int totalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string? normalizedQuery, CancellationToken ct);

    /// <summary>Returns artists whose normalized name starts with the normalized query (max 5).</summary>
    Task<IEnumerable<Artist>> SearchByNameAsync(string normalizedQuery, int maxResults, CancellationToken ct);

    Task<Artist?> GetByIdAsync(int id, CancellationToken ct);
    Task<Artist?> GetByExternalIdAsync(string externalId, string provider, CancellationToken ct);
    Task<bool> ExistsByNameAsync(string normalizedName, CancellationToken ct);
    Task<bool> ExistsByNameAsync(string normalizedName, int excludeId, CancellationToken ct);
    Task AddAsync(Artist artist, CancellationToken ct);
    Task UpdateAsync(Artist artist, CancellationToken ct);
    Task DeleteAsync(IEnumerable<int> ids, CancellationToken ct);
}
```

**ISongRepository.cs**
```csharp
namespace MyVocaList.Domain.Interfaces;

public interface ISongRepository
{
    Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedByArtistAsync(
        int artistId, int pageNumber, int pageSize, string? normalizedQuery, CancellationToken ct);

    Task<Song?> GetByIdAsync(int id, CancellationToken ct);
    Task<Song?> GetByExternalIdAsync(string externalId, string provider, CancellationToken ct);
    Task<IEnumerable<SongListItemDto>> SearchByTitleAsync(int artistId, string normalizedQuery, int maxResults, CancellationToken ct);
    Task<bool> ExistsByTitleForArtistAsync(int artistId, string normalizedTitle, CancellationToken ct);
    Task<bool> ExistsByTitleForArtistAsync(int artistId, string normalizedTitle, int excludeId, CancellationToken ct);
    Task<int> CountByArtistAsync(int artistId, CancellationToken ct);
    Task<int> CountByArtistsAsync(IEnumerable<int> artistIds, CancellationToken ct);
    Task AddAsync(Song song, CancellationToken ct);
    Task UpdateAsync(Song song, CancellationToken ct);
    Task DeleteAsync(IEnumerable<int> ids, CancellationToken ct);
}
```

**Build:** `dotnet build` — confirm zero errors.

---

## Task 4 — Service Interfaces

**Files:**
- `MyVocaList.Services/Interfaces/IArtistService.cs` (new — in Services project to keep Domain clean)
- `MyVocaList.Services/Interfaces/ISongService.cs` (new)
- `MyVocaList.Services/Interfaces/IMusicMetadataProvider.cs` (new)
- `MyVocaList.Services/Interfaces/IMusicMetadataService.cs` (new)

**Note:** Place these in `MyVocaList.Services/Interfaces/` as they depend on Contracts DTOs which Domain doesn't reference.

**IArtistService.cs**
```csharp
namespace MyVocaList.Services;

public interface IArtistService
{
    (bool isValid, string message) ValidateNameInput(string name);
    Task<(bool success, string message, Artist? artist)> CreateArtistAsync(string name, CancellationToken ct = default);
    Task<(bool success, string message)> UpdateArtistAsync(int id, string name, CancellationToken ct = default);
    Task<(bool success, string message)> DeleteArtistsAsync(IEnumerable<int> ids, CancellationToken ct = default);
    Task<(IEnumerable<ArtistListItemDto> items, int totalCount)> GetPagedArtistsForListAsync(
        int pageNumber, int pageSize, string? query = null, CancellationToken ct = default);
    Task<IEnumerable<ArtistListItemDto>> SearchArtistsByNameAsync(string query, int maxResults = 5, CancellationToken ct = default);
    Task<string> GetDeleteConfirmationAsync(IEnumerable<int> ids, CancellationToken ct = default);
    bool ShouldShowCharacterCounter(int currentLength);
    (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength);
}
```

**ISongService.cs**
```csharp
namespace MyVocaList.Services;

public interface ISongService
{
    (bool isValid, string message) ValidateTitleInput(string title);
    Task<(bool success, string message, Song? song)> CreateSongAsync(int artistId, string title, CancellationToken ct = default);
    Task<(bool success, string message)> UpdateSongAsync(int id, string title, CancellationToken ct = default);
    Task<(bool success, string message)> DeleteSongsAsync(IEnumerable<int> ids, CancellationToken ct = default);
    Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedSongsForListAsync(
        int artistId, int pageNumber, int pageSize, string? query = null, CancellationToken ct = default);
    bool ShouldShowCharacterCounter(int currentLength);
    (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength);
}
```

**IMusicMetadataProvider.cs**
```csharp
namespace MyVocaList.Services;

public interface IMusicMetadataProvider
{
    string ProviderName { get; }
    Task<IEnumerable<MusicSearchResultDto>> SearchAsync(string artistName, string songTitle, CancellationToken ct);
}
```

**IMusicMetadataService.cs**
```csharp
namespace MyVocaList.Services;

public interface IMusicMetadataService
{
    Task<IEnumerable<MusicSearchResultDto>> SearchAsync(string artistName, string songTitle, CancellationToken ct);
}
```

**Build:** `dotnet build` — confirm zero errors.

---

## Task 5 — EF Core Configuration + Migration

**Files to modify:**
- `MyVocaList.Infra/AppDbContext.cs` — add `DbSet<Artist>` and `DbSet<Song>`
- `MyVocaList.Infra/Configurations/ArtistConfiguration.cs` (new)
- `MyVocaList.Infra/Configurations/SongConfiguration.cs` (new)

**AppDbContext.cs — add:**
```csharp
public DbSet<Artist> Artists { get; set; }
public DbSet<Song> Songs { get; set; }
```
Add to `OnModelCreating`:
```csharp
modelBuilder.ApplyConfiguration(new ArtistConfiguration());
modelBuilder.ApplyConfiguration(new SongConfiguration());
```

**ArtistConfiguration.cs**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyVocaList.Domain.Entities;

namespace MyVocaList.Infra.Configurations;

public class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
    public void Configure(EntityTypeBuilder<Artist> builder)
    {
        builder.ToTable("Artists");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name).IsRequired().HasMaxLength(100);
        builder.Property(a => a.NameNormalized).IsRequired().HasMaxLength(100);
        builder.Property(a => a.ExternalProvider).IsRequired(false).HasMaxLength(50);
        builder.Property(a => a.ExternalId).IsRequired(false).HasMaxLength(100);
        builder.Property(a => a.HasManualEdits).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();

        builder.HasIndex(a => a.NameNormalized)
               .HasDatabaseName("IX_Artists_NameNormalized");

        builder.HasIndex(a => new { a.ExternalProvider, a.ExternalId })
               .IsUnique()
               .HasFilter("[ExternalId] IS NOT NULL")
               .HasDatabaseName("IX_Artists_ExternalProvider_ExternalId");

        builder.HasMany(a => a.Songs)
               .WithOne(s => s.Artist)
               .HasForeignKey(s => s.ArtistId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

**SongConfiguration.cs**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyVocaList.Domain.Entities;

namespace MyVocaList.Infra.Configurations;

public class SongConfiguration : IEntityTypeConfiguration<Song>
{
    public void Configure(EntityTypeBuilder<Song> builder)
    {
        builder.ToTable("Songs");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title).IsRequired().HasMaxLength(200);
        builder.Property(s => s.TitleNormalized).IsRequired().HasMaxLength(200);
        builder.Property(s => s.ExternalProvider).IsRequired(false).HasMaxLength(50);
        builder.Property(s => s.ExternalId).IsRequired(false).HasMaxLength(100);
        builder.Property(s => s.HasManualEdits).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();

        builder.HasIndex(s => new { s.ArtistId, s.TitleNormalized })
               .HasDatabaseName("IX_Songs_ArtistId_TitleNormalized");

        builder.HasIndex(s => new { s.ExternalProvider, s.ExternalId })
               .IsUnique()
               .HasFilter("[ExternalId] IS NOT NULL")
               .HasDatabaseName("IX_Songs_ExternalProvider_ExternalId");
    }
}
```

**Generate migration:**
```bash
cd MyVocaList.Infra
dotnet ef migrations add AddArtistsSongs --startup-project ../MyVocaList/MyVocaList.csproj
```

**Build:** `dotnet build` — confirm zero errors.

---

## Task 6 — Repository Implementations (TDD)

### 6a — Write tests first

**File:** `MyVocaList.Tests/Integration/Repositories/ArtistRepositoryTests.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Entities;
using MyVocaList.Infra;
using MyVocaList.Infra.Repositories;
using MyVocaList.Tests.Infrastructure;

namespace MyVocaList.Tests.Integration.Repositories;

public class ArtistRepositoryTests : IAsyncLifetime
{
    private AppDbContext _db;
    private ArtistRepository _repo;

    public async Task InitializeAsync()
    {
        _db = TestDbContextFactory.Create();
        await _db.Database.EnsureCreatedAsync();
        _repo = new ArtistRepository(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.Database.EnsureDeletedAsync();
        await _db.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_ValidArtist_PersistedAndReturnedById()
    {
        var artist = new Artist { Name = "The Beatles", NameNormalized = "the beatles", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        await _repo.AddAsync(artist, CancellationToken.None);
        await _db.SaveChangesAsync();

        var found = await _repo.GetByIdAsync(artist.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal("The Beatles", found.Name);
    }

    [Fact]
    public async Task GetPagedAsync_NoQuery_ReturnsSongCount()
    {
        var artist = new Artist { Name = "Queen", NameNormalized = "queen", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Artists.Add(artist);
        await _db.SaveChangesAsync();
        _db.Songs.AddRange(
            new Song { ArtistId = artist.Id, Title = "Bohemian Rhapsody", TitleNormalized = "bohemian rhapsody", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Song { ArtistId = artist.Id, Title = "We Will Rock You", TitleNormalized = "we will rock you", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var (items, total) = await _repo.GetPagedAsync(1, 20, null, CancellationToken.None);

        var row = items.Single();
        Assert.Equal(2, row.songCount);
        Assert.Equal(1, total);
    }

    [Fact]
    public async Task GetPagedAsync_SearchQuery_ReturnsOnlyMatching()
    {
        _db.Artists.AddRange(
            new Artist { Name = "Queen", NameNormalized = "queen", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Artist { Name = "The Beatles", NameNormalized = "the beatles", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var (items, total) = await _repo.GetPagedAsync(1, 20, "queen", CancellationToken.None);

        Assert.Equal(1, total);
        Assert.Equal("Queen", items.Single().artist.Name);
    }

    [Fact]
    public async Task GetPagedAsync_CaseInsensitive_FindsMatch()
    {
        _db.Artists.Add(new Artist { Name = "Queen", NameNormalized = "queen", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var (items, total) = await _repo.GetPagedAsync(1, 20, "QUEEN", CancellationToken.None);

        Assert.Equal(1, total);
    }

    [Fact]
    public async Task ExistsByNameAsync_ExistingNormalizedName_ReturnsTrue()
    {
        _db.Artists.Add(new Artist { Name = "Queen", NameNormalized = "queen", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var result = await _repo.ExistsByNameAsync("queen", CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingIds_RemovesArtistsAndCascadesSongs()
    {
        var artist = new Artist { Name = "Queen", NameNormalized = "queen", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Artists.Add(artist);
        await _db.SaveChangesAsync();
        _db.Songs.Add(new Song { ArtistId = artist.Id, Title = "Bohemian Rhapsody", TitleNormalized = "bohemian rhapsody", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        await _repo.DeleteAsync([artist.Id], CancellationToken.None);
        await _db.SaveChangesAsync();

        Assert.Equal(0, await _db.Artists.CountAsync());
        Assert.Equal(0, await _db.Songs.CountAsync());
    }
}
```

**Run tests — confirm Red:**
```bash
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --verbosity normal
```

### 6b — Implement ArtistRepository

**File:** `MyVocaList.Infra/Repositories/ArtistRepository.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using MyVocaList.Domain.Entities;
using MyVocaList.Domain.Interfaces;

namespace MyVocaList.Infra.Repositories;

public class ArtistRepository : IArtistRepository
{
    private readonly AppDbContext _db;

    public ArtistRepository(AppDbContext db) => _db = db;

    public async Task<(IEnumerable<(Artist artist, int songCount)> items, int totalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string? normalizedQuery, CancellationToken ct)
    {
        var q = _db.Artists.AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            var pattern = normalizedQuery.Trim() + "%";
            q = q.Where(a => EF.Functions.Like(
                EF.Functions.Collate(a.NameNormalized, "NOCASE_NOACCENT"),
                EF.Functions.Collate(pattern, "NOCASE_NOACCENT")));
        }

        var totalCount = await q.CountAsync(ct);
        var rows = await q
            .OrderBy(a => a.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new { Artist = a, SongCount = a.Songs.Count() })
            .ToListAsync(ct);

        return (rows.Select(r => (r.Artist, r.SongCount)), totalCount);
    }

    public async Task<IEnumerable<Artist>> SearchByNameAsync(string normalizedQuery, int maxResults, CancellationToken ct)
    {
        var pattern = normalizedQuery.Trim() + "%";
        return await _db.Artists
            .Where(a => EF.Functions.Like(
                EF.Functions.Collate(a.NameNormalized, "NOCASE_NOACCENT"),
                EF.Functions.Collate(pattern, "NOCASE_NOACCENT")))
            .OrderBy(a => a.Name)
            .Take(maxResults)
            .ToListAsync(ct);
    }

    public Task<Artist?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Artists.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<bool> ExistsByNameAsync(string normalizedName, CancellationToken ct) =>
        _db.Artists.AnyAsync(a => EF.Functions.Like(
            EF.Functions.Collate(a.NameNormalized, "NOCASE_NOACCENT"),
            EF.Functions.Collate(normalizedName, "NOCASE_NOACCENT")), ct);

    public async Task AddAsync(Artist artist, CancellationToken ct) =>
        await _db.Artists.AddAsync(artist, ct);

    public async Task DeleteAsync(IEnumerable<int> ids, CancellationToken ct)
    {
        var artists = await _db.Artists.Where(a => ids.Contains(a.Id)).ToListAsync(ct);
        _db.Artists.RemoveRange(artists);
        await _db.SaveChangesAsync(ct);
    }
}
```

**File:** `MyVocaList.Infra/Repositories/SongRepository.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entities;
using MyVocaList.Domain.Interfaces;

namespace MyVocaList.Infra.Repositories;

public class SongRepository : ISongRepository
{
    private readonly AppDbContext _db;

    public SongRepository(AppDbContext db) => _db = db;

    public async Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedByArtistAsync(
        int artistId, int pageNumber, int pageSize, string? normalizedQuery, CancellationToken ct)
    {
        var q = _db.Songs.Where(s => s.ArtistId == artistId);

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            var pattern = normalizedQuery.Trim() + "%";
            q = q.Where(s => EF.Functions.Like(
                EF.Functions.Collate(s.TitleNormalized, "NOCASE_NOACCENT"),
                EF.Functions.Collate(pattern, "NOCASE_NOACCENT")));
        }

        var totalCount = await q.CountAsync(ct);
        var rows = await q
            .OrderBy(s => s.Title)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SongListItemDto(s.Id, s.ArtistId, s.Title, s.Artist.Name, s.ExternalProvider, s.HasManualEdits))
            .ToListAsync(ct);

        return (rows, totalCount);
    }

    public Task<Song?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Songs.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<bool> ExistsByTitleForArtistAsync(int artistId, string normalizedTitle, CancellationToken ct) =>
        _db.Songs.AnyAsync(s => s.ArtistId == artistId && EF.Functions.Like(
            EF.Functions.Collate(s.TitleNormalized, "NOCASE_NOACCENT"),
            EF.Functions.Collate(normalizedTitle, "NOCASE_NOACCENT")), ct);

    public Task<int> CountByArtistAsync(int artistId, CancellationToken ct) =>
        _db.Songs.CountAsync(s => s.ArtistId == artistId, ct);

    public Task<int> CountByArtistsAsync(IEnumerable<int> artistIds, CancellationToken ct) =>
        _db.Songs.CountAsync(s => artistIds.Contains(s.ArtistId), ct);

    public async Task AddAsync(Song song, CancellationToken ct) =>
        await _db.Songs.AddAsync(song, ct);

    public async Task DeleteAsync(IEnumerable<int> ids, CancellationToken ct)
    {
        var songs = await _db.Songs.Where(s => ids.Contains(s.Id)).ToListAsync(ct);
        _db.Songs.RemoveRange(songs);
        await _db.SaveChangesAsync(ct);
    }
}
```

**Run tests — confirm Green:**
```bash
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --verbosity normal
```

---

## Task 7 — Service Implementations (TDD)

### 7a — Write tests first

**File:** `MyVocaList.Tests/Unit/Services/ArtistServiceTests.cs`

```csharp
using Microsoft.Extensions.Logging;
using MyVocaList.Domain.Entities;
using MyVocaList.Domain.Interfaces;
using MyVocaList.Services;

namespace MyVocaList.Tests.Unit.Services;

public class ArtistServiceTests
{
    private readonly Mock<IArtistRepository> _artistRepoMock = new();
    private readonly Mock<ISongRepository> _songRepoMock = new();
    private readonly Mock<ILogger<ArtistService>> _loggerMock = new();

    private ArtistService CreateSut() => new(_artistRepoMock.Object, _songRepoMock.Object, _loggerMock.Object);

    [Fact]
    public void ValidateNameInput_EmptyName_ReturnsInvalid()
    {
        var sut = CreateSut();
        var (isValid, message) = sut.ValidateNameInput(string.Empty);
        Assert.False(isValid);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void ValidateNameInput_NameTooLong_ReturnsInvalid()
    {
        var sut = CreateSut();
        var (isValid, message) = sut.ValidateNameInput(new string('x', 101));
        Assert.False(isValid);
        Assert.NotEmpty(message);
    }

    [Fact]
    public void ValidateNameInput_ValidName_ReturnsValid()
    {
        var sut = CreateSut();
        var (isValid, _) = sut.ValidateNameInput("The Beatles");
        Assert.True(isValid);
    }

    [Fact]
    public async Task CreateArtistAsync_DuplicateName_ReturnsFalse()
    {
        _artistRepoMock.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(true);
        var sut = CreateSut();

        var (success, message, artist) = await sut.CreateArtistAsync("Queen");

        Assert.False(success);
        Assert.Null(artist);
        _artistRepoMock.Verify(r => r.AddAsync(It.IsAny<Artist>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateArtistAsync_ValidName_ReturnsSuccessAndEntity()
    {
        _artistRepoMock.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(false);
        var sut = CreateSut();

        var (success, message, artist) = await sut.CreateArtistAsync("Queen");

        Assert.True(success);
        Assert.NotNull(artist);
        Assert.Equal("Queen", artist.Name);
    }

    [Fact]
    public async Task DeleteArtistsAsync_WithSongs_ConfirmationMessageIncludesSongCount()
    {
        _songRepoMock.Setup(r => r.CountByArtistsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(5);
        var sut = CreateSut();

        var message = await sut.GetDeleteConfirmationAsync([1, 2]);

        Assert.Contains("5", message);
    }
}
```

**Run tests — confirm Red.**

### 7b — Implement ArtistService

**File:** `MyVocaList.Services/ArtistService.cs`

```csharp
using Microsoft.Extensions.Logging;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entities;
using MyVocaList.Domain.Interfaces;

namespace MyVocaList.Services;

public class ArtistService : IArtistService
{
    private const int MaxNameLength = 100;
    private const int ShowCounterAt = 80;

    private readonly IArtistRepository _artistRepo;
    private readonly ISongRepository _songRepo;
    private readonly ILogger<ArtistService> _logger;

    public ArtistService(IArtistRepository artistRepo, ISongRepository songRepo, ILogger<ArtistService> logger)
    {
        _artistRepo = artistRepo;
        _songRepo = songRepo;
        _logger = logger;
    }

    public (bool isValid, string message) ValidateNameInput(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Artist name is required.");
        if (name.Trim().Length > MaxNameLength)
            return (false, $"Artist name cannot exceed {MaxNameLength} characters.");
        return (true, string.Empty);
    }

    public async Task<(bool success, string message, Artist? artist)> CreateArtistAsync(string name, CancellationToken ct = default)
    {
        var (isValid, validationMessage) = ValidateNameInput(name);
        if (!isValid) return (false, validationMessage, null);

        var trimmed = name.Trim();
        var normalized = Normalize(trimmed);

        if (await _artistRepo.ExistsByNameAsync(normalized, ct))
            return (false, $"An artist named \"{trimmed}\" already exists.", null);

        var artist = new Artist
        {
            Name = trimmed,
            NameNormalized = normalized,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _artistRepo.AddAsync(artist, ct);
        _logger.LogInformation("Artist created: {Name}", trimmed);
        return (true, $"Artist \"{trimmed}\" created.", artist);
    }

    public async Task<(bool success, string message)> UpdateArtistAsync(int id, string name, CancellationToken ct = default)
    {
        var (isValid, validationMessage) = ValidateNameInput(name);
        if (!isValid) return (false, validationMessage);

        var trimmed = name.Trim();
        var normalized = Normalize(trimmed);

        var artist = await _artistRepo.GetByIdAsync(id, ct);
        if (artist == null) return (false, "Artist not found.");

        if (!string.Equals(artist.NameNormalized, normalized, StringComparison.OrdinalIgnoreCase))
        {
            if (await _artistRepo.ExistsByNameAsync(normalized, ct))
                return (false, $"An artist named \"{trimmed}\" already exists.");
        }

        artist.Name = trimmed;
        artist.NameNormalized = normalized;
        artist.HasManualEdits = true;
        artist.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation("Artist updated: {Id} → {Name}", id, trimmed);
        return (true, $"Artist updated.");
    }

    public async Task<(bool success, string message)> DeleteArtistsAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        await _artistRepo.DeleteAsync(idList, ct);
        _logger.LogInformation("Artists deleted: {Ids}", string.Join(", ", idList));
        return (true, idList.Count == 1 ? "Artist deleted." : $"{idList.Count} artists deleted.");
    }

    public async Task<(IEnumerable<ArtistListItemDto> items, int totalCount)> GetPagedArtistsForListAsync(
        int pageNumber, int pageSize, string? query = null, CancellationToken ct = default)
    {
        var normalized = string.IsNullOrWhiteSpace(query) ? null : Normalize(query.Trim());
        var (rows, totalCount) = await _artistRepo.GetPagedAsync(pageNumber, pageSize, normalized, ct);
        var dtos = rows.Select(r => new ArtistListItemDto(r.artist.Id, r.artist.Name, r.artist.ExternalProvider, r.artist.HasManualEdits, r.songCount));
        return (dtos, totalCount);
    }

    public async Task<IEnumerable<ArtistListItemDto>> SearchArtistsByNameAsync(string query, int maxResults = 5, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        var normalized = Normalize(query.Trim());
        var artists = await _artistRepo.SearchByNameAsync(normalized, maxResults, ct);
        var result = new List<ArtistListItemDto>();
        foreach (var a in artists)
        {
            var count = await _songRepo.CountByArtistAsync(a.Id, ct);
            result.Add(new ArtistListItemDto(a.Id, a.Name, a.ExternalProvider, a.HasManualEdits, count));
        }
        return result;
    }

    public async Task<string> GetDeleteConfirmationAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        var songCount = await _songRepo.CountByArtistsAsync(idList, ct);
        var artistLabel = idList.Count == 1 ? "artist" : $"{idList.Count} artists";
        if (songCount == 0)
            return $"Delete {artistLabel}? This cannot be undone.";
        var songLabel = songCount == 1 ? "1 song" : $"{songCount} songs";
        return $"Delete {artistLabel} and their {songLabel}? This cannot be undone.";
    }

    public bool ShouldShowCharacterCounter(int currentLength) => currentLength > ShowCounterAt;

    public (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength)
    {
        var remaining = MaxNameLength - currentLength;
        var text = $"{currentLength}/{MaxNameLength}";
        return (text, remaining <= 20, remaining < 0);
    }

    private static string Normalize(string value) =>
        value.Normalize(System.Text.NormalizationForm.FormD)
             .ToLowerInvariant()
             .Trim();
}
```

**File:** `MyVocaList.Services/SongService.cs`

```csharp
using Microsoft.Extensions.Logging;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.Domain.Entities;
using MyVocaList.Domain.Interfaces;

namespace MyVocaList.Services;

public class SongService : ISongService
{
    private const int MaxTitleLength = 200;
    private const int ShowCounterAt = 160;

    private readonly ISongRepository _songRepo;
    private readonly ILogger<SongService> _logger;

    public SongService(ISongRepository songRepo, ILogger<SongService> logger)
    {
        _songRepo = songRepo;
        _logger = logger;
    }

    public (bool isValid, string message) ValidateTitleInput(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return (false, "Song title is required.");
        if (title.Trim().Length > MaxTitleLength)
            return (false, $"Song title cannot exceed {MaxTitleLength} characters.");
        return (true, string.Empty);
    }

    public async Task<(bool success, string message, Song? song)> CreateSongAsync(int artistId, string title, CancellationToken ct = default)
    {
        var (isValid, validationMessage) = ValidateTitleInput(title);
        if (!isValid) return (false, validationMessage, null);

        var trimmed = title.Trim();
        var normalized = Normalize(trimmed);

        if (await _songRepo.ExistsByTitleForArtistAsync(artistId, normalized, ct))
            return (false, $"A song titled \"{trimmed}\" already exists for this artist.", null);

        var song = new Song
        {
            ArtistId = artistId,
            Title = trimmed,
            TitleNormalized = normalized,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _songRepo.AddAsync(song, ct);
        _logger.LogInformation("Song created: {Title} (artist {ArtistId})", trimmed, artistId);
        return (true, $"Song \"{trimmed}\" added.", song);
    }

    public async Task<(bool success, string message)> UpdateSongAsync(int id, string title, CancellationToken ct = default)
    {
        var (isValid, validationMessage) = ValidateTitleInput(title);
        if (!isValid) return (false, validationMessage);

        var trimmed = title.Trim();
        var normalized = Normalize(trimmed);

        var song = await _songRepo.GetByIdAsync(id, ct);
        if (song == null) return (false, "Song not found.");

        if (!string.Equals(song.TitleNormalized, normalized, StringComparison.OrdinalIgnoreCase))
        {
            if (await _songRepo.ExistsByTitleForArtistAsync(song.ArtistId, normalized, ct))
                return (false, $"A song titled \"{trimmed}\" already exists for this artist.");
        }

        song.Title = trimmed;
        song.TitleNormalized = normalized;
        song.HasManualEdits = true;
        song.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation("Song updated: {Id} → {Title}", id, trimmed);
        return (true, "Song updated.");
    }

    public async Task<(bool success, string message)> DeleteSongsAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        await _songRepo.DeleteAsync(idList, ct);
        _logger.LogInformation("Songs deleted: {Ids}", string.Join(", ", idList));
        return (true, idList.Count == 1 ? "Song deleted." : $"{idList.Count} songs deleted.");
    }

    public async Task<(IEnumerable<SongListItemDto> items, int totalCount)> GetPagedSongsForListAsync(
        int artistId, int pageNumber, int pageSize, string? query = null, CancellationToken ct = default)
    {
        var normalized = string.IsNullOrWhiteSpace(query) ? null : Normalize(query.Trim());
        return await _songRepo.GetPagedByArtistAsync(artistId, pageNumber, pageSize, normalized, ct);
    }

    public bool ShouldShowCharacterCounter(int currentLength) => currentLength > ShowCounterAt;

    public (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength)
    {
        var remaining = MaxTitleLength - currentLength;
        var text = $"{currentLength}/{MaxTitleLength}";
        return (text, remaining <= 40, remaining < 0);
    }

    private static string Normalize(string value) =>
        value.Normalize(System.Text.NormalizationForm.FormD)
             .ToLowerInvariant()
             .Trim();
}
```

**Run tests — confirm Green.**

---

## Task 8 — Music Metadata Providers

**Files:**
- `MyVocaList.Services/Providers/MusicBrainzProvider.cs` (new)
- `MyVocaList.Services/Providers/DeezerProvider.cs` (new)
- `MyVocaList.Services/MusicMetadataService.cs` (new)

**MusicBrainzProvider.cs**
```csharp
using Microsoft.Extensions.Logging;
using MyVocaList.Contracts.DTOs;
using System.Net.Http;
using System.Text.Json;

namespace MyVocaList.Services.Providers;

public class MusicBrainzProvider : IMusicMetadataProvider
{
    public string ProviderName => "musicbrainz";

    private readonly HttpClient _http;
    private readonly ILogger<MusicBrainzProvider> _logger;
    private static readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private static DateTime _lastRequest = DateTime.MinValue;

    public MusicBrainzProvider(HttpClient http, ILogger<MusicBrainzProvider> logger)
    {
        _http = http;
        _logger = logger;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("MyVocaList/1.0 (heldercsousa@gmail.com)");
        _http.BaseAddress = new Uri("https://musicbrainz.org/ws/2/");
    }

    public async Task<IEnumerable<MusicSearchResultDto>> SearchAsync(string artistName, string songTitle, CancellationToken ct)
    {
        await _rateLimiter.WaitAsync(ct);
        try
        {
            var elapsed = (DateTime.UtcNow - _lastRequest).TotalMilliseconds;
            if (elapsed < 1100)
                await Task.Delay((int)(1100 - elapsed), ct);

            _lastRequest = DateTime.UtcNow;

            var query = Uri.EscapeDataString($"artist:{artistName} recording:{songTitle}");
            var response = await _http.GetAsync($"recording?query={query}&fmt=json&limit=5", ct);
            if (!response.IsSuccessStatusCode) return [];

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var recordings = doc.RootElement.GetProperty("recordings");

            return recordings.EnumerateArray().Select(r => new MusicSearchResultDto(
                r.GetProperty("id").GetString() ?? string.Empty,
                "musicbrainz",
                r.GetProperty("artist-credit")[0].GetProperty("artist").GetProperty("name").GetString() ?? string.Empty,
                r.GetProperty("title").GetString() ?? string.Empty)).ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "MusicBrainz search failed for {Artist} / {Title}", artistName, songTitle);
            return [];
        }
        finally
        {
            _rateLimiter.Release();
        }
    }
}
```

**DeezerProvider.cs**
```csharp
using Microsoft.Extensions.Logging;
using MyVocaList.Contracts.DTOs;
using System.Net.Http;
using System.Text.Json;

namespace MyVocaList.Services.Providers;

public class DeezerProvider : IMusicMetadataProvider
{
    public string ProviderName => "deezer";

    private readonly HttpClient _http;
    private readonly ILogger<DeezerProvider> _logger;

    public DeezerProvider(HttpClient http, ILogger<DeezerProvider> logger)
    {
        _http = http;
        _http.BaseAddress = new Uri("https://api.deezer.com/");
    }

    public async Task<IEnumerable<MusicSearchResultDto>> SearchAsync(string artistName, string songTitle, CancellationToken ct)
    {
        try
        {
            var query = Uri.EscapeDataString($"artist:\"{artistName}\" track:\"{songTitle}\"");
            var response = await _http.GetAsync($"search?q={query}&limit=5", ct);
            if (!response.IsSuccessStatusCode) return [];

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            return data.EnumerateArray().Select(r => new MusicSearchResultDto(
                r.GetProperty("id").GetInt64().ToString(),
                "deezer",
                r.GetProperty("artist").GetProperty("name").GetString() ?? string.Empty,
                r.GetProperty("title").GetString() ?? string.Empty)).ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Deezer search failed for {Artist} / {Title}", artistName, songTitle);
            return [];
        }
    }
}
```

**MusicMetadataService.cs**
```csharp
using Microsoft.Extensions.Logging;
using MyVocaList.Contracts.DTOs;

namespace MyVocaList.Services;

public class MusicMetadataService : IMusicMetadataService
{
    private readonly IEnumerable<IMusicMetadataProvider> _providers;
    private readonly ILogger<MusicMetadataService> _logger;

    public MusicMetadataService(IEnumerable<IMusicMetadataProvider> providers, ILogger<MusicMetadataService> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public async Task<IEnumerable<MusicSearchResultDto>> SearchAsync(string artistName, string songTitle, CancellationToken ct)
    {
        foreach (var provider in _providers)
        {
            var results = await provider.SearchAsync(artistName, songTitle, ct);
            var list = results.ToList();
            if (list.Count > 0) return list;
            _logger.LogInformation("{Provider} returned no results, trying next", provider.ProviderName);
        }
        return [];
    }
}
```

**Build:** `dotnet build` — confirm zero errors.

---

## Task 9 — DI Registration

**File:** `MyVocaList/MauiProgram.cs`

Add to the service registration section (Scoped):
```csharp
// Repositories
builder.Services.AddScoped<IArtistRepository, ArtistRepository>();
builder.Services.AddScoped<ISongRepository, SongRepository>();

// Services
builder.Services.AddScoped<IArtistService, ArtistService>();
builder.Services.AddScoped<ISongService, SongService>();
builder.Services.AddScoped<IMusicMetadataService, MusicMetadataService>();

// Music metadata providers (registered as IEnumerable<IMusicMetadataProvider>, order = priority)
builder.Services.AddHttpClient<MusicBrainzProvider>();
builder.Services.AddHttpClient<DeezerProvider>();
builder.Services.AddTransient<IMusicMetadataProvider, MusicBrainzProvider>(sp =>
    sp.GetRequiredService<MusicBrainzProvider>());
builder.Services.AddTransient<IMusicMetadataProvider, DeezerProvider>(sp =>
    sp.GetRequiredService<DeezerProvider>());

// Pages + ViewModels (Transient)
builder.Services.AddTransient<ArtistsPage>();
builder.Services.AddTransient<ArtistsViewModel>();
builder.Services.AddTransient<ArtistFormPage>();
builder.Services.AddTransient<ArtistFormViewModel>();
builder.Services.AddTransient<SongsPage>();
builder.Services.AddTransient<SongsViewModel>();
builder.Services.AddTransient<SongFormPage>();
builder.Services.AddTransient<SongFormViewModel>();
```

Add routes to `Navigation/Routes.cs`:
```csharp
public const string ArtistForm = nameof(ArtistFormPage);
public const string Songs = nameof(SongsPage);
public const string SongForm = nameof(SongFormPage);
```

Register routes in `AppShell.xaml.cs`:
```csharp
Routing.RegisterRoute(Routes.ArtistForm, typeof(ArtistFormPage));
Routing.RegisterRoute(Routes.Songs, typeof(SongsPage));
Routing.RegisterRoute(Routes.SongForm, typeof(SongFormPage));
```

**Build:** `dotnet build` — confirm zero errors.

---

## Task 10 — ArtistsViewModel

**File:** `MyVocaList/UI/ViewModels/ArtistsViewModel.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.UI.Models;
using System.Collections;

namespace MyVocaList.UI.ViewModels;

public partial class ArtistsViewModel : ViewModelBase
{
    private readonly IArtistService _artistService;
    private readonly ISnackbarService _snackbar;
    private readonly ILogger<ArtistsViewModel> _logger;

    private CancellationTokenSource _searchCts;
    private int _loadingPage;

    public ObservableRangeCollection<ArtistListItemDto> Artists { get; } = [];
    public ObservableRangeCollection<ArtistListItemDto> SelectedArtists { get; } = [];
    public IList SelectedArtistsRaw => SelectedArtists;

    [ObservableProperty] private bool _isInitialLoading;
    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] private bool _hasMoreItems;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isSearchMode;
    [ObservableProperty] private bool _isScrolled;
    [ObservableProperty] private int _selectedCount;
    [ObservableProperty] private BottomSheetState _confirmSheetState = BottomSheetState.Hidden;
    [ObservableProperty] private string _confirmMessage = string.Empty;
    [ObservableProperty] private string _confirmActionText = "Delete";

    public string AppBarTitle => SelectedCount > 0 ? $"{SelectedCount} selected" : "Artists";
    public bool CanEditSelected => SelectedCount == 1;
    public bool CanDeleteSelected => SelectedCount > 0;
    public bool IsEmptyNoItems => !IsInitialLoading && Artists.Count == 0 && string.IsNullOrEmpty(SearchText);
    public bool IsEmptyNoResults => !IsInitialLoading && Artists.Count == 0 && !string.IsNullOrEmpty(SearchText);

    public ArtistsViewModel(IArtistService artistService, ISnackbarService snackbar, ILogger<ArtistsViewModel> logger)
    {
        _artistService = artistService;
        _snackbar = snackbar;
        _logger = logger;
    }

    partial void OnSelectedCountChanged(int value)
    {
        OnPropertyChanged(nameof(AppBarTitle));
        OnPropertyChanged(nameof(CanEditSelected));
        OnPropertyChanged(nameof(CanDeleteSelected));
    }

    partial void OnSearchTextChanged(string value) => TriggerSearchDebounce();

    public void OnSelectionChanged(int count)
    {
        SelectedCount = count;
    }

    public async Task InitializeAsync()
    {
        if (!IsInitialLoading && Artists.Count > 0) return;
        IsInitialLoading = true;
        await Task.Yield();
        await LoadPageAsync(1);
        IsInitialLoading = false;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadPageAsync(1);
        IsRefreshing = false;
    }

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (!HasMoreItems) return;
        await LoadPageAsync(_loadingPage + 1);
    }

    [RelayCommand]
    private void OpenSearch()
    {
        IsSearchMode = true;
    }

    [RelayCommand]
    private void CloseSearch()
    {
        IsSearchMode = false;
        if (!string.IsNullOrEmpty(SearchText))
            SearchText = string.Empty;
        else
            _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        await Shell.Current.GoToAsync(Routes.ArtistForm);
    }

    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private async Task EditSelectedAsync()
    {
        var selected = SelectedArtists.FirstOrDefault();
        if (selected == null) return;
        await Shell.Current.GoToAsync($"{Routes.ArtistForm}?artistId={selected.Id}&artistName={Uri.EscapeDataString(selected.Name)}");
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelected))]
    private async Task DeleteSelectedAsync()
    {
        var ids = SelectedArtists.Select(a => a.Id).ToList();
        ConfirmMessage = await _artistService.GetDeleteConfirmationAsync(ids);
        ConfirmSheetState = BottomSheetState.HalfExpanded;
    }

    [RelayCommand]
    private async Task ConfirmDeleteAsync()
    {
        ConfirmSheetState = BottomSheetState.Hidden;
        var ids = SelectedArtists.Select(a => a.Id).ToList();
        var (success, message) = await _artistService.DeleteArtistsAsync(ids);
        if (success)
        {
            await _snackbar.ShowSuccessAsync(message);
            await RefreshAsync();
        }
        else
        {
            await _snackbar.ShowErrorAsync(message);
        }
    }

    [RelayCommand]
    private void DismissConfirm() => ConfirmSheetState = BottomSheetState.Hidden;

    [RelayCommand]
    private void SelectAll()
    {
        if (SelectedArtists.Count == Artists.Count)
        {
            RunOnUiThread(() =>
            {
                SelectedArtists.ClearRange();
                SelectedCount = 0;
            });
        }
        else
        {
            RunOnUiThread(() =>
            {
                SelectedArtists.ReplaceRange([.. Artists]);
                SelectedCount = Artists.Count;
            });
        }
    }

    private void TriggerSearchDebounce()
    {
        try { _searchCts?.Cancel(); _searchCts?.Dispose(); } catch { }
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(400, token);
                await LoadPageAsync(1, token);
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    private async Task LoadPageAsync(int page, CancellationToken ct = default)
    {
        try
        {
            _loadingPage = page;
            var (items, total) = await _artistService.GetPagedArtistsForListAsync(
                page, AppPagination.DefaultPageSize, SearchText, ct);

            var list = items.ToList();
            RunOnUiThread(() =>
            {
                if (page == 1)
                    Artists.ReplaceRange(list);
                else
                    Artists.AddRange(list);

                if (SelectedArtists.Count > 0)
                {
                    SelectedArtists.ClearRange();
                    SelectedCount = 0;
                }

                HasMoreItems = Artists.Count < total;
                NotifyEmptyStates();
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load artists (page {Page})", page);
        }
    }

    private void NotifyEmptyStates()
    {
        OnPropertyChanged(nameof(IsEmptyNoItems));
        OnPropertyChanged(nameof(IsEmptyNoResults));
    }

    public ICommand ConfirmActionCommand => ConfirmDeleteCommand;
}
```

**Build:** `dotnet build` — confirm zero errors.

---

## Task 11 — ArtistsPage XAML + Code-Behind

**File:** `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml`

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:dx="http://schemas.devexpress.com/maui"
             xmlns:dxcv="clr-namespace:DevExpress.Maui.CollectionView;assembly=DevExpress.Maui.CollectionView"
             xmlns:appbars="clr-namespace:MyVocaList.UI.Components.AppBars"
             xmlns:toolbars="clr-namespace:MyVocaList.UI.Components.Toolbars"
             xmlns:lists="clr-namespace:MyVocaList.UI.Components.Lists"
             xmlns:states="clr-namespace:MyVocaList.UI.Components.States"
             xmlns:sheets="clr-namespace:MyVocaList.UI.Components.Sheets"
             xmlns:vm="clr-namespace:MyVocaList.UI.ViewModels"
             xmlns:dto="clr-namespace:MyVocaList.Contracts.DTOs.List;assembly=MyVocaList.Contracts"
             xmlns:conv="clr-namespace:MyVocaList.UI.Converters"
             x:Class="MyVocaList.UI.Pages.Artists.ArtistsPage"
             x:DataType="vm:ArtistsViewModel"
             SafeAreaEdges="Container"
             BackgroundColor="{StaticResource Background}">

    <ContentPage.Resources>
        <conv:InverseBoolConverter x:Key="InverseBoolConverter" />
    </ContentPage.Resources>

    <Shell.TitleView>
        <Grid>
            <appbars:SmallAppBar
                Title="{Binding AppBarTitle}"
                Action1Icon="search_outlined"
                Action1Command="{Binding OpenSearchCommand}"
                IsElevated="{Binding IsScrolled}"
                IsVisible="{Binding IsSearchMode, Converter={StaticResource InverseBoolConverter}}" />
            <appbars:SearchAppBar
                SearchText="{Binding SearchText, Mode=TwoWay}"
                Placeholder="Search artists..."
                BackCommand="{Binding CloseSearchCommand}"
                IsElevated="{Binding IsScrolled}"
                IsVisible="{Binding IsSearchMode}" />
        </Grid>
    </Shell.TitleView>

    <Grid>
        <!-- Shimmer skeleton -->
        <dx:ShimmerView IsLoading="{Binding IsInitialLoading}">
            <dx:ShimmerView.LoadingView>
                <VerticalStackLayout Spacing="0">
                    <dx:DXBorder Style="{StaticResource SkeletonBone}" />
                    <dx:DXBorder Style="{StaticResource SkeletonBone}" />
                    <dx:DXBorder Style="{StaticResource SkeletonBone}" />
                    <dx:DXBorder Style="{StaticResource SkeletonBone}" />
                    <dx:DXBorder Style="{StaticResource SkeletonBone}" />
                    <dx:DXBorder Style="{StaticResource SkeletonBone}" />
                </VerticalStackLayout>
            </dx:ShimmerView.LoadingView>
            <dx:ShimmerView.Content>
                <Grid>
                    <dxcv:DXCollectionView x:Name="collectionView"
                        ItemsSource="{Binding Artists}"
                        SelectionMode="Multiple"
                        IndicatorColor="{StaticResource Primary}"
                        Margin="0,0,0,88"
                        IsPullToRefreshEnabled="True"
                        IsRefreshing="{Binding IsRefreshing, Mode=TwoWay}"
                        PullToRefreshCommand="{Binding RefreshCommand}"
                        IsLoadMoreEnabled="{Binding HasMoreItems}"
                        LoadMoreCommand="{Binding LoadMoreCommand}"
                        ItemSeparatorThickness="0"
                        Scrolled="OnCollectionViewScrolled"
                        SelectionChanged="OnSelectionChanged">

                        <dxcv:DXCollectionView.ItemTemplate>
                            <DataTemplate x:DataType="dto:ArtistListItemDto">
                                <lists:ListItem Headline="{Binding Name}"
                                               SupportingText="{Binding SongCountText}"
                                               IsSelected="False">
                                    <lists:ListItem.LeadingContent>
                                        <lists:ListItemLeadingIcon Icon="person_outlined" />
                                    </lists:ListItem.LeadingContent>
                                    <lists:ListItem.TrailingContent>
                                        <dx:CheckEdit IsChecked="False"
                                                      CheckedCheckBoxColor="{dx:ThemeColor Primary}"
                                                      InputTransparent="True"
                                                      VerticalOptions="Center" />
                                    </lists:ListItem.TrailingContent>
                                </lists:ListItem>
                            </DataTemplate>
                        </dxcv:DXCollectionView.ItemTemplate>

                        <dxcv:DXCollectionView.SelectedItemTemplate>
                            <DataTemplate x:DataType="dto:ArtistListItemDto">
                                <lists:ListItem Headline="{Binding Name}"
                                               SupportingText="{Binding SongCountText}"
                                               IsSelected="True">
                                    <lists:ListItem.LeadingContent>
                                        <lists:ListItemLeadingIcon Icon="person_outlined" />
                                    </lists:ListItem.LeadingContent>
                                    <lists:ListItem.TrailingContent>
                                        <dx:CheckEdit IsChecked="True"
                                                      CheckedCheckBoxColor="{dx:ThemeColor Primary}"
                                                      InputTransparent="True"
                                                      VerticalOptions="Center" />
                                    </lists:ListItem.TrailingContent>
                                </lists:ListItem>
                            </DataTemplate>
                        </dxcv:DXCollectionView.SelectedItemTemplate>
                    </dxcv:DXCollectionView>

                    <!-- Empty states -->
                    <states:EmptyState
                        Illustration="person_outlined"
                        Headline="No artists registered"
                        IsVisible="{Binding IsEmptyNoItems}"
                        Margin="32,32,32,80" />
                    <states:EmptyState
                        Illustration="search_outlined"
                        Headline="No artists found"
                        IsVisible="{Binding IsEmptyNoResults}"
                        Margin="32,32,32,80" />
                </Grid>
            </dx:ShimmerView.Content>
        </dx:ShimmerView>

        <!-- FloatingToolbar + FAB -->
        <HorizontalStackLayout HorizontalOptions="Center" VerticalOptions="End"
                               Margin="0,0,0,16" Spacing="8">
            <toolbars:FloatingToolbar
                VerticalOptions="Center"
                Action1Icon="checklist_outlined"
                Action1Command="{Binding SelectAllCommand}"
                Action1Description="Select all"
                Action2Icon="edit_outlined"
                Action2Command="{Binding EditSelectedCommand}"
                Action2Description="Edit artist"
                Action2IsSelected="{Binding CanEditSelected}"
                Action3Icon="delete_outlined"
                Action3Command="{Binding DeleteSelectedCommand}"
                Action3Description="Delete artists"
                Action3IsSelected="{Binding CanDeleteSelected}" />
            <dx:DXButton Style="{StaticResource Fab}"
                         Icon="add_outlined"
                         VerticalOptions="Center"
                         Command="{Binding AddCommand}" />
        </HorizontalStackLayout>

        <!-- Confirm Delete BottomSheet (inline — NOT ConfirmSheet component) -->
        <dx:BottomSheet x:Name="confirmSheet"
                        HalfExpandedRatio="0.28"
                        AllowedState="HalfExpanded"
                        IsModal="True"
                        ShowGrabber="True"
                        AllowDismiss="True"
                        BackgroundColor="{StaticResource Surface}"
                        CornerRadius="28"
                        StateChanged="OnConfirmSheetStateChanged">
            <VerticalStackLayout>
                <Label Text="{Binding ConfirmMessage}"
                       FontFamily="RobotoMedium" FontSize="16"
                       TextColor="{StaticResource OnSurface}"
                       HorizontalTextAlignment="Center"
                       Margin="24,20" />
                <BoxView Style="{StaticResource Divider}" />
                <dx:DXButton Content="{Binding ConfirmActionText}"
                             Style="{StaticResource BottomSheetDestructiveAction}"
                             Command="{Binding ConfirmActionCommand}" />
                <BoxView Style="{StaticResource Divider}" />
                <dx:DXButton Content="Cancel"
                             Style="{StaticResource BottomSheetCancelAction}"
                             Command="{Binding DismissConfirmCommand}" />
            </VerticalStackLayout>
        </dx:BottomSheet>
    </Grid>
</ContentPage>
```

**File:** `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml.cs`

```csharp
using DevExpress.Maui.CollectionView;
using DevExpress.Maui.Controls;
using MyVocaList.UI.ViewModels;
using System.Collections;

namespace MyVocaList.UI.Pages.Artists;

public partial class ArtistsPage : ContentPage
{
    private readonly ArtistsViewModel _viewModel;

    public ArtistsViewModel ViewModel => _viewModel;

    public ArtistsPage(ArtistsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        collectionView.SelectedItems = _viewModel.SelectedArtistsRaw;
        _ = _viewModel.InitializeAsync();
    }

    private void OnCollectionViewScrolled(object sender, DXCollectionViewScrolledEventArgs e)
    {
        _viewModel.IsScrolled = e.Offset > 0;
    }

    private void OnSelectionChanged(object sender, CollectionViewSelectionChangedEventArgs e)
    {
        var count = (collectionView.SelectedItems as ICollection)?.Count ?? 0;
        _viewModel.OnSelectionChanged(count);
    }

    private void OnConfirmSheetStateChanged(object? sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        if (e.NewValue == BottomSheetState.Hidden && _viewModel.ConfirmSheetState != BottomSheetState.Hidden)
            _viewModel.DismissConfirmCommand.Execute(null);

        if (_viewModel.ConfirmSheetState == BottomSheetState.HalfExpanded && e.NewValue == BottomSheetState.Hidden)
            return;

        if (e.NewValue != BottomSheetState.Hidden)
            confirmSheet.Show();
        else
            confirmSheet.Close();
    }

    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.ConfirmSheetState != BottomSheetState.Hidden)
        {
            _viewModel.ConfirmSheetState = BottomSheetState.Hidden;
            confirmSheet.Close();
            return true;
        }
        if (_viewModel.IsSearchMode)
        {
            _viewModel.CloseSearchCommand.Execute(null);
            return true;
        }
        return false;
    }
}
```

**Build:** `dotnet build` — confirm zero errors.

---

## Task 12 — ArtistFormPage

**File:** `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace MyVocaList.UI.ViewModels;

[QueryProperty(nameof(ArtistId), "artistId")]
[QueryProperty(nameof(ArtistName), "artistName")]
public partial class ArtistFormViewModel : ViewModelBase
{
    private readonly IArtistService _artistService;
    private readonly ISnackbarService _snackbar;
    private readonly ILogger<ArtistFormViewModel> _logger;

    [ObservableProperty] private int? _artistId;
    [ObservableProperty] private string _artistName = string.Empty;
    [ObservableProperty] private bool _nameHasError;
    [ObservableProperty] private string _nameErrorText = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _characterCounterText = string.Empty;
    [ObservableProperty] private bool _showCharacterCounter;
    [ObservableProperty] private bool _isCharacterCounterWarning;
    [ObservableProperty] private bool _isCharacterCounterError;

    public bool IsEditMode => ArtistId.HasValue;
    public string PageTitle => IsEditMode ? "Edit Artist" : "New Artist";

    public ArtistFormViewModel(IArtistService artistService, ISnackbarService snackbar, ILogger<ArtistFormViewModel> logger)
    {
        _artistService = artistService;
        _snackbar = snackbar;
        _logger = logger;
    }

    partial void OnArtistNameChanged(string value)
    {
        var (isValid, message) = _artistService.ValidateNameInput(value);
        NameHasError = !isValid && !string.IsNullOrEmpty(value);
        NameErrorText = NameHasError ? message : string.Empty;

        ShowCharacterCounter = _artistService.ShouldShowCharacterCounter(value.Length);
        if (ShowCharacterCounter)
        {
            var (text, isWarning, isError) = _artistService.GetCharacterCounterInfo(value.Length);
            CharacterCounterText = text;
            IsCharacterCounterWarning = isWarning;
            IsCharacterCounterError = isError;
        }
    }

    [RelayCommand]
    private Task CancelAsync() => Shell.Current.GoToAsync("..");

    [RelayCommand]
    private async Task SaveAsync()
    {
        var (isValid, message) = _artistService.ValidateNameInput(ArtistName);
        if (!isValid)
        {
            NameHasError = true;
            NameErrorText = message;
            return;
        }

        IsBusy = true;
        try
        {
            if (IsEditMode)
            {
                var (success, msg) = await _artistService.UpdateArtistAsync(ArtistId!.Value, ArtistName);
                if (!success) { NameHasError = true; NameErrorText = msg; return; }
                await _snackbar.ShowSuccessAsync(msg);
            }
            else
            {
                var (success, msg, _) = await _artistService.CreateArtistAsync(ArtistName);
                if (!success) { NameHasError = true; NameErrorText = msg; return; }
                await _snackbar.ShowSuccessAsync(msg);
            }
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

**File:** `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml`

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:dx="http://schemas.devexpress.com/maui"
             xmlns:dxe="clr-namespace:DevExpress.Maui.Editors;assembly=DevExpress.Maui.Editors"
             xmlns:appbars="clr-namespace:MyVocaList.UI.Components.AppBars"
             xmlns:vm="clr-namespace:MyVocaList.UI.ViewModels"
             x:Class="MyVocaList.UI.Pages.Artists.ArtistFormPage"
             x:DataType="vm:ArtistFormViewModel"
             SafeAreaEdges="All"
             BackgroundColor="{StaticResource Background}"
             Title="{Binding PageTitle}">

    <Shell.TitleView>
        <appbars:SmallAppBar
            Title="{Binding PageTitle}"
            NavigationIcon="arrow_back_outlined"
            NavigationCommand="{Binding CancelCommand}" />
    </Shell.TitleView>

    <ScrollView>
        <VerticalStackLayout Padding="24" Spacing="16">

            <dxe:TextEdit x:Name="nameEdit"
                          Text="{Binding ArtistName, Mode=TwoWay}"
                          LabelText="Artist name"
                          PlaceholderText="Enter artist name"
                          BoxMode="Outlined"
                          FocusedBorderColor="{StaticResource Primary}"
                          BorderColor="{StaticResource Outline}"
                          BackgroundColor="{StaticResource SurfaceContainerHighest}"
                          TextColor="{StaticResource OnSurface}"
                          MaxCharacterCount="100"
                          HasError="{Binding NameHasError}"
                          ErrorText="{Binding NameErrorText}" />

            <Label Text="{Binding CharacterCounterText}"
                   IsVisible="{Binding ShowCharacterCounter}"
                   FontFamily="RobotoRegular" FontSize="12"
                   HorizontalOptions="End">
                <Label.Triggers>
                    <DataTrigger TargetType="Label"
                                 Binding="{Binding IsCharacterCounterError}"
                                 Value="True">
                        <Setter Property="TextColor" Value="{StaticResource Error}" />
                    </DataTrigger>
                    <DataTrigger TargetType="Label"
                                 Binding="{Binding IsCharacterCounterWarning}"
                                 Value="True">
                        <Setter Property="TextColor" Value="{StaticResource Warning}" />
                    </DataTrigger>
                </Label.Triggers>
            </Label>

            <HorizontalStackLayout HorizontalOptions="End" Spacing="8">
                <dx:DXButton Content="Cancel"
                             Style="{StaticResource OutlinedButton}"
                             Padding="24,0"
                             Command="{Binding CancelCommand}" />
                <dx:DXButton Content="Save"
                             Style="{StaticResource FilledButton}"
                             Padding="24,0"
                             Command="{Binding SaveCommand}"
                             IsEnabled="{Binding IsBusy, Converter={StaticResource InverseBoolConverter}}" />
            </HorizontalStackLayout>

        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

**File:** `MyVocaList/UI/Pages/Artists/ArtistFormPage.xaml.cs`

```csharp
using MyVocaList.UI.ViewModels;

namespace MyVocaList.UI.Pages.Artists;

public partial class ArtistFormPage : ContentPage
{
    private readonly ArtistFormViewModel _viewModel;

    public ArtistFormPage(ArtistFormViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        nameEdit.Focus();
    }
}
```

**Build:** `dotnet build` — confirm zero errors.

---

## Task 13 — SongsViewModel

**File:** `MyVocaList/UI/ViewModels/SongsViewModel.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MyVocaList.Contracts.DTOs.List;
using MyVocaList.UI.Models;
using System.Collections;

namespace MyVocaList.UI.ViewModels;

[QueryProperty(nameof(ArtistId), "artistId")]
[QueryProperty(nameof(ArtistName), "artistName")]
public partial class SongsViewModel : ViewModelBase
{
    private readonly ISongService _songService;
    private readonly ISnackbarService _snackbar;
    private readonly ILogger<SongsViewModel> _logger;

    private CancellationTokenSource _searchCts;
    private int _loadingPage;

    [ObservableProperty] private int _artistId;
    [ObservableProperty] private string _artistName = string.Empty;

    public ObservableRangeCollection<SongListItemDto> Songs { get; } = [];
    public ObservableRangeCollection<SongListItemDto> SelectedSongs { get; } = [];
    public IList SelectedSongsRaw => SelectedSongs;

    [ObservableProperty] private bool _isInitialLoading;
    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] private bool _hasMoreItems;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isSearchMode;
    [ObservableProperty] private bool _isScrolled;
    [ObservableProperty] private int _selectedCount;
    [ObservableProperty] private BottomSheetState _confirmSheetState = BottomSheetState.Hidden;
    [ObservableProperty] private string _confirmMessage = string.Empty;

    public string AppBarTitle => SelectedCount > 0 ? $"{SelectedCount} selected" : ArtistName;
    public bool CanEditSelected => SelectedCount == 1;
    public bool CanDeleteSelected => SelectedCount > 0;
    public bool IsEmptyNoItems => !IsInitialLoading && Songs.Count == 0 && string.IsNullOrEmpty(SearchText);
    public bool IsEmptyNoResults => !IsInitialLoading && Songs.Count == 0 && !string.IsNullOrEmpty(SearchText);

    public SongsViewModel(ISongService songService, ISnackbarService snackbar, ILogger<SongsViewModel> logger)
    {
        _songService = songService;
        _snackbar = snackbar;
        _logger = logger;
    }

    partial void OnArtistNameChanged(string value) => OnPropertyChanged(nameof(AppBarTitle));

    partial void OnSelectedCountChanged(int value)
    {
        OnPropertyChanged(nameof(AppBarTitle));
        OnPropertyChanged(nameof(CanEditSelected));
        OnPropertyChanged(nameof(CanDeleteSelected));
    }

    partial void OnSearchTextChanged(string value) => TriggerSearchDebounce();

    public void OnSelectionChanged(int count) => SelectedCount = count;

    public async Task InitializeAsync()
    {
        if (!IsInitialLoading && Songs.Count > 0) return;
        IsInitialLoading = true;
        await Task.Yield();
        await LoadPageAsync(1);
        IsInitialLoading = false;
    }

    [RelayCommand] private async Task RefreshAsync() { IsRefreshing = true; await LoadPageAsync(1); IsRefreshing = false; }
    [RelayCommand] private async Task LoadMoreAsync() { if (HasMoreItems) await LoadPageAsync(_loadingPage + 1); }
    [RelayCommand] private void OpenSearch() => IsSearchMode = true;
    [RelayCommand] private void CloseSearch() { IsSearchMode = false; if (!string.IsNullOrEmpty(SearchText)) SearchText = string.Empty; else _ = RefreshAsync(); }

    [RelayCommand]
    private async Task AddAsync() =>
        await Shell.Current.GoToAsync($"{Routes.SongForm}?artistId={ArtistId}&artistName={Uri.EscapeDataString(ArtistName)}");

    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private async Task EditSelectedAsync()
    {
        var selected = SelectedSongs.FirstOrDefault();
        if (selected == null) return;
        await Shell.Current.GoToAsync($"{Routes.SongForm}?songId={selected.Id}&songTitle={Uri.EscapeDataString(selected.Title)}&artistId={ArtistId}&artistName={Uri.EscapeDataString(ArtistName)}");
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelected))]
    private async Task DeleteSelectedAsync()
    {
        var count = SelectedSongs.Count;
        ConfirmMessage = count == 1
            ? $"Delete \"{SelectedSongs.First().Title}\"? This cannot be undone."
            : $"Delete {count} songs? This cannot be undone.";
        ConfirmSheetState = BottomSheetState.HalfExpanded;
    }

    [RelayCommand]
    private async Task ConfirmDeleteAsync()
    {
        ConfirmSheetState = BottomSheetState.Hidden;
        var ids = SelectedSongs.Select(s => s.Id).ToList();
        var (success, message) = await _songService.DeleteSongsAsync(ids);
        if (success) { await _snackbar.ShowSuccessAsync(message); await RefreshAsync(); }
        else await _snackbar.ShowErrorAsync(message);
    }

    [RelayCommand] private void DismissConfirm() => ConfirmSheetState = BottomSheetState.Hidden;

    [RelayCommand]
    private void SelectAll()
    {
        if (SelectedSongs.Count == Songs.Count)
            RunOnUiThread(() => { SelectedSongs.ClearRange(); SelectedCount = 0; });
        else
            RunOnUiThread(() => { SelectedSongs.ReplaceRange([.. Songs]); SelectedCount = Songs.Count; });
    }

    private void TriggerSearchDebounce()
    {
        try { _searchCts?.Cancel(); _searchCts?.Dispose(); } catch { }
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;
        _ = Task.Run(async () => { try { await Task.Delay(400, token); await LoadPageAsync(1, token); } catch (OperationCanceledException) { } }, token);
    }

    private async Task LoadPageAsync(int page, CancellationToken ct = default)
    {
        try
        {
            _loadingPage = page;
            var (items, total) = await _songService.GetPagedSongsForListAsync(ArtistId, page, AppPagination.DefaultPageSize, SearchText, ct);
            var list = items.ToList();
            RunOnUiThread(() =>
            {
                if (page == 1) Songs.ReplaceRange(list); else Songs.AddRange(list);
                if (SelectedSongs.Count > 0) { SelectedSongs.ClearRange(); SelectedCount = 0; }
                HasMoreItems = Songs.Count < total;
                NotifyEmptyStates();
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _logger.LogError(ex, "Failed to load songs (page {Page})", page); }
    }

    private void NotifyEmptyStates() { OnPropertyChanged(nameof(IsEmptyNoItems)); OnPropertyChanged(nameof(IsEmptyNoResults)); }

    public ICommand ConfirmActionCommand => ConfirmDeleteCommand;
}
```

**Build:** `dotnet build` — confirm zero errors.

---

## Task 14 — SongsPage XAML + Code-Behind

**File:** `MyVocaList/UI/Pages/Songs/SongsPage.xaml`

Mirror the ArtistsPage structure — key differences:
- `Placeholder="Search songs..."`
- `ItemsSource="{Binding Songs}"`
- `SelectedItems` → `SelectedSongsRaw`
- `Headline="{Binding Title}"`, `SupportingText="{Binding ArtistName}"`
- Leading: `ListItemLeadingIcon Icon="music_note_outlined"`
- No FAB needed (songs are scoped to an artist; Add is via toolbar only)
- Action slots: SelectAll / Edit / Delete

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:dx="http://schemas.devexpress.com/maui"
             xmlns:dxcv="clr-namespace:DevExpress.Maui.CollectionView;assembly=DevExpress.Maui.CollectionView"
             xmlns:appbars="clr-namespace:MyVocaList.UI.Components.AppBars"
             xmlns:toolbars="clr-namespace:MyVocaList.UI.Components.Toolbars"
             xmlns:lists="clr-namespace:MyVocaList.UI.Components.Lists"
             xmlns:states="clr-namespace:MyVocaList.UI.Components.States"
             xmlns:vm="clr-namespace:MyVocaList.UI.ViewModels"
             xmlns:dto="clr-namespace:MyVocaList.Contracts.DTOs.List;assembly=MyVocaList.Contracts"
             xmlns:conv="clr-namespace:MyVocaList.UI.Converters"
             x:Class="MyVocaList.UI.Pages.Songs.SongsPage"
             x:DataType="vm:SongsViewModel"
             SafeAreaEdges="Container"
             BackgroundColor="{StaticResource Background}">

    <ContentPage.Resources>
        <conv:InverseBoolConverter x:Key="InverseBoolConverter" />
    </ContentPage.Resources>

    <Shell.TitleView>
        <Grid>
            <appbars:SmallAppBar
                Title="{Binding AppBarTitle}"
                NavigationIcon="arrow_back_outlined"
                NavigationCommand="{Binding CancelCommand}"
                Action1Icon="search_outlined"
                Action1Command="{Binding OpenSearchCommand}"
                IsElevated="{Binding IsScrolled}"
                IsVisible="{Binding IsSearchMode, Converter={StaticResource InverseBoolConverter}}" />
            <appbars:SearchAppBar
                SearchText="{Binding SearchText, Mode=TwoWay}"
                Placeholder="Search songs..."
                BackCommand="{Binding CloseSearchCommand}"
                IsElevated="{Binding IsScrolled}"
                IsVisible="{Binding IsSearchMode}" />
        </Grid>
    </Shell.TitleView>

    <Grid>
        <dx:ShimmerView IsLoading="{Binding IsInitialLoading}">
            <dx:ShimmerView.LoadingView>
                <VerticalStackLayout Spacing="0">
                    <dx:DXBorder Style="{StaticResource SkeletonBone}" />
                    <dx:DXBorder Style="{StaticResource SkeletonBone}" />
                    <dx:DXBorder Style="{StaticResource SkeletonBone}" />
                    <dx:DXBorder Style="{StaticResource SkeletonBone}" />
                    <dx:DXBorder Style="{StaticResource SkeletonBone}" />
                    <dx:DXBorder Style="{StaticResource SkeletonBone}" />
                </VerticalStackLayout>
            </dx:ShimmerView.LoadingView>
            <dx:ShimmerView.Content>
                <Grid>
                    <dxcv:DXCollectionView x:Name="collectionView"
                        ItemsSource="{Binding Songs}"
                        SelectionMode="Multiple"
                        IndicatorColor="{StaticResource Primary}"
                        Margin="0,0,0,88"
                        IsPullToRefreshEnabled="True"
                        IsRefreshing="{Binding IsRefreshing, Mode=TwoWay}"
                        PullToRefreshCommand="{Binding RefreshCommand}"
                        IsLoadMoreEnabled="{Binding HasMoreItems}"
                        LoadMoreCommand="{Binding LoadMoreCommand}"
                        ItemSeparatorThickness="0"
                        Scrolled="OnCollectionViewScrolled"
                        SelectionChanged="OnSelectionChanged">

                        <dxcv:DXCollectionView.ItemTemplate>
                            <DataTemplate x:DataType="dto:SongListItemDto">
                                <lists:ListItem Headline="{Binding Title}" IsSelected="False">
                                    <lists:ListItem.LeadingContent>
                                        <lists:ListItemLeadingIcon Icon="music_note_outlined" />
                                    </lists:ListItem.LeadingContent>
                                    <lists:ListItem.TrailingContent>
                                        <dx:CheckEdit IsChecked="False"
                                                      CheckedCheckBoxColor="{dx:ThemeColor Primary}"
                                                      InputTransparent="True"
                                                      VerticalOptions="Center" />
                                    </lists:ListItem.TrailingContent>
                                </lists:ListItem>
                            </DataTemplate>
                        </dxcv:DXCollectionView.ItemTemplate>

                        <dxcv:DXCollectionView.SelectedItemTemplate>
                            <DataTemplate x:DataType="dto:SongListItemDto">
                                <lists:ListItem Headline="{Binding Title}" IsSelected="True">
                                    <lists:ListItem.LeadingContent>
                                        <lists:ListItemLeadingIcon Icon="music_note_outlined" />
                                    </lists:ListItem.LeadingContent>
                                    <lists:ListItem.TrailingContent>
                                        <dx:CheckEdit IsChecked="True"
                                                      CheckedCheckBoxColor="{dx:ThemeColor Primary}"
                                                      InputTransparent="True"
                                                      VerticalOptions="Center" />
                                    </lists:ListItem.TrailingContent>
                                </lists:ListItem>
                            </DataTemplate>
                        </dxcv:DXCollectionView.SelectedItemTemplate>
                    </dxcv:DXCollectionView>

                    <states:EmptyState
                        Illustration="music_note_outlined"
                        Headline="No songs added"
                        IsVisible="{Binding IsEmptyNoItems}"
                        Margin="32,32,32,80" />
                    <states:EmptyState
                        Illustration="search_outlined"
                        Headline="No songs found"
                        IsVisible="{Binding IsEmptyNoResults}"
                        Margin="32,32,32,80" />
                </Grid>
            </dx:ShimmerView.Content>
        </dx:ShimmerView>

        <HorizontalStackLayout HorizontalOptions="Center" VerticalOptions="End"
                               Margin="0,0,0,16" Spacing="8">
            <toolbars:FloatingToolbar
                VerticalOptions="Center"
                Action1Icon="checklist_outlined"
                Action1Command="{Binding SelectAllCommand}"
                Action1Description="Select all"
                Action2Icon="add_outlined"
                Action2Command="{Binding AddCommand}"
                Action2Description="Add song"
                Action3Icon="edit_outlined"
                Action3Command="{Binding EditSelectedCommand}"
                Action3Description="Edit song"
                Action3IsSelected="{Binding CanEditSelected}"
                Action4Icon="delete_outlined"
                Action4Command="{Binding DeleteSelectedCommand}"
                Action4Description="Delete songs"
                Action4IsSelected="{Binding CanDeleteSelected}" />
        </HorizontalStackLayout>

        <dx:BottomSheet x:Name="confirmSheet"
                        HalfExpandedRatio="0.28"
                        AllowedState="HalfExpanded"
                        IsModal="True"
                        ShowGrabber="True"
                        AllowDismiss="True"
                        BackgroundColor="{StaticResource Surface}"
                        CornerRadius="28"
                        StateChanged="OnConfirmSheetStateChanged">
            <VerticalStackLayout>
                <Label Text="{Binding ConfirmMessage}"
                       FontFamily="RobotoMedium" FontSize="16"
                       TextColor="{StaticResource OnSurface}"
                       HorizontalTextAlignment="Center"
                       Margin="24,20" />
                <BoxView Style="{StaticResource Divider}" />
                <dx:DXButton Content="Delete"
                             Style="{StaticResource BottomSheetDestructiveAction}"
                             Command="{Binding ConfirmActionCommand}" />
                <BoxView Style="{StaticResource Divider}" />
                <dx:DXButton Content="Cancel"
                             Style="{StaticResource BottomSheetCancelAction}"
                             Command="{Binding DismissConfirmCommand}" />
            </VerticalStackLayout>
        </dx:BottomSheet>
    </Grid>
</ContentPage>
```

**File:** `MyVocaList/UI/Pages/Songs/SongsPage.xaml.cs`

```csharp
using DevExpress.Maui.CollectionView;
using DevExpress.Maui.Controls;
using MyVocaList.UI.ViewModels;
using System.Collections;

namespace MyVocaList.UI.Pages.Songs;

public partial class SongsPage : ContentPage
{
    private readonly SongsViewModel _viewModel;

    public SongsViewModel ViewModel => _viewModel;

    public SongsPage(SongsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        collectionView.SelectedItems = _viewModel.SelectedSongsRaw;
        _ = _viewModel.InitializeAsync();
    }

    private void OnCollectionViewScrolled(object sender, DXCollectionViewScrolledEventArgs e) =>
        _viewModel.IsScrolled = e.Offset > 0;

    private void OnSelectionChanged(object sender, CollectionViewSelectionChangedEventArgs e)
    {
        var count = (collectionView.SelectedItems as ICollection)?.Count ?? 0;
        _viewModel.OnSelectionChanged(count);
    }

    private void OnConfirmSheetStateChanged(object? sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        if (e.NewValue != BottomSheetState.Hidden) confirmSheet.Show();
        else confirmSheet.Close();
    }

    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.ConfirmSheetState != BottomSheetState.Hidden)
        {
            _viewModel.DismissConfirmCommand.Execute(null);
            confirmSheet.Close();
            return true;
        }
        if (_viewModel.IsSearchMode)
        {
            _viewModel.CloseSearchCommand.Execute(null);
            return true;
        }
        return false;
    }
}
```

**Build:** `dotnet build` — confirm zero errors.

---

## Task 15 — SongFormPage

**File:** `MyVocaList/UI/ViewModels/SongFormViewModel.cs`

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace MyVocaList.UI.ViewModels;

[QueryProperty(nameof(SongId), "songId")]
[QueryProperty(nameof(SongTitle), "songTitle")]
[QueryProperty(nameof(ArtistId), "artistId")]
[QueryProperty(nameof(ArtistName), "artistName")]
public partial class SongFormViewModel : ViewModelBase
{
    private readonly ISongService _songService;
    private readonly ISnackbarService _snackbar;
    private readonly ILogger<SongFormViewModel> _logger;

    [ObservableProperty] private int? _songId;
    [ObservableProperty] private string _songTitle = string.Empty;
    [ObservableProperty] private int _artistId;
    [ObservableProperty] private string _artistName = string.Empty;
    [ObservableProperty] private bool _titleHasError;
    [ObservableProperty] private string _titleErrorText = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _characterCounterText = string.Empty;
    [ObservableProperty] private bool _showCharacterCounter;
    [ObservableProperty] private bool _isCharacterCounterWarning;
    [ObservableProperty] private bool _isCharacterCounterError;

    public bool IsEditMode => SongId.HasValue;
    public string PageTitle => IsEditMode ? "Edit Song" : "New Song";

    public SongFormViewModel(ISongService songService, ISnackbarService snackbar, ILogger<SongFormViewModel> logger)
    {
        _songService = songService;
        _snackbar = snackbar;
        _logger = logger;
    }

    partial void OnSongTitleChanged(string value)
    {
        var (isValid, message) = _songService.ValidateTitleInput(value);
        TitleHasError = !isValid && !string.IsNullOrEmpty(value);
        TitleErrorText = TitleHasError ? message : string.Empty;

        ShowCharacterCounter = _songService.ShouldShowCharacterCounter(value.Length);
        if (ShowCharacterCounter)
        {
            var (text, isWarning, isError) = _songService.GetCharacterCounterInfo(value.Length);
            CharacterCounterText = text;
            IsCharacterCounterWarning = isWarning;
            IsCharacterCounterError = isError;
        }
    }

    [RelayCommand]
    private Task CancelAsync() => Shell.Current.GoToAsync("..");

    [RelayCommand]
    private async Task SaveAsync()
    {
        var (isValid, message) = _songService.ValidateTitleInput(SongTitle);
        if (!isValid) { TitleHasError = true; TitleErrorText = message; return; }

        IsBusy = true;
        try
        {
            if (IsEditMode)
            {
                var (success, msg) = await _songService.UpdateSongAsync(SongId!.Value, SongTitle);
                if (!success) { TitleHasError = true; TitleErrorText = msg; return; }
                await _snackbar.ShowSuccessAsync(msg);
            }
            else
            {
                var (success, msg, _) = await _songService.CreateSongAsync(ArtistId, SongTitle);
                if (!success) { TitleHasError = true; TitleErrorText = msg; return; }
                await _snackbar.ShowSuccessAsync(msg);
            }
            await Shell.Current.GoToAsync("..");
        }
        finally { IsBusy = false; }
    }
}
```

**File:** `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` — mirrors ArtistFormPage with `LabelText="Song title"`, `MaxCharacterCount="200"`, bound to `SongTitle` and `SongFormViewModel`.

**File:** `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` — same pattern as ArtistFormPage.

**Build:** `dotnet build` — confirm zero errors.

---

## Task 16 — Wire ArtistsPage into Shell Navigation

**File:** `MyVocaList/AppShell.xaml`

Add Artists to the flyout or tab bar as appropriate per the existing shell structure. If a tab entry is needed:
```xml
<TabBar>
    <!-- existing tabs ... -->
    <Tab Title="Artists" Icon="person_outlined">
        <ShellContent ContentTemplate="{DataTemplate pages:ArtistsPage}" Route="Artists" />
    </Tab>
</TabBar>
```

If route-only (navigated from another page):
```csharp
// AppShell.xaml.cs
Routing.RegisterRoute(Routes.ArtistForm, typeof(ArtistFormPage));
Routing.RegisterRoute(Routes.Songs, typeof(SongsPage));
Routing.RegisterRoute(Routes.SongForm, typeof(SongFormPage));
```

**Update `tasks.md`** — check off all 8 phases.

**Final build + test run:**
```bash
dotnet build
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --verbosity normal
```

**Run `/project:review` before committing.**

---

## Implementation Order Summary

| Task | Phase | TDD? | Build after? |
|------|-------|------|-------------|
| 1 | Domain Entities | No | Yes |
| 2 | Contracts/DTOs | No | Yes |
| 3 | Repository Interfaces | No | Yes |
| 4 | Service Interfaces | No | Yes |
| 5 | EF Config + Migration | No | Yes |
| 6 | Repository Implementations | **Yes** | Yes |
| 7 | Service Implementations | **Yes** | Yes |
| 8 | Music Metadata Providers | No | Yes |
| 9 | DI Registration | No | Yes |
| 10 | ArtistsViewModel | No | Yes |
| 11 | ArtistsPage XAML + CB | No | Yes |
| 12 | ArtistFormPage | No | Yes |
| 13 | SongsViewModel | No | Yes |
| 14 | SongsPage XAML + CB | No | Yes |
| 15 | SongFormPage | No | Yes |
| 16 | Shell wiring + final | No | Yes |
