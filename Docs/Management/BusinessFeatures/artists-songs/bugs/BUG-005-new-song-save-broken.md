# BUG-005 — New Song Save Has No Effect

**Filed:** 2026-06-11
**Feature area:** Artists & Songs — SongFormPage (Add mode)
**Severity:** High — core CRUD path silently broken

---

## Symptom

On `SongFormPage` in Add mode (no `songId` query parameter), tapping **Save** with a valid title and
a selected artist produces no observable result: no snackbar, no navigation, no error message. The song
is not persisted. The page may occasionally show a title-field error if the service returns a failure
tuple — but in the runtime-exception path, no feedback is shown at all.

---

## Root Cause

### Primary — `SaveAsync` silently drops exceptions thrown during `CreateSongAsync`

`SongFormViewModel.SaveAsync` (line 174–211 in `SongFormViewModel.cs`) wraps the service call in a
`try { ... } finally { IsBusy = false; }` block. This `finally` only resets the busy flag; it does
**not** catch exceptions.

```csharp
// SongFormViewModel.cs — SaveAsync, Add path (lines 193–205)
try
{
    // ...
    var (success, message, _) = await _songService.CreateSongAsync(
        SelectedArtistId.Value, title, FeaturedArtists?.Trim(), Lyrics?.Trim());
    if (success)
    {
        await _snackbarService.ShowSuccessAsync("Song created");
        await Shell.Current.GoToAsync("..");
    }
    else
    {
        TitleHasError = true;
        TitleErrorText = message;
    }
}
finally
{
    IsBusy = false;
}
```

`SaveCommand` is constructed as `new AsyncRelayCommand(SaveAsync)`. When `SaveAsync` throws,
`AsyncRelayCommand` (CommunityToolkit.Mvvm) stores the exception in `ExecutionTask.Exception` and does
**not** surface it to the user. The result is a silent failure with zero UI feedback.

### Secondary — zero unit test coverage for the Add mode save path

`SongFormViewModelTests.cs` contains tests only for `RemoveUrlCommand` (URL undo scenarios). There are
no tests for `SaveCommand` in Add mode, which means the failure mode was never encoded as a failing
test and went undetected.

### Triggering conditions for the runtime exception

The exception that triggers the silent failure is most likely one of:

1. **`DbUpdateException` from EF Core:** If `_songRepository.SaveChangesAsync` throws due to a SQLite
   constraint violation (e.g., duplicate `(ArtistId, Title)` that bypassed the service-level
   `ExistsByTitleForArtistAsync` check due to a collation edge case), the exception propagates out of
   `SaveAsync` and is silently captured by `AsyncRelayCommand`.

2. **`InvalidOperationException` from EF Core ChangeTracker:** `AppDbContext` is registered as
   `Scoped` (`AddDbContext` defaults to `ServiceLifetime.Scoped`). `SongFormPage` and
   `SongFormViewModel` are both `AddTransient` and are resolved via Shell route navigation, which
   resolves from the **root `IServiceProvider`** — not from a scoped container. A Scoped service
   resolved from the root container behaves as a singleton (created once, never disposed). Over time
   the `ChangeTracker` accumulates stale entries; a subsequent `SaveChangesAsync` call may throw if it
   encounters an entity in an unexpected state.

3. **Null reference on `song.OriginalArtist`:** `SongService.CreateSongAsync` retrieves the `Artist`
   entity via `_artistRepository.GetByIdAsync`, but only uses it for the null check — the `Artist`
   object is never assigned to `song.OriginalArtist`. EF Core with `ValueGeneratedOnAdd` on `Song.Id`
   and a valid `Song.ArtistId` FK should not need the navigation property for insert. However, if a
   future EF Core configuration change adds a required navigation property, this will throw on
   `SaveChanges`.

Condition 2 is the most reliable failure path in production (happens on every launch after the first
`SongFormPage` visit), because the root-container scope issue is structural, not conditional.

---

## Affected Files

| File | Location | What is affected |
|------|----------|-----------------|
| `SongFormViewModel.cs` | `MyVocaList/UI/ViewModels/SongFormViewModel.cs` | `SaveAsync` — missing `catch` block in Add path |
| `SongFormViewModelTests.cs` | `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` | No tests for `SaveCommand` in Add mode |
| `MauiProgram.cs` | `MyVocaList/MauiProgram.cs` | `SongFormViewModel` registered as `AddTransient`; `ISongService`/`ISongRepository`/`AppDbContext` as `AddScoped` — resolved from root when Shell navigates |

---

## Fix Approach

### Fix 1 — Catch exceptions in `SaveAsync` (required, `SongFormViewModel.cs`)

Wrap the entire `try` body with an inner `catch (Exception ex)` that logs the error and shows a
snackbar. This ensures that any unexpected exception from `CreateSongAsync` (including `DbUpdateException`
or `InvalidOperationException`) is surfaced to the user rather than silently dropped.

```csharp
// Proposed fix — SaveAsync Add path
try
{
    if (IsEditMode)
    {
        // ... existing edit path unchanged ...
    }
    else
    {
        var (success, message, _) = await _songService.CreateSongAsync(
            SelectedArtistId.Value, title, FeaturedArtists?.Trim(), Lyrics?.Trim());
        if (success)
        {
            await _snackbarService.ShowSuccessAsync("Song created");
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            TitleHasError = true;
            TitleErrorText = message;
        }
    }
}
catch (Exception ex)
{
    _logger.LogError(ex, "Save failed in {Mode} mode", IsEditMode ? "Edit" : "Add");
    await _snackbarService.ShowErrorAsync("Failed to save song. Please try again.");
}
finally
{
    IsBusy = false;
}
```

This fix guarantees the user always receives feedback on failure, regardless of the exception type.

### Fix 2 — Add unit tests for `SaveCommand` in Add mode (required, `SongFormViewModelTests.cs`)

Add the following test cases to `SongFormViewModelTests`:

- `SaveCommand_AddMode_ValidInput_CallsCreateSongAsync` — verifies `ISongService.CreateSongAsync` is
  called with correct arguments when title and artist are set
- `SaveCommand_AddMode_ServiceReturnsSuccess_ShowsSuccessSnackbar` — verifies
  `ISnackbarComponent.ShowSuccessAsync` is called
- `SaveCommand_AddMode_ServiceReturnsFailure_SetsTitleError` — verifies `TitleHasError = true` and
  `TitleErrorText` is set
- `SaveCommand_AddMode_ServiceThrows_ShowsErrorSnackbar` — verifies the catch block shows an error
  snackbar (the key regression test for this bug)
- `SaveCommand_AddMode_MissingArtist_SetsArtistError` — verifies `ArtistHasError = true` when
  `SelectedArtistId` is null

### Fix 3 — Scope investigation (deferred, architectural)

The Scoped-from-root issue with `AppDbContext` is a structural problem shared with all other form
pages (`VenueFormPage`, `PersonFormPage`, `ArtistFormPage`). The investigation and fix for that issue
should be tracked separately and applied uniformly. For this bug, Fix 1 (exception handler) is
sufficient to ensure the user sees feedback; it does not fully prevent the underlying `DbContext`
state issue.

Proposed scope: create a separate architectural bug or DevCycleCraft item:
"BUG-006 — Scoped services resolved from root container in MAUI Shell navigation."

---

## Acceptance Criteria

| AC ID | Criterion | Verification |
|-------|-----------|-------------|
| AC-BUG005-01 | Tapping Save with valid title and selected artist either navigates back with "Song created" snackbar, OR shows an error snackbar with "Failed to save song" — never silent | Manual test: add a song with valid inputs |
| AC-BUG005-02 | Tapping Save with no artist selected shows the artist field error ("Artist is required" or "Search and select...") — this already works but must be confirmed regression-free | Manual test: tap Save with empty artist field |
| AC-BUG005-03 | `SaveCommand_AddMode_ServiceThrows_ShowsErrorSnackbar` test passes (the key regression test) | `dotnet test --filter SongFormViewModelTests` |
| AC-BUG005-04 | All four `SaveCommand_AddMode_*` tests pass | `dotnet test --filter SongFormViewModelTests` |
| AC-BUG005-05 | Existing `RemoveUrlAsync` tests continue to pass (no regression) | `dotnet test --filter SongFormViewModelTests` |

---

## Out of Scope for This Fix

- The Scoped-from-root `AppDbContext` architectural issue (tracked separately as BUG-006)
- Edit mode save path (code inspection shows Edit mode has the same missing catch — fix it in the same
  commit, but it is not the reported symptom)
- `NavigateToYouTubeSearchAsync` message handler returning early when `!SongId.HasValue` in Add mode
  (a separate usability issue — URLs cannot be added until after first save; this is documented
  behavior in the "Paste URL" error text "Save the song first before adding URLs")
- `IsArtistLocked` is never set to `true` (the artist field remains enabled even when an `artistId`
  query param is passed) — this is a UX gap, not a functional bug

---

## Related Files (read-only, not modified by fix)

- `Services/SongService.cs` — `CreateSongAsync` is correctly implemented; service is not the bug
- `Infra/Repository/SongRepository.cs` — `AddAsync` + `SaveChangesAsync` is correctly implemented
- `Domain/ServicesInterfaces/ISongService.cs` — interface is correct
- `Domain/RepositoryInterface/ISongRepository.cs` — interface is correct
- `Infra/EntityEFConfig/SongConfiguration.cs` — entity config is correct
