# Findings: shared code between the autocomplete field and the CRUD list

> Research only, 2026-11-24. No code was changed. Helder's request is quoted in [README.md](README.md).
> Sources: two read-only Opus Explore passes over `develop` @ `4ff95629`, plus the unmerged branches `task/sf-autocomplete` @ `9cae323d` and `task/sf-crudlistview` @ `dc66e1d7`. All line numbers refer to those commits. **Nothing here is a decision.** The decision is made in the follow-up discussion.

## 0. Verdict

**Do not build one component that serves as both the autocomplete and the CRUD list. Do not extract a shared debounced-search helper now.** The two look alike from the outside, but they share very little logic:

- At the control level, almost everything is behaviour only one side has.
- At the ViewModel level, the one repeated routine differs in about 9–11 details from one copy to the next.
- Joining them would couple two governed components. Every later change to the autocomplete would then put the four CRUD lists at risk, and the reverse. That is the "increased chance of trouble" risk Helder raised, and the evidence says it is real.

What is worth doing is small and independent of any sharing:

1. **Let the DX↔Sf duplicate disappear by itself.** It is the only true copy (§4). It goes away when the migration deletes the DX field in Phase 5. No work is needed.
2. **Fix the real defects the comparison found** (§5), as separate bug or cleanup items. They are worth fixing whatever is decided about sharing.
3. **Add characterization tests for the autocomplete search timing before anyone touches it** (§6). Today the most delicate routine has no behaviour tests.
4. **Keep "full screen" as a separate page, not a mode of the autocomplete,** if it ever comes back (§7). The model would be the YouTube search page, which returns its pick to the caller.

## 1. Comparison target (decided)

| Role | Choice | Why |
|---|---|---|
| Baseline for behaviour | **DX `AutocompleteDXField`** | The only shipped and tested version. |
| Target for any future shared code | **Sf versions** (`AutocompleteFieldSf`, `CrudListViewSf`) after migration Phase 5 | Approach B (migration `design.md:13,19`) forbids editing the DX originals and the shared ViewModels until Phase 5. Code built on DX would be thrown away. |
| Cross-check | Sf branches read as they are | `AutocompleteFieldSf` copies the DX bindable API and the same search routine (§4). `CrudListViewSf` changes only the view and reuses the existing ViewModel pipeline (`CrudListViewSf.xaml.cs:55`). The verdict is therefore the same for DX and Sf. |

## 2. What each side actually is

### 2.1 Autocomplete: `AutocompleteDXField` + `TapToEditField`

- **Size and scope:** about 770 lines (field 584, logic 103, xaml 86). Almost all of it is behaviour the CRUD list does not have:
  - pick-lock with the X button;
  - ignoring its own text echo after a pick (REQ-ACF-25);
  - forwarding a blur without a pick;
  - hiding the soft keyboard;
  - Locked / ReadOnly / Editable modes;
  - DX workarounds.
- **Search:** the control runs it. The ViewModel supplies only `IMenuOptionSource.SearchAsync` (`UI/Models/IMenuOptionSource.cs:8-12`).
  - Debounce is 300 ms (`SearchConstants`), minimum query length is 2 (REQ-ACF-13/14).
  - It shows no loading indicator and no "no results" row (REQ-ACF-31; Helder ruling of 2026-11-11, `ux-patterns.md:98-114`).
- **Pick:** the control runs `SelectCommand`. The **ViewModel** decides what a pick does, and may not delegate navigation to the component (REQ-ACF-12):
  - `SongFormViewModel.SelectTitle`: a local pick navigates to that song (REQ-SFT-10); a Deezer pick fills and locks the field.
  - `ArtistFormViewModel.SelectName`: the same split.
  - `PersonFormViewModel`: always navigates.
- **Consumers:**
  - SongForm Title and Artist (inside `TapToEditField`);
  - ArtistForm Name (inside `TapToEditField`);
  - PersonForm Name (no wrapper, `IsPickLockEnabled=False`).

### 2.2 CRUD list: `CrudListView` + `CrudListPageBase` + `CrudListViewModelBase<TItem>`

- **Size and scope:** about 1,300 lines. Almost all of it is behaviour the autocomplete does not have:
  - paging and load-more, pull-to-refresh;
  - multi-select with long-press, the floating toolbar and the FAB;
  - confirm and item-action sheets, the overflow menu;
  - back-button order, two empty states, the shimmer (REQ-DLH-05), the filter slot.
- **Search:** the **ViewModel** runs it.
  - `SearchText` → `TriggerSearchDebounce`, a literal 400 ms (`CrudListViewModelBase.cs:333`).
  - Then `Task.Run` → `FetchPageAsync`, with a load-generation guard.
- **Tap:** `OnItemTappedAsync` → `NavigateToEditAsync` (REQ-CLSG-01).
- **App bar:** not part of the component. Each page puts `SmallAppBar` in `Shell.TitleView`.
- **Consumers:** Venues, People, Songs, Artists.

## 3. Overlap matrix

| Concern | Autocomplete | CRUD list | Shared today? | Verdict |
|---|---|---|---|---|
| Search input | text field (DX `AutoCompleteEdit` / `SfAutocomplete`) | MD3 docked `SearchBar` | No | **Different MD3 components.** Not shareable. |
| Who runs the query | the control | the ViewModel | No | Different layers. |
| Debounce | 300 ms, constant | 400 ms, literal | No | Same idea, different values (§5 B3). |
| Latest search wins | CTS + pick counter + mode recheck | CTS + load generation (also covers Reload and Init) | No | Looks alike only (§4). |
| Minimum length | 2 | 0 (blank = all) | No | Different by design. |
| Paging, refresh | none (top 5) | yes | — | List only. |
| Loading indicator | forbidden | shimmer | — | **Opposite rules.** |
| Empty state | forbidden | two `EmptyState`s | — | **Opposite rules.** |
| Row template | fixed `AutocompleteSuggestion` row | page-supplied `ListItem` templates | Leaf parts (`ListItemLeading*`) | Already shared where possible. |
| Tap action | `SelectCommand`, ViewModel decides | `ItemTapCommand` → navigate | — | **Already configurable in both ViewModels.** |
| Selection | single pick → lock | multi-select, sheets, menu | — | Different. |
| Several sources | local + promoted + Deezer, labelled | one local source | — | Autocomplete only. |
| Back / keyboard | keyboard hidden after a pick | `HandleBack` order | — | Different. |
| Underlying query | `SearchTitleCoreAsync` | `GetPagedAsync` / `SearchPagedAsync` | Same full-text index and `FtsQueryBuilder` | Already shared at the repository level. |

**About Helder's hypothesis:** the listed differences (app bar, floating toolbar, tap action) are real but not the main ones. The bigger differences are the input component, the loading and empty-state rules, paging, pick-lock and the selection model. The one he expected to be hardest, the tap action, is already configurable.

## 4. The repeated routine, compared copy by copy

The "cancel previous → wait → query → drop stale result → apply" routine exists in these places:

| # | Where | Trigger | Delay | Cancellation | What counts as stale | Threading | Errors | Before the wait |
|---|---|---|---|---|---|---|---|---|
| 1 | DX field `AutocompleteDXField.xaml.cs:365-446` | user keystroke | 300, constant | CTS + `_pickCount` | newer key, pick, blur, programmatic text, mode change | UI context | only cancellation caught | rejected query closes the menu |
| 2 | CRUD `CrudListViewModelBase.cs:177-339` | any `SearchText` change | 400, literal | CTS + load generation | newer key, or any newer shimmered load | `Task.Run` + dispatch | only cancellation caught | shimmer on |
| 3 | SongForm artist `SongFormViewModel.cs:393-410, 522` | via field 1 | (inherits 300) | generation only; **token dropped** | newer call | caller (UI) | none | blank → `[]` |
| 4 | YouTube `YouTubeSearchViewModel.cs:51-88` | submit | none | CTS (in practice never used) | newer submit (blocked anyway) | UI | logged + error state | loading on |
| 5 | Venue `VenueMapPickerViewModel.cs:138-169` | submit | none | **none** | nothing | UI | service turns errors into Unavailable | searching on |
| 6 | Sf field `AutocompleteFieldSf.xaml.cs:463-503` + `AutocompleteFilterSf.cs:39-82` (branch) | control filter callback | 300, constant | CTS in filter + `_pickCount` | same as #1 | UI | handled by the control | same as #1 |
| — | Frozen `AutocompleteDebouncer.cs` (not built) | `Trigger()` | parameter | CTS | newer trigger | `Task.Run` + `MainThread` | lost | — |

**Same in meaning:**

- **#1 ↔ #6, DX ↔ Sf.** Same steps in the same order, the same `AutocompleteDXFieldLogic` calls, about 40 lines. This is the only real duplicate. It ends when Phase 5 deletes the DX field, so it needs no extraction work.
- **#2 ↔ the frozen debouncer.** Same idiom, apparently a clone (inferred from identical text).

**Alike only in shape:**

- **#1 ↔ #2.** They differ in threading, delay, side effects before the wait, gate, stale rules and clean-up.

A single helper for #1 + #2 would need about **9 options**, and about **11** to cover every caller:

1. delay and time source;
2. threading policy;
3. gate before scheduling;
4. what to do on reject;
5. what to do before the wait (and returning a generation);
6. extra stale checks after the query;
7. whether to apply or return the result;
8. clean-up when nothing was applied;
9. error policy;
10. an outside cancel entry point;
11. debounced or submit.

Also, the CRUD generation cannot move into the helper, because it also guards `ReloadAsync` and `InitializeAsync`, which are not debounced.

**Lines:**

- About 40–45 mechanism lines could go on `develop` (70–110 counting the Sf branch).
- A safe helper would add about 50–70 lines, plus 100–150 lines of tests.
- The net is about **+100 lines**, and a configurable abstraction sits in the middle of two governed components.

## 5. Defects and inconsistencies found (independent of any sharing decision)

| ID | Status | Where | What |
|---|---|---|---|
| B1 | Confirmed | `SongFormViewModel.cs:522-523, 401` | `SearchArtistOptionsAsync` drops its `CancellationToken`. A superseded artist query still runs its DB read to the end. ArtistForm and PersonForm pass their tokens. |
| B2 | Confirmed | `SongFormViewModel.cs:401` | `maxResults: 5` is a literal instead of `SearchConstants.MaxSearchResults`. |
| B3 | Confirmed | `CrudListViewModelBase.cs:333` | The list debounce is a literal 400 ms; no requirement or constant documents it. |
| B4 | Suspected | autocomplete option sources | No `Task.Run`, and `UnitOfWork.ExecuteReadAsync` does not offload. By the repo's own SQLITE-WORKAROUND note (`CrudListViewModelBase.cs:189-193`), field searches may run SQLite on the UI thread. The 2026-11-17 backlog item mentions the same for Artist. Not measured. |
| B5 | Confirmed | DX `:385, :434`; CRUD `:329-337`; `CrudListPageBase.cs:77` | Exceptions other than cancellation become unobserved tasks. They are logged only at GC finalization, the user sees nothing, and the list can stay empty or stale. `LoadMoreAsync` and YouTube do log. |
| B6 | Suspected race | `CrudListViewModelBase.cs:272-316` | A load-more from the old query can append its page after a new search has replaced the rows. |
| B7 | Suspected race | `CrudListViewModelBase.cs:262-267` | Pull-to-refresh has no token and no generation. A slow refresh can overwrite newer search results. |
| B8 | Suspected, low | `CrudListViewModelBase.cs:209, 214` | The stale check runs before the UI dispatch, so stale rows can briefly pass (probably hidden under the shimmer). |
| B9 | Confirmed, minor | CRUD `_searchCts`; venue `_tapCts` | The last CTS is never disposed. |
| B10 | Suspected | `VenueMapPickerViewModel.cs:138-169` | The submit search has no cancellation. A map tap during a search gets overwritten, and leaving the page does not cancel it. |
| B11 | Confirmed, smell | `YouTubeSearchViewModel.cs:51-58` | `AllowConcurrentExecutions=false` makes the cancel-previous code dead. |
| B12 | Confirmed, dead code | `SongFormViewModel` `ArtistSuggestions`, `SearchArtistsCommand`, `_searchGeneration` | No XAML binds them; only tests use them (BUG-051/056 tests). |
| B13 | Suspected, minor | DX field | A pending search is not cancelled on Unloaded. |

## 6. Test coverage, the main risk factor

- **DX field search timing: no behaviour tests at all.** No test builds an `AutocompleteDXField`. Only the pure logic (`AutocompleteDXFieldLogicTests`) and the 300 ms constant are tested. Pick counter, echo, mode recheck and apply are not tested.
- **CRUD list:** real-time tests that wait the real 400 ms (`CrudListViewModelBaseTests.cs:108-141`, `LoadingHoldShimmerTests.cs`). Races B6 and B7 and the error paths are not tested.
- **Sf filter** (branch): good `TaskCompletionSource` tests for the latest-wins rule (`AutocompleteFilterSfTests.cs:50-103`), with no timing involved.
- **Time control:** `TimeProvider.System` is registered in DI (`ServiceCollectionExtensions.cs:56`), and a reusable test `ManualTimeProvider` exists (`PlaceLookupGateTests.cs:155-222`). No search routine uses them yet.

**Why this matters for the decision:** refactoring the autocomplete search into a shared helper today means changing the least-tested, most timing-sensitive code in the area with no safety net. That is exactly the "more trouble on every future change" outcome.

## 7. Inline or full-screen, with a configurable tap

- **Already tried and dropped:**
  - The frozen `AutocompleteField` opened `AutocompleteMobileField` as a full-screen modal on phones. It was frozen by D-AC1 (2026-07-19) after BUG-043..047.
  - On 2026-11-11 Helder ruled out a full-screen Search View on phones (`ux-patterns.md:98-114`), and the MD3 mobile guideline is marked ABANDONED.
  - Bringing back a full-screen mode means reopening that ruling first.
- **Working example to follow if it comes back:** `YouTubeSearchPage` is a separate full-screen page with a search app bar, shimmer, empty state and a list. A tap returns the pick to the caller (`YouTubeVideoPickedMessage`, then go back). This is the "full-screen autocomplete with fill-the-field behaviour", built as a page, not as a mode of the field.
- **The configurable tap needs no new component.** Both ViewModels already decide what a tap does. If a list ever needs "pick and return" (for example a full-screen song picker), it is one ViewModel override on `CrudListViewModelBase.OnItemTappedAsync` plus the messenger return, not a merged control.
- **Pending items that would test any merge:**
  - Deezer results in the Songs list (paused: `CrudListView` has no slot for a second kind of row);
  - a loading indicator for field searches (would reverse the autocomplete's no-loading rule).

  Both pull the two components further apart, not closer.

## 8. Pros and cons of each option

| Option | Pros | Cons and risks | Effort | Worth it? |
|---|---|---|---|---|
| **A. One component, inline or full screen, configurable tap** | One concept for the user and the code | Reverses the 2026-11-11 ruling. Repeats the abandoned D-AC1 design. Merges two governed components (4-gate change touching 4 lists + 4 form fields). The loading and empty-state rules are opposite, so the merged component branches on mode everywhere. Blocked by the migration until Phase 5. Highest regression risk. | Very high | **No** |
| **B. Shared debounced-search helper (field + list)** | One latest-wins pattern, one delay constant | 9–11 options = a configurable abstraction (false DRY). Net about +100 lines. Couples field and list timing, so a fix for one changes the other. The DX path has no tests. Blocked until Phase 5. | Medium | **Not now.** Revisit only if the precondition in §9 is met. |
| **C. Small primitive only: "latest-wins runner"** (cancel previous, owner disposes, ignore cancellation, `TimeProvider` delay), used only where the meaning is identical | No options. Each caller keeps its own rules. `AutocompleteFilterSf.FilterAsync` already is this primitive minus the delay. | Small gain (about 20–30 lines). Another type to know. | Low | **Maybe, after Phase 5**, if a third caller with the same meaning appears. |
| **D. No merge; fix the defects (§5) and add the missing tests (§6)** | Removes real risk (dropped token, unobserved errors, races, UI-thread DB). Nothing gets coupled. Each fix is small and local. | Duplication stays (most of it is not real duplication). | Low, per item | **Yes** |
| **E. Do nothing** | Zero effort | B1, B5 and the races stay. The autocomplete search stays untested. | None | No; D is cheap. |

## 9. Recommendation, with conditions

1. **Choose D now**, as separate items (not bundled; each touches a governed component or its ViewModel, so the component gates apply where they do):
   - B1 + B2 + B12: SongForm artist search clean-up (pass the token, use the constant, delete the dead state and the tests that only cover it).
   - B5: log and surface search failures in the CRUD debounce and the field search.
   - B6 + B7 (+ B8): confirm with a failing test first, then put load-more and refresh under the same generation guard.
   - B3: name the 400 ms as a constant, or align it with 300 (a UX choice Helder makes).
   - B4: measure first; offload the field option sources only if there is visible jank.
   - B10: venue search cancellation (low priority).
   - Timing on the CRUD list items must be scheduled with the migration: either wait for Phase 5, or fix them in the shared ViewModel base with migration-plan approval, because the ViewModels are frozen during migration.
2. **Precondition for ever extracting shared search code (option C):** characterization tests for the Sf field search (pick counter, echo, mode recheck, stale drop) using the existing `ManualTimeProvider`, written and passing *before* the extraction.
3. **Do not reopen the full-screen autocomplete** unless a concrete feature needs it. If one does, build it as a separate page on `CrudListViewSf`/`SearchAppBarSf` with a pick-returns-result ViewModel (the YouTube pattern), not as a mode of `AutocompleteFieldSf`.
4. **Code practices that keep the risk low**, whatever is chosen:
   - Prefer composition over configuration: a small primitive the caller *uses*, not a base class or component the caller *configures*.
   - Share only code whose meaning is identical (rule of three, by meaning, not by shape).
   - Inject time (`TimeProvider`) so timing is testable without real waits.
   - Keep policy in the caller: gates, shimmer, pick-lock, navigation.

## 10. Not verified

- Whether the CRUD debounce was literally copied from `AutocompleteDebouncer`. This is inferred from identical text.
- Whether SQLite really blocks the UI thread on the field search path (B4). Not measured.
- How the unmerged Sf branches behave at runtime, including how `SfAutocomplete` handles faulted filter tasks (taken from comments in the branch, not run).
- Whether the root of `AutocompleteMobileField` is a `ContentPage` (inferred from `PushModalAsync`).
- B6, B7, B8, B10 and B13 are read from the code; none is reproduced.
