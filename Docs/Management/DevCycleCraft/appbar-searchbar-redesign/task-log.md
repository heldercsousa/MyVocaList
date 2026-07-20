# Task Log — Persistent SearchBar in CrudListView

---
## Task: T1 + T2 — SearchBar component + CrudListView integration
**Plan:** Docs/Management/DevCycleCraft/appbar-searchbar-redesign/tasks.md
**Status:** To Review
**Started:** 2026-07-20
**Completed:** 2026-07-20
**Branch/worktree:** `feature/persistent-searchbar` @ `MyVocaList-wt-searchbar` (based on develop, ancestor verified)

### Changed files:
- `MyVocaList/UI/Components/AppBars/SearchBar.xaml` (new)
- `MyVocaList/UI/Components/AppBars/SearchBar.xaml.cs` (new)
- `MyVocaList/UI/Components/CrudListView.xaml`
- `MyVocaList/UI/Components/CrudListView.xaml.cs`
- `Docs/Management/DevCycleCraft/appbar-searchbar-redesign/tasks.md` (T1/T2 checked)
- `Docs/Management/DevCycleCraft/appbar-searchbar-redesign/task-log.md` (this file)

### AC traceability

| AC ID | Criterion (short) | Implementation location | Test method |
|---|---|---|---|
| REQ-SEARCHBAR-01 | Persistent bar docked at top of list content | CrudListView.xaml Row 0 `appbars:SearchBar` | Emulator smoke (T3–T6/Helder gate); pages show bar with placeholder already after T2 |
| REQ-SEARCHBAR-02 | M3 standalone spec: 56dp pill CR28, SurfaceContainerLow, 16dp margins, leading search_outlined (non-interactive StartIcon), auto clear icon, bodyLarge 16sp, ReturnType Search | SearchBar.xaml | Visual review + emulator smoke |
| REQ-SEARCHBAR-03 | No back arrow, no auto-focus | SearchBar.xaml/.cs (no leading button, no BackCommand, no Focus() override) | Emulator smoke: keyboard closed on load |
| REQ-SEARCHBAR-09 | CrudListView TwoWay `SearchText` BP; `SearchPlaceholder` now live | CrudListView.xaml.cs BPs + XAML bindings to internal SearchBar | Covered by existing SearchText debounce pipeline tests once pages wire it (T3+) |
| REQ-SEARCHBAR-10 | Chips row stacks below search bar | CrudListView.xaml rows 0/1/2 (filterRow shifted to Row 1) | Emulator smoke on ArtistsPage (T5) |
| REQ-SEARCHBAR-11 | Lift-on-scroll SurfaceContainerLow → SurfaceContainer | SearchBar.UpdateContainerColor via AppBarBase.IsElevated; CrudListView `IsSearchBarElevated` BP | Emulator smoke: scroll list |
| REQ-SEARCHBAR-12 | Empty-search-results state keeps working | IsEmptyNoResults EmptyState untouched (moved to Row 2 only) | Emulator smoke after page conversion |

Test note (Level C plumbing): T1/T2 add XAML + BindableProperty plumbing only — no business logic. Per `testing.md` Level C, no mandatory unit test; behavioral coverage lands with T8 (`CrudListViewModelBaseTests`) and the Helder emulator gate.

### Build notes
Build: passed (0 errors, `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`) — after T1 and again after T2. Tests: not run (no `.cs` test-relevant logic changed; ViewModels untouched). Commit SHA: recorded at commit.
Files written and re-read: SearchBar.xaml, SearchBar.xaml.cs, CrudListView.xaml (rows section), CrudListView.xaml.cs (BP section).

### Implementation notes / decisions (within scope)
- Leading non-interactive `search_outlined` rendered via `dxe:TextEdit` `StartIcon`/`StartIconColor` (the documented DX search-bar-in-DXBorder pattern in `dx-editors.md`) rather than a separate Image — satisfies "non-interactive, no button" with fewer visual-tree elements.
- `UpdateContainerColor` uses keys `SurfaceContainerLow`/`SurfaceContainer` (vs SearchAppBar's `Surface`/`SurfaceContainer`) per design table.
- D-4 elevation source: exposed `IsSearchBarElevated` BP forwarded via XAML binding to `SearchBar.IsElevated` (page binds `IsScrolled`), as briefed. Internal forwarding from `OnCollectionViewScrolled` was NOT added to avoid double-wiring; pages wire it in T3–T6.

### Checkpoint
- Branch/worktree: `feature/persistent-searchbar` @ `C:\Users\helde\source\repos\MyVocaList-wt-searchbar`
- Step: T1 and T2 complete (2 of 2 assigned); STOPPED after T2 per briefing
- Last build: 0 errors (android, post-T2)
- Next command: none — awaiting review; next tasks T3+ belong to a later dispatch
- Context manifest:
  - `Docs/Management/DevCycleCraft/appbar-searchbar-redesign/tasks.md` — task list, T1/T2 checked
  - `Docs/Management/DevCycleCraft/appbar-searchbar-redesign/design.md` — architecture + D-1..D-5
  - `MyVocaList/UI/Components/AppBars/SearchBar.xaml` — new component
  - `MyVocaList/UI/Components/AppBars/SearchBar.xaml.cs` — BPs + elevation
  - `MyVocaList/UI/Components/CrudListView.xaml` — new Row 0 + shifted rows
  - `MyVocaList/UI/Components/CrudListView.xaml.cs` — SearchText/IsSearchBarElevated BPs
