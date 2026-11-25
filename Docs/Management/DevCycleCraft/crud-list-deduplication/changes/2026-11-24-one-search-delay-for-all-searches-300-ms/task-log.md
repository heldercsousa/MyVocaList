---
## Task: One search delay for all searches (300 ms)
**Plan:** Docs/Management/DevCycleCraft/crud-list-deduplication/changes/2026-11-24-one-search-delay-for-all-searches-300-ms/README.md
**Status:** To Review
**Started:** 2026-11-25
**Completed:** 2026-11-25

### Spec update (first)
README `## Decision` gained a `> **Spec updated 2026-11-25:**` note: every search waits `SearchConstants.AutocompleteDebounceMilliseconds` (300 ms).

### Consumer map (governed-adjacent: shared constant and CRUD list base)
- `SearchConstants.AutocompleteDebounceMilliseconds` consumers: `AutocompleteFieldSf.xaml.cs:496` (Sf field, unchanged), `AutocompleteDXField.xaml.cs:403` (DX original, untouched), `CrudListViewModelBase.cs` (changed: literal 400 replaced).
- `CrudListViewModelBase` is the base of every CRUD list VM (Songs, Artists, Persons, Venues); the change touches only the debounce delay.

### Per-consumer risk
| Consumer | What could break | Verification |
|---|---|---|
| Songs / Artists / Persons / Venues list VMs | Shimmer may flicker on each keystroke at 300 ms | Unit: `CrudListViewModelBaseTests`, `LoadingHold*` (57 passed incl. SearchConstants); device check pending (see E2E) |
| AutocompleteFieldSf | None (constant value unchanged at 300) | `AutocompleteFieldSfCharacterizationTests` (item 1) |
| AutocompleteDXField | None (not edited) | n/a |

### Test notes
- The CRUD list tests do not wait a literal 400 ms: they poll with 5 s deadlines (`CrudListViewModelBaseTests.cs`, `LoadingHoldShimmerTests.cs` use 50/200/10 ms probes only). No test edit was needed; no test was weakened.
- Constant name kept (`AutocompleteDebounceMilliseconds`) because the DX original references it; rename deferred to the DX removal.

### E2E
E2E: emulator not run in this session. Manual check needed: 300 ms must not make the shimmer flash on every keystroke on device (README requirement). If it does, report to Helder rather than picking another value.

### Changed files:
- `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs` (literal 400 → shared constant; `using MyVocaList.Domain.Constants`)
- `Domain/Constants/SearchConstants.cs` (doc comment only)
- `Docs/Management/DevCycleCraft/crud-list-deduplication/changes/2026-11-24-one-search-delay-for-all-searches-300-ms/README.md` (spec note)
- `Docs/Management/DevCycleCraft/crud-list-deduplication/changes/2026-11-24-one-search-delay-for-all-searches-300-ms/task-log.md` (new)
- `MyVocaList.sln` (registration)

### Verification evidence
- `dotnet test --filter CrudListViewModelBaseTests|LoadingHold|SearchConstantsTests|CrudListStaleLoadTests`: 57 passed, 0 failed.

### Build notes
Build: see commit (Android build run before commit) | Tests: 57 passed (filtered), full suite runs in pre-commit hook | Commit SHA: see git log
Files written and re-read: `CrudListViewModelBase.cs` (line 359 and using block), `SearchConstants.cs` (L36-40), item README decision note.

### Round 2 (coordinator request): emulator check at 300 ms
- Emulator: Pixel 5 API 36 AVD started (`emulator-5554`); debug build deployed from this worktree (clean obj/bin for net10.0-android first). App opened on the Artists Sf list (catalog list).
- Fast typing "queen" (0.2 s between keys) in the Artists search: the list settled to "No results found" within the screenshot delay. Shimmer was NOT captured in a screenshot (`screencap` latency is about 1 s, longer than the 300 ms shimmer), so the flash itself is not visually confirmed.
- Proxy measurement (`adb logcat -c` then `-d`, `Choreographer: Skipped N frames`): idle 0 skipped frames; during fast typing 0 skipped frames. The only skips seen were at app launch (32-71 frames, before any typing).
- Limit: the Songs `SongFormSf` page is not reachable from the UI (the Sf pages live only on the Debug preview route), so the artist-field check used the DX song form, which shares `SongFormViewModel`.
- Verdict: no visible or logged jank from the 300 ms delay on the emulator. Helder to confirm the shimmer does not flash on device.

### Round 3: shimmer continuity under fast typing (REQ-DLH-05)
- Test `CrudListStaleLoadTests.TypingFasterThanTheDebounce_KeepsTheShimmerOnUntilTheLastSearchApplies`: keystrokes 20 ms apart; `IsInitialLoading` changes exactly `[true, false]` (on at the first keystroke, off when the last search's rows apply). No off/on between keystrokes. Passed 4/4 runs (3 repeats). No flicker found; behaviour unchanged.
- Caveat: real-time keystroke spacing (20 ms, well under 300 ms). Under extreme CPU load a gap over 300 ms would start a search early and the assertion could fail; not observed.

### Emulator visual check of the placeholder (orchestrator, 2026-11-25)
- Build: HEAD 145e8a4f (the APK deployed to Drive). Pixel 5 API 36 emulator, Venues list.
- Screen recorded while typing "adegab" one key at a time (`adb input` gives about 1.2 s per key on this emulator), frames read at 8 per second.
- Result: once the first key lands, the loading placeholder shows and stays on through every keystroke. It never flashes back to rows between keys. This matches the unit test `TypingFasterThanTheDebounce_KeepsTheShimmerOnUntilTheLastSearchApplies`.
- Open, not resolved: in the 10-second recording no results appeared after the last key, and later attempts to repeat the search were spoiled by driving errors (taps and keys landed on other pages; the search box once showed "aqeda"). Whether results take that long on the emulator, or a search sometimes doesn't apply, needs a check on a real phone: type "adega" on Venues and see that the "Adega ..." rows appear within about a second.
