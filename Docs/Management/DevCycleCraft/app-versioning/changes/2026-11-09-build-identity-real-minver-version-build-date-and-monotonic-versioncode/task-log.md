# Task log — Build Identity

---
## Task 1: Version wiring, identity line, git build details (pieces 1–7)
**Plan:** `plan.md` (this folder)
**Status:** To Review
**Started:** 2026-11-09
**Completed:** 2026-11-09

### Changed files:
- `MyVocaList/MyVocaList.csproj`
- `Services/BuildInfo.cs`
- `MyVocaList/UI/ViewModels/AboutViewModel.cs`
- `MyVocaList/UI/Pages/About/AboutPage.xaml`
- `MyVocaList.Tests/Unit/Services/BuildInfoTests.cs`
- `MyVocaList.sln` (task-log.md registration)

### Step 1 — target ordering (binlog `obj/bid.binlog`)
- The Android SDK (Microsoft.Android.Sdk.Windows 36.1.118) copies `ApplicationDisplayVersion` / `ApplicationVersion`
  into `_AndroidVersionName` / `_AndroidVersionCode` inside target **`_GetAndroidPackageName`**
  (`Xamarin.Android.Common.targets` lines 614–622); the manifest generator reads those copies. The copy is made at
  target time, not evaluation time. At evaluation, MinVerVersion is empty, so the old csproj properties resolved to
  `1.0.0` / `1` (the SDK defaults).
- MinVer 6.1.0 runs `BeforeTargets="BeforeCompile;GetAssemblyVersion;CoreCompile"`, i.e. AFTER `_GetAndroidPackageName`
  in a normal build. So `AfterTargets="MinVer"` + `BeforeTargets="_GetAndroidPackageName"` is contradictory.
- **Deviation:** `SetVersionFromMinVer` uses `DependsOnTargets="MinVer"` with
  `BeforeTargets="_GetAndroidPackageName;GenerateAssemblyInfo"`. This pulls MinVer forward (it still runs once) so the
  version exists before both consumers. Same mechanism (target ordering), no new mechanism; verified in step 8.

### Step 8 evidence (Release, net10.0-android)
- `obj/Release/net10.0-android/android/AndroidManifest.xml`: `android:versionCode="1653" android:versionName="0.1.0-alpha.0.1058"`.
  `git rev-list --count --first-parent HEAD` = `1653`; versionName = MinVer version for this commit (no `v0.2.0-alpha.0` tag yet, D11).
- `obj/Release/net10.0-android/build-details.txt` (three sections): `#branch` `task/bid-build-identity`; `#uncommitted` lists the
  modified/untracked paths; `#commits` lists 10 subjects, one per line (`;` and `%` verbatim — `%%s` in the Exec because
  Exec runs through a .cmd file).
- REQ-BID-06 (git off PATH, `-p:MinVerSkip=true` to reach our target; without it MinVer fails first with
  `MINVER1007: "git" is not present in PATH`): `MyVocaList.csproj(191,3): error : versionCode: git rev-list --count failed - git history is required (REQ-BID-06).`
- REQ-BID-10 (`-p:ShowBuildDetails=false -t:WriteBuildDetails`): target skipped, `build-details.txt` not created, no EmbeddedResource added.
  With the default it is created.
- Commit-hook: committed with `--no-verify` is NOT used unless noted below.

### AC traceability
| AC | Implementation | Test |
|----|----------------|------|
| REQ-BID-01 version real | csproj `SetVersionFromMinVer`; `BuildInfo.ParseIdentity` | `ParseIdentity_WithSha_SplitsVersionAndShortSha` + step 8 manifest |
| REQ-BID-02 short SHA | `ParseIdentity` | `ParseIdentity_WithSha_…`, `ParseIdentity_NoPlus_EmptySha` |
| REQ-BID-03 build time local | `AssemblyMetadata BuildTimestampUtc`, `ParseIdentity` | `ParseIdentity_UtcToLocal_Formats`, `ParseIdentity_NullInputs_ReturnEmptyStrings` |
| REQ-BID-04 versionName | `SetVersionFromMinVer` | step 8 manifest (manual) |
| REQ-BID-05 versionCode | `SetVersionFromMinVer` | step 8 manifest (manual) |
| REQ-BID-06 git missing fails | `Error` task | step 8 manual output |
| REQ-BID-07 branch / detached | `WriteBuildDetails`, `ReadDetails` | `ReadDetails_Branch`, `ReadDetails_DetachedHead_ShowsShortSha` |
| REQ-BID-08 recent commits | same | `ReadDetails_CommitsInOrder`, `_FewerThanTenCommits`, `_SubjectWithSemicolonAndPercent_Verbatim`, `_Crlf_SameResult`, `ToGroups_HidesEmptyGroups_LabelsWithCounts`, `DefaultGroupIndex_PrefersInProgressThenChanges` |
| REQ-BID-09 uncommitted | same | `ReadDetails_PorcelainLines_TrimsStatusPrefix`, `_EmptyUncommitted_NotDirty` |
| REQ-BID-10 details off | `ShowBuildDetails` condition; `ReadDetails(null)` | `ReadDetails_NullStream_ReturnsNull`, `_EmptyStream_ReturnsNull`, `DefaultGroupIndex_EmptyList_ReturnsMinusOne`; MSBuild path manual |

### Build notes
Build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -c Release` 0 errors | Tests: 943 passed, 0 failed (17 new BuildInfoTests)
Not done: E2E on emulator/device (About page visual) — requires manual verification (Check build).

---
## Task 2: Status snapshot (pieces 8–9; REQ-BID-11…13)
**Plan:** `plan.md` (this folder)
**Status:** To Review
**Started:** 2026-11-09
**Completed:** 2026-11-09

### Changed files:
- `.claude/scripts/backlog/backlog_gen.py` (`snapshot` verb, `snapshot_lines`, `cmd_snapshot`)
- `.claude/scripts/backlog/tests/test_backlog_gen.py` (`SnapshotTests`, 8 tests)
- `Services/BuildInfo.cs` (`ReadStatus`)
- `MyVocaList.Tests/Unit/Services/BuildInfoTests.cs` (7 new tests)
- `MyVocaList/UI/ViewModels/AboutViewModel.cs` (reads status, passes it to `ToGroups`)
- `MyVocaList/MyVocaList.csproj` (`WriteBuildDetails`: delete, snapshot Exec, embed `build-status.txt`)
- `Docs/Management/DevCycleCraft/app-versioning/changes/2026-11-09-build-identity-real-minver-version-build-date-and-monotonic-versioncode/design.md` (spec note: explicit `--root`)
- `Docs/.../task-log.md` (this entry)

### Red evidence (observed before implementation)
- Python: `AttributeError: module 'backlog_gen' has no attribute 'cmd_snapshot'`, 8 of 8 snapshot tests errored.
- C#: `error CS0117: "BuildInfo" não contém uma definição para "ReadStatus"` (7 new tests, compile failure).

### Deviations
- `pytest` was not installed in the environment; installed it with `pip install pytest` to run the exit command. The `unittest` run gives the same result (180 OK).
- The snapshot Exec passes `--root "<repo root>"`, because Exec runs in the project directory. Recorded in `design.md`.
- `ToGroups` already accepted a status in Task 1, so only `ReadStatus` was new in C#.
- `DoneThisMonth` includes `✅ Fixed` as well as `✅ Done` (both are `✅`, per design piece 8).

### Evidence
- `python -m pytest .claude/scripts/backlog/tests -q`: 180 passed, 0 failed.
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`: 0 errors (after fixing one XML comment that contained `--`).
- `dotnet test MyVocaList.Tests`: 950 passed, 0 failed (943 before + 7 new).
- `obj/Debug/net10.0-android/build-status.txt`: 18 lines, first lines:
  - `D	venuesSeedForTest migration ships ~504 fake venues and its Down wipes the table`
  - `P	Song artist field — correctness fixes + inline "create new artist"	on-device re-run #5 failed 2026-08-02 — …`
  - `P	Song form title-first — one search over local and music APIs	Helder's on-device E2E gate (plan § 7.2).`
- Target run: `dotnet msbuild … -t:WriteBuildDetails -v:d` shows the Delete and the snapshot Exec, and the file is produced.
- Emulator check: not run (About page UI change needs manual verification, `Check build`).

### AC traceability
| AC | Implementation | Test |
|----|----------------|------|
| REQ-BID-11 in-progress list + gate | `snapshot_lines` P branch; `ReadStatus` | `test_snapshot_in_progress_line`, `test_snapshot_gate_from_goal`, `test_snapshot_empty_gate`; `ReadStatus_InProgressWithGate`, `ReadStatus_EmptyGate`, `ToGroups_WithStatus_AddsInProgressAndDone` |
| REQ-BID-12 done this month | `snapshot_lines` D branch | `test_snapshot_done_this_month`, `test_snapshot_non_done_terminal_excluded`, `test_snapshot_other_month_excluded`; `ReadStatus_Done` |
| REQ-BID-13 failure tolerated, groups omitted | Delete + ContinueOnError Exec; conditional embed; `ReadStatus(null)` → null; malformed lines skipped | `test_snapshot_empty_tree_prints_nothing_exit_0`, `test_snapshot_never_writes_backlog`; `ReadStatus_NullStream_ReturnsNull`, `ReadStatus_MalformedLines_Ignored`, `DefaultGroupIndex_InProgressPresent_SelectsIt` |

### Build notes
Build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` 0 errors | Tests: Python 180 passed; .NET 950 passed, 0 failed | Commit SHA: (see commit)
Files written and re-read: `backlog_gen.py` (snapshot block, parser, main dispatch); `test_backlog_gen.py` (SnapshotTests); `Services/BuildInfo.cs` (ReadStatus); `BuildInfoTests.cs` (new tests); `AboutViewModel.cs` (status read, ToGroups call); `MyVocaList.csproj` (WriteBuildDetails tail); `design.md` (spec note)

---
## Minor UI fix: About build-chip selected state and wrapping (task/bid-chips)
- Severity Minor, no folder; commit message is the artifact.
- Cause: ChoiceChipGroup used DevExpress default chip colours (no selected contrast on dark theme) and no `IsMultiline`, so the row overflowed; the default group margin offset it from the content margin.
- Fix (`AboutPage.xaml` only): `IsMultiline="True"`, `Margin="0"`, `ChipTextColor`/`ChipSelectedBackgroundColor`/`ChipSelectedTextColor` bound to MD3 tokens (`OnSurface`, `SecondaryContainer`, `OnSecondaryContainer`).
- Verification: Android build 0 errors (exit 0); `dotnet test --filter BuildInfo` 24 passed. Emulator visual check not run (orchestrator deploys): selected state and x-offset need on-device confirmation.
