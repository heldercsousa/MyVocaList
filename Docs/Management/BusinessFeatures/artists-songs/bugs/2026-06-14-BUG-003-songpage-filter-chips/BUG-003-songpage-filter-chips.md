# BUG-003: Filter Chips — Use FilterChipGroup Instead of DXButton (ArtistsPage)

**Status:** ✅ Recorded standard (preventative)
**Severity:** Medium (MD3 non-compliance)
**Discovered:** 2026-06-09
**Corrected:** 2026-06-14

---

> **Attribution correction (Helder, 2026-06-14):** This bug was originally filed against "SongsPage Filter Chips." That was a misattribution — **the only filter-chip surface in the app is `ArtistsPage`** (Author/Performer role filter). SongsPage has no filter UI and no song-filter domain concept. All "SongsPage chips" references have been re-pointed to **ArtistsPage**. The active fix for the ArtistsPage chip regression is tracked under branch `fix/artists-filter-regression` ("Artists CRUD List filter issue").

---

## Summary

Filter chips in this app must use `dxe:FilterChipGroup` per MD3 spec, **not** plain `DXButton` elements styled as chips. The canonical (and currently only) implementation is **ArtistsPage** (Author/Performer).

Root cause of the original lesson: developers may code filters without consulting DX documentation or the `.claude/library/devexpress-patterns.md` pre-implementation checklist, defaulting to the generic `DXButton`.

---

## Standing requirement

| Aspect | Specification |
|--------|---|
| **Component** | `dxe:FilterChipGroup` (DevExpress built-in MD3 Filter Chip component) |
| **Not:** | Plain `DXButton` elements with manual styling |
| **Reference** | `.claude/library/devexpress-patterns.md § FilterChipGroup` |
| **MD3 Spec** | m3.material.io/components/chips/overview — Filter variant |

---

## Reference implementation (the real chips surface)

See `ArtistsPage.xaml` and `ArtistsViewModel.cs` (Phase 16A Wave 2 — `4453bd7`):
- **XAML:** `dxe:FilterChipGroup` in the `CrudListView.FilterContent` slot with `SelectedItems` binding (TwoWay)
- **ViewModel:** `SelectedRoleFilters` IList property with `OnSelectedRoleFiltersChanged` partial method mapping chip selection to the Author/Performer domain enum, triggering a reload

---

## Not a new task

This is preventative documentation of the standing pattern. **Closure condition:** if any future page gains a filter feature, verify it uses `dxe:FilterChipGroup` as specified here. No SongsPage filter exists or is planned.
