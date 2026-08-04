# Task Log — AppBar / SearchAppBar Interaction Redesign

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

---
## Task: T7 — Remove swap machinery from base layer / T8 — Update unit tests
**Plan:** `Docs/Management/DevCycleCraft/appbar-searchbar-redesign/plan.md`
**Status:** To Review
**Started:** 2026-07-20
**Completed:** 2026-07-20

T3–T6 (page conversions) were already committed (`a8bc5dd`) but left unchecked in `tasks.md` — corrected in this pass. T7's deletions (`IsSearchMode`, `OpenSearchCommand`, `CloseSearchCommand`, `CloseSearch()` from `CrudListViewModelBase`; `IsSearchMode`/`CloseSearchCommand` from `ICrudListViewModel`; the search branch in `CrudListPageBase.OnBackButtonPressed`) were found already applied in the working tree, uncommitted. Verified them against the task description — all three files match exactly what T7 specifies, no deviations. A solution-wide grep for the four deleted member names found matches only in `Docs/` and `.claude/library/` (historical spec/guideline prose) — zero remaining references in `.cs`/`.xaml` source or test code.

`CrudListViewModelBaseTests.cs` had no swap-member tests to remove (none existed in this file — the "remove/replace" clause was a no-op check). Added two new tests for the `SearchText` → debounce → fetch pipeline (T8, REQ-SEARCHBAR-14, Level B): one confirms a single `SearchText` change debounces into a `FetchPageAsync` call carrying the trimmed query; the other confirms rapid successive edits cancel the pending debounce so only the final query is fetched (proves `TriggerSearchDebounce`'s CTS-cancel behavior). Both use the existing `TestCrudListViewModel` double, extended with `LastFetchQuery`/`FetchPageCallCount` capture fields and a `ResetFetchCapture()` helper (called after `InitializeAsync()` so the initial load's fetch doesn't pollute assertions).

### AC traceability
| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|--------------------------|-------------|
| REQ-SEARCHBAR-07 | Back gesture closes confirm-sheet or navigates back — no search-mode branch | `CrudListPageBase.OnBackButtonPressed` (search branch removed) | Manual/E2E (UI navigation — no unit-testable surface; existing confirm-sheet/back-nav tests unaffected) |
| REQ-SEARCHBAR-08 | Swap machinery (`IsSearchMode`, open/close commands) fully removed from base VM/interface | `CrudListViewModelBase.cs`, `ICrudListViewModel.cs` | Compile-level: solution build 0 errors confirms no remaining consumer references |
| REQ-SEARCHBAR-14 | `CrudListViewModelBaseTests` updated — swap-member tests removed/replaced; `SearchText` → debounce → query pipeline covered | `MyVocaList.Tests/Unit/ViewModels/CrudListViewModelBaseTests.cs` | `SearchText_Changed_DebouncesIntoFetchWithTrimmedQuery`, `SearchText_RapidChanges_OnlyFetchesFinalQuery` |

### Changed files:
- `Docs/Management/DevCycleCraft/appbar-searchbar-redesign/tasks.md` (T3–T8 checked)
- `MyVocaList/UI/Pages/Base/CrudListPageBase.cs` (search branch removed — already staged, verified)
- `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs` (swap machinery removed — already staged, verified)
- `MyVocaList/UI/ViewModels/ICrudListViewModel.cs` (2 members removed — already staged, verified)
- `MyVocaList.Tests/Unit/ViewModels/CrudListViewModelBaseTests.cs` (2 new debounce-pipeline tests)

### Build notes
Build: solution-wide `dotnet build MyVocaList.sln` passed (0 errors, 110 pre-existing DX-trial/warnings). Android target build hit a transient `XA0142 llvm-objcopy` packaging error on first attempt (unrelated toolchain flake — no CS errors); retry succeeded 0 errors.
Tests: full `MyVocaList.Tests` suite — 524 passed, 0 failed, 0 skipped.
Files written and re-read: `CrudListViewModelBaseTests.cs` (re-read after edit — new tests + test-double fields confirmed in place); `tasks.md` (re-read — T3–T8 checkboxes confirmed).

### Checkpoint
- Branch/worktree: `feature/persistent-searchbar` @ `C:\Users\helde\source\repos\MyVocaList-wt-searchbar`
- Step: T7+T8 complete and committed. Task done — no further steps this dispatch.
- Last build/test: solution build 0 errors; `dotnet test` 524/524 passed
- Next command: none (stopped per briefing scope — T9/T10/T11 excluded)
- Context manifest: `tasks.md` (all checked through T8); `task-log.md` (this entry); `CrudListViewModelBaseTests.cs` (new tests); `CrudListViewModelBase.cs`, `ICrudListViewModel.cs`, `CrudListPageBase.cs` (T7 deletions, committed)

---
## Task: T9 — Verify picker pages untouched (REQ-SEARCHBAR-13)
**Plan:** `Docs/Management/DevCycleCraft/appbar-searchbar-redesign/tasks.md`
**Status:** To Review
**Started:** 2026-07-20
**Completed:** 2026-07-20
**Branch/worktree:** `feature/persistent-searchbar` @ `MyVocaList-wt-searchbar` (based on develop)

Verification only, no code changes. Grepped all 4 picker pages for `SearchAppBar` — each still references it as sole `Shell.TitleView` element:
`MyVocaList/UI/Pages/Songs/SongPickerPage.xaml`, `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml`, `MyVocaList/UI/Pages/Queue/QueueSongPickerPage.xaml`, `MyVocaList/UI/Pages/Songs/YouTubeSearchPage.xaml` — 1 match each, confirmed.

### AC traceability
| AC ID | Criterion | Implementation location | Test method |
|---|---|---|---|
| REQ-SEARCHBAR-13 | 4 picker pages remain untouched, still use `SearchAppBar` | (no change — verification) | Grep confirms `SearchAppBar` present in all 4; targeted `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` = 0 errors |

### Changed files:
- `Docs/Management/DevCycleCraft/appbar-searchbar-redesign/tasks.md` (T9 checked)
- `Docs/Management/DevCycleCraft/appbar-searchbar-redesign/task-log.md` (this entry)

### Build notes
Build: passed (0 errors, 9 warnings — pre-existing nullable/DX-trial warnings unrelated to this feature) — `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` (00:06:15). Tests: not run (no `.cs`/`.xaml` changed).
Files written and re-read: none edited; grep output reviewed for all 4 target files.
---
## Task: T10 — Guideline amendments
**Plan:** `Docs/Management/DevCycleCraft/appbar-searchbar-redesign/tasks.md`
**Status:** To Review
**Started:** 2026-07-20
**Completed:** 2026-07-20
**Branch/worktree:** `feature/persistent-searchbar` @ `MyVocaList-wt-searchbar` (based on develop)

Mechanical transcription of `design.md § Guideline amendments` (already Helder-approved via the design doc, not new policy):
- `crud-appbar-list-toolbar.md`: Law rewritten — `SmallAppBar` is now the sole `Shell.TitleView` occupant on CRUD list pages (no Grid/toggle); `SearchAppBar` retired for CRUD, still valid for the 4 picker pages pending their own migration. Standard-configuration XAML replaced with the `CrudListView SearchText`/`SearchPlaceholder` pattern. "Never" bullet updated to prohibit re-adding the bar-swap to CRUD pages.
- `m3-appbars.md`: "M3 Search App Bar" section gains a retirement-status note pointing to the new law. "M3 Search (standalone/detached)" section promoted from "NOT yet implemented" to "implemented: `SearchBar`" — documents the shipped component's spec table and its differences from `SearchAppBar` (no BackCommand, no auto-focus, no leading-icon toggle).
- `component-safety-gate.md`: governed-component table gains `SearchBar` (`UI/Components/AppBars/`) — 4 consumers (Venues/People/Artists/Songs via `CrudListView`).
- `Docs/Changelog/changelog.md`: new `amend:` entry (old rule / what was wrong / new rule / backward-compat / effective date) per `CLAUDE.md § Amending These Rules`.

No new policy invented — every change traces to a specific `design.md § Guideline amendments` line.

### Changed files:
- `.claude/library/crud-appbar-list-toolbar.md`
- `.claude/library/m3-appbars.md`
- `.claude/library/component-safety-gate.md`
- `Docs/Changelog/changelog.md`
- `Docs/Management/DevCycleCraft/appbar-searchbar-redesign/tasks.md` (T10 checked)
- `Docs/Management/DevCycleCraft/appbar-searchbar-redesign/task-log.md` (this entry)

### Build notes
Build: N/A (documentation-only edit, no `.cs`/`.xaml` touched). Tests: N/A.
Files written and re-read: all 3 library files and the changelog entry re-read after edit to confirm content landed correctly and Markdown fences/tables are intact.


---
## Task: T11 — BACKLOG follow-up registration
**Plan:** `Docs/Management/DevCycleCraft/appbar-searchbar-redesign/tasks.md`
**Status:** To Review
**Started:** 2026-07-20
**Completed:** 2026-07-20
**Branch/worktree:** `feature/persistent-searchbar` @ `MyVocaList-wt-searchbar` (based on develop)

`Docs/Management/BACKLOG.md` updated:
- Parent row **"AppBar / SearchAppBar Interaction Redesign — page-nav pattern + persistent search bar"** status `📋 Spec` → `🟡 In Progress`; Gate note rewritten to state T1–T11 code complete (build green) pending Helder's two manual gates (D-1 confirmation + emulator smoke test across the 4 CRUD pages) before final ✅.
- New nested row **"↳ SearchAppBar retirement — picker pages migration"** added directly under the parent — Goal: migrate the 4 picker pages off the retired `SearchAppBar` pattern; Gate: deferred by decision D-1, blocked on parent reaching ✅; Pointer to `design.md § Design decisions (D-1)`.
Both rows follow the BACKLOG header's PO-level template (Goal + Gate + one Pointer, ≤3 sentences, no technical detail in the row).

### Changed files:
- `Docs/Management/BACKLOG.md`
- `Docs/Management/DevCycleCraft/appbar-searchbar-redesign/tasks.md` (T11 checked)
- `Docs/Management/DevCycleCraft/appbar-searchbar-redesign/task-log.md` (this entry)

### Build notes
Build: N/A (documentation-only edit). Tests: N/A.
Files written and re-read: `BACKLOG.md` re-read after edit — both rows confirmed present with correct status glyphs and template shape.

All three remaining tasks (T9, T10, T11) complete. Feature code-complete pending Helder's manual gates noted above. No push attempted — git-credential-manager auth issue on this worktree per session briefing; local commits only, Helder to push.

---

## 2026-07-20/21 — Emulator smoke-test bug triage (BUG-048, BUG-049)

Found by Helder during S23 emulator smoke test of `feature/persistent-searchbar` (worktree `MyVocaList-wt-searchbar`, pushed at `997e784`). Registered in BACKLOG.md before investigation per `bug-tracking.md` HARD RULE. Root-cause investigation performed by a read-only Explore subagent against the same worktree.

### BUG-048 — CrudListView pagination reload (Major)

**Symptom:** reaching the end of the currently-loaded page reloads/re-renders the list as if it were the first page load, instead of appending the next page.

**Root cause:** `ObservableRangeCollection.AddRange()` (`MyVocaList/UI/Collections/ObservableRangeCollection.cs:25`) raises `NotifyCollectionChangedAction.Reset` instead of an `Add` notification. DevExpress `DXCollectionView` treats `Reset` as "entire source changed" and re-renders + resets scroll position. The pagination bookkeeping itself (`CrudListViewModelBase.LoadMoreAsync`, `_currentPage`/`HasMoreItems`/`_totalCount`) is correct — confirmed via `git log` that `ObservableRangeCollection.cs` was not touched by the persistent-searchbar commits. **Pre-existing latent bug**, not a regression from this feature — surfaced now because smoke testing exercises the CRUD list pages more thoroughly.

**Governance:** fix is contained entirely within `ObservableRangeCollection.cs` (change `AddRange` to raise an `Add`-action event with correct starting index/new items). No `CrudListView.xaml`/`.xaml.cs` change required → **four-gate governed-component process does NOT apply**.

**Scope decision (Helder, 2026-07-21):** fix in a fresh session, single-file worktree task, normal bug-fix workflow (worktree, regression test — Major/testable — mandatory, commit-message-as-spec).

### BUG-049 — VenueFormPage post-save navigation (Major)

**Symptom:** after editing and saving a Venue, navigation sometimes lands on Queue (home) instead of VenuesPage.

**Root cause:** `SaveCommand` in `VenueFormViewModel.cs:44` (and `PersonFormViewModel.cs:70`) has no re-entrancy guard (`AsyncRelayCommandOptions`/`IsBusy` `CanExecute`). A fast double-tap fires `SaveAsync` twice concurrently; each successful path ends in a bare relative `Shell.Current.GoToAsync("..")` (`VenueFormViewModel.cs:114`). The second pop overshoots the section's nav-stack root once the first pop has already returned to it, and Shell falls back to the first-declared `FlyoutItem` in `AppShell.xaml` (`queue`, `AppShell.xaml:68-70`).

**Relation to BUG-037:** different symptom (BUG-037 = no navigation happens, likely a null-Shell issue already patched) but same *family*: fragile bare relative `Shell.Current.GoToAsync("..")` with no re-entrancy/stack-state guard. `PersonFormViewModel` already has a safer pattern elsewhere (`INavigationService`, from the BUG-044 fix) not applied consistently to `SaveCommand`.

**Scope decision (Helder, 2026-07-21):** fix all four form ViewModels (`VenueFormViewModel`, `PersonFormViewModel`, `ArtistFormViewModel`, `SongFormViewModel`) in one task — consistent re-entrancy guard on `SaveCommand`, not a Venue-only patch. Single worktree task, fresh session. Regression test mandatory (Major/testable) — at minimum a ViewModel-level test proving a second concurrent `SaveAsync` invocation is a no-op while the first is in flight.

### Next step
Both fixes dispatched together as one bug-fix task in a fresh session (see BACKLOG rows below, status → 📋 Ready). Neither requires component-change governance.

---

## 2026-07-21 — BUG-048 / BUG-049 fix (worktree `bug-048-049`, branch `fix/bug-048-049-collection-reentrancy`)

**Plan:** N/A — commit-message-as-spec bug-fix pattern (`workflow.md § Bug Fix Pattern`).
**Status:** To Review
**Started:** 2026-07-21
**Completed:** 2026-07-21

### Changed files:
- `MyVocaList/UI/Collections/ObservableRangeCollection.cs` — BUG-048: `AddRange` now raises a single `NotifyCollectionChangedAction.Add` event (correct `NewStartingIndex` + `NewItems`) instead of `Reset`; also raises `Count`/`Item[]` `PropertyChanged` (previously missing, since `Items.Add` bypasses `ObservableCollection`'s own notification path).
- `MyVocaList/UI/ViewModels/VenueFormViewModel.cs` — BUG-049: `SaveCommand` now built with an explicit `() => !IsBusy` `CanExecute`; `OnIsBusyChanged` raises `NotifyCanExecuteChanged`; `SaveAsync` has an early-return `if (IsBusy) return;` guard.
- `MyVocaList/UI/ViewModels/PersonFormViewModel.cs` — same BUG-049 pattern.
- `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs` — same BUG-049 pattern.
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — same BUG-049 pattern.
- `MyVocaList.Tests/Unit/Collections/ObservableRangeCollectionTests.cs` — new file, 6 tests (BUG-048 regression + preserved null/empty no-op behavior).
- `MyVocaList.Tests/Unit/ViewModels/VenueFormViewModelTests.cs` — added `SaveCommand_DoubleInvokedWhileSaving_CallsCreateOnlyOnce` (BUG-049 regression).
- `MyVocaList.Tests/Unit/ViewModels/PersonFormViewModelTests.cs` — same, `CreatePersonAsync` variant.
- `MyVocaList.Tests/Unit/ViewModels/ArtistFormViewModelTests.cs` — same, `CreateArtistAsync` variant.
- `MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs` — same, `ISongResolutionService.ResolveAsync` variant (Song's save path routes through the resolution engine before any create/update call).

### Verification evidence

**Red/Green protocol (Major severity — mandatory per `bug-tracking.md`):**
- `git stash push -- MyVocaList/UI/Collections/ObservableRangeCollection.cs` → ran the 6 new collection tests → 3 FAILED (Reset instead of Add, wrong `NewStartingIndex`, no `NewItems`) → `git stash pop` → re-ran → 6/6 PASSED.
- `git stash push -- <4 ViewModel files>` → ran the 4 new `DoubleInvokedWhileSaving` tests → all 4 FAILED (`Moq.MockException: Expected invocation once, but was 2 times`) → `git stash pop` → re-ran → 4/4 PASSED.

**Build:** `dotnet build MyVocaList.Tests/MyVocaList.Tests.csproj` → 0 errors.
**Full test suite:** `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` → 511 passed, 0 failed.

### AC / regression-test traceability

| Bug | Regression test | Result |
|-----|------------------|--------|
| BUG-048 | `ObservableRangeCollectionTests.AddRange_OnPopulatedCollection_RaisesAddAction_NotReset` + 2 companion tests | Red→Green confirmed |
| BUG-049 | `{Venue,Person,Artist,Song}FormViewModelTests.SaveCommand_DoubleInvokedWhileSaving_CallsCreateOnlyOnce` (one per ViewModel) | Red→Green confirmed |

### Bug-fix commit messages (see git log for exact SHAs)
```
fix: ObservableRangeCollection — BUG-048 pagination reload resets scroll position

Root cause: AddRange raised NotifyCollectionChangedAction.Reset instead of Add, so DXCollectionView
treated every "load more" page fetch as a full source change and re-rendered/reset scroll position.
Fix: AddRange now raises a single Add-action event with the correct starting index and new items.
Regression risk: Low — ReplaceRange/ClearRange (which legitimately need Reset) are untouched.
```
```
fix: {Venue,Person,Artist,Song}FormViewModel — BUG-049 double-tap Save overshoots nav stack

Root cause: SaveCommand had no re-entrancy guard; a fast double-tap fired SaveAsync twice
concurrently, and the second bare relative Shell.Current.GoToAsync("..") overshot the nav stack
root, landing on the Queue FlyoutItem instead of the entity's list page.
Fix: SaveCommand now has an explicit CanExecute tied to IsBusy (NotifyCanExecuteChanged on
OnIsBusyChanged), plus an early-return `if (IsBusy) return;` guard inside SaveAsync as
defense-in-depth against callers that invoke the command without checking CanExecute.
Regression risk: Low — guard only blocks a second concurrent invocation; single-tap Save flow
is unchanged in all four ViewModels.
```

### Design concern (documented per Subagent Scope Constraint — not acted on)
CommunityToolkit.Mvvm's `AsyncRelayCommand` already defaults `AllowConcurrentExecutions = false`,
which should make `CanExecute` return `false` while a previous execution is still running. The
observed bug implies the DevExpress `DXButton` bound via `Command="{Binding SaveCommand}"` may
invoke `ICommand.Execute` without checking `CanExecute` first (or does not re-check between the
two taps of a fast double-tap). The explicit `IsBusy`-driven `CanExecute` + in-method early-return
guard added here is a safe, standard MVVM reinforcement regardless of the exact `DXButton` internals,
but the `DXButton` re-entrancy behavior itself was not root-caused further (out of scope — no
`DXButton`/XAML file was touched). Flagging for awareness in case other `DXButton`-bound commands
across the app have the same latent exposure.

