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
