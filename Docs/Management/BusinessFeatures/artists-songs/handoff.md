# Handoff — Phase 16 (zippy-cuddling-penguin)

**Created:** 2026-05-17  
**Status:** Plan approved. tasks.md updated. NO code changes yet. Ready to dispatch Wave 1.

---

## Exact resume point

Wave 1 of Sub-phase A is the next action. Read the plan file (`zippy-cuddling-penguin.md`) and `tasks.md` (Phase 16A tasks) before starting.

No files in the MAUI project have been modified this session. The only change was updating `tasks.md` to add phases 16A, 16B, 16C.

---

## What was done this session

1. Identified 6 issues after Phases 10–15 (see plan file for full list)
2. Consulted MD3 docs (m3.material.io) via Playwright — confirmed Filter Chips are the correct MD3 pattern for list filtering
3. Confirmed DevExpress has `dxe:FilterChipGroup` built-in (no custom component needed)
4. Created plan file `zippy-cuddling-penguin.md` — user approved
5. Updated `Docs/specs/artists-songs/tasks.md` — replaced old Phase 16 with 16A/16B/16C

---

## Wave 1 — dispatch now (parallel, C# only, no XAML)

### A.1 — Fix AppShellViewModel.NavigateAsync
**File:** `MyVocaList/UI/ViewModels/AppShellViewModel.cs`  
**Problem:** Line 45 — `NavigationConfig.PageTypes.TryGetValue(route, out var pageType)` fails for routes like `"artists?mode=author"` because the dictionary key is `"artists"`.  
**Fix:** Split route at `?`, use base route for PageTypes lookup, parse query params, apply to ViewModel after PushAsync.

Current code (lines 28–50):
```csharp
private async Task NavigateAsync(string route)
{
    Shell.Current.FlyoutIsPresented = false;

    if (route == Routes.Queue)
    {
        await Shell.Current.Navigation.PopToRootAsync(animated: false);
        return;
    }

    if (route == Routes.Exit)
    {
        await Shell.Current.Navigation.PopToRootAsync(animated: false);
        ExitRequested?.Invoke();
        return;
    }

    if (!NavigationConfig.PageTypes.TryGetValue(route, out var pageType))
        return;

    var page = (Page)_serviceProvider.GetRequiredService(pageType);
    await Shell.Current.Navigation.PushAsync(page);
}
```

Required fix: split route at `?`, use `baseRoute` for `PageTypes.TryGetValue`, then after creating the page, parse key=value pairs from the query string and apply them to the page's `BindingContext` via reflection (QueryProperty pattern already used by Shell) or use `IQueryAttributable` if pages implement it.

**Simpler approach**: After `PushAsync(page)`, check if page.BindingContext implements `IQueryAttributable`, and if so call `ApplyQueryAttributes(dict)` where dict is parsed from the query string.

Check `ArtistsPage.xaml.cs` or `ArtistsViewModel.cs` to confirm whether `IQueryAttributable` is used or if it's `[QueryProperty]` attributes on the ViewModel. If `[QueryProperty]`, use reflection to set matching properties.

### A.2 — Simplify NavigationConfig.cs menu
**File:** `MyVocaList/Navigation/NavigationConfig.cs`  
**Fix:** Replace the two "Authors"/"Performers" entries in the Catalog group with a single "Artists" entry, no query param:

```csharp
new MenuGroup("Catalog", [
    new MenuItemDescription("Artists", "artist_outlined", Routes.Artists, navigateCommand),
    new MenuItemDescription("Songs",   "music_note_outlined", Routes.Songs, navigateCommand),
]),
```

Choose the best available icon for Artists (check what icons are used elsewhere — `mic_outlined` or `person_outlined` may not work in DX v25.2). Safe choices: `group_outlined` (used in People menu), `mic_outlined`, `music_note_outlined`.

---

## Wave 2 — after Wave 1 builds (XAML — incremental: one file at a time)

### A.3 + A.4 — ArtistsPage.xaml + ArtistsViewModel.cs
**ArtistsPage.xaml:**
- Add `dxe:FilterChipGroup` below Shell.TitleView, above ShimmerView
  - `ItemsSource` = inline `x:Array` of strings `["Authors", "Performers"]`
  - `SelectedItems` two-way bound to `SelectedRoleFilters` on ViewModel
  - The `dxe` namespace is `xmlns:dxe="clr-namespace:DevExpress.Maui.Editors;assembly=DevExpress.Maui.Editors"` (verify it's already declared)
- Fix empty state icon: `Illustration="person_outlined"` → `Illustration="group_outlined"` (around line 122)

**ArtistsViewModel.cs:**
- Add `[ObservableProperty] private IList _selectedRoleFilters;` (initialized to empty list)
- Add `partial void OnSelectedRoleFiltersChanged(IList value)` that maps selection to `ArtistRoleFilter`:
  - Both or neither selected → `All`
  - Only "Authors" → `AuthorsOnly`  
  - Only "Performers" → `PerformersOnly`
- Then call existing `OnRoleFilterChanged(_roleFilter)` or set `_roleFilter` directly and trigger reload

### A.5 — SongsPage.xaml (SEPARATE build pass after A.3+A.4)
- Add `<Shell.BackButtonBehavior IsVisible="False" IsEnabled="False" />` before `<Shell.TitleView>`
- Pattern from VenuesPage.xaml lines 20–26

---

## Wave 3 (after Wave 2 builds) — A.7 library files

**Files to update:**
- `.claude/library/devexpress-patterns.md` — add FilterChipGroup section
- `.claude/library/m3-components.md` — add Filter Chip section

FilterChipGroup confirmed pattern (from Context7 session):
```xml
<dxe:FilterChipGroup ItemsSource="{Binding FilterItems}"
                     SelectedItems="{Binding SelectedItems, Mode=TwoWay}"
                     DisplayMember="DisplayName" />
```
With inline items:
```xml
<dxe:FilterChipGroup SelectedItems="{Binding SelectedRoleFilters, Mode=TwoWay}">
    <dxe:FilterChipGroup.ItemsSource>
        <x:Array Type="{x:Type x:String}">
            <x:String>Authors</x:String>
            <x:String>Performers</x:String>
        </x:Array>
    </dxe:FilterChipGroup.ItemsSource>
</dxe:FilterChipGroup>
```

---

## Sub-phase B — API form integration (after 16A is green)

### B.1 ArtistFormViewModel.cs
Inject `IMusicMetadataService`. Add:
- `[ObservableProperty] private string _apiSearchText;`
- `[ObservableProperty] private IEnumerable<MusicSearchResultDto> _apiResults;`
- `[ObservableProperty] private bool _isApiSearching;`
- `[ObservableProperty] private string _apiStatusMessage;`
- `[ObservableProperty] private IEnumerable<MusicSearchResultDto> _duplicateSuggestions;`
- `SearchApiCommand` (AsyncRelayCommand) → calls `_metadataService.SearchArtistsAsync(ApiSearchText)`
- `SelectApiResultCommand` (RelayCommand<MusicSearchResultDto>) → sets ArtistName, ExternalId, ExternalProvider
- `SelectDuplicateCommand` (RelayCommand<MusicSearchResultDto>) → navigate to edit form for that artist
- Debounced duplicate detection via `IArtistService.SearchArtistsByNameAsync`

### B.2 ArtistFormPage.xaml
Below Name field:
- Duplicate suggestions (AutocompleteField style, "Did you mean?" list)
- API search strip: `dxe:TextEdit` + DXButton "Search" → `SearchApiCommand`
- API status label (hidden when empty)
- API results list (up to 5 items, ArtistName + Provider, tap → `SelectApiResultCommand`)

### B.3 SongFormViewModel.cs
Inject `IMusicMetadataService`. Add same API state properties + `SearchApiCommand` + `SelectApiResultCommand`.
`SelectApiResultCommand` also locks Artist field (`IsArtistLocked = true`) when result has matching artist.

### B.4 SongFormPage.xaml
Below Title field: API strip (same pattern as B.2).

---

## Key interfaces (already exist, no changes needed)
- `IMusicMetadataService.SearchArtistsAsync(string term, CancellationToken ct = default)`
- `IMusicMetadataService.SearchSongsAsync(string term, string? artistHint = null, CancellationToken ct = default)`
- `MusicSearchResultDto(ExternalId, Provider, ArtistName, SongTitle?, FeaturedArtists?)`

## DI registrations (already done in Phase 4, MauiProgram.cs lines 56–76)
- `IMusicMetadataService` / `MusicMetadataService` registered
- `MusicBrainzProvider`, `DeezerProvider` registered

---

## Final checklist before "done"
- [ ] All 16A tasks checked in tasks.md
- [ ] All 16B tasks checked in tasks.md  
- [ ] Build green, 157 tests passing
- [ ] Library files updated (devexpress-patterns.md, m3-components.md)
- [ ] End-to-end smoke test (16C.1)
- [ ] changelog.md updated
- [ ] Committed
