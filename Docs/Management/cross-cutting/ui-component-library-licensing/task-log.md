# Task log — UI component library licensing

Closed entries are in the `task-log-archive-YYYY-MM.md` files beside this log.

## 2026-11-21 — Syncfusion POC (spike, throwaway) — Closed

Helder's directive (2026-11-21): do the POC. Write new code only; existing code stays unchanged. Duplicate existing code under different names so the two can be compared side by side. Work in a worktree, and keep every step's status here so the job can resume after an abrupt stop.

- **Branch:** `task/spike-syncfusion` (from `develop`), never merged to `develop`.
- **Worktree:** `C:\Users\helde\source\repos\MyVocaList\.claude\worktrees\spike-syncfusion`
- **Naming rule:** each duplicate carries the `Sf` suffix (`CrudListViewSf`, `VenuesSfPage`, …). The originals are never edited.
- **Allowed additive edits to shared files** (on the spike branch only):
  - package entries in `Directory.Packages.props` / the `.csproj`;
  - one Syncfusion registration call in `MauiProgram.cs`;
  - one DI line per new page;
  - one Shell route / flyout entry, so the copy can be reached.

### Checkpoint

**Resume rule:** read this block, then `git -C <worktree> log --oneline -10`, then continue at the first step not marked `done`.

| # | Step | Status | Evidence / notes |
|---|------|--------|------------------|
| S0 | Checkpoint log on develop + worktree created | done | worktree at `efd4cdf3` (develop tip 2026-11-21) |
| S1 | Context7: Syncfusion MAUI packages (Toolkit vs commercial suite), net10 versions, license-key behaviour | done | Context7 `/syncfusion/maui-toolkit`. **Syncfusion Toolkit for .NET MAUI** (`Syncfusion.Maui.Toolkit`, latest 1.0.12 on nuget.org, LICENSE.txt in package — MIT per GitHub repo) has BottomSheet, Buttons, Chips, Shimmer, TextInputLayout, Popup, Expander, NavigationDrawer, TabView, PullToRefresh, SegmentedControl, Picker, Cards. Registered with `.ConfigureSyncfusionToolkit()`; **no license key**. It has **no ListView** → the POC uses stock MAUI `CollectionView` for the list. Commercial `SfListView` (needs a key) is not used. Context7 has no entry for the commercial suite. |
| S2 | Duplicate `CrudListView` → `CrudListViewSf` (Syncfusion controls) | done | `cb570587`. New `CrudListViewSf` + `SearchBarSf` copies. Verified 2026-11-21: `git diff --numstat` shows 0 deleted lines; originals untouched |
| S3 | Duplicate Venues page → `VenuesSfPage` using `CrudListViewSf`; route + DI | done | `1f696102`. `VenuesViewModel` reused unchanged. Hidden route `venues-sf`; DEBUG-only launch hook in `AppShell.xaml.cs` opens it first |
| S4 | Build net10.0-android, 0 errors | done | 0 errors, 10 warnings, none from Syncfusion. Tests pass (1 flaky repository test passed on retry) |
| S5 | Emulator run: screenshots of the DX vs Sf Venues pages (list, shimmer, selection, search, empty state, sheets) | done (partial) | `1a9e9974`, `spike-screenshots/`. List + delete sheet OK. **Failed on Sf:** (a) `SfTextInputLayout`+`Entry` search — no EditText in the native tree, no keyboard; (b) row tap / long-press never reach the VM (cause unconfirmed). Shimmer not captured; back-closes-sheet not checked |
| S5b | Diagnose + fix (a) and (b) in the Sf copies only (≤3 attempts each); re-capture search / empty / selection; check back button closing the sheet | done | `21aa8bba` (pushed). (a) `SfTextInputLayout` never sized its `Entry` → replaced by a plain MAUI `Entry` (works). (b) XAML command binding never executed → forwarded from code-behind (tap opens the form; long-press selects). Back button matches DX. Shimmer still not captured. 0 deleted lines vs base re-checked |
| S6 | Spike results written into `findings.md` § Spike results | done | `findings.md` § 7. Orchestrator screenshot review added a white strip behind the FAB, and noted the Toolkit has no Autocomplete/ComboBox |
| S7 | Commit spike branch + push; docs commit on develop | done | spike branch pushed at `21aa8bba`; this docs commit on develop |

**POC closed 2026-11-21.** Emulator left running. The worktree is kept, so Helder can deploy the branch and compare: the DEBUG build opens "Venues (Syncfusion POC)" first, and the original Venues is in the flyout.

**Context manifest** (≤ 8 files; spike branch files are listed in `findings.md` § 7):
- `Docs/Management/cross-cutting/ui-component-library-licensing/README.md`
- `Docs/Management/cross-cutting/ui-component-library-licensing/findings.md`
- this file

## 2026-11-22 — Migration implementation (plan.md, tasks.md)

Plan approved by Helder 2026-11-22 ("plan approved. Start implementation"); autonomy grant the same day (any step inside this project folder, push included). Orchestrator ledger: `.superpowers/sdd/plan/progress.md` (gitignored).

### Checkpoint

**Resume rule:** read this block, then the ledger, then `git log --oneline -10`; continue at the first step not marked `done`.

| # | Step | Status | Evidence / notes |
|---|------|--------|------------------|
| M0 | Item 🟡; wave 1 claimed `[~]` | done | `794c9f10` |
| M1 | Wave 1: T0.1a, T0.1b, T0.1c (scratch worktrees `spike-sf-controls-a/b/c`), T0.2 (`check-dx-script`) | done | all four reported; results below |
| M2 | Review T0.2, merge to develop; apply T0.1 results to design.md | done | T0.2 merged `0ae29cea`; T0.1 results applied to design.md (this commit) |
| M3 | T0.3 packages + license target | done | merged `db9b6d90` |
| M4 | Wave: T0.4a theme + T0.6a preview registry | pending | |

**Context manifest:** `plan.md`, `tasks.md`, `design.md`, `requirements.md`, this file, `.superpowers/sdd/plan/progress.md`.

## T3.2 — `TapToEditFieldSf` — Done (2026-11-23)

Commit `54f35e72`. Review: spec ✅, quality Approved, 3 Minor findings. Merged.
- The copy keeps every BindableProperty and every state transition (tap, focus, blur, commit, cancel). `TapToEditFieldLogic` is shared and unchanged.
- `DXImage` became an `Image` with `IconTint`.
- Emulator proof is in `artifacts/parity/t3.2/`: tap enters edit mode, the keyboard opens, text binds, and commit and cancel work. Consumers set `ContainerType="Outlined"` on their `SfTextInputLayout`.

**Deviation, carried to T3.3 by orchestrator ruling:** the original's `AutocompleteDXField` handling is removed. That handling covered three things: it released `IsLocked` on tap, it had a special case in `FocusEditor`, and a pick returned the field to text state. `ReleasesPickOnEdit` stays in the API but does nothing for now. T3.3 restores the hook for `AutocompleteFieldSf` at three points in `TapToEditFieldSf`: `OnTextTapped`, `FocusWrappedEditor`, and `Attach`/`Detach`. For that, T3.3 owns `TapToEditFieldSf`.

## T1.5–T1.8 — Phase 1 cut-overs — Done (2026-11-23)

Helder's go for these cut-overs is covered by his pre-authorization of 2026-11-22. The commits are:
- `098e1ce9` Settings: both `ShellContent`s plus `PageTypes`;
- `13cf75c5` About;
- `50cb9e42` BackupRestore;
- `b2bd3611` VenueMapPicker, a pushed route in `AppShell.xaml.cs`.

Each commit holds only the X1 lines and that page's `PreviewRoutes` removal; the orchestrator checked the combined diff. Merged.

**Verification (X2):**
- Build: 0 errors.
- Tests: 2056 passed. One failure, `SongRepositorySearchTests.BuildTrigramMatch_NoWordOfThreeChars_ReturnsNull`, is a known SQLite `ObjectDisposedException` flake that is unrelated to navigation.
- Pages: the flyout opens the Sf Settings, About and Backup pages. The venue form's "Pick on map" opens the Sf picker. "Syncfusion preview" now lists no pages. Screenshots are in `artifacts/parity/cutover-p1/`.

**Revert check (REQ-UIMIG-10):** reverting `b2bd3611` builds with 0 errors and restores the DX registration. The revert was aborted afterwards.

**Pending:** REQ-UIMIG-16 waits for Helder's Syncfusion key. A Debug build without the key shows no trial notice.

The DX originals stay in the code until T5.1a.

## T2.5 — `SearchBarSf`, `SmallAppBarSf` — Done (2026-11-23)

The implementor committed `05694b7a` and `e2ec34e9`. Fix round 1 (`18f10cef`) gave `OutlinedTextFieldSf` two new properties: `ShowOutline` (default true) and `ReturnType`. `SearchBarSf` uses a borderless field with the Search key. The re-review marked both Important findings ADDRESSED. Merged.

The search field is `OutlinedTextFieldSf`, an accepted deviation. Emulator results (`artifacts/parity/t2.5/*-v2.png`):
- the keyboard opens;
- text reaches the VM;
- the Search key fires Completed;
- clear empties the field;
- no underline and no inner outline appear.

`SmallAppBarSf` renders correctly. No unit test (Level C).

`OutlinedTextFieldSf` now has a second consumer pattern, so it is governed once a second consumer binds to it. At that point it goes into `component-safety-gate.md`.

## T2.7 — `SearchAppBarSf`, `ListItemOverflowButtonSf` — Done (2026-11-24)

Merged. The work landed in three commits:
- `ef890c67`: `VerticalOptions=Center` on `OverflowMenuButtonSf`, the Minor deferred from T2.6.
- `544070a4`: `SearchAppBarSf`.
- `b55ae05c`: `ListItemOverflowButtonSf`.

Review: spec ✅, quality Approved, 3 Minor.

The original `SearchAppBar` uses a bare `TextEdit`, borderless with the Search key. The copy matches it with `OutlinedTextFieldSf` (`ShowOutline=False`, `ReturnType=Search`).

**Notes carried forward:**
- **T2.8/T2.9:** `OverflowMenuButtonSf` does nothing when it has no `Menu` and no `IMenuHostSf` ancestor, so the Sf page or `CrudListViewSf` must implement `IMenuHostSf` where the DX one implements `IMenuHost`.
- **Coupling:** `SearchAppBarSf` focuses the inner `Entry` through `OutlinedTextFieldSf.Content`. A public Focus/Unfocus forward on `OutlinedTextFieldSf` is the cleaner fix. It is a governed-component change, so it is logged as a follow-up.

## T3.8 — `FeedbackSfPage` copy, subagent half — merged (2026-11-24)

Implementor `20a892f8`. Review: spec ✅, Minor findings only. Merged; parity (P4–P6) is still to do, so the box stays `[~]`.

**Replacements:**
- `ComboBoxEdit` → `SfComboBox` inside `SfTextInputLayout`. This nesting is supported; `ShowBorder="False"` on the combo; the dark properties include `SelectedDropDownItemTextStyle`.
- Text and multiline fields → `SfTextInputLayout` with `ContainerType="Outlined"`.
- Buttons → `SfFilledButton`.

**Fix at parity:** the message `Editor` has no maximum height. DX capped it at 10 lines, so add `MaximumHeightRequest` ≈ 240. If the page crashes at launch, `SfComboBox.TextColor` is the first suspect: it is absent from the 35.2.12 XML, although T0.1b accepted it.

**P3 lines:** `builder.Services.AddTransient<FeedbackSfPage>();`, `("Feedback", "feedback-sf", typeof(FeedbackSfPage))`.

## T2.8a / T2.8b — `CrudListViewSf` — Done (2026-11-24)

Implementor commits:
- `bfb90519`: list body on `SfListView` inside `SfPullToRefresh`, using the T2.0 patterns. `SelectedItems` syncs two ways; tap and long-press go to the VM commands; `LoadMoreOption` follows `HasMoreItems`; the loading skeleton is a stock `Border`.
- `dc66e1d7`: two nested `SfBottomSheet`s bound through both converters, `FloatingToolbarSf`, the FAB (an `ImageButton` in the `SfIconButton` style), and `IMenuHostSf` hosting `MenuSf`.

Fix round 1, `f3c38368`: the sheets now wrap the whole control, so the overlay covers the search bar and filters. `ContentPadding=0`, and the half ratios are 0.32 and 0.25 (a deviation recorded in design). The re-review marked this ADDRESSED. Merged.

The review found that sheet sequencing is not a regression: the DX menu path that waited for the menu to close was dead code. The public API matches `CrudListView`, except for the ruled `HostedMenu` type. Build had 0 errors and 2057 tests passed.

**T2.9 must check on the device:**
- the overlay covers the search bar and filter chips;
- the Cancel and Delete rows fit;
- a sheet opened from the VM stays open;
- the load-more indicator and the row ripple show;
- the P10 Back order works, with the page closing the sheet through the VM;
- the confirm sheet does not visibly overlap the item-actions sheet.

## T3.3 — `AutocompleteFieldSf` — Done (2026-11-24)

Merged after one fix round. The re-review marked every finding ADDRESSED.

| Commit | Content |
|---|---|
| `e29f383f` | `AutocompleteFieldSf`, plus `AutocompleteFilterSf` and its 7 tests |
| `9cae323d` | The `TapToEditFieldSf` autocomplete hook, restored as in the DX original |
| `becdd2e5` | Fix round 1: rows sized from the measured row template (no fixed height); search errors logged, with no stale options left showing; 2 new tests; cancellation tests retagged REQ-ACF-26 |

**How it is built.** An `SfAutocomplete` inside an outlined `SfTextInputLayout`, with the same public API as `AutocompleteDXField`. The filter delegates to the field and the shared logic, which is unchanged. Outline and hint colors are set per state in code-behind, because the Core layout ignores the app theme keys; they use tokens only.

**Verification.**
- Tests: RED, then GREEN; 2066 pass.
- P13 on the emulator (`artifacts/parity/t3.3/`): suggestions appear after the async search; picking locks the value; the field returns to text state; a tap releases the pick and focuses the field; clear and the error state work. A 56 dp image row is not clipped.

| AC | Tests |
|---|---|
| REQ-ACF-26 | cancellation tests |
| REQ-ACF-31 | no stale options |
| REQ-UIMIG-01 | logging (no AC covers search-error logging) |

**Notes for Helder.**
- A Syncfusion trial-license dialog appears in Debug when this page opens. Your license key removes it (REQ-UIMIG-16).
- Large font scale has not been checked on a device.
- After a pick, focus jumps to another field. The DX original makes the same `Unfocus()` call; check this on the Song form at parity.
