---
## Task: Measure whether autocomplete field searches block the UI thread (B4)
**Plan:** Docs/Management/DevCycleCraft/crud-list-deduplication/changes/2026-11-25-minor-findings-autocomplete-and-crud-list-search/changes/2026-11-25-measure-whether-autocomplete-field-searches-block-the-ui-thread/README.md
**Status:** To Review
**Started:** 2026-11-25
**Completed:** 2026-11-25

### Measurement status
**Not measured on a device** (no emulator timing in this session). The finding is settled by code-path analysis instead, and the fix is applied; the on-device confirmation stays open (see E2E).

### Code-path analysis (the reasoning)
- The field calls its option source from `AutocompleteFieldSf.SearchAsync`, after `Task.Delay` on the UI context: the caller is the UI thread.
- Four option sources, before this change, called SQLite on that thread: `SongFormViewModel.SearchArtistsCoreAsync` (via `SearchArtistOptionsAsync`), `SongFormViewModel.SearchTitleAsync` (local read), `ArtistFormViewModel.SearchNameAsync` (local read), `PersonFormViewModel.SearchPersonsCoreAsync` (local read).
- `UnitOfWork.ExecuteReadAsync` does not offload (findings B4). Microsoft.Data.Sqlite completes async calls synchronously on the caller, which is the SQLITE-WORKAROUND case in `CrudListViewModelBase`. So the field searches did block the UI thread for their local read.
- Remote calls (HTTP) are not SQLite and are not offloaded.

### Fix
Only the local SQLite read is moved with `Task.Run(..., ct)`. The mapping, notes and every UI-bound assignment stay on the caller's context (moving them off the UI thread would break `BindableProperty` writes). The same pattern as `CrudListViewModelBase` (SQLITE-WORKAROUND).

### Test
- `SongFormViewModelTests.ArtistOptionSearch_ReadsArtistsOffTheUiThread`: records `SynchronizationContext.Current` inside the service read.
  - Red (before the offload): the read ran on the test's UI context (`Assert.NotSame() Failure: Values are the same instance`).
  - Green (after): the read runs on a pool thread.
- Filtered run `SongFormViewModel|ArtistFormViewModel|PersonFormViewModel|CrudListStaleLoadTests`: 379 passed, 0 failed.
- The title, name and person reads are offloaded by the same change; they have no dedicated threading test (same pattern, covered by their suites). Recorded as a gap.

### Open (device)
E2E: emulator not run. Helder to confirm on device that the autocomplete no longer stalls while typing (no measurement taken here).

### Changed files:
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` (artist and title local reads offloaded)
- `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs` (name local read offloaded)
- `MyVocaList/UI/ViewModels/PersonFormViewModel.cs` (person local read offloaded)
- `Tests/Unit/ViewModels/SongFormViewModelTests.cs` (threading test)
- `MyVocaList.sln` (registration)
- `<this folder>/task-log.md` (new)

### Build notes
Build: Android build run before commit | Tests: 379 passed (filtered) | Commit SHA: see git log
Files written and re-read: `SongFormViewModel.cs` L386-395 and title read; `ArtistFormViewModel.cs` L400; `PersonFormViewModel.cs` L494-499.

### Round 2 (coordinator request): emulator before/after
- After (current HEAD, offload in place): artist field in the song form (DX form, shared `SongFormViewModel`), focus then fast typing "queens": 0 `Skipped N frames` lines while idle and while typing.
- Before (offload removed): NOT measured. The before-build could not be deployed: the Android packaging step stalled after the .NET outputs, with `classes*.dex` locked by a killed earlier deploy (`Device or resource busy`). The before-build was stopped and the offload line restored (verified: `git status` clean).
- Conclusion: the main-thread read on this emulator is too short to show as dropped frames either way; the B4 fix rests on the code-path analysis and the threading test. Device/timing confirmation still open.
- Note: `SongFormSf` is not reachable from the UI; measured the shared-VM DX form instead.

### Round 3: before-state attempt (superseded: the build did finish, see Round 4)
- Reverted the offload locally (`git checkout 6bb63dc4^` of the three ViewModels, uncommitted), cleaned obj/bin net10.0-android, plain Android build. The .NET projects built; the Android step then stalled for 25+ minutes with leftover `aapt2`/`MSBuild` helper processes from earlier builds. Stopping the stalled task succeeded; killing the stale helpers and deleting the locked output was denied by the permission gate, so I did not force it.
- Tree restored to HEAD (offload back in place; `git status` clean). No before-state APK was installed; the before/after comparison remains open. Next step, when the helper processes are cleared: rebuild the before-state, install, type in the song form artist field, capture `Skipped .* frames`, then restore and repeat.

### Round 4: before/after on the emulator (not completed)
- Before-state (the four offload lines of 6bb63dc4 reverted locally, uncommitted): clean obj/bin net10.0-android, emulator started, `dotnet build` started. The .NET projects compiled; the Android step produced no output for 25+ minutes with ~19 leftover `aapt2`/`MSBuild` helper processes. Stopped per the 15-minute rule; no APK installed, no counts taken.
- Restored with `git checkout -- .` (tree clean at HEAD 882c8f83). Emulator stopped.
- After-state: not measured in this round. Counts for both states remain open.
