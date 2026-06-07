# Step 7e — Migrate ArtistsPage.xaml to CrudListView

**Depends on:** Step 7d (SongsPage migrated, build green)  
**Unblocks:** BACKLOG Step 6 — post-migration guideline review (a separate task, not a predecessor of Step 7e; it can only start after all migrations including this one are complete)  
**Risk:** Medium — most structurally complex page: extra FilterChipGroup row, ViewCatalog button in trailing content, 2-row Grid layout

---

## Entity-Specific Details

| Property | Value |
|----------|-------|
| `ItemsSource` | `{Binding Artists}` |
| `SelectedItemsSource` | `{Binding SelectedArtistsRaw}` |
| `IsEmptyNoItems` | `{Binding IsEmptyNoArtists}` |
| `SearchPlaceholder` | `"Search artists..."` |
| `EmptyNoItemsIllustration` | `"group_outlined"` |
| `EmptyNoItemsHeadline` | `"No artist registered"` |
| `FabCommand` | `{Binding AddArtistCommand}` |
| `FabDescription` | `"Add artist"` |
| `FilterContent` | FilterChipGroup for role filtering (Authors / Performers) |
| `AppBarSubtitle` | not set |
| `ItemTapCommand` | not set |

### FilterContent slot

ArtistsPage has a `FilterChipGroup` above the list. This moves into `CrudListView.FilterContent`:

```xml
<views:CrudListView.FilterContent>
    <dxe:FilterChipGroup SelectedItems="{Binding SelectedRoleFilters, Mode=TwoWay}"
                         Margin="16,4,16,4">
        <dxe:FilterChipGroup.ItemsSource>
            <x:Array Type="{x:Type x:String}">
                <x:String>Authors</x:String>
                <x:String>Performers</x:String>
            </x:Array>
        </dxe:FilterChipGroup.ItemsSource>
    </dxe:FilterChipGroup>
</views:CrudListView.FilterContent>
```

`CrudListView` internally shows the `FilterContent` view in `Grid.Row="0"` when it is not
null; the list occupies `Grid.Row="1"`. When `FilterContent` is null (all other pages), the
internal Grid uses a single row.

### ViewCatalog trailing button

ArtistsPage item templates have a `dx:DXButton` in `TrailingContent` binding to
`ViewCatalogCommand` via `RelativeSource AncestorType`. This button lives **inside the
DataTemplate** — it is passed as `CrudListView.ItemTemplate` / `SelectedItemTemplate`.
No changes needed to `CrudListView` for this — it's purely a template detail.

### Leading content difference

ArtistsPage leading content is a `CheckEdit` (not a `ListItemLeadingIcon`). This is inside
the DataTemplate — no impact on `CrudListView`.

### ArtistsPage current 2-row Grid layout

Currently ArtistsPage wraps everything in:
```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />  <!-- FilterChipGroup -->
        <RowDefinition Height="*" />     <!-- list + overlays -->
    </Grid.RowDefinitions>
```

After migration, this entire Grid is replaced by a single `<views:CrudListView ...>` with
`FilterContent` set. The 2-row layout is handled internally by `CrudListView`.

---

## Files Owned

| Action | File |
|--------|------|
| Edit | `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml` |
| Edit | `MyVocaList/UI/Pages/Artists/ArtistsPage.xaml.cs` |
| Edit | `MyVocaList/UI/Pages/CrudListPageBase.cs` |

---

## Tasks

- [x] **Edit `ArtistsPage.xaml`**
  - Move ItemTemplate + SelectedItemTemplate (including ViewCatalog DXButton) to resources / inline
  - Replace entire Grid (both rows) with `<views:CrudListView ...>` + `FilterContent` slot
  - Keep Shell.BackButtonBehavior and Shell.TitleView unchanged
  - Add `xmlns:views` namespace; keep `xmlns:dxe` for FilterChipGroup

- [x] **Edit `ArtistsPage.xaml.cs`**
  - Remove both event subscription lambdas from constructor (if not already removed in earlier steps)
  - Constructor body: InitializeComponent, _viewModel, BindingContext, AttachViewModel only

- [x] **Remove `[Obsolete]` events from `CrudListPageBase`**
  - Delete `ConfirmSheetStateRequired` and `SelectionItemsWireUpRequired` event declarations
    (marked `[Obsolete]` in Step 7a; now safe to delete since all 4 pages are migrated)
  - Confirm no remaining subscribers anywhere in the codebase

- [x] **`dotnet build` — 0 errors**
- [x] **`dotnet test` — 0 failures**
- [ ] **Emulator smoke test**
  - ArtistsPage loads with FilterChipGroup visible above list
  - Tap "Authors" chip → list filters to authors only
  - Tap "Performers" chip → list shows performers
  - Tap filter again to deselect → shows all
  - ViewCatalog button opens SongsPage for that artist
  - Select + delete confirmation works
  - FAB adds new artist

---

## Demo

> Open ArtistsPage. Verify filter chips are visible. Tap "Authors" — verify list updates
> without shimmer flash (ReloadAsync behaviour). Tap ViewCatalog on an artist — verify
> SongsPage opens with artist name in AppBar subtitle.

---

## Post-migration cleanup

After this step completes:
- Step 6 (post-migration guideline review) becomes actionable — update `.claude/library/crud-pages.md`
- BACKLOG parent row **Code Cleanup — CRUD List Page Deduplication** can move to `✅ Done`
  once Step 6 is also complete

---

## Review lane: Elevated
(Higher complexity — FilterContent slot + [Obsolete] event removal + 2-row Grid refactor)
