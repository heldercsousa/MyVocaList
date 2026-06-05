# Step 7d — Migrate SongsPage.xaml to CrudListView

**Depends on:** Step 7c (PeoplePage migrated, build green)  
**Unblocks:** Step 7e (ArtistsPage migration)  
**Risk:** Low-Medium — two entity-specific extras: AppBar subtitle binding and the Tap gesture on DXCollectionView (currently an empty no-op handler)

---

## Entity-Specific Details

| Property | Value |
|----------|-------|
| `ItemsSource` | `{Binding Songs}` |
| `SelectedItemsSource` | `{Binding SelectedSongsRaw}` |
| `IsEmptyNoItems` | `{Binding IsEmptyNoSongs}` |
| `SearchPlaceholder` | `"Search songs..."` |
| `EmptyNoItemsIllustration` | `"music_note_outlined"` |
| `EmptyNoItemsHeadline` | `"No song registered"` |
| `FabCommand` | `{Binding PrimaryFabCommand}` |
| `FabDescription` | `"Add song"` |
| `AppBarSubtitle` | `{Binding AppBarSubtitle}` |
| `FilterContent` | not set |
| `ItemTapCommand` | null (Tap handler is currently a no-op; see note below) |

**Item template leading:** `ListItemLeadingIcon Icon="music_note_outlined"`  
**Item template headline:** `{Binding Title}`  
**Item template support text:** `{Binding FeaturedArtists}`  
**Item template trailing:** `CheckEdit` (same as Venues)

### Tap gesture note

`SongsPage` currently declares `Tap="OnItemTapped"` on the DXCollectionView, with an empty
handler body and the comment *"Row tap = selection toggle only. Edit via FloatingToolbar
edit button."*

The tap is intentionally a no-op — selection toggle happens via DXCollectionView's built-in
`SelectionMode="Multiple"` behaviour, not the Tap event. Passing `ItemTapCommand=null` to
`CrudListView` (or not passing it at all) is the correct migration — no tap wiring needed.

If `CrudListView` conditionally wires Tap only when `ItemTapCommand != null`, SongsPage
omits this property entirely and the behaviour is preserved.

### AppBarSubtitle

`SmallAppBar` has a `Subtitle` property. SongsPage binds `{Binding AppBarSubtitle}` which
contains the artist name when in catalog mode. Pass `AppBarSubtitle="{Binding AppBarSubtitle}"`
to `CrudListView`; `CrudListView` forwards it to the internal SmallAppBar's Subtitle.

Wait — the AppBar is in the **page's** `Shell.TitleView`, not inside `CrudListView`. The
Subtitle is already on the SmallAppBar in the page XAML (Shell.TitleView stays in the page).
No `AppBarSubtitle` BindableProperty is needed on `CrudListView`. This was a planning
artefact — `AppBarSubtitle` can be removed from `CrudListView`'s BindableProperty list.

---

## Files Owned

| Action | File |
|--------|------|
| Edit | `MyVocaList/UI/Pages/Songs/SongsPage.xaml` |
| Edit | `MyVocaList/UI/Pages/Songs/SongsPage.xaml.cs` |

---

## Tasks

- [ ] **Edit `SongsPage.xaml`**
  - Move ItemTemplate + SelectedItemTemplate to resources / inline
  - Replace Grid body with `<views:CrudListView ...>` — omit `ItemTapCommand` (no-op)
  - Keep `Shell.TitleView` including `Subtitle="{Binding AppBarSubtitle}"` on SmallAppBar
  - Keep Shell.BackButtonBehavior unchanged
  - Add `xmlns:views` namespace

- [ ] **Edit `SongsPage.xaml.cs`**
  - Remove both event subscription lambdas from constructor
  - Keep `OnItemTapped` method if it is still referenced anywhere; otherwise remove it
    (it is only wired via XAML `Tap="OnItemTapped"` which is being removed)

- [ ] **Confirm `AppBarSubtitle` BindableProperty is NOT added to CrudListView**
  - The subtitle is already in SmallAppBar inside the page's Shell.TitleView — not in CrudListView

- [ ] **`dotnet build` — 0 errors**
- [ ] **Emulator smoke test**
  - Songs list loads with title + featured artists
  - Row tap triggers selection toggle (not navigation)
  - In catalog mode (ArtistName shown in AppBar subtitle) — verify subtitle still displays
  - Delete confirmation works
  - FAB navigates to SongFormPage

---

## Demo

> Open SongsPage (via Artists → View Catalog). Verify AppBar subtitle shows artist name.
> Tap a song row — verify it is selected (not navigated). Select + delete one song via
> confirmation sheet.

---

## Review lane: Standard
