# Build identity — Design

Ceremony: Standard (build configuration plus one UI page, one layer). Requirements:
`requirements.md` (REQ-BID-01…13).

## Approach — nine small pieces, no new runtime services

| # | Piece | Where | Satisfies |
|---|-------|-------|-----------|
| 1 | Set `ApplicationDisplayVersion` in a target with `AfterTargets="MinVer"` and an explicit `BeforeTargets` list, never at evaluation. The list holds the Android target that writes `versionName`/`versionCode` into the manifest, plus `GenerateAssemblyInfo`. | `MyVocaList.csproj` | 01, 04 |
| 2 | Same target: `ApplicationVersion` = `git rev-list --count --first-parent HEAD` (MSBuild `Exec` with `ConsoleToMSBuild`). An `Error` task follows it when the output is empty or not a number. | `MyVocaList.csproj` | 05, 06 |
| 3 | `AssemblyMetadata` item `BuildTimestampUtc` = UTC build time, ISO 8601 | `MyVocaList.csproj` | 03 |
| 4 | A pure static helper `BuildInfo.Parse(informationalVersion, buildTimestampUtc)` returns the version, the 7-character SHA and the local `yyyy-MM-dd HH:mm` text. `AboutViewModel` reads both attributes, calls the helper and binds one extra line in `AboutPage.xaml`. | helper + `AboutViewModel.cs` + `AboutPage.xaml` | 01, 02, 03 |
| 5 | **Build-details target** (only when `$(ShowBuildDetails)` is not `false`). It runs with `BeforeTargets="PrepareForBuild;AssignTargetPaths"`. It has **no `Inputs`/`Outputs`**, so it runs on every build; skipping it would leave the details stale after a new commit with unchanged sources. It writes `$(IntermediateOutputPath)build-details.txt` in three marker-separated sections:<br>• `#branch`, then the output of `git rev-parse --abbrev-ref HEAD` (captured with `ConsoleToMSBuild`); these lines plus `#uncommitted` are written with `WriteLinesToFile Overwrite="true"`;<br>• then `git status --porcelain >> "<file>"`, one line per uncommitted path;<br>• then `#commits`, appended with `WriteLinesToFile`;<br>• then `git log --no-color -n 10 --first-parent --format=%s >> "<file>"`.<br>Git output always goes through `Exec` shell redirection, so `;` and `%` in subjects or paths never pass through MSBuild item escaping.<br>Every git `Exec` here sets `ContinueOnError="true"`. A details failure never breaks the build; REQ-BID-06 applies to `versionCode` only. The file is added as `EmbeddedResource` with `LogicalName="build-details.txt"`. | `MyVocaList.csproj` | 07, 08, 09, 10 |
| 6 | `BuildInfo.ReadDetails(Stream?, string sha7)` parses the resource by section marker:<br>• porcelain lines become paths with the 2-character status prefix trimmed;<br>• it tolerates CRLF and LF line endings and fewer than 10 subjects;<br>• a branch of `HEAD` (detached checkout) displays as `detached @ <sha7>`;<br>• a missing resource or empty stream gives "no details".<br>`AboutViewModel` loads the resource by its logical name, and `AboutPage.xaml` shows the "Build details" block bound to `IsVisible`. | helper + `AboutViewModel.cs` + `AboutPage.xaml` | 07, 08, 09, 10 |
| 7 | Delete the stale comment above the version properties (`MyVocaList.csproj` lines 49–51), which describes the old zero-padded formula | `MyVocaList.csproj` | — |
| 8 | New **read-only** `backlog_gen.py snapshot` verb (an argparse subparser beside `query`). It reuses `walk()`/`Item`, and never calls `regen` or writes BACKLOG.<br>Output is UTF-8 through the module's existing UTF-8 stdout handling, one line per item, titles without `**`:<br>• `P<TAB>title<TAB>gate` for each item with status `🟡 In Progress`;<br>• `D<TAB>title` for each item with a `✅` status **and** `closed:` equal to the build machine's local `YYYY-MM`.<br>Gate source: `item.gate` if set, else the text after `Gate:` in `goal`, else empty.<br>Malformed READMEs (`walk()` errors) are skipped, never fatal, and the exit code is 0.<br>Pytest cases: `P` line, `P` line with empty gate, gate taken from `goal`, `D` line, non-✅ terminal status excluded, empty result. | `.claude/scripts/backlog/` | 11, 12 |
| 9 | The build-details target first **deletes** any old `build-status.txt`, so a failed run can never embed an outdated snapshot. It then runs `python …backlog_gen.py snapshot > "<file>"` (plain `python`, as the repo hooks use) with `ContinueOnError="true"`, and embeds the file as `LogicalName="build-status.txt"`. `BuildInfo.ReadStatus(Stream?)` reads UTF-8 and tolerates an empty gate field, and the About page renders the two groups. Task 2 edits the same target as task 1, so the two tasks are strictly sequential. | `MyVocaList.csproj` + helper + About | 11, 12, 13 |

> **Spec updated 2026-11-09:** piece 9's `snapshot` Exec passes `--root "<repo root>"` explicitly, because Exec runs in the project directory and the default root (`.`) would find no `Docs/Management`. Impl decision: `<!-- impl decision: Exec cwd is the project dir, so the root must be explicit -->`

## Key decisions (for Helder's approval)

**D1 — The SHA comes from the SDK, not from git calls in the csproj.** Since .NET 8 the SDK
appends the full commit SHA to `AssemblyInformationalVersion`. That is already true here:
`obj/…/MyVocaList.AssemblyInfo.cs` holds `0.1.0-alpha.0.1054+9f9907cd…`. The app splits it on
`+` and takes 7 characters. This piece adds no new code to the build. *Reversibility: easy.*

**D2 — `versionCode` is the first-parent commit count, not MAJOR/MINOR/PATCH and not a
timestamp.** This is the common pattern for git-based builds without CI.
- Deterministic: the same commit gives the same `versionCode`.
- It does not change on every Debug build, so it does not force a full APK reinstall on each
  deploy, which a timestamp would.
- Tags cannot reset it. The original formula gave `100` for every `0.1.x` build. A formula that
  adds MinVer's height would drop when a stable tag lands after its alphas.
- Ceiling 2,100,000,000; today ≈ 1,056.
- Known edge: a feature-branch build can outrank a later `develop` build until `develop` catches
  up. Android then refuses that install as a downgrade. Workaround: uninstall first. This is
  acceptable for a single-developer MVP.
- History rewrites (rebase, squash) on `develop` can lower the count. `develop` is never
  rewritten, so this rule must keep holding.
- *Reversibility: easy before the first store upload. After that the code can only grow, so a
  later formula must start above the last code uploaded.*

**D3 — Show the build time, not the commit time.** Helder asked for the build date. Build time also
identifies Debug builds made from uncommitted work, which the commit SHA alone misses. Cost: the
generated `AssemblyInfo.cs` changes on every build, so the app assembly always recompiles.
During development that happens anyway after an edit. *Reversibility: easy.*

**D4 — The build fails if git is missing (REQ-BID-06).** Without git, MinVer falls back to
`0.0.0-alpha.0` silently, so only the `rev-list` `Exec` can detect the problem. An explicit
`Error` task whose message names `versionCode` makes the failure loud. MinVer already requires
git, so builds gain no new dependency. *Reversibility: easy.*

**D5 — Parsing lives in a pure static helper, not in the ViewModel.** The logic is thin and
presentational, so keeping it in the ViewModel would be defensible. A static helper is still cheap:
it can be unit-tested without MAUI and leaves no "logic in ViewModel" question. It is a function,
not a service, so it needs no DI registration. *Reversibility: easy.*

**D6 — Build details go into an embedded text file, gated by one property.** A file, not
`AssemblyMetadata`, because commit subjects may contain `;`, `%` and quotes, which MSBuild item
lists split or mangle. git writes the subjects straight into the file for the same reason. The file is not embedded when `ShowBuildDetails=false`, and the app then shows
nothing (REQ-BID-10). The default is on for every configuration, because Helder tests Release
builds on device too. *Reversibility: easy.*

**D7 — The first-parent log is the "what's in it" list.** On `develop`, first-parent commits are
mostly the `merge:` commits, which name the item that landed, so they read as a feature list. On a
task branch they are the task's own commits. Showing the last 10 subjects keeps the page short.
The SHA plus `git log` remains the full answer. *Reversibility: easy.*

**D8 — The status snapshot reads item frontmatter, never the LEDGER.** The frontmatter is
structured and validated by the generator. The LEDGER is a hand-written prose table, with no
format the build could parse reliably.
- A task-branch build snapshots that branch's copy of the frontmatter, which can lag `develop`
  because docs land on `develop`. The branch line and the commit list cover that case.
- A large `🟡` item can be partly merged. Its status alone does not show which of its tasks are
  in the build; the commit list does.
- Python is needed at build time. When it is missing, the groups are omitted and nothing fails
  (REQ-BID-13).
*Reversibility: easy.*

**D9 — Delivered as two implementation tasks.**
- Task 1: pieces 1–7 (identity and commit details).
- Task 2: pieces 8–9 (status snapshot).
The tasks run strictly in sequence. *Amended 2026-11-09 (Helder): there is no device stop between
them; Helder checks only the final result on the S23.* *Reversibility: n/a.*

**D10 — Build details use one DevExpress `ChoiceChipGroup` (single select) over a single list,
not tabs.** *Helder delegated the UI choice on 2026-11-09.*
- One chip per group, with its count: `Uncommitted (n)`, `Changes (10)`, `In progress (n)`,
  `Done (n)`.
- A chip with 0 items is hidden.
- Default selection: `In progress`, which answers "what do I test?". If that group is empty the
  default is `Changes`.
- The `⚠ Built with uncommitted changes` line stays visible above the chips whatever is selected,
  because it is a warning, not a list.
Why chips rather than DevExpress `TabView`:
- The page is a `ScrollView`, and a `TabView` needs a bounded height inside one.
- Chips show the counts at a glance and wrap on narrow screens.
- DevExpress 25.2.4 ships `ChoiceChipGroup` (`DevExpress.Maui.Editors`), so the rule "DevExpress
  first" holds.
The selected group is presentation state in `AboutViewModel`; the grouping itself comes from the
`BuildInfo` helper. *Reversibility: easy.*

**D11 — Start the `0.2.0-alpha` line before merging (REQ-BID-14).** *Found during planning on
2026-11-09.*
- `VersionCheckService` compares `AppInfo.VersionString` with the live manifest. The shipped
  AC-UC-06 says a prerelease sorts below its release, and a test pins that.
- Today `1.0.0` passes the check by accident. After piece 1 the version becomes
  `0.1.0-alpha.0.N`, which is below `minRequiredVersion` `0.1.0`, so every dev build would hit the
  forced-update gate.
- The fix is the step `app-versioning/design.md` already prescribes, which was never run: start
  the feature with `/sln-release minor alpha`, which creates tag `v0.2.0-alpha.0` on `develop`.
  Builds then read `0.2.0-alpha.0.N`, above `0.1.0`, so the app shows no prompt.
- No code change, and the shipped update-check spec is untouched.
- **Gate:** `/sln-release` requires Helder's explicit confirmation before the tag is created and
  pushed. The merge into `develop` waits for that tag. Without it, every build from `develop`
  in any parallel session would hit the forced-update gate.
*Reversibility: a pushed tag must never be deleted (`/sln-release` rule); later tags only move
forward.*

## UI

The page title stays `v{version}`. One supporting-text line goes under "Since 2025":
`Build 9f9907c · 2026-11-09 18:16`. Below the License section sits a "Build details" block,
hidden when there are no details:

- the branch line;
- the uncommitted-changes warning (REQ-BID-09);
- the D10 chip group;
- the selected group's rows below it, as a `BindableLayout` of `Label`s (as the page's existing
  "What's New" list already does). "In progress" rows show the title, with the gate as supporting
  text underneath.

No new component and no governed component is touched, and the `SafeAreaEdges` rule is
unaffected.

## Verification

- Build the Android Release APK, then check `obj/…/AndroidManifest.xml` for the `versionName` (the
  MinVer version) and the `versionCode` (the commit count).
- Unit tests (Level B) on `BuildInfo.Parse`, each with its `// [AC] REQ-BID-0x` line:
  - "has `+sha`" → version plus 7-character SHA (01, 02);
  - "no `+`" → version with an empty SHA (02);
  - UTC timestamp → local `yyyy-MM-dd HH:mm` with a fixed time zone (03);
  - `ReadDetails`: branch line (07); `HEAD` → `detached @ <sha7>` (07); 10 subjects in order and
    fewer than 10 (08); CRLF input (08); porcelain lines → warning plus trimmed paths, and an empty `#uncommitted` section → no warning (09); `null`
    stream → no details (10).
  - `ReadStatus`: `P` lines → "In progress (n)" with gate (11); `P` line with an empty gate (11); `D` lines → "Done this month
    (n)" (12); `null` stream → no groups (13).
  - REQ-BID-10's MSBuild path (`ShowBuildDetails=false` embeds nothing) is covered only by the
    manual check below. This is accepted for its size.
- Manual check on device: build from a worktree with one uncommitted edit and confirm the branch
  name and the warning appear. Build with `-p:ShowBuildDetails=false` and confirm the block is
  absent.
- REQ-BID-06, manual check: build from a copy without `.git`, or with git removed from PATH, and
  confirm the build stops with the `versionCode` error.
- Helder's on-device check on the S23: the About page shows the version, SHA and build time;
  Settings → Apps shows the same version name.

## Risks

- Target ordering (piece 1). The constraint is: after `MinVer`, and before both the Android target
  that writes the manifest's version fields and `GenerateAssemblyInfo`. The exact name of the
  Android target is not written here from memory. Implementation step 1 reads it from a binary
  log (`-bl`) of an Android build and puts it in `BeforeTargets`. Acceptance check: the manifest
  in `obj/` and the APK both show the MinVer `versionName` and the commit-count `versionCode`.
  If the target cannot be ordered this way, stop and escalate to Helder. Do not try another
  mechanism without him.
