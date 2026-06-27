# Fix BUG-015 + BUG-016 — Plan

**Date:** 2026-06-25  
**Target:** Fix both bugs and merge to develop  
**Estimated effort:** 2–3 hours total  

---

## Global Constraints

- **Pattern:** Follow bug-tracking.md nesting format — bugs live under parent feature (Artists & Songs)
- **Testing:** Write regression tests first (Red), then fix (Green)  
- **Regression test requirement (per bug-tracking.md):** Both are Major/Critical → MANDATORY regression tests before close
- **Code compliance:** `myvocalist-coding` skill required for UI changes  
- **Build gate:** `dotnet build` 0 errors before each task completes  
- **Test gate:** All tests must pass before commit

---

## Wave 1 — Regression Tests (Tester subagent)

### Task A: BUG-015 — ArtistsViewModel.ViewCatalogCommand binding test (Red)

**Scope:** Write failing unit test proving the issue.

**Test location:** `MyVocaList.Tests/Unit/ViewModels/ArtistsViewModelTests.cs`

**Test requirement (from spec):**
- Create `ArtistsViewModel` with mocked service
- Call `ViewCatalogCommand.Execute(artistItem)` with a known `ArtistListItemDto`
- Verify command called `Shell.Current.GoToAsync("songs?artistId=X&artistName=Y")`
- Confirm test FAILS (proving the binding is broken)

---

### Task B: BUG-016 — SongsViewModel.AddSongCommand FAB navigation test (Red)

**Scope:** Write failing unit test proving the FAB navigation is broken.

**Test location:** `MyVocaList.Tests/Unit/ViewModels/SongsViewModelTests.cs`

**Test requirement (from spec):**
- Create `SongsViewModel` with mocked service
- Call `AddSongCommand.Execute()`
- Verify command called `Shell.Current.GoToAsync("song-form")`
- Confirm test FAILS (proving the route collision crashes navigation)

---

## Wave 2 — Bug Fixes (Builder subagents in parallel worktrees)

### Task C: Fix BUG-015 — ArtistsPage binding

**Root cause:** `RelativeSource AncestorType={x:Type vm:ArtistsViewModel}` cannot resolve ViewModel type in DataTemplate inside ContentView.

**Fix approach (Option A from spec):**
1. Add `x:Name="artistsPage"` to `ContentPage` root
2. Replace both trailing button bindings in `ItemTemplate` and `SelectedItemTemplate`:
   - From: `Command="{Binding Source={RelativeSource AncestorType={x:Type vm:ArtistsViewModel}}, Path=ViewCatalogCommand}"`
   - To: `Command="{Binding Source={x:Reference artistsPage}, Path=BindingContext.ViewCatalogCommand}"`
3. Verify test from Wave 1 Task A now passes (Green)

**Files to modify:**
- `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml`
- `MyVocaList/UI/ViewModels/ArtistsViewModel.cs` (no changes needed; validate command exists)

---

### Task D: Fix BUG-016 — Route collision

**Root cause:** `QueueSongPickerPage` FlyoutItem in `AppShell.xaml` registered route `"song-picker"` collides with `SongPickerPage` programmatic registration of same route in `AppShell.xaml.cs`.

**Fix approach (from spec):**
1. Rename `QueueSongPickerPage` route: `"song-picker"` → `"queue-song-picker"` in `AppShell.xaml`
2. Add constant to `Routes.cs`: `public const string QueueSongPicker = "queue-song-picker";`
3. Update all `GoToAsync("song-picker")` call sites that target `QueueSongPickerPage` to use new route
4. Verify test from Wave 1 Task B now passes (Green)

**Files to modify:**
- `MyVocaList/UI/AppShell.xaml`
- `MyVocaList/Navigation/Routes.cs`
- `MyVocaList/AppShell.xaml.cs` (find `Routing.RegisterRoute` call sites)
- Search codebase for `GoToAsync("song-picker")` to update callers

---

## Wave 3 — Post-Fix (Main agent)

### Task E: Verify & Commit

1. **Build:** `dotnet build` → 0 errors
2. **Test:** `dotnet test` → all pass (regression tests now Green)
3. **Update BACKLOG.md pattern** for both bugs:
   - Change status from `💡 Pending` to `✅ Fixed` (or `🟢 Done` per bug-tracking.md)
   - Ensure nesting under Artists & Songs feature is correct
4. **Commit:** Single commit with all bug fixes + BACKLOG update

---

## Acceptance Criteria

- [ ] BUG-015 fix: trailing button on ArtistsPage navigates to filtered Songs catalog
- [ ] BUG-016 fix: FAB on SongsPage navigates to SongFormPage without crash
- [ ] Both regression tests pass (Green)
- [ ] Build: 0 errors
- [ ] Test: all pass
- [ ] BACKLOG pattern corrected and committed

