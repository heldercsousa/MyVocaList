# Plan: CRUD XAML Sharing — BACKLOG entries + plan documents

## Context

Steps 1–5 of CRUD list deduplication are done (C# base classes). The 4 CRUD XAML pages
(VenuesPage, PeoplePage, SongsPage, ArtistsPage) still repeat ~130–190 lines of identical
structural XAML (ShimmerView, DXCollectionView configuration, FloatingToolbar, FAB,
BottomSheet, EmptyStates). A new tracked initiative will evaluate the best MAUI pattern to
eliminate this repetition and, once decided, migrate all four pages.

---

## Recommended XAML Sharing Approach (pre-decided by analysis)

After inspecting all four XAML files and applying the `maui-current-apis` guardrail, the
recommended approach is:

### `CrudListView` — ContentView with BindableProperties (Option A)

This is the standard MAUI composite-control pattern. A `ContentView` subclass owns all the
shared XAML elements and exposes BindableProperties for the entity-specific parts.

**Why not ControlTemplate:** ControlTemplate on ContentPage supports one `ContentPresenter`
slot — not enough for the ItemTemplate + SelectedItemTemplate + FilterContent triple-slot
requirement. ControlTemplate also cannot bind typed DataTemplates cleanly.

**Why not "no sharing":** The BottomSheet alone is 22 lines duplicated verbatim; the full
shared block is ~110 lines × 4 = 440 lines of copy-paste. Any future fix to the confirm
sheet or FloatingToolbar must be applied 4 times.

### What `CrudListView` owns (moves inside ContentView)

| Element | x:Name | Currently in |
|---------|--------|-------------|
| ShimmerView + SkeletonBones | — | All 4 pages |
| DXCollectionView | `collectionView` | All 4 pages (events wired via CrudListPageBase) |
| EmptyState "no items" | — | All 4 pages |
| EmptyState "no results" | — | All 4 pages |
| HorizontalStackLayout + FloatingToolbar | — | All 4 pages |
| FAB DXButton | — | All 4 pages |
| BottomSheet confirmSheet | `confirmSheet` | All 4 pages (events wired via CrudListPageBase) |

### BindableProperties required on `CrudListView`

| Property | Type | Description |
|----------|------|-------------|
| `ItemsSource` | `IList` | Entity collection (Venues, Songs, etc.) |
| `SelectedItemsSource` | `IList` | Entity selected collection (for DXCollectionView.SelectedItems) |
| `ItemTemplate` | `DataTemplate` | Unselected row DataTemplate |
| `SelectedItemTemplate` | `DataTemplate` | Selected row DataTemplate |
| `SearchPlaceholder` | `string` | "Search venues...", "Search songs...", etc. |
| `EmptyNoItemsIllustration` | `string` | Icon name for "no items" EmptyState |
| `EmptyNoItemsHeadline` | `string` | "No venue registered", etc. |
| `IsEmptyNoItems` | `bool` | Bound to entity-specific VM property (IsEmptyNoVenues, etc.) |
| `FabCommand` | `ICommand` | AddVenueCommand, AddSongCommand, etc. |
| `FabDescription` | `string` | SemanticProperties.Description for FAB |
| `FabIcon` | `string` | Default "add_outlined" |
| `AppBarSubtitle` | `string` | Optional subtitle (SongsPage only; empty = hidden) |
| `FilterContent` | `View` | Optional slot above list (ArtistsPage FilterChipGroup) |
| `ItemTapCommand` | `ICommand` | Optional. Wires DXCollectionView Tap (SongsPage only) |

### `ICrudListViewModel` extension needed

Add `bool IsEmptyNoResults { get; }` to the interface so `CrudListView` can bind to it via
BindingContext without compiled-binding type knowledge. All 4 VMs already implement this
property — it's just not on the interface yet.

### Impact on `CrudListPageBase`

- **Remove**: `ConfirmSheetStateRequired` event and `SelectionItemsWireUpRequired` event
  (these were bridges for the page to reach its own XAML elements; elements now live in
  `CrudListView` which handles wiring internally)
- **Keep**: `OnAppearing` → `InitializeAsync()`, `OnBackButtonPressed` logic

Each page code-behind constructor shrinks by ~10 lines (event subscriptions removed).

### What stays in the page XAML (after migration)

```xml
<pages:CrudListPageBase ...>
    <Shell.BackButtonBehavior>...</Shell.BackButtonBehavior>
    <Shell.TitleView>
        <Grid>
            <appbars:SmallAppBar ... />
            <appbars:SearchAppBar ... />
        </Grid>
    </Shell.TitleView>

    <!-- Optional: entity-specific content above list (ArtistsPage only) -->
    <pages:CrudListPageBase.FilterContent>
        <dxe:FilterChipGroup ... />
    </pages:CrudListPageBase.FilterContent>

    <views:CrudListView
        ItemsSource="{Binding Venues}"
        SelectedItemsSource="{Binding SelectedVenuesRaw}"
        IsEmptyNoItems="{Binding IsEmptyNoVenues}"
        SearchPlaceholder="Search venues..."
        EmptyNoItemsIllustration="nightlife_outlined"
        EmptyNoItemsHeadline="No venue registered"
        FabCommand="{Binding AddVenueCommand}"
        FabDescription="Add venue"
        ItemTemplate="{StaticResource VenueItemTemplate}"
        SelectedItemTemplate="{StaticResource VenueSelectedItemTemplate}" />

</pages:CrudListPageBase>
```

DataTemplates (ItemTemplate, SelectedItemTemplate) move to `ResourceDictionary` within
each page file, still typed with `x:DataType="dto:VenueListItemDto"`.

### Estimated line savings

| File | Before | After | Saved |
|------|--------|-------|-------|
| VenuesPage.xaml | 187 | ~60 | ~127 |
| PeoplePage.xaml | ~190 | ~65 | ~125 |
| SongsPage.xaml | 185 | ~65 | ~120 |
| ArtistsPage.xaml | 214 | ~75 | ~139 |
| CrudListView.xaml (new) | — | ~120 | — |
| **Net** | **~776** | **~385** | **~391 lines (~50%)** |

---

## BACKLOG Entries to Add

Append under the existing Step 6 row (still nested under **Code Cleanup — CRUD List Page Deduplication**):

```
| 2026-06 | ↳ Step 7: CRUD XAML sharing — approach evaluation & design | 💡 Pending |
  Research + decide MAUI XAML sharing pattern for 4 CRUD list pages. Deliverable: design.md
  with chosen approach + BindableProperty spec. Plan: crud-list-deduplication/xaml-sharing/design.md

| 2026-06 | ↳↳ Step 7a: Implement CrudListView ContentView + update CrudListPageBase | 🔴 Blocked |
  Blocked on Step 7. Create CrudListView.xaml/cs, add BindableProperties, extend
  ICrudListViewModel, simplify CrudListPageBase. Plan: crud-list-deduplication/xaml-sharing/plan-7a.md

| 2026-06 | ↳↳ Step 7b: Migrate VenuesPage.xaml to CrudListView | 🔴 Blocked |
  Blocked on Step 7a green. Plan: crud-list-deduplication/xaml-sharing/plan-7b.md

| 2026-06 | ↳↳ Step 7c: Migrate PeoplePage.xaml to CrudListView | 🔴 Blocked |
  Blocked on Step 7b green. Plan: crud-list-deduplication/xaml-sharing/plan-7c.md

| 2026-06 | ↳↳ Step 7d: Migrate SongsPage.xaml to CrudListView | 🔴 Blocked |
  Blocked on Step 7c green. Plan: crud-list-deduplication/xaml-sharing/plan-7d.md

| 2026-06 | ↳↳ Step 7e: Migrate ArtistsPage.xaml to CrudListView | 🔴 Blocked |
  Blocked on Step 7d green. Most complex — FilterChipGroup slot + ViewCatalog trailing.
  Plan: crud-list-deduplication/xaml-sharing/plan-7e.md
```

---

## Plan Documents to Create

All under `Docs/Management/DevCycleCraft/crud-list-deduplication/xaml-sharing/`:

### `design.md` (Step 7 — evaluation deliverable)
Documents the approach decision: why ContentView beats ControlTemplate, the full
BindableProperty table, the ICrudListViewModel extension, and CrudListPageBase changes.
Content: the "Recommended XAML Sharing Approach" section of this plan, formalized.

### `plan-7a.md` (implement CrudListView + update base)
Tasks:
- [ ] Extend `ICrudListViewModel` with `IsEmptyNoResults`
- [ ] Create `MyVocaList/UI/Views/CrudListView.xaml` + `.cs` with all BindableProperties
- [ ] Update `CrudListPageBase`: remove events, simplify constructor contract
- [ ] Register `CrudListView` in `MauiProgram.cs` if needed (ContentViews don't need DI)
- [ ] `dotnet build` green
- [ ] Register new files in MyVocaList.sln

### `plan-7b.md` (VenuesPage migration)
Tasks:
- [ ] Move ItemTemplate/SelectedItemTemplate to page ResourceDictionary
- [ ] Replace shared structural XAML with `<views:CrudListView ...>`
- [ ] Remove ConfirmSheetStateRequired + SelectionItemsWireUpRequired event subscriptions from VenuesPage.xaml.cs
- [ ] `dotnet build` green — smoke test on emulator

### `plan-7c.md` (PeoplePage migration)
Same structure as 7b. PeoplePage has `ListItemLeadingMonogram` in its item template — note this explicitly.

### `plan-7d.md` (SongsPage migration)
Same structure as 7b. Extra: wire `ItemTapCommand` BindableProperty (currently no-op handler).

### `plan-7e.md` (ArtistsPage migration — most complex)
Same structure. Extra:
- FilterChipGroup moves into `FilterContent` View slot on `CrudListView`
- ArtistsPage has `ViewCatalogCommand` button in trailing content — stays inside DataTemplate
- ArtistsPage Grid layout (2 rows) collapses since CrudListView handles layout internally

---

## Files to Create / Modify

| Action | Path |
|--------|------|
| Create | `Docs/Management/DevCycleCraft/crud-list-deduplication/xaml-sharing/design.md` |
| Create | `Docs/Management/DevCycleCraft/crud-list-deduplication/xaml-sharing/plan-7a.md` |
| Create | `Docs/Management/DevCycleCraft/crud-list-deduplication/xaml-sharing/plan-7b.md` |
| Create | `Docs/Management/DevCycleCraft/crud-list-deduplication/xaml-sharing/plan-7c.md` |
| Create | `Docs/Management/DevCycleCraft/crud-list-deduplication/xaml-sharing/plan-7d.md` |
| Create | `Docs/Management/DevCycleCraft/crud-list-deduplication/xaml-sharing/plan-7e.md` |
| Edit | `Docs/Management/BACKLOG.md` — append 6 rows |
| Edit | `MyVocaList.sln` — register all 6 new doc files |

---

## Verification

This plan produces only documentation + BACKLOG entries — no code. Verification:
- All 6 doc files readable via VS Solution Explorer (sln registered)
- BACKLOG rows have correct nesting (↳↳ prefix), statuses, and plan file links
- design.md contains the BindableProperty table and CrudListPageBase impact section
- Each plan-7x.md contains a checkboxed task list

---

## Review

Plan documents must be dispatched to a spec-reviewer subagent before Helder's approval gate
(per `.claude/agents/spec-reviewer.md`). The reviewer should check:
- design.md: is the approach complete? Are all BindableProperties covered? Is ICrudListViewModel
  extension impact correctly assessed?
- plan-7a.md: are task boundaries atomic? Is .sln registration included?
- plan-7b–7e: does each include a smoke-test step? Are entity-specific quirks called out?
