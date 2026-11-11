# Build Identity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: use superpowers:subagent-driven-development. Steps
> use checkbox (`- [ ]`) syntax. Task state lives in `tasks.md` (Rule 4).

**Goal:** The About page shows which exact build is installed and what it contains: version, SHA,
build time, branch, uncommitted paths, recent commits, and in-progress and done items. Android
gets a real `versionName` and a monotonic `versionCode`.

**Architecture:**
- MSBuild targets in `MyVocaList.csproj` compute the version after MinVer runs and write two
  embedded text resources: git details and a backlog status snapshot.
- A pure static `BuildInfo` class in `Services` parses those resources and the assembly
  attributes into records.
- `AboutViewModel` loads them, and `AboutPage.xaml` renders them through a DevExpress
  `ChoiceChipGroup` over one list.

**Tech stack:** MSBuild (MinVer, `Exec`, `WriteLinesToFile`), C# / xUnit, Python `unittest`, MAUI
XAML with DevExpress 25.2.4 (`DevExpress.Maui.Editors.ChoiceChipGroup`).

**Spec:** `requirements.md` (REQ-BID-01…14) and `design.md` (pieces 1–9, D1–D11), in this folder.
Read both before starting.

## Global constraints

- Exact versions come from the `.csproj`. Look up DevExpress and MAUI APIs with Context7 at the
  `.csproj` version (CLAUDE.md § MCP & Skills); use the `devexpress-maui` MCP for example code.
- Business logic goes in Services. `AboutViewModel` only loads the inputs and binds the results.
- Never construct `AboutViewModel` in a test: its constructor path reads `AppInfo`, which throws
  off-device (LEDGER `COPY-YR`). Test `BuildInfo` only.
- Edit XAML incrementally: change one file, build, then the next. Keep
  `SafeAreaEdges="Container"`. English only. No native dialogs.
- Every test's first comment line is `// [AC] REQ-BID-NN: <criterion>` (or `# [AC] …` in Python).
- All work happens in a worktree on a task branch based on `develop`. Verify with
  `git merge-base --is-ancestor develop HEAD`.
- **Merge gate (D11):** do not merge into `develop` until tag `v0.2.0-alpha.0` exists on `develop`.
  Only the orchestrator creates it, after Helder confirms.

## Review focus

1. **Detached HEAD in a worktree:** the branch shows `detached @ <sha7>`, not `HEAD`. Tested in
   Task 1 (`ReadDetails_DetachedHead_ShowsShortSha`).
2. **Porcelain path with spaces or a rename (`R  old -> new`):** the 2-character status and the
   following space are trimmed, and the rest is kept verbatim. Tested in Task 1
   (`ReadDetails_PorcelainLines_TrimsStatusPrefix`).
3. **Commit subject containing `;` or `%`:** shown verbatim. Tested in Task 1
   (`ReadDetails_SubjectWithSemicolonAndPercent_Verbatim`).
4. **No embedded resource at all (`ShowBuildDetails=false`, or git or Python failed):** the
   section is hidden and nothing throws. Tested in Task 1 (`ReadDetails_NullStream_ReturnsNull`)
   and Task 2 (`ReadStatus_NullStream_ReturnsNull`).
5. **The forced-update gate after the version fix (REQ-BID-14):** covered by the D11 tag. The
   orchestrator checks it manually: the version is `0.2.0-alpha.0.N` and no update sheet appears
   on launch.

---

## Interfaces (shared by both tasks)

`Services/BuildInfo.cs`, namespace `MyVocaList.Services`:

```csharp
public sealed record BuildIdentity(string Version, string ShortSha, string BuiltAtLocal);
public sealed record BuildDetails(string Branch, IReadOnlyList<string> UncommittedPaths,
                                  IReadOnlyList<string> RecentCommits)
{ public bool IsDirty => UncommittedPaths.Count > 0; }
public sealed record BuildStatusItem(string Title, string Gate);
public sealed record BuildStatus(IReadOnlyList<BuildStatusItem> InProgress,
                                 IReadOnlyList<string> DoneThisMonth);
public sealed record BuildRow(string Text, string SupportingText);   // "" = no supporting line
public sealed record BuildGroup(string Key, string Label, IReadOnlyList<BuildRow> Rows);

public static class BuildInfo
{
    public const string DetailsResourceName = "build-details.txt";
    public const string StatusResourceName  = "build-status.txt";
    public static BuildIdentity ParseIdentity(string? informationalVersion,
                                              string? buildTimestampUtc, TimeZoneInfo zone);
    public static BuildDetails? ReadDetails(Stream? stream, string shortSha);
    public static BuildStatus?  ReadStatus(Stream? stream);                   // Task 2
    public static IReadOnlyList<BuildGroup> ToGroups(BuildDetails? details, BuildStatus? status);
    public static int DefaultGroupIndex(IReadOnlyList<BuildGroup> groups);
}
```

**Group keys, in order:** `uncommitted`, `changes`, `inprogress`, `done`.

**Labels:** `Uncommitted (n)`, `Changes (n)`, `In progress (n)`, `Done (n)`. A group with 0 rows is
omitted.

**Default group:** the index of `inprogress` if present, else `changes`, else 0. With an empty
list the result is -1.

---

### Task 1: Version wiring, identity line, git build details (pieces 1–7)

**Files:**
- Modify: `MyVocaList/MyVocaList.csproj`. Delete the version comment and the version property
  lines 49–54 (keep `MinVerTagPrefix`), then add the targets below.
- Create: `Services/BuildInfo.cs`.
- Modify: `MyVocaList/UI/ViewModels/AboutViewModel.cs`, `MyVocaList/UI/Pages/About/AboutPage.xaml`.
- Test: `MyVocaList.Tests/Unit/Services/BuildInfoTests.cs`.

- [ ] **Step 1: Find the Android target that consumes the version.** Run
  `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -c Release -bl:obj/bid.binlog` and
  look in the binlog for the first target that reads `ApplicationDisplayVersion` or
  `ApplicationVersion`. Also check whether the Android SDK copies them into other properties at
  *evaluation* time. If it does, the target must set those copies too.
  **Stop and report to the orchestrator** if no target-time ordering can reach the manifest
  (design § Risks).

- [ ] **Step 2: Write the failing tests** in `BuildInfoTests.cs`, with exact values:
  - `ParseIdentity_WithSha_SplitsVersionAndShortSha` (REQ-BID-01/02):
    `"0.2.0-alpha.0.5+9f9907cd5fedc53e2f"` → Version `0.2.0-alpha.0.5`, ShortSha `9f9907c`.
  - `ParseIdentity_NoPlus_EmptySha` (02): `"0.2.0"` → ShortSha `""`.
  - `ParseIdentity_UtcToLocal_Formats` (03): `"2026-11-09T21:16:00.0000000Z"`, with zone
    `TimeZoneInfo.CreateCustomTimeZone("t", TimeSpan.FromHours(-3), "t", "t")` →
    `2026-11-09 18:16`.
  - `ParseIdentity_NullInputs_ReturnEmptyStrings`.
  - `ReadDetails` uses this text:
    `"#branch\ndevelop\n#uncommitted\n M MyVocaList/a.cs\nR  old.cs -> new name.cs\n#commits\nmerge: BUG-085 — x\nfeat: 50% done; ok\n"`.
    The tests:
    - `ReadDetails_Branch` (07);
    - `ReadDetails_DetachedHead_ShowsShortSha` (07: `HEAD` → `detached @ 9f9907c`);
    - `ReadDetails_PorcelainLines_TrimsStatusPrefix` (09: `MyVocaList/a.cs`,
      `old.cs -> new name.cs`);
    - `ReadDetails_EmptyUncommitted_NotDirty` (09);
    - `ReadDetails_CommitsInOrder` (08);
    - `ReadDetails_FewerThanTenCommits` (08);
    - `ReadDetails_SubjectWithSemicolonAndPercent_Verbatim` (08);
    - `ReadDetails_Crlf_SameResult` (08);
    - `ReadDetails_NullStream_ReturnsNull` (10);
    - `ReadDetails_EmptyStream_ReturnsNull` (10).
  - `ToGroups_HidesEmptyGroups_LabelsWithCounts` (08/09):
    - a clean build gives the groups `["changes"]` with label `Changes (2)`;
    - a dirty build gives the `uncommitted` group first, labelled `Uncommitted (2)`.
  - `DefaultGroupIndex_PrefersInProgressThenChanges`.
  - `DefaultGroupIndex_EmptyList_ReturnsMinusOne`.

- [ ] **Step 3: Run the tests and confirm they fail.**
  `dotnet test MyVocaList.Tests --filter FullyQualifiedName~BuildInfoTests`. Expected: a compile
  failure, because `BuildInfo` does not exist yet.

- [ ] **Step 4: Implement `Services/BuildInfo.cs`** to the Interfaces block. (design.md calls
  `ParseIdentity` `BuildInfo.Parse`; this plan's name wins.)
  - `ParseIdentity` parses the timestamp with
    `DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, …)`,
    then converts it once with `TimeZoneInfo.ConvertTime`. Format the result with
    `yyyy-MM-dd HH:mm` and the invariant culture.
  - `ReadDetails` reads UTF-8 and splits lines on `\n`, trimming a trailing `\r`.
  - Sections start at the `#branch`, `#uncommitted` and `#commits` markers. Blank lines are
    skipped.
  - A porcelain line keeps `line[3..]`.
  - It returns `null` for a `null` stream, or when no `#branch` section has content.

- [ ] **Step 5: Run the tests and confirm they pass.** Same command. Expected: all pass.

- [ ] **Step 6: csproj target `SetVersionFromMinVer`.**
  - It runs `AfterTargets="MinVer"`, with `BeforeTargets` set to the target from step 1 plus
    `GenerateAssemblyInfo`.
  - `Exec` runs `git rev-list --count --first-parent HEAD` with `ConsoleToMSBuild="true"` and
    `StandardOutputImportance="low"`.
  - `<Error Condition="'$(_BidCount)' == '' Or !$([System.Text.RegularExpressions.Regex]::IsMatch('$(_BidCount)', '^\d+$'))" Text="versionCode: git rev-list --count failed — git history is required (REQ-BID-06)." />`
  - It sets `ApplicationDisplayVersion` = `$(MinVerVersion)` and `ApplicationVersion` =
    `$(_BidCount)`.
  - Add `<AssemblyMetadata Include="BuildTimestampUtc" Value="$([System.DateTime]::UtcNow.ToString('o'))" />`
    in an evaluation-time `ItemGroup`.

- [ ] **Step 7: csproj target `WriteBuildDetails`.**
  - Condition `'$(ShowBuildDetails)' != 'false'`, `BeforeTargets="PrepareForBuild;AssignTargetPaths"`,
    no `Inputs`/`Outputs`.
  - It writes the file exactly as design piece 5 specifies. Every git `Exec` has
    `ContinueOnError="true"`.
  - It adds `<EmbeddedResource Include="$(IntermediateOutputPath)build-details.txt" LogicalName="build-details.txt" />`
    inside the target.

- [ ] **Step 8: Verify the build output.** Build Release for Android, then open
  `MyVocaList/obj/Release/net10.0-android/**/AndroidManifest.xml`.
  - `android:versionName` must equal the MinVer version, which is
    `dotnet msbuild … -t:MinVer -getProperty:MinVerVersion`.
  - `android:versionCode` must equal the output of `git rev-list --count --first-parent HEAD`.
  - `obj/…/build-details.txt` must exist with the three sections.
  - Paste all of this into `task-log.md`.
  - **REQ-BID-06:** build again with git hidden from PATH. The build must fail with the
    `versionCode:` error.
  - **REQ-BID-10:** build with `-p:ShowBuildDetails=false`. `build-details.txt` must not be
    embedded; check that `obj/…` has no such `EmbeddedResource`, or that the file is absent.
  - Paste both outputs into `task-log.md`.

- [ ] **Step 9: `AboutViewModel`.** Inject nothing new. Load the inputs in the constructor or in
  `InitializeAsync`:
  - the `AssemblyInformationalVersionAttribute` and the `AssemblyMetadataAttribute` with key
    `BuildTimestampUtc`, both from `typeof(AboutViewModel).Assembly`;
  - the resource through `GetManifestResourceStream(BuildInfo.DetailsResourceName)`.
  The view model exposes:
  - `Version` (`v{identity.Version}`);
  - `BuildLine` (`Build {ShortSha} · {BuiltAtLocal}`);
  - `HasBuildDetails`, `BranchText` (`Branch: {Branch}`), `IsDirty`;
  - `Groups` (`IReadOnlyList<BuildGroup>`);
  - `[ObservableProperty] BuildGroup? _selectedGroup`, defaulting to
    `DefaultGroupIndex`.
  Use `TimeZoneInfo.Local`.

- [ ] **Step 10: `AboutPage.xaml`.** Add the `BuildLine` label under `Since`, then, after the
  License block, a Build details block with `IsVisible="{Binding HasBuildDetails}"` (design § UI
  and D10).
  - The branch line, and the warning `⚠ Built with uncommitted changes` with
    `IsVisible="{Binding IsDirty}"`.
  - A `dxe:ChoiceChipGroup`: `ItemsSource="{Binding Groups}"`, `DisplayMember="Label"`, and
    `SelectedItem` two-way to `SelectedGroup`. Confirm the property names in Context7 for
    DevExpress 25.2.4.
  - A `BindableLayout` over `SelectedGroup.Rows`. `Text` uses `Body.Medium` and `SupportingText`
    uses `Body.Small` with `OnSurfaceVariant`, hidden when empty.
  - Build after each edit.

- [ ] **Step 11: Full verification.** Run `dotnet build MyVocaList/MyVocaList.csproj -f
  net10.0-android` (0 errors) and `dotnet test MyVocaList.Tests` (all green). Paste the counts
  into `task-log.md`.

- [ ] **Step 12: Commit** on the task branch, e.g. `feat(about): build identity — MinVer version,
  versionCode, build details`. Then fill in the `task-log.md` entry: Changed files, the AC
  traceability matrix for REQ-BID-01…10, and the evidence from step 8.

### Task 2: Status snapshot (pieces 8–9)

**Interfaces:**
- Consumes: Task 1's `BuildInfo`, `ToGroups`, `WriteBuildDetails` target.
- Produces: `BuildInfo.ReadStatus`, `backlog_gen.py snapshot`.

**Files:**
- Modify: `.claude/scripts/backlog/backlog_gen.py` (new `snapshot` subparser and
  `cmd_snapshot(root, month)`).
- Test: `.claude/scripts/backlog/tests/test_backlog_gen.py`.
- Modify: `MyVocaList/MyVocaList.csproj` (extend `WriteBuildDetails`), `Services/BuildInfo.cs`,
  `MyVocaList/UI/ViewModels/AboutViewModel.cs`.
- Test: `MyVocaList.Tests/Unit/Services/BuildInfoTests.cs`.

- [ ] **Step 1: Python failing tests.** Use the `unittest` style and temp-tree helpers already in
  `test_backlog_gen.py`, and call `cmd_snapshot(root, "2026-11")` with stdout captured.
  - `test_snapshot_in_progress_line`: `P\t<title without **>\t<gate>` (REQ-BID-11).
  - `test_snapshot_gate_from_goal` (11): the text after `Gate:` in `goal`, trimmed.
  - `test_snapshot_empty_gate` (11): `P\t<title>\t`.
  - `test_snapshot_done_this_month` (12).
  - `test_snapshot_non_done_terminal_excluded` (12).
  - `test_snapshot_other_month_excluded` (12).
  - `test_snapshot_empty_tree_prints_nothing_exit_0` (13).
  Run with `python -m pytest .claude/scripts/backlog/tests -q -k snapshot`. Expected: FAIL.

- [ ] **Step 2: Implement `cmd_snapshot`.**
  - It is read-only: use `walk()`, skip errors, and emit through `_emit`.
  - The optional `--month YYYY-MM` argument defaults to `datetime.now().strftime("%Y-%m")`.
  - Register the `snapshot` subparser.

- [ ] **Step 3: Run the full Python suite.** `python -m pytest .claude/scripts/backlog/tests -q`.
  Expected: all pass.

- [ ] **Step 4: C# failing tests.**
  - `ReadStatus_InProgressWithGate` (11).
  - `ReadStatus_EmptyGate` (11).
  - `ReadStatus_Done` (12).
  - `ReadStatus_NullStream_ReturnsNull` (13).
  - `ToGroups_WithStatus_AddsInProgressAndDone` (11/12): the row for an in-progress item has
    `SupportingText` = its gate.
  - `DefaultGroupIndex_InProgressPresent_SelectsIt`.

- [ ] **Step 5: Implement `ReadStatus`.** It splits each line on tab and ignores malformed lines.
  Run the tests and confirm they pass.

- [ ] **Step 6: Extend `WriteBuildDetails`.**
  - Delete `$(IntermediateOutputPath)build-status.txt` first.
  - Then run `Exec` with
    `python "$(MSBuildThisFileDirectory)../.claude/scripts/backlog/backlog_gen.py" snapshot > "<file>"`
    and `ContinueOnError="true"`.
  - Embed the file only when it exists, with `LogicalName="build-status.txt"`. If `python` is
    missing, the shell redirect still leaves an empty file. That is safe: `ReadStatus` returns a
    status with no items, so `ToGroups` hides both groups (REQ-BID-13).
  - `AboutViewModel` passes `ReadStatus(...)` into `ToGroups`.

- [ ] **Step 7: Verify.** Build Android (0 errors), run the full `dotnet test` (green), and check
  that `obj/…/build-status.txt` lists the current `🟡` items. Paste the evidence into
  `task-log.md`.

- [ ] **Step 8: Commit and record in `task-log.md`**, including the AC matrix for REQ-BID-11…13.

## Orchestrator steps (not for implementors)

1. Run the Verifier after each task.
2. **D11 gate:** with Helder's confirmation, run `/sln-release minor alpha` to create
   `v0.2.0-alpha.0` on `develop`.
3. Merge into `develop`, then build and confirm that the version is `0.2.0-alpha.0.N` and no
   update sheet appears on launch (REQ-BID-14).
4. Hand over to Helder's S23 check: the About page and Settings → Apps → version name.
