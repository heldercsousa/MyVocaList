# Plan: DB-Side Collation — Remove All Normalized Columns

## Context

The codebase has two collation strategies in conflict:

- **Venue (correct):** no shadow column; queries hit `Name` directly via `EF.Functions.Collate(v.Name, "NOCASE_NOACCENT")`. The `CollationInterceptor` overrides SQLite's `like()` function so LIKE is also accent+case insensitive.
- **Artist / Song / Person (broken):** shadow `*Normalized` columns written by C# via `ToLowerInvariant()`. Case is handled; accents are **not** stripped. Searching "cafe" will not find "Café". UNIQUE indexes are on the shadow columns, not the display columns.

**Decided direction (Helder, 2026-06-01):** All normalization is the database's job. Shadow columns are removed entirely. UNIQUE indexes move to the original display columns using the collation. The `CollationInterceptor` cannot be removed — it is the only place SQLite learns about `NOCASE_NOACCENT` (collations are connection-scoped), and it overrides the `like()` function because SQLite's built-in LIKE ignores registered collations.

---

## CollationInterceptor — Verdict: Keep, No Changes

`Infra/Interceptor/CollationInterceptor.cs` does two irreplaceable things per connection:
1. Registers `NOCASE_NOACCENT` collation (FormD decomposition → strip `NonSpacingMark` → lowercase)
2. Overrides `like(pattern, text)` to normalize both sides — without this, `LIKE` ignores collation entirely

Registered in three places (all required):
- `MyVocaList/MauiProgram.cs` — production
- `Infra/AppDbContextFactory.cs` — EF migrations design-time
- `MyVocaList.Tests/Infrastructure/TestDbContextFactory.cs` — integration tests

---

## Implementation Phases

### Phase 1 — Verify: Add Accent+Case Tests to VenueRepositoryTests (run first, must pass before touching anything)

Add to `MyVocaList.Tests/Integration/Repositories/VenueRepositoryTests.cs`:

```csharp
[Fact]
public async Task GetPagedWithEventInfoAsync_AccentInsensitive_FindsAccentedVenue()
{
    await _repo.AddAsync(new Venue { Name = "Café do Brasil" });
    await _repo.SaveChangesAsync();
    var (items, total) = await _repo.GetPagedWithEventInfoAsync(1, 20, "cafe");
    Assert.Equal(1, total);
    Assert.Equal("Café do Brasil", items.First().venue.Name);
}

[Fact]
public async Task GetPagedWithEventInfoAsync_AccentAndCaseInsensitive_Combined()
{
    await _repo.AddAsync(new Venue { Name = "João's Bar" });
    await _repo.SaveChangesAsync();
    var (items, total) = await _repo.GetPagedWithEventInfoAsync(1, 20, "JOAO");
    Assert.Equal(1, total);
    Assert.Equal("João's Bar", items.First().venue.Name);
}

[Fact]
public async Task SearchByNameStartsWithAsync_AccentInsensitive_FindsMatch()
{
    await _repo.AddAsync(new Venue { Name = "Müller's Hall" });
    await _repo.SaveChangesAsync();
    var results = await _repo.SearchByNameStartsWithAsync("muller", 10);
    Assert.Single(results);
}
```

Run `dotnet test --filter "VenueRepositoryTests"` — all 3 must pass. If they do, the CollationInterceptor+Venue pattern is confirmed correct. Only then proceed.

---

### Phase 2 — Domain Entities: Remove Normalized Properties

**`Domain/Entity/Artist.cs`:** remove `NameNormalized` property.

**`Domain/Entity/Song.cs`:** remove `TitleNormalized` property.

**`Domain/Entity/Person.cs`:** remove `FullNameNormalized` property and `SetNormalizedName()` method.

---

### Phase 3 — EF Core Configurations: Move Indexes to Original Columns

**`Infra/EntityEFConfig/ArtistConfiguration.cs`:**
- Remove `builder.Property(a => a.NameNormalized)...`
- Change unique index: `builder.HasIndex(a => a.Name).IsUnique().HasDatabaseName("IX_Artists_Name")`
- Add `.UseCollation("NOCASE_NOACCENT")` to the `Name` property

**`Infra/EntityEFConfig/SongConfiguration.cs`:**
- Remove `builder.Property(s => s.TitleNormalized)...`
- Change composite unique index: `builder.HasIndex(s => new { s.ArtistId, s.Title }).IsUnique().HasDatabaseName("IX_Songs_ArtistId_Title")`
- Add `.UseCollation("NOCASE_NOACCENT")` to the `Title` property

**`Infra/EntityEFConfig/PersonConfiguration.cs`:**
- Remove `builder.Property(p => p.FullNameNormalized)...`
- Simple index: `builder.HasIndex(p => p.FullName).HasDatabaseName("IX_Persons_FullName")`
- Composite unique: `builder.HasIndex(p => new { p.FullName, p.BirthdayDayMonth }).IsUnique().HasFilter("[BirthdayDayMonth] IS NOT NULL").HasDatabaseName("IX_Persons_Name_Birthday")`
- Add `.UseCollation("NOCASE_NOACCENT")` to `FullName` property

---

### Phase 4 — Repositories: Query Original Columns with NOCASE_NOACCENT

Pattern: replace every `a.NameNormalized` / `s.TitleNormalized` / `p.FullNameNormalized` reference with the original column. Use `"NOCASE_NOACCENT"` consistently (not `"NOCASE"`). The Venue repository is the reference pattern.

**`Infra/Repository/ArtistRepository.cs`:**
- `GetPagedAsync`: `EF.Functions.Collate(a.Name, "NOCASE_NOACCENT")` + `OrderBy(a => a.Name)`
- `SearchByNameAsync`: same
- `ExistsByNameAsync` (both overloads): same
- Update parameter names from `normalizedName` → `name`, `normalizedQuery` → `query`

**`Infra/Repository/SongRepository.cs`:**
- `GetPagedAsync`: `EF.Functions.Collate(s.Title, "NOCASE_NOACCENT")` + `OrderBy(s => s.Title)`
- `ExistsByTitleForArtistAsync` (both overloads): same
- Update parameter names from `normalizedQuery/normalizedTitle` → `query/title`

**`Infra/Repository/PersonRepository.cs`:**
- All LIKE queries on `FullNameNormalized` → `FullName` with `"NOCASE_NOACCENT"`
- `OrderBy(p => p.FullNameNormalized)` → `OrderBy(p => p.FullName)`

**`Infra/Repository/CatalogRepository.cs`:** update any normalized query parameter forwarding.

---

### Phase 5 — Repository Interfaces: Update Signatures

**`Domain/RepositoryInterface/IArtistRepository.cs`:** rename `normalizedName` → `name`, `normalizedQuery` → `query` in method signatures.

**`Domain/RepositoryInterface/ISongRepository.cs`:** same for `normalizedTitle`, `normalizedQuery`.

**`Domain/RepositoryInterface/IPersonRepository.cs`:** same if applicable.

---

### Phase 6 — Services: Remove All ToLowerInvariant() Normalization

**`Services/ArtistService.cs`:** remove all `var normalized = name.ToLowerInvariant()` / `query.ToLowerInvariant()` lines. Pass `name.Trim()` / `query.Trim()` directly to repository calls.

**`Services/SongService.cs`:** same — remove all `normalized` local variables. Remove `TitleNormalized =` assignment from entity construction. Pass `title.Trim()` directly.

**`Services/CatalogService.cs`:** remove `normalized` variable, pass trimmed query directly.

**`Services/PersonService.cs`:** remove `SetNormalizedName(trimmedName)` call. `person.FullName = trimmedName` is sufficient.

---

### Phase 7 — EF Core Migration

```bash
dotnet ef migrations add RemoveNormalizedColumns --project Infra --startup-project MyVocaList
```

The migration must:
1. Drop columns: `Artists.NameNormalized`, `Songs.TitleNormalized`, `Persons.FullNameNormalized`
2. Drop old indexes: `IX_Artists_NameNormalized`, `IX_Songs_ArtistId_TitleNormalized`, `IX_Persons_FullNameNormalized`, `IX_Persons_Name_Birthday`
3. Create new indexes on original columns with COLLATE `NOCASE_NOACCENT`

> **SQLite limitation:** SQLite does not support `ALTER TABLE DROP COLUMN` on columns referenced by indexes. The migration will need to recreate the tables. EF Core's SQLite provider handles this automatically by generating `CREATE TABLE new`, copy data, drop old, rename. Verify the generated migration SQL before applying.

---

### Phase 8 — Tests: Fix and Add Collation Coverage

**Integration tests to update** (`MyVocaList.Tests/Integration/Repositories/`):
- `ArtistRepositoryTests.cs`: remove `NameNormalized =` from `MakeArtist()` helper; add accent-insensitive test (e.g., store "Björk", search "bjork")
- `SongRepositoryTests.cs`: remove `TitleNormalized =` from `MakeSong()` helper; add accent-insensitive test (e.g., store "Cliché", search "cliche")
- `PersonRepositoryTests.cs`: remove `SetNormalizedName()` from `Create()` helper; verify existing "João" test now tests accent-insensitive search properly

**Unit tests to update** (`MyVocaList.Tests/Unit/Services/`):
- `ArtistServiceTests.cs`, `SongServiceTests.cs`: remove `NameNormalized`/`TitleNormalized` from test entity construction
- Mock setups pass raw (non-lowercased) names — the service no longer normalizes before calling the repo

---

### Phase 9 — Multi-DB Provider Abstraction (Future-Proofing)

Add `Infra/Collation/CollationConstants.cs`:

```csharp
public static class CollationConstants
{
    // SQLite: registered via CollationInterceptor (FormD decomposition + lowercase)
    // PostgreSQL: "und-x-icu" with ICU or pg_trgm; or use citext extension
    // MSSQL: "SQL_Latin1_General_CP1_CI_AI" (CI=case insensitive, AI=accent insensitive)
    // MySQL: "utf8mb4_0900_ai_ci"
    public const string Default = "NOCASE_NOACCENT";
}
```

Replace all hardcoded `"NOCASE_NOACCENT"` strings in configurations and repositories with `CollationConstants.Default`. When a second DB provider is added, only `CollationConstants.Default` and a new interceptor/configuration need to change — no business logic is touched.

---

## Files Changed (summary)

| Area | Files |
|------|-------|
| Domain entities | `Artist.cs`, `Song.cs`, `Person.cs` |
| EF configurations | `ArtistConfiguration.cs`, `SongConfiguration.cs`, `PersonConfiguration.cs` |
| Repository interfaces | `IArtistRepository.cs`, `ISongRepository.cs`, `IPersonRepository.cs` |
| Repositories | `ArtistRepository.cs`, `SongRepository.cs`, `PersonRepository.cs`, `CatalogRepository.cs` |
| Services | `ArtistService.cs`, `SongService.cs`, `CatalogService.cs`, `PersonService.cs` |
| Migration | New `*_RemoveNormalizedColumns.cs` + snapshot update |
| Tests | `VenueRepositoryTests.cs` (new tests), `ArtistRepositoryTests.cs`, `SongRepositoryTests.cs`, `PersonRepositoryTests.cs`, `ArtistServiceTests.cs`, `SongServiceTests.cs` |
| New file | `Infra/Collation/CollationConstants.cs` |

---

## Verification

1. Phase 1 tests pass (Venue accent+case) before any other change is made
2. After all changes: `dotnet build` → 0 errors
3. `dotnet test` → all integration tests pass (including new accent tests for Artist, Song, Person)
4. Manual smoke: on the emulator, add "Café" as an artist; search "cafe" — must find it
5. Add "Café" and "CAFE" as two artists — the DB UNIQUE constraint must reject the second insert
