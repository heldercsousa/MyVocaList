# AutocompleteDXField (pick-lock via X) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Plan status:** written 2026-09-25 against the spec **APPROVED by Helder 2026-09-25** (`requirements.md` § Spec APPROVED). `tasks.md` is the authoritative task list; this plan turns it into executable steps and does not redesign. Where the plan sequences a task differently from a `tasks.md` marker, § 0.3 says so and why.

**Goal:** One reusable DX autocomplete field, `AutocompleteDXField`, owns all autocomplete behaviour. A picked option is read-only until X is tapped, X looks enabled, there is no magnifier, and debounce/threshold come from `SearchConstants`. The Person form's Full Name is the first consumer.

**Architecture:** A `ContentView` hosts one `dxe:AutoCompleteEdit`. Every decision is delegated to a pure `internal static` class, `AutocompleteDXFieldLogic`, which is unit-tested at Level A. Consumers pass an `IMenuOptionSource` per field and bind commands. The lock mechanism (B "read-only + End-icon X" or C "soft lock") is chosen by an on-device spike (T1) before the component is built.

**Tech Stack:** .NET MAUI 10 (Controls 10.0.41), DevExpress.Maui.Editors **25.2.4**, CommunityToolkit.Mvvm, xUnit + Moq. Versions come from `Directory.Packages.props`, never from this file.

**Spec (read all of them, in this order):** `README.md`, `requirements.md`, `design.md` and `tasks.md`, all in this folder:
`Docs/Management/DevCycleCraft/autocomplete-component/changes/2026-09-24-shared-autocomplete-field-pick-lock-via-x-centralised-behaviour/`

---

## Global Constraints

Every task implicitly includes these. Values are copied from the spec or the project rules.

- DevExpress `AutoCompleteEdit` **25.2.4** only. Context7 lookups are pinned to 25.2 (CLAUDE.md § MCP). If Context7 is unavailable, stop with `blocked: MCP unavailable`. Do not guess.
- Minimum query length: `SearchConstants.MinimumLocalQueryLength` (= 2), measured on **trimmed** text. Never a literal.
- Debounce: `SearchConstants.AutocompleteDebounceMilliseconds` (= 300, new in T2). Never a literal.
- Pick-lock is always on. **No** `IsPickLockEnabled`, `CreateNewCommand` or `CreateNewNoun` property, and no create-new row (rulings Q1, Q3).
- X colours: `{StaticResource OnSurfaceVariant}`, or `{StaticResource Error}` while `HasError` (REQ-ACF-08). Never the disabled style.
- `SubmitIconVisibility="Never"` (REQ-ACF-10).
- Component location: `MyVocaList/UI/Components/TextFields/`. Name: `AutocompleteDXField` (MD3 exception, `design.md` § 2).
- The frozen `MyVocaList/UI/Components/AutocompleteField/` family is **not touched** (REQ-DXAC-11).
- `SongFormPage` / `SongFormViewModel` are **not touched** in any committed change (they are owned by the song-form title-first item). The only exception is the T1 spike's throwaway, never-merged branch.
- No `DisplayAlert`/`DisplayActionSheet`/`DisplayPromptAsync`. English only. DevExpress first, even on the spike page.
- Incremental XAML: edit ONE file → build → fix → next file (CLAUDE.md § Constitutional Constraints).
- Business logic stays in ViewModels/Services. The component validates nothing, creates nothing, navigates nowhere (REQ-ACF-12).
- Every test covering an AC starts with `// [AC] REQ-ACF-NN: <criterion>`, one AC per test (`testing.md`).
- Builder never edits a test (`testing.md` § Builder Must Not Modify Tests).
- Build: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` (`/sln-build`). Tests: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj`. 3-attempt build cap, then `Build failure` and stop.
- Censuses and "zero hits" checks use a **Python substring scan**, never `grep` (the rtk proxy drops matches; memory `rtk-rewrites-grep`).

## Review Focus

These are the five failure modes the spec implies but that no Level-A test pins. Each one is assigned to the task that owns the code, as a code-review item and/or a device check.

1. **Stale in-flight results after a pick.** A search started while the field was unlocked finishes after the pick locked it. Expected: no menu opens and no options are shown. **Owner: T4.** After `await SearchAsync`, run `ct.ThrowIfCancellationRequested()` **and** re-evaluate `ShouldRequestOptions(Mode(...), …)` before assigning results. If the result is `false`, drop them (REQ-ACF-04, -26).
2. **`SelectedItem = null` re-entrancy.** The BUG-061 reset re-fires `SelectionChanged` with `null`. Expected: no second `SelectCommand` call, no lock flip. **Owner: T4.** The handler goes through `RoutePick`, and `null` → `None` → no-op (the T3 test `RoutePick_NullOrNonSuggestion_ReturnsNone` pins the logic half).
3. **X on a locked field under B: order of unlock vs focus.** DX `Focus()` shows the keyboard only when `!IsReadOnly` (design § 5.1, IL finding 5). Expected: after X the keyboard is up. **Owner: T4.** In the X handler, the order is `Text = ""` → `IsLocked = false` → mechanism un-applied (inner `IsReadOnly = false`) → `ClearCommand` → `FocusEditor()`. Device proof: T1 B6.
4. **Hydration sets `Text` and `IsLocked` in either order.** Expected: the field ends in `Locked` whichever property the binding engine assigns first. **Owner: T4.** Recompute and apply the mode in the `propertyChanged` callbacks of **all three** of `Text`, `IsLocked` and `IsReadOnly` (not only `IsLocked`). Device proof: the T1 "lock programmatically" button, which sets `IsLocked` first and then `Text`.
5. **`HasError` or `SupportingText` changes while locked or read-only.** Expected: the bottom line and the X colour follow at once. **Owner: T4.** Recompute `HelpText = BottomSupportingText(HasError, SupportingText)` in the callbacks of both `HasError` and `SupportingText`. The X-colour trigger keys on `HasError` for both the clear icon and the End icon. Device proof: T1 S2, T7 line 10.

---

## 0. Orchestration

### 0.1 Wave structure (one task branch per task)

Every code task gets its **own** worktree and branch, based on `develop`. That way each merge is a single revertable unit. No worktree is shared between tasks, except T3's Tester and Builder, who run in sequence on one branch.

| Wave | Tasks (parallel within a wave) | Depends on | Branch / worktree | Model |
|------|-------------------------------|------------|-------------------|-------|
| **Step 0** | Orchestrator doc prep on `develop` (§ 0.4) | — | `develop`, main working tree | orchestrator |
| **Wave 1** | **T1a** spike page build · **T2** constant · **T3-Tester** logic tests | Step 0 | `spike/acf-t1-lock-mechanism` @ `.worktrees/acf-t1-spike` · `task/acf-t2-debounce-constant` @ `.worktrees/acf-t2` · `task/acf-t3-field-logic` @ `.worktrees/acf-t3` | Sonnet · Haiku · Sonnet |
| **Wave 1b** | **T3-Builder** (same branch as T3-Tester) | T3-Tester committed Red | `task/acf-t3-field-logic` @ `.worktrees/acf-t3` | Sonnet |
| **STOP 1** | **T1b** Helder runs the spike on device · **T1c** orchestrator records the verdict | T1a | — | Helder / orchestrator |
| **Wave 2** *(may run during STOP 1)* | **T5** Person VM option source | T3 merged | `task/acf-t5-person-option-source` @ `.worktrees/acf-t5` | Sonnet |
| **Wave 3** | **T3-C** `TextToRestore` (**only if Helder accepted C**) | STOP 1 verdict = C | `task/acf-t3c-text-to-restore` @ `.worktrees/acf-t3c` | Sonnet |
| **Wave 4** | **T4** component | STOP 1 verdict ∈ {B, C}; T2 + T3 (+ T3-C) merged | `task/acf-t4-component` @ `.worktrees/acf-t4` | **Opus** |
| **Wave 5** | **T6** Person page migration | T4 + T5 merged | `task/acf-t6-person-migration` @ `.worktrees/acf-t6` | Sonnet |
| **STOP 2** | **T7** Helder device gate (+ T7-prep throwaway branch) | T6 merged | `spike/acf-t7-supporting-text` @ `.worktrees/acf-t7-prep` (never merged) | Helder (+ Sonnet prep) |

**Wave cap:** at most 3 subagents are ever in flight (Wave 1). This is within the cap of 4.

**What can run before the spike result (mechanism-independent):** T2, T3 (Tester + Builder, **without** `TextToRestore`) and T5. **What waits for the verdict:** T3-C (conditional), T4, T6, T7.

**Merge order into `develop`:** T2 → T3 → T5 → (T3-C) → T4 → T6. Each merge is `git merge --no-ff <branch>` from the main working tree on `develop`. After every merge, run the post-merge verification (§ 0.5) **before** the next merge. The T1 and T7-prep spike branches are **never merged**.

### 0.2 File-overlap check (single-writer)

| Task | Files owned | Overlap |
|------|-------------|---------|
| T1a | spike page (new, throwaway), `AppShell.xaml`, `Navigation/NavigationConfig.cs`, `SongFormPage.xaml(.cs)`: **uncommitted-to-develop, spike branch only** | `AppShell.xaml` is sequential-only, but it is never merged, so no product conflict. No other Wave-1 task touches these files. |
| T2 | `Domain/Constants/SearchConstants.cs`, `MyVocaList.Tests/Unit/Domain/SearchConstantsTests.cs` (new) | none |
| T3 | `MyVocaList/UI/Models/IMenuOptionSource.cs` (new), `MyVocaList/UI/Components/TextFields/AutocompleteDXFieldLogic.cs` (new), `MyVocaList.Tests/Unit/Components/AutocompleteDXFieldLogicTests.cs` (new) | none |
| T5 | `MyVocaList/UI/ViewModels/PersonFormViewModel.cs`, `MyVocaList.Tests/Unit/ViewModels/PersonFormViewModelTests.cs` | none (Wave 2, alone) |
| T3-C | `AutocompleteDXFieldLogic.cs`, `AutocompleteDXFieldLogicTests.cs` | the same files as T3, but T3 is merged first |
| T4 | `MyVocaList/UI/Components/TextFields/AutocompleteDXField.xaml` + `.xaml.cs` (new) | none |
| T6 | `MyVocaList/UI/Pages/People/PersonFormPage.xaml` + `.xaml.cs` | none |
| Docs | `task-log.md`, `findings.md`, `tasks.md`, `MyVocaList.sln`, `LEDGER.md` | **orchestrator only, on `develop`.** Implementors write only their own pre-created section of `task-log.md` (§ 0.4) in their worktree copy. They **never** edit `tasks.md` or `MyVocaList.sln`. |

The exit checklist in `implementor.md` says "checkboxes ticked in `tasks.md`". For this plan that step belongs to the **orchestrator**, after each merge, because `tasks.md` is sequential-only and several branches are live at once.

### 0.3 Sequencing deviations from `tasks.md` (not redesign)

Every deviation below is recorded **on `develop` by the orchestrator at Step 0** (§ 0.4 step 0.6), as `> **Spec updated 2026-09-25:**` notes. The spec is not shipped, so in-place notes are allowed. No implementor edits `tasks.md` or `design.md` for these.

1. **T3 is split into T3 (mechanism-independent) and T3-C (conditional).** `tasks.md` marks T3 `[SEQUENTIAL, after the T1 verdict]`. Only one of its functions depends on the verdict: `TextToRestore`, which exists "only if mechanism C is accepted" (`design.md` § 4). Every other § 4 function is identical under B and C. So T3 runs in Wave 1 and `TextToRestore` becomes T3-C.
2. **AC tags 19, 22 and 24 in T3.** `tasks.md` T3 lists them. `design.md` § 4 and § 10 say that REQ-ACF-19 and -24 are proven by code review and on device, and REQ-ACF-22 at the VM seam (T5). The plan follows `design.md`, which is more specific. Under C, T3-C gives REQ-ACF-19 and -24 real unit tests (`TextToRestore` returns `null` for unlocked user input and for programmatic changes). Under B they stay review + device items.
3. **T1's `findings.md` and the `.sln` registration** are done by the orchestrator (Step 0 skeleton, T1c results), not by the T1 implementor. This keeps `MyVocaList.sln` single-writer while three worktrees are live.
4. **T1 local commits.** `tasks.md` T1 says "No production file is committed". The plan allows local `spike:` commits on the throwaway branch `spike/acf-t1-lock-mechanism`, so that Helder deploys a known state. The branch is never pushed or merged, and it is deleted at T1c. The same applies to `spike/acf-t7-supporting-text`.
5. **`design.md` § 3.2 parameter name and term.** The design writes `SearchPersonsStartsWithAsync(term, ct: ct)`. The real parameter is `cancellationToken` (`Domain/ServicesInterfaces/IPersonService.cs:24`), so the call becomes `SearchPersonsStartsWithAsync(trimmed, cancellationToken: ct)`. The core method passes the **trimmed** term, consistent with REQ-ACF-13's trimmed threshold. The service already normalises its input itself (`Services/PersonService.cs:216`, `NormalizeSearchQuery()`), so this changes nothing for unpadded input.

### 0.4 Step 0: orchestrator prep on `develop` (docs only, before Wave 1)

- [ ] **0.1** Session-start protocol (Rule 7) and hook health check. Record the baseline: run `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` on `develop` and write the pass count into `task-log.md` § Baseline (it is used for the "+N tests" checks below).
- [ ] **0.2** Status: `python .claude/scripts/backlog/backlog_gen.py status shared-autocomplete-field-pick-lock-via-x-centralised-behaviour "🟢"` (spec approved, plan ready). Move it to 🟡 at Wave 1 dispatch. Run `regen` and inspect the diff (`workflow-rule-2.md` § Generated artifacts).
- [ ] **0.3** Create `task-log.md` in this folder with these pre-created sections, one per task, so that parallel worktrees edit different hunks: `## Baseline`, `## T1 — Spike`, `## T2`, `## T3`, `## T5`, `## T3-C`, `## T4`, `## T6`, `## T7 — Device gate`. Each section holds only a `### Checkpoint` placeholder.
- [ ] **0.4** Create `findings.md` in this folder with two parts:
  - an empty `## Environment` section (filled at T1c from the T1a handback);
  - the **T1 run sheet** (§ T1b below, copied verbatim, with empty Result/Evidence cells).
- [ ] **0.5** Register `task-log.md` and `findings.md` in `MyVocaList.sln` (this folder's SolutionItems, using the same `sln_add.py` helper used for `plan.md`).
- [ ] **0.6** Record every § 0.3 deviation on `develop` as `> **Spec updated 2026-09-25:**` notes:
  - **`tasks.md` T3:** the split into T3 + T3-C (item 1), and the AC-tag change (19/22/24 handled per `design.md` § 4, item 2);
  - **`tasks.md` T1:** local `spike:` commits allowed, never pushed or merged (item 4), and `findings.md`/`.sln` owned by the orchestrator (item 3);
  - **`design.md` § 3.2:** the `cancellationToken:` parameter name and the trimmed term (item 5).
- [ ] **0.7** Add the LEDGER rows for Wave 1 (`/sln-ledger`): branch, worktree, phase, status.
- [ ] **0.8** Commit on `develop`: `docs(acf): plan prep — task-log + findings skeletons, deviation notes`.
- [ ] **0.9** Check that `.worktrees/` is ignored: `git check-ignore -q .worktrees`. Create **only the Wave-1 worktrees** now (`acf-t1-spike`, `acf-t2`, `acf-t3`). Create each later worktree (T5, T3-C, T4, T6, T7-prep) **only after its dependencies are merged**, so that it branches from the updated `develop`. Use `git worktree add .worktrees/<name> -b <branch> develop`, then check `git -C .worktrees/<name> merge-base --is-ancestor develop HEAD` and that the dependency's merge commit is in `git -C .worktrees/<name> log --oneline -5`.

### 0.5 Post-merge verification (after EVERY merge into develop)

Run from the main working tree on `develop`:

```
dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android     # expect: Build succeeded, 0 Error(s)
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj              # expect: Failed: 0; Passed = previous + the task's new tests
```

Then do the following:

- Review the merged task's `task-log.md` section. It needs `### Changed files`, Verification evidence, and the AC matrix for Level A tasks.
- Sync its docs to develop (`/sln-docs-sync`).
- Tick the task's box in `tasks.md`.
- Update the LEDGER.
- Dispatch a **Verifier** subagent (Sonnet) on the merge diff.
- Remove the worktree: `git worktree remove .worktrees/<name>`.

**Known flaky test (BUG-076).** If the **only** failure is the known-flaky SQLite test that throws `ObjectDisposedException`, re-run `dotnet test` **once**. Record both runs (the failing test name and the pass/fail counts of each) in the merged task's `task-log.md` section.
- The re-run is fully green → continue.
- It fails again, or any **other** test fails in either run → treat it as red (below).

If the build or the tests go red: **do not merge the next branch.** Roll back the merge with `git revert -m 1 <merge-sha>`, then diagnose on the task branch (`superpowers:systematic-debugging`).

### 0.6 Implementor briefing template (every task)

Each briefing contains:

- **Role scope:** files owned (from the task), files off-limits (`tasks.md`, `MyVocaList.sln`, `LEDGER.md`, `BACKLOG.md`, any `README.md` frontmatter, every file not owned).
- **Permitted MCPs.**
- The worktree path. The implementor must edit **the worktree's copies**, never the main repo paths (memory `worktree-edit-path-discipline`).
- The task section of this plan and the spec files to read.
- The checkpoint duty: the `### Checkpoint` block in its own `task-log.md` section (`session-ops.md` § Checkpoint Ping).
- The **exit checklist** (`workflow-rule-2.md`): verification-before-completion → build 0 errors → test → post-edit re-read → (no `.sln` work; the orchestrator owns it) → living-spec check (`> **Spec updated YYYY-MM-DD:**` notes only) → task-log (`### Changed files`, evidence, AC matrix) → commit → `git push origin HEAD`. **No `tasks.md` tick.**

---

## Phase 0: Spike

### Task T1: On-device lock-mechanism spike (B, then C), HARD STOP for Helder

**Split:** T1a = an implementor builds the throwaway page (Sonnet, ≤ 60 min). T1b = Helder runs the run sheet on his Android device (2 h hard time-box). T1c = the orchestrator records the results and the verdict in `findings.md` on `develop`.

> **HARD STOP.** Nothing that depends on the mechanism (T3-C, T4, T6, T7) starts until **(1)** Helder reports the T1b results and **(2)** the orchestrator has recorded in `findings.md` which row of the `design.md` § 5 decision table applied: **B**, **C (accepted by Helder)** or **escalate**. On "escalate", the orchestrator presents options (i)–(iii) from `design.md` § 5 to Helder and **picks none**. T2, T3 and T5 may continue during the wait.

**Files (spike branch `spike/acf-t1-lock-mechanism` only, never merged, never pushed):**
- Create: `MyVocaList/UI/Pages/Spike/AcfSpikePage.xaml`, `MyVocaList/UI/Pages/Spike/AcfSpikePage.xaml.cs`
- Modify (temporary entry point): `MyVocaList/AppShell.xaml`. Add `<FlyoutItem Route="acfspike" FlyoutItemIsVisible="False"><ShellContent ContentTemplate="{DataTemplate spike:AcfSpikePage}" /></FlyoutItem>` next to the other hidden `FlyoutItem`s, plus `xmlns:spike`.
- Modify (temporary entry point): `MyVocaList/Navigation/NavigationConfig.cs`. Add one menu row, `new MenuItemDescription("ACF Spike", "close_outlined", "acfspike", navigateCommand),` at the end of the list. The spike therefore appears as the last row of the flyout menu, only in builds from this branch.
- Modify (B5 only): `MyVocaList/UI/Pages/Songs/SongFormPage.xaml` (+ `.xaml.cs` if needed). Apply mechanism B to the Artist `AutoCompleteEdit` while `IsArtistLocked`.

Committing on the spike branch is allowed only as a local checkpoint (`spike:` prefix), so Helder can deploy a known state. The branch is deleted after T1c: `git worktree remove --force .worktrees/acf-t1-spike` then `git branch -D spike/acf-t1-lock-mechanism`. **No file from this branch reaches `develop`.**

#### T1a: build the spike page (implementor, Sonnet)

- [ ] **Step 1.** Create the worktree (§ 0.4 0.9). Invoke `myvocalist-coding`. Use Context7 (DevExpress MAUI, 25.2) to confirm these member names on `AutoCompleteEdit`/`EditBase`: `EndIcon`, `IsEndIconVisible`, `EndIconColor`, `EndIconClicked`, `ClearIconVisibility`, `ClearIconColor`, `ClearIconClicked`, `SubmitIconVisibility`, `HelpText`, `HasError`, `ErrorText`, `IsReadOnly`, `IsDropDownOpen`, the `TextChanged` args `Reason` (`UserInput`/`ProgrammaticChange`/`ItemSelected`), `AsyncItemsSourceProvider.RequestDelay` (record its **type**, int or TimeSpan, in the T1 task-log section: T4 needs it).
- [ ] **Step 2.** Build `AcfSpikePage.xaml` (`SafeAreaEdges="Container"`, inside a `dx:DXScrollView`). It has six blocks:
  1. **Field B**: a `dxe:AutoCompleteEdit` fed by `AsyncItemsSourceProvider` (`RequestDelay` 300, `CharacterCountThreshold` 2) from a static list of 5 `AutocompleteSuggestion`s: "John Doe", "Joan Baez", "Johnny Cash", "Jon Bon Jovi", "Joni Mitchell" (StartsWith, OrdinalIgnoreCase, trimmed length ≥ 2). `SubmitIconVisibility="Never"`. Code-behind implements mechanism B exactly as specified in `tasks.md` T1 "B procedure". While locked: `IsReadOnly=True`, `ClearIconVisibility=Never`, `EndIcon=close_outlined`, `IsEndIconVisible=True`, `EndIconColor=OnSurfaceVariant`, and an `EndIconClicked` handler that logs, then unlocks: `IsReadOnly=False` → `IsEndIconVisible=False` → `ClearIconVisibility=Auto` → `Text=""` → `Focus()`. A pick locks and sets `SelectedItem=null`.
  2. **Field C**: the same list and settings, with mechanism C per `tasks.md` T1 "C procedure": `IsReadOnly=False` and the built-in clear icon. While locked, revert `TextChanged` when `Reason==UserInput`, skip `ItemsRequested`, and call `Unfocus()` on `Focused`. It is built now so that one device session covers both. Helder measures it **only if B fails**.
  3. **Field R** (read-only check): `IsReadOnly=True`, `ClearIconVisibility=Never`, `IsEndIconVisible=False`, `Text="Don't Cry"`.
  4. **Controls** (DX `DXButton`s / `DXSwitch`): "Lock B programmatically" (sets `IsLocked` first, **then** `Text="Johnny Cash"`, to exercise Review Focus 4), "Lock C programmatically", "Reset all", and three switches:
     - "Supporting text": `HelpText="Online search unavailable"` on all three fields.
     - "HasError (component rule)": `HasError=True`, `ErrorText="Title is required"` on all three fields, and, per the component rule, `HelpText=""` while in error.
     - "HasError, keep HelpText": `HasError=True` and `ErrorText` as above, but `HelpText` is **left as set**. This answers S2's question: does DX hide `HelpText` by itself?
  5. **Reference**: a DX `TextEdit` with `Text="Reference"`, `ClearIconVisibility=Auto` and `ClearIconColor=OnSurfaceVariant`, for the B3 side-by-side.
  6. **Event log**: a `Label` (max 12 lines, newest first) plus counters. Each line reads `HH:mm:ss.fff <field> <event> <detail>` for: `ItemsRequested` (count, and whether a search ran), `TextChanged` (Reason, new text), `SelectionChanged`, `Focused`/`Unfocused`, `EndIconClicked`, `ClearIconClicked`, and `IsDropDownOpen` changes. The log is on screen, so the video records it and no logcat is needed.
- [ ] **Step 3.** Add the temporary `AppShell.xaml` and `NavigationConfig.cs` entries. Build after each file: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` → 0 errors.
- [ ] **Step 4.** B5 prep: in `SongFormPage.xaml`, on the Artist `AutoCompleteEdit`, add B while `IsArtistLocked` is true. Use a `DataTrigger` (or code-behind) that sets `IsReadOnly=True`, `ClearIconVisibility=Never`, `EndIcon=close_outlined`, `IsEndIconVisible=True` and `EndIconColor=OnSurfaceVariant`. `EndIconClicked` calls the existing clear path (the handler behind today's `ClearIconClicked`). Build → 0 errors.
- [ ] **Step 5.** Smoke-test on the emulator (`dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -t:Run`): the flyout shows "ACF Spike", the page opens, typing "jo" in Field B lists 5 options, and the log fills. Commit locally: `spike: ACF T1 throwaway page (never merge)`. **Do not push.**
- [ ] **Step 6: Handback, not a worktree file.** The spike branch is deleted at T1c, so nothing the verdict depends on may live only there. **Do not** write the T1 results into the worktree's `task-log.md`. Return them in the **handback message** to the orchestrator:
  - the branch and the local commit SHA;
  - the `RequestDelay` type (int or TimeSpan);
  - the Context7 results per member (confirmed / renamed / missing);
  - the `devexpress-maui` MCP result ("returned empty" or the hits);
  - build and emulator smoke results.
  
  Status `Check build`. Stop.

#### T1b: Helder runs the spike on device (HARD STOP)

**Deploy:** from `.worktrees/acf-t1-spike`, with the device connected over USB: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -t:Run` (or open that worktree's `MyVocaList.sln` in VS). Open the flyout and tap **ACF Spike**. For B5, open **Songs → an existing song (edit)**.

**Recording setup (once):**
1. Developer options → **Show taps** ON (taps are then visible in every frame).
2. Record at 60 fps. Preferred: `scrcpy --record=<name>.mp4 --max-fps=60 --no-audio`. Fallback: `adb shell screenrecord --bit-rate 20000000 /sdcard/<name>.mp4`, then `adb pull` (3-minute limit per clip). Record one clip per criterion group, named `t1-<criterion>-<attempt>.mp4`. Keep the clips **outside the repo** (e.g. `C:\Users\helde\Videos\acf-t1\`); they are too large to commit.
3. Frame check: step through frames in the player (VLC: `E` = next frame), or extract them with `ffmpeg -i t1-b2-1.mp4 -vf fps=60 frames/%05d.png`. Confirm the fps with `ffprobe` (`r_frame_rate`). If a clip is below 60 fps, write down the real rate next to the result.
4. Screenshots (B3, R1, S1, S2): device screenshot, same zoom, light theme (and dark theme if time allows).

**Run sheet (copied into `findings.md` at Step 0; Helder fills Result and Evidence).** The criteria are verbatim from `tasks.md` T1.

| # | Setup | Action | Pass iff | Result | Evidence (clip/frames/log) |
|---|-------|--------|----------|--------|----------------------------|
| B1 | Field B, "Lock B programmatically" | Tap the text, 5× | No keyboard and no cursor in any frame | | |
| B2 | Field B: type "jo", pick "Johnny Cash", **keyboard still up** | 10 keystrokes + 1 long-press paste | Text byte-equal "Johnny Cash"; 0 frames show a typed/pasted char | | |
| B3 | Field B locked; Reference `TextEdit` beside it | Screenshot both; then unlock Field B, type "jo" and screenshot its built-in clear icon | End icon in `OnSurfaceVariant`, not dimmed. End-icon X (locked) vs built-in clear icon (unlocked): same glyph, size, position, colour? Record every difference | | |
| B4 | Field B locked | Tap X, 5× (re-lock between taps) | Log shows `EndIconClicked` 5/5 | | |
| B5a | Field B | Programmatic lock, 5× | No menu opens (log: no `IsDropDownOpen=true`), 5/5 | | |
| B5b | Spike SongFormPage, edit a saved song with an artist | Open edit, 5× | No menu opens on load, 5/5 | | |
| B5c | Field B and spike SongFormPage | Pick an option, 5× each | No menu reopens after the pick, 5/5 | | |
| B6 | Field B locked | Tap X | Keyboard opens; typing "jo" logs `ItemsRequested` with a search and shows options | | |
| R1 | Field R | Screenshot | Text in the normal (not disabled) colour; no X in any frame | | |
| R2 | Field R | Tap, long-press, 10 keystrokes | Text byte-equal "Don't Cry"; no keyboard, no cursor in any frame | | |
| R3 | Field R | Page load + tap, 5× | No menu opens, 5/5 | | |
| S1 | "Supporting text" ON | Screenshot Field B unlocked, Field B locked, Field R | "Online search unavailable" under each field | | |
| S2 | "Supporting text" ON, then "HasError (component rule)" ON | Screenshot | Only "Title is required" shows, on one line, with no layout jump beyond DX's own bottom line | | |
| S2b | "Supporting text" ON, then "HasError, keep HelpText" ON | Screenshot | Informational: does DX hide `HelpText` by itself while `HasError`? Record yes/no and what renders | | |
| M | Any field | Look | No magnifier icon (`SubmitIconVisibility=Never`) | | |
| H | Field B and Field C | Pick, then reopen the menu by unlocking + typing | Does any row stay highlighted? (per mechanism; informational) | | |
| C1 | *Only if B failed.* Field C locked | 10 keystrokes | Text byte-equal; 0 frames show a typed char | | |
| C2 | *Only if B failed.* Field C locked | Long-press paste; long-press "select all" + keystroke; Gboard swipe one word; accented char via long-press | Each leaves the text byte-equal; 0 frames show a change | | |
| C3 | *Only if B failed.* Field C locked | Type, paste | 0 `ItemsRequested` searches in the log; 0 frames with the menu | | |
| C4 | *Only if B failed.* Field C locked | Tap X, 5× | Clears 5/5; never swallowed by the `Unfocus` | | |
| C5 | *Only if B failed.* Field C locked | Tap the text, 5× | Frames with the keyboard visible (measure; target 0; **Helder sets the threshold**) | | |

**Verdict rule (`design.md` § 5 decision table):** all of B1–B6 pass → **B**. Otherwise, if C1–C5 pass → **escalate Q5**, and C is built only if Helder accepts it after seeing the numbers. Otherwise → **stop and escalate** with options (i)–(iii). If R1–R3 fail, tell Helder regardless of the verdict: the song-form item's read-only Title depends on them.

#### T1c: orchestrator records the verdict (docs only, on develop)

- [ ] Write the T1a handback (SHA, `RequestDelay` type, Context7 and MCP results, smoke results) into the T1 section of `task-log.md` and into `findings.md` § Environment, on `develop`, **before** the spike branch is deleted.
- [ ] Paste Helder's results into `findings.md`. Add the verdict line (`Verdict: B` / `Verdict: C (accepted by Helder YYYY-MM-DD)` / `Verdict: escalated — <Helder's choice>`), the decision-table row applied, the S2 finding (whether DX hides HelpText on error), the B3 finding (whether the End icon and the built-in icon look identical; if not, `design.md` § 5 says to use the End icon as X in both states, and that goes to T4), and the `RequestDelay` type from T1a.
- [ ] If the verdict leads to a design change (for example the B3 difference, or escalation option ii/iii), add a `> **Spec updated YYYY-MM-DD:**` note to `design.md` § 5 **before** T4 is briefed.
- [ ] Commit on develop: `docs(acf): T1 spike findings — verdict <B|C|escalated>`. Delete the spike worktree and branch. Update the LEDGER.
- [ ] If escalated with no decision: `blocked: spike inconclusive`. T3-C, T4, T6 and T7 do not start.
- [ ] **If Helder picks escalation option (ii) or (iii)** (`design.md` § 5): these change the component contract, so the already-merged T3 logic may need rework.
  - Option (ii) is a separate read-only `TextEdit` + "Change" button. It affects `Mode`/`ShowClearIcon`, and REQ-ACF-07/08 wording changes.
  - Option (iii) allows type-over, which drops pick-lock. It affects `Mode`, `LockAfterPick` and `ShouldRequestOptions`.
  
  **Spec first:** record the decision in `requirements.md`/`design.md` (a `> **Spec updated YYYY-MM-DD:**` note plus the changed ACs), and have it reviewed. Then re-plan T3 (a new rework task, Tester/Builder, with the old tests changed **only** where the updated spec changes the AC) and T4 before any code. T5 is unaffected.

---

## Phase 1: Domain

### Task T2: `SearchConstants.AutocompleteDebounceMilliseconds = 300` [P, Wave 1, mechanism-independent]

**Model:** Haiku (Sonnet if Haiku is unavailable). **Level:** B. **Review lane:** Standard.

**Files:**
- Modify: `Domain/Constants/SearchConstants.cs` (append the constant after `MaxSearchResults`)
- Create: `MyVocaList.Tests/Unit/Domain/SearchConstantsTests.cs` (namespace `MyVocaList.Tests.Unit.Domain`)

**Interfaces — Produces:** `public const int SearchConstants.AutocompleteDebounceMilliseconds = 300;` (consumed by T4).

- [ ] **Step 1: Write the failing test**

```csharp
public class SearchConstantsTests
{
    [Fact]
    // [AC] REQ-ACF-14: every autocomplete field shares one 300 ms debounce delay.
    public void AutocompleteDebounceMilliseconds_Is300()
        => Assert.Equal(300, SearchConstants.AutocompleteDebounceMilliseconds);
}
```

- [ ] **Step 2: Run it and confirm Red.** `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~SearchConstantsTests"`. Expected: a build failure, `CS0117: 'SearchConstants' does not contain a definition for 'AutocompleteDebounceMilliseconds'`.
- [ ] **Step 3: Implement.** Add the constant, with an XML doc in the style of its siblings: "Debounce delay, in milliseconds, before an autocomplete field requests options after the last keystroke. REQ-ACF-14 (shared autocomplete field); makes REQ-DXAC-07's ≈300 ms concrete. One value for every autocomplete field (Helder 2026-09-23/24)."
- [ ] **Step 4: Run it and confirm Green.** The same filter → `Passed: 1`. Then the full suite → `Failed: 0`.
- [ ] **Step 5: Build.** `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` → 0 errors.
- [ ] **Step 6: Exit checklist** (§ 0.6), then commit:

```
feat(search-constants): add AutocompleteDebounceMilliseconds = 300 (REQ-ACF-14)
```

**Merge risk / rollback:** Low (additive const, no consumer yet). Rollback: `git revert -m 1 <merge-sha>`.

---

## Phase 2: UI logic (Level A)

### Task T3: `IMenuOptionSource` + `AutocompleteDXFieldLogic` (without `TextToRestore`) [Wave 1 + 1b, mechanism-independent]

**Model:** Tester = Sonnet, Builder = Sonnet (two separate dispatches, one branch; `testing-tdd-roles.md`). **Level:** A. **Review lane:** Standard.

**Files:**
- Create: `MyVocaList/UI/Models/IMenuOptionSource.cs` (namespace `MyVocaList.UI.Models`)
- Create: `MyVocaList/UI/Components/TextFields/AutocompleteDXFieldLogic.cs` (namespace `MyVocaList.UI.Components.TextFields`)
- Create: `MyVocaList.Tests/Unit/Components/AutocompleteDXFieldLogicTests.cs` (namespace `MyVocaList.Tests.Unit.Components`)

`internal` is visible to the tests through `[assembly: InternalsVisibleTo("MyVocaList.Tests")]` (`MyVocaList/Properties/AssemblyInfo.cs:3`).

**Interfaces — Produces (exact; consumed by T3-C, T4, T5):**

```csharp
// IMenuOptionSource.cs (design.md § 3.2, verbatim)
public interface IMenuOptionSource
{
    /// <summary>Returns THIS query's options (BUG-056: never read back from an observable).</summary>
    Task<IReadOnlyList<AutocompleteSuggestion>> SearchAsync(string query, CancellationToken ct);
}
public sealed class MenuOptionSource(Func<string, CancellationToken, Task<IReadOnlyList<AutocompleteSuggestion>>> search)
    : IMenuOptionSource
{
    public Task<IReadOnlyList<AutocompleteSuggestion>> SearchAsync(string query, CancellationToken ct) => search(query, ct);
}

// AutocompleteDXFieldLogic.cs (design.md § 4)
internal enum FieldMode { Editable, Locked, ReadOnly }
internal enum PickRoute { None, Select }
internal static class AutocompleteDXFieldLogic
{
    internal static FieldMode Mode(bool isReadOnly, bool isLocked, string? text);
    internal static string BottomSupportingText(bool hasError, string? supportingText);
    internal static bool ShowClearIcon(FieldMode mode, string? text);
    internal static bool ShouldRequestOptions(FieldMode mode, bool isSearchEnabled, IMenuOptionSource? source, string? query);
    internal static PickRoute RoutePick(object? selectedItem);
    internal static bool LockAfterPick(PickRoute route);
    internal static bool ShouldForwardBlur(object? selectedItem, FieldMode mode);
}
```

#### Wave 1: Tester (writes the tests and skeletons, then stops)

- [ ] **Step 1.** Create `IMenuOptionSource.cs` with the complete code above. It is a contract, not logic: the interface and the one-line delegate adapter. Create `AutocompleteDXFieldLogic.cs` with the two enums and the seven signatures above, each body `=> throw new NotImplementedException();`. Build → 0 errors.
- [ ] **Step 2.** Write all the tests below in `AutocompleteDXFieldLogicTests.cs`. Each one starts with its `// [AC]` line and covers one AC. Use a private `static readonly IMenuOptionSource Source = new MenuOptionSource((_, _) => Task.FromResult<IReadOnlyList<AutocompleteSuggestion>>([]));` and a sample `new AutocompleteSuggestion("John Doe", "JD", null)` (check the constructor in `Contracts/Models/AutocompleteSuggestion.cs`).

| Test | `// [AC]` | Asserts |
|------|-----------|---------|
| `Mode_ReadOnly_ReturnsReadOnly_WhateverLockAndText` (Theory: locked true/false × text "Don't Cry"/"") | REQ-ACF-12b | `ReadOnly` |
| `Mode_LockedWithText_ReturnsLocked` | REQ-ACF-02 | `Mode(false, true, "Johnny Cash") == Locked` |
| `Mode_LockedWithEmptyOrWhitespaceText_ReturnsEditable` (Theory: `null`, `""`, `"   "`) | REQ-ACF-05a | `Editable` |
| `Mode_NotLockedWithText_ReturnsEditable` | REQ-ACF-03 | `Editable` |
| `BottomSupportingText_NoError_ReturnsSupportingText` | REQ-ACF-12c | `"Online search unavailable"` |
| `BottomSupportingText_HasError_ReturnsEmpty` | REQ-ACF-12c | `""` |
| `BottomSupportingText_NoErrorNullText_ReturnsEmpty` | REQ-ACF-12c | `""` |
| `ShowClearIcon_EditableWithText_ReturnsTrue` (Fact) | REQ-ACF-07 | `ShowClearIcon(FieldMode.Editable, "jo") == true` |
| `ShowClearIcon_LockedWithText_ReturnsTrue` (Fact) | REQ-ACF-07 | `ShowClearIcon(FieldMode.Locked, "Johnny Cash") == true` |
| `ShowClearIcon_EmptyText_ReturnsFalse` (Theory over `string?`: `null`, `""`; mode fixed to `FieldMode.Editable` inside the body) | REQ-ACF-07 | `false` |
| `ShowClearIcon_ReadOnlyWithText_ReturnsFalse` | REQ-ACF-12b | `ShowClearIcon(FieldMode.ReadOnly, "Don't Cry") == false` |
| `ShouldRequestOptions_EditableSearchOnTrimmedTwoChars_ReturnsTrue` (Theory over `string`: `"ab"`, `" ab "`) | REQ-ACF-13 | `ShouldRequestOptions(FieldMode.Editable, true, Source, query) == true` |
| `ShouldRequestOptions_BelowTrimmedThreshold_ReturnsFalse` (Theory over `string?`: `null`, `""`, `"a"`, `" a"`, `"a "`, `"   "`) | REQ-ACF-13 | `ShouldRequestOptions(FieldMode.Editable, true, Source, query) == false` |
| `ShouldRequestOptions_Locked_ReturnsFalse` | REQ-ACF-04 | `ShouldRequestOptions(FieldMode.Locked, true, Source, "Johnny Cash") == false` |
| `ShouldRequestOptions_SearchDisabled_ReturnsFalse` | REQ-ACF-12a | `ShouldRequestOptions(FieldMode.Editable, false, Source, "ab") == false` |
| `ShouldRequestOptions_ReadOnly_ReturnsFalse` | REQ-ACF-12b | `ShouldRequestOptions(FieldMode.ReadOnly, true, Source, "ab") == false` |
| `ShouldRequestOptions_NullSource_ReturnsFalse` | REQ-ACF-11 | `ShouldRequestOptions(FieldMode.Editable, true, null, "ab") == false` |
| `RoutePick_AutocompleteSuggestion_ReturnsSelect` | REQ-ACF-11 | `Select` |
| `RoutePick_NullOrNonSuggestion_ReturnsNone` (Theory: `null`, `"text"`, `42`) | REQ-ACF-11 | `None` (Review Focus 2) |
| `LockAfterPick_Select_ReturnsTrue` | REQ-ACF-01 | `true` |
| `LockAfterPick_None_ReturnsFalse` | REQ-ACF-01 | `false` |
| `ShouldForwardBlur_NoPickEditable_ReturnsTrue` | REQ-ACF-17 | `ShouldForwardBlur(null, FieldMode.Editable) == true` |
| `ShouldForwardBlur_WithSelectedItem_ReturnsFalse` | REQ-ACF-11 | `ShouldForwardBlur(suggestion, FieldMode.Editable) == false` |
| `ShouldForwardBlur_Locked_ReturnsFalse` | REQ-ACF-11 | `ShouldForwardBlur(null, FieldMode.Locked) == false` |
| `ShouldForwardBlur_ReadOnly_ReturnsFalse` | REQ-ACF-12b | `ShouldForwardBlur(null, FieldMode.ReadOnly) == false` |

**Accessibility rule (compile-critical).** `FieldMode` and `PickRoute` are `internal`. The xUnit test class and its methods are `public`, so a test method **must never take `FieldMode` or `PickRoute` as a parameter**: that is `CS0051: Inconsistent accessibility`. Use `internal` enum values only **inside method bodies**. Every row above that varies the mode is therefore a separate `[Fact]`. Theories (`[InlineData]`) vary only `string`, `bool` or `object` parameters. Where an AC letter matters, the comment says so (e.g. `REQ-ACF-11 (b): pick routing`).

- [ ] **Step 3: Confirm Red.** `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~AutocompleteDXFieldLogicTests"`. Expected: **every** test fails with `System.NotImplementedException : The method or operation is not implemented.` and none passes. Any other failure (a compile error or a wrong assertion setup) must be fixed in the test before continuing. The full suite keeps the baseline pass count.
- [ ] **Step 4.** In the T3 section of `task-log.md`: status `Red — tests written`, the list of test names, the Red output excerpt. Commit:

```
test(acf): AutocompleteDXFieldLogic Level-A tests (Red) + IMenuOptionSource contract
```

Push. Stop.

#### Wave 1b: Builder (same branch, fresh subagent)

- [ ] **Step 5.** Read the committed tests and `design.md` § 4. Replace each `NotImplementedException` body with the rule from § 4, verbatim:
  - `Mode`: `isReadOnly ? ReadOnly : (isLocked && !string.IsNullOrWhiteSpace(text) ? Locked : Editable)`
  - `BottomSupportingText`: `hasError ? "" : (supportingText ?? "")`
  - `ShowClearIcon`: `mode != ReadOnly && !string.IsNullOrEmpty(text)`
  - `ShouldRequestOptions`: `source != null && isSearchEnabled && mode == Editable && (query?.Trim().Length ?? 0) >= SearchConstants.MinimumLocalQueryLength`
  - `RoutePick`: `selectedItem is AutocompleteSuggestion ? Select : None`
  - `LockAfterPick`: `route == Select`
  - `ShouldForwardBlur`: `selectedItem is null && mode == Editable`

  Add XML docs citing the REQ IDs. **Do not edit the test file.** If a test looks wrong: `blocked: spec gap`, stop.
- [ ] **Step 6: Confirm Green.** The same filter → all pass. Full suite → `Failed: 0`, pass count = baseline + T2's tests (if merged) + T3's.
- [ ] **Step 7: Build** → 0 errors. Exit checklist. The AC matrix (AC | criterion | `AutocompleteDXFieldLogic.<fn>` | test) goes in the T3 section. Commit:

```
feat(acf): AutocompleteDXFieldLogic pure rules (Green) — REQ-ACF-01..13 logic
```

**Merge risk / rollback:** Low. The files are new with no consumer yet, and `internal`. Rollback: `git revert -m 1 <merge-sha>`. Risk to watch: the new test file in `Unit/Components/` must not collide with the six frozen tests excluded in `MyVocaList.Tests.csproj:38-39` (a different file name, so it should not).

### Task T3-C: `TextToRestore` (**only if Helder accepted mechanism C**) [Wave 3]

**Model:** Sonnet (one agent, one test at a time). **Level:** A. **Review lane:** Standard. **Estimate:** 30 min. **Skipped entirely under B.** The tests are `[Fact]`s only: `FieldMode` is internal, so it must never be a test-method parameter (see the T3 accessibility rule). In that case the orchestrator marks it in `tasks.md` as `[CANCELLED: T1 verdict B — TextToRestore not needed]`.

**Files:** Modify `AutocompleteDXFieldLogic.cs` and `AutocompleteDXFieldLogicTests.cs`.

**Interfaces — Produces:** `internal static string? TextToRestore(AutoCompleteEditTextChangeReason reason, FieldMode mode, string lockedText)`. Confirm the exact DX enum type name with Context7 (DevExpress MAUI 25.2; its values are `UserInput`, `ProgrammaticChange`, `ItemSelected`) before writing the test.

- [ ] **Steps.** Do one test at a time: write → run (Red: `CS0117 … does not contain a definition for 'TextToRestore'` for the first test, then an assertion failure for the later ones) → implement `reason == UserInput && mode == Locked ? lockedText : null` → Green.

| Test | `// [AC]` |
|------|-----------|
| `TextToRestore_UserInputWhileLocked_ReturnsLockedText` | REQ-ACF-01 |
| `TextToRestore_UserInputWhileEditable_ReturnsNull` | REQ-ACF-19 |
| `TextToRestore_ProgrammaticChangeWhileLocked_ReturnsNull` | REQ-ACF-24 |
| `TextToRestore_ItemSelectedWhileLocked_ReturnsNull` | REQ-ACF-05 |
| `TextToRestore_ReadOnly_ReturnsNull` | REQ-ACF-12b |

- [ ] Full suite green, build 0 errors, exit checklist. Commit: `feat(acf): TextToRestore for soft-lock mechanism C (REQ-ACF-01/19/24)`.

**Merge risk / rollback:** Low (an additive function). `git revert -m 1 <merge-sha>`.

---

## Phase 3: Component

### Task T4: `AutocompleteDXField` component (XAML + code-behind) [Wave 4, after the verdict]

> **Spec updated 2026-11-03 (decompiled-DLL findings, DevExpress.Maui.Editors 25.2.4, T4 checkpoint).**
> These override the Step 5 snippet:
> (1) the "no request" branch must still assign a delegate that returns `null`, because DX invokes
> a null `Request` and throws an NRE in an async-void runner;
> (2) `RequestAsync` is a `Func<Task<IEnumerable>>`, not generic; catch `OperationCanceledException`
> inside it and return `null` (allowed try-catch pattern 1);
> (3) `ItemsRequested` runs on a timer thread, so UI property sets go through `Dispatcher.Dispatch`;
> (4) `ClearIconClicked` fires before DX clears the text, so dispatch `ClearCommand` to keep the
> design § 3.1 order;
> (5) `SelectionChanged` fires only from the dropdown, after `SelectedItem` is set; capturing the
> option before the lock is correct.
> Context7 was unavailable this session; the members were verified by ilspy decompilation.

**Model:** **Opus.** The DX event wiring, the mechanism realisation, the BindableProperty change ordering and Review Focus 1–5 all need design judgement. **Level:** C (no mandatory test; record the no-test decision in the task-log). **Review lane:** Elevated (new shared component; MD3 review of the icon tokens).

**Files:**
- Create: `MyVocaList/UI/Components/TextFields/AutocompleteDXField.xaml`
- Create: `MyVocaList/UI/Components/TextFields/AutocompleteDXField.xaml.cs`

**Interfaces:**
- **Consumes:** `SearchConstants.AutocompleteDebounceMilliseconds`, `SearchConstants.MinimumLocalQueryLength` (T2); `IMenuOptionSource`, `AutocompleteDXFieldLogic`, `FieldMode`, `PickRoute` (T3); `TextToRestore` (T3-C, C only); the T1 verdict and findings (`findings.md`: mechanism, B3 icon finding, `RequestDelay` type).
- **Produces (exact; T6 and the song-form item bind to these):** `design.md` § 3.1, verbatim:

| BindableProperty | Type | Default binding mode | Default |
|------------------|------|----------------------|---------|
| `LabelText`, `PlaceholderText` | `string` | OneWay | `""` |
| `Text` | `string` | **TwoWay** | `""` |
| `IsLocked` | `bool` | **TwoWay** | `false` |
| `IsReadOnly` | `bool` | OneWay | `false` |
| `SupportingText` | `string` | OneWay | `""` |
| `IsSearchEnabled` | `bool` | OneWay | `true` |
| `HasError` / `ErrorText` | `bool` / `string` | OneWay | `false` / `""` |
| `IsDropDownOpen` | `bool` | **TwoWay** | `false` |
| `OptionSource` | `IMenuOptionSource` | OneWay | `null` |
| `SelectCommand` | `ICommand` (param `AutocompleteSuggestion`) | OneWay | `null` |
| `ClearCommand` | `ICommand` | OneWay | `null` |
| `BlurWithoutPickCommand` | `ICommand` | OneWay | `null` |

Public method: `public bool FocusEditor()`, which focuses the inner editor.

Property pattern (the convention of `UI/Components/AppBars/SearchBar.xaml.cs:17-35`):

```csharp
public static readonly BindableProperty IsLockedProperty = BindableProperty.Create(
    nameof(IsLocked), typeof(bool), typeof(AutocompleteDXField), false,
    BindingMode.TwoWay, propertyChanged: (b, _, _) => ((AutocompleteDXField)b).ApplyMode());
public bool IsLocked { get => (bool)GetValue(IsLockedProperty); set => SetValue(IsLockedProperty, value); }
```

**Steps (incremental: one file → build → next):**

- [ ] **Step 1: Context gate.** Read the spec, `findings.md` (the verdict), and `myvocalist-coding` → `devexpress-patterns.md`, `m3-components.md`, `theme-locale.md`. Use `maui-current-apis`. Look up with Context7 (DX MAUI 25.2): the `AutoCompleteEdit` events `ItemsRequested` (args, `CancellationToken`, how results are assigned), `SelectionChanged`, `Unfocused`, `ClearIconClicked`, `EndIconClicked`, `TextChanged`, and the `RequestDelay` type. Also run `devexpress-maui` MCP `search_maui_demo_app_code` for `AutoCompleteEdit` (an empty result is acceptable and must be recorded as "returned empty").
- [ ] **Step 2: XAML shell.** Create `AutocompleteDXField.xaml` (`ContentView`, `x:Name="self"`) hosting one `dxe:AutoCompleteEdit x:Name="editor"`:
  - `LabelText`, `PlaceholderText`, `Text` (TwoWay), `HasError`, `ErrorText`, `IsDropDownOpen` (TwoWay) bound with `Source={x:Reference self}`;
  - `SubmitIconVisibility="Never"`;
  - `ClearIconColor="{StaticResource OnSurfaceVariant}"`, and, under B, `EndIcon="close_outlined"` with `EndIconColor="{StaticResource OnSurfaceVariant}"`, plus a `DataTrigger` on `HasError` → both `{StaticResource Error}`;
  - `AsyncItemsSourceProvider` with `CharacterCountThreshold="{x:Static constants:SearchConstants.MinimumLocalQueryLength}"`. If `RequestDelay` is an `int`, use `RequestDelay="{x:Static constants:SearchConstants.AutocompleteDebounceMilliseconds}"`. If it is a `TimeSpan`, set it in the constructor with `TimeSpan.FromMilliseconds(SearchConstants.AutocompleteDebounceMilliseconds)`;
  - an `ItemTemplate` with Headline + optional SupportingText, copied from `PersonFormPage.xaml:32-42`.
  
  No event attributes yet. Create `AutocompleteDXField.xaml.cs` with only the constructor (`InitializeComponent()`). **Build** → 0 errors.
- [ ] **Step 3: BindableProperties.** Add all 13 properties from the table. `Text`, `IsLocked` and `IsReadOnly` → `ApplyMode()`; `HasError` and `SupportingText` → `ApplySupportingText()` (Review Focus 4, 5). **Build.**
- [ ] **Step 4: Mode application.** Add `private FieldMode CurrentMode => AutocompleteDXFieldLogic.Mode(IsReadOnly, IsLocked, Text);`. `ApplyMode()` reads `var mode = CurrentMode;` and applies it:
  - **ReadOnly:** `editor.IsReadOnly = true`, `ClearIconVisibility = Never`, `IsEndIconVisible = false`, `IsDropDownOpen = false`.
  - **Locked, under B:** `editor.IsReadOnly = true`, `ClearIconVisibility = Never`, `IsEndIconVisible = true`, `IsDropDownOpen = false`, `editor.SelectedItem = null`.
  - **Locked, under C:** `editor.IsReadOnly = false`, built-in clear icon, `IsDropDownOpen = false`, `editor.SelectedItem = null`, and remember `_lockedText = Text`.
  - **Editable:** `editor.IsReadOnly = false`, `IsEndIconVisible = false`, `ClearIconVisibility = Auto`.
  
  If the T1 B3 finding was "the icons differ", use the End icon as X in the Editable state too (design § 5.1 note) and keep `ClearIconVisibility = Never` throughout. `ApplySupportingText()`: `editor.HelpText = AutocompleteDXFieldLogic.BottomSupportingText(HasError, SupportingText)`. **Build.**
- [ ] **Step 5: Event adapters.** Wire them in XAML and the code-behind:
  - `ItemsRequested` (a synchronous handler; the pattern matches today's glue at `PersonFormPage.xaml.cs:37-47`):
    ```csharp
    private void OnItemsRequested(object sender, ItemsRequestEventArgs e)
    {
        var text = e.Text;                    // the request's own text, not editor.Text
        var token = e.CancellationToken;
        var source = OptionSource;
        if (!AutocompleteDXFieldLogic.ShouldRequestOptions(CurrentMode, IsSearchEnabled, source, text))
        { IsDropDownOpen = false; return; }   // no RequestAsync assigned → no request
        e.RequestAsync = async () =>
        {
            var options = await source.SearchAsync(text, token);
            token.ThrowIfCancellationRequested();          // inside DX's awaited delegate, never async void
            // Review Focus 1: the field may have locked while the search ran
            return AutocompleteDXFieldLogic.ShouldRequestOptions(CurrentMode, IsSearchEnabled, source, text)
                ? options : [];
        };
    }
    ```
    Hand DX exactly `options`. Never read a VM collection (REQ-ACF-22, -26). **No `async void` handler and no throwing outside `e.RequestAsync`.** A cancellation throw is legal only inside the delegate that DX awaits. Check the return type DX expects from `RequestAsync` with Context7 and adapt the `[]`/`options` expression to it.
  - `SelectionChanged`: **capture the option first**: `var option = editor.SelectedItem; var route = RoutePick(option);`. `None` → return (Review Focus 2). `Select` → then `IsLocked = LockAfterPick(route)`. This triggers `ApplyMode()`, which nulls `SelectedItem`, which is why the capture must come before it. Then `SelectCommand` (`CanExecute`/`Execute` with the **captured** `option`) → `editor.SelectedItem = null` (idempotent) → `IsDropDownOpen = false` (REQ-ACF-01, BUG-061).
  - `Unfocused`: if `ShouldForwardBlur(editor.SelectedItem, Mode)` → `BlurWithoutPickCommand`. **Never write `Text` here** (REQ-ACF-19).
  - X while Locked (`EndIconClicked` under B, `ClearIconClicked` under C): `Text = ""` → `IsLocked = false` → `ApplyMode()` → `ClearCommand` → `FocusEditor()`. The order matters (Review Focus 3; REQ-ACF-06).
  - X while Editable (`ClearIconClicked`, which DX clears itself): `ClearCommand` → focus stays (REQ-ACF-09).
  - **C only:** `TextChanged` → `TextToRestore(e.Reason, Mode, _lockedText)`; if non-null, write it back. `Focused` while Locked → `editor.Unfocus()`.
  - `FocusEditor()` → `editor.Focus()`.
  
  **Build.**
- [ ] **Step 6: XML docs.** The class doc names the MD3 *exposed dropdown menu* pattern and the MD3 exception (`design.md` § 2). Each property doc cites its REQ.
- [ ] **Step 7: Self-review against the Review Focus list** (1–5) and INV-1…INV-5 (`design.md` § 7). Record each as a line in the T4 section of `task-log.md`: invariant → where it is enforced (method name).
- [ ] **Step 8.** Full suite: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` → `Failed: 0` (no new tests; record the Level-C no-test decision). Build → 0 errors. The component is on no page yet, so the E2E status is `Check build`. Exit checklist. Commit:

```
feat(acf): AutocompleteDXField shared component — mechanism <B|C>, pick-lock via X, MD3 trailing-icon tokens
```

**Merge risk / rollback:** Medium. The component is new with no consumer, so the merge cannot break a page. The risk is latent: wrong DX wiring only shows up in T6/T7. Rollback: `git revert -m 1 <merge-sha>` (T6 must be reverted first if it is already merged).

---

## Phase 4: ViewModel

### Task T5: `PersonFormViewModel.SearchPersonsCoreAsync` + `NameOptionSource` [Wave 2, mechanism-independent, after T3 merged]

**Model:** Sonnet (single agent, one test at a time). **Level:** A. **Review lane:** Standard.

**Files:**
- Modify: `MyVocaList/UI/ViewModels/PersonFormViewModel.cs` (`SearchPersonsAsync` at `:280-293`)
- Modify: `MyVocaList.Tests/Unit/ViewModels/PersonFormViewModelTests.cs` (append a `// ── NameOptionSource ──` region; existing tests are **not edited**)

**Interfaces:**
- **Consumes:** `IMenuOptionSource`, `MenuOptionSource` (T3).
- **Produces:**

```csharp
internal async Task<IReadOnlyList<AutocompleteSuggestion>> SearchPersonsCoreAsync(string term, CancellationToken ct = default);
public IMenuOptionSource NameOptionSource { get; }   // = new MenuOptionSource((q, ct) => SearchPersonsCoreAsync(q, ct)), created in the constructor
```

Access: `internal` or `private` for the core method (the tests go through `NameOptionSource`). The design names only the method, so choose `private`, and write the choice back as an impl note if it differs.

**Service call (per the `design.md` § 3.2 note the orchestrator adds at Step 0; the implementor does not edit spec files):**
- Call `SearchPersonsStartsWithAsync(trimmed, cancellationToken: ct)`, where `var trimmed = term.Trim();`. The service receives the **trimmed** term (REQ-ACF-13). The real parameter name is `cancellationToken` (`Domain/ServicesInterfaces/IPersonService.cs:24`), not `ct`.
- `maxResults` is **not** passed. It defaults to `SearchConstants.MaxSearchResults` (= 5), exactly as today's call `SearchPersonsStartsWithAsync(term)` (`PersonFormViewModel.cs:288`). An expression-tree Moq setup must still name it explicitly, so the tests use `SearchConstants.MaxSearchResults`. The existing test `SearchPersonsCommand_ValidTerm_MapsToDtos` sets up `("jo", 5, default)`. It still matches: "jo" trims to "jo", and `SearchPersonsAsync` passes the default token.

- [ ] **Step 1: Write the failing test (REQ-ACF-16).**

```csharp
[Fact]
// [AC] REQ-ACF-16: the Person option source returns the search's own result list and passes the request token through; it never reads Suggestions.
public async Task NameOptionSource_SearchAsync_ReturnsMappedServiceResults_AndPassesToken()
{
    using var cts = new CancellationTokenSource();
    var person = new Person("John Doe");
    _serviceMock.Setup(s => s.SearchPersonsStartsWithAsync("jo", SearchConstants.MaxSearchResults, cts.Token))
                .ReturnsAsync(new List<Person> { person });
    var sut = CreateSut();

    var result = await sut.NameOptionSource.SearchAsync("jo", cts.Token);

    var option = Assert.Single(result);
    Assert.Equal("John Doe", option.Headline);
    Assert.Same(person, option.Data);
    Assert.Empty(sut.Suggestions);   // BUG-056: the observable is neither the source nor written
    _serviceMock.Verify(s => s.SearchPersonsStartsWithAsync("jo", SearchConstants.MaxSearchResults, cts.Token), Times.Once);
}
```

- [ ] **Step 2: Run it, confirm Red.** `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~PersonFormViewModelTests.NameOptionSource"`. Expected: a build failure, `CS1061: 'PersonFormViewModel' does not contain a definition for 'NameOptionSource'`.
- [ ] **Step 3: Implement.** Add `SearchPersonsCoreAsync` (the body of today's method, returning the mapped list, with the token passed through) and `NameOptionSource`. Change `SearchPersonsAsync` to `Suggestions = [.. await SearchPersonsCoreAsync(term)];`. Keep `Suggestions`' existing type, so use `.ToList()` if it is a `List<>`. Run → Green.
- [ ] **Step 4: Write the second failing test (REQ-ACF-13).**

```csharp
[Theory]
[InlineData("a")]
[InlineData(" a")]
[InlineData("a ")]
// [AC] REQ-ACF-13: below the trimmed minimum query length the option source returns nothing and never calls the service.
public async Task NameOptionSource_SearchAsync_BelowTrimmedThreshold_ReturnsEmptyAndSkipsService(string term)
{
    var sut = CreateSut();

    var result = await sut.NameOptionSource.SearchAsync(term, CancellationToken.None);

    Assert.Empty(result);
    _serviceMock.Verify(s => s.SearchPersonsStartsWithAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
}
```

- [ ] **Step 5: Run it, confirm Red** for `" a"` and `"a "`. Today's check uses the raw `term.Length`, so the service is called: the expected failure is `Moq.MockException : Expected invocation on the mock should never have been performed, but was 1 times`. `"a"` may already pass; that is expected, and the two padded cases carry the Red.
- [ ] **Step 6: Implement the trimmed check.** `var trimmed = term?.Trim() ?? ""; if (trimmed.Length < SearchConstants.MinimumLocalQueryLength) return [];`, and pass `trimmed` to the service. Run → Green.
- [ ] **Step 7: Regression.** The full suite → `Failed: 0`. Specifically, `SearchPersonsCommand_ShortTerm_SetsEmptySuggestions`, `SearchPersonsCommand_ValidTerm_MapsToDtos` (its setup passes `default` as the token, which still matches) and every `PersonFormViewModelBug044Tests` test pass **unchanged** (REQ-ACF-20/21/28). If one of them fails, do not edit it: `blocked: spec gap`.
- [ ] **Step 8.** Build → 0 errors. The E2E gate is not applicable (no UI change yet; the VM change is not bound until T6), so the status is `To Review`. Exit checklist, with the AC matrix rows REQ-ACF-13 and -16. Commit:

```
feat(person-form): NameOptionSource + trimmed SearchPersonsCoreAsync with token pass-through (REQ-ACF-13/16)
```

**Merge risk / rollback:** Low. The existing command is kept; the only behaviour change is that padded input like "a " no longer searches. Rollback: `git revert -m 1 <merge-sha>` (revert T6 first if it is merged).

---

## Phase 5: Page migration

### Task T6: Migrate `PersonFormPage` Full Name onto `AutocompleteDXField` [Wave 5, after T4 + T5 merged]

**Model:** Sonnet. **Level:** C (page swap; covered by existing VM tests + T7). **Review lane:** Standard.

**Files:**
- Modify: `MyVocaList/UI/Pages/People/PersonFormPage.xaml` (replace `:19-43`)
- Modify: `MyVocaList/UI/Pages/People/PersonFormPage.xaml.cs` (delete `:33-61`, change `:30`)

**Interfaces — Consumes:** the T4 surface (§ T4 table), `FocusEditor()`; the T5 `NameOptionSource`.

- [ ] **Step 1: Edit the XAML only.** Replace `PersonFormPage.xaml:19-43` with:

```xml
<textfields:AutocompleteDXField x:Name="nameField"
    LabelText="Full Name"
    PlaceholderText="First and last name"
    Text="{Binding PersonName, Mode=TwoWay}"
    HasError="{Binding NameHasError}"
    ErrorText="{Binding NameErrorText}"
    OptionSource="{Binding NameOptionSource}"
    SelectCommand="{Binding SuggestionSelectedCommand}"
    BlurWithoutPickCommand="{Binding ValidateNameCommand}" />
```

Add `xmlns:textfields="clr-namespace:MyVocaList.UI.Components.TextFields"`. `IsLocked` stays unbound. Keep any character-counter element that sits outside `:19-43` untouched (REQ-ACF-28).
- [ ] **Step 2: Build.** Expected: the **only** errors are in `PersonFormPage.xaml.cs` (`nameField.SelectedItem` and the handler signatures, because `ContentView` has no `SelectedItem`). Any other error must be fixed before Step 3 (3-attempt cap).
- [ ] **Step 3: Edit the code-behind.** Delete `OnNameItemsRequested`, `OnNameSelectionChanged` and `OnNameUnfocused` (`:33-61`). Change `:30` to `nameField.FocusEditor();`. Keep the birthday and email blur handlers.
- [ ] **Step 4: Build → 0 errors; full `dotnet test` → `Failed: 0`** (the count is unchanged from after the T5 merge).
- [ ] **Step 5: REQ-ACF-11 checklist, as a Python substring scan (not grep):**

```bash
python -c "import pathlib,sys; x=pathlib.Path('MyVocaList/UI/Pages/People/PersonFormPage.xaml').read_text(encoding='utf-8'); cs=pathlib.Path('MyVocaList/UI/Pages/People/PersonFormPage.xaml.cs').read_text(encoding='utf-8'); bad=[t for t in ['OnNameItemsRequested','OnNameSelectionChanged','OnNameUnfocused','SelectedItem'] if t in x or t in cs]+[t for t in ['RequestDelay','CharacterCountThreshold','AutoCompleteEdit'] if t in x]; print('FAIL',bad) if bad else print('PASS'); sys.exit(1 if bad else 0)"
```

Expected: `PASS`. Paste the output into the task-log.
- [ ] **Step 6: E2E emulator gate** (`implementor.md`): `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -t:Run`. New Singer: type "jo" → matching people → pick one → that person's edit form replaces the page. If the emulator is unavailable: status `Check build` with the note.
- [ ] **Step 7:** Exit checklist. Commit:

```
feat(person-form): Full Name on AutocompleteDXField — page glue removed (REQ-ACF-11, -15..18)
```

**Merge risk / rollback:** Medium. This is the first real consumer, so a DX wiring defect in T4 surfaces here (no options, no navigation, focus lost). Rollback: `git revert -m 1 <merge-sha>` restores the old inline `AutoCompleteEdit` and glue. T4 and T5 stay merged and harmless.

---

## Phase 6: Device gate

### Task T7: On-device E2E gate (Helder, Android, Person form), HARD STOP

> **HARD STOP.** The item does not move to ✅ until Helder has completed lines 1–10 on device and given his verdict. The orchestrator records his pass/fail per line in the T7 section of `task-log.md`, then sets the status per his verdict (`backlog_gen.py status … "✅"` or back to 🟡 with the failing lines registered as bugs via `backlog_gen.py register --kind bug`).

**T7-prep (Sonnet implementor, ≤ 15 min):** create a throwaway worktree `.worktrees/acf-t7-prep` on `spike/acf-t7-supporting-text` from `develop`. Add `SupportingText="Test"` to the `AutocompleteDXField` in `PersonFormPage.xaml`, then build → 0 errors. Commit locally only, never push, never merge. It is deleted after T7.

**Deploy:** lines 1–9 from `develop` (main working tree); line 10 from `.worktrees/acf-t7-prep`. `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android -t:Run` with the device connected.

**Checklist (verbatim from `tasks.md` T7; Helder records pass/fail):**

1. New Singer: the Full Name field has focus on open (REQ-ACF-18).
2. Typing 1 char, or a space plus 1 char → no menu; 2 chars → up to 5 people after about 300 ms (REQ-ACF-13, -14, -15).
3. Pick a person → that person's edit form replaces the page (no stacked form), with the name editable (REQ-ACF-03, -15, -20); no menu opens on the edit form (REQ-ACF-25).
4. *Informational only:* just before navigation completes, the picked name cannot be edited and no menu reopens (REQ-ACF-01, -04).
5. X is visible whenever there is text, looks enabled, and there is no magnifier icon (REQ-ACF-07, -08, -10).
6. Type a name, tap X → empty, focus kept, keyboard up (REQ-ACF-09).
7. Type an invalid name and blur → the existing name validation shows; the text is kept (REQ-ACF-17, -19).
8. The character counter and error clearing while typing behave as before (REQ-ACF-28).
9. No row stays highlighted after a pick; the field style matches the other Outlined fields (REQ-ACF-23, -27).
10. (T7-prep build) "Test" shows under the field; an invalid-name blur shows only the error (REQ-ACF-12c).

- [ ] Orchestrator: record the results, delete the T7-prep worktree and branch, fill the requirements traceability seed with the real test names, run the Rebuild Test (CLAUDE.md § Spec Quality Check), do the session-end spec ritual, and commit on develop: `docs(acf): T7 device gate — <PASS|FAIL lines n,m>`.

**Risk:** High. This is the only check of DX rendering on a real page. A failure → regression bug(s) registered first, then fixed on a new task branch.

---

## Model routing and estimates

| Task | Model | Estimate | Review lane | Why this model |
|------|-------|----------|-------------|----------------|
| Step 0 | Orchestrator | 30 min | — | Docs only |
| T1a spike page | Sonnet | 60 min | Architectural (via T1) | Mechanical: the procedure is fully specified in `tasks.md` T1 |
| T1b device run | Helder | ≤ 2 h (hard time-box) | — | On-device |
| T1c verdict record | Orchestrator | 20 min | — | Docs only |
| T2 constant | Haiku (Sonnet fallback) | 15 min | Standard | A trivial const + one assertion |
| T3 Tester / Builder | Sonnet / Sonnet | 45 min / 30 min | Standard | The rules are verbatim in `design.md` § 4 |
| T3-C (C only) | Sonnet | 30 min | Standard | One pure function, a 5-row table |
| T4 component | **Opus** | 2 h (see the split below) | Elevated | DX event wiring, mechanism realisation, property-change ordering, Review Focus 1–5 |
| T5 VM | Sonnet | 45 min | Standard | A small refactor with prescribed tests |
| T6 page | Sonnet | 45 min | Standard | Prescribed XAML swap + deletion |
| T7-prep | Sonnet | 15 min | — | One attribute |
| T7 device gate | Helder | 45 min | Architectural | On-device |
| Verifier (after every merge) | Sonnet | 15 min each | — | Diff vs spec checklist |

**Pre-planned T4 split (if T4 exceeds 2 h, the sizing limit in `workflow-rule-2.md`).** The implementor stops at the end of the current step, commits green, and records a checkpoint. The orchestrator then dispatches the remainder as its own task on the same branch:
- **T4a** = Steps 1–4 + 6: XAML shell, all BindableProperties, `CurrentMode`/`ApplyMode`/`ApplySupportingText`, XML docs. The build is green and there are no event adapters yet. Commit: `feat(acf): AutocompleteDXField shell + properties + mode application`.
- **T4b** = Steps 5, 7, 8: event adapters, the Review Focus / invariant self-review, and the final suite. Commit: the T4 message.

Both halves use Opus. The merge happens only after T4b.

## Self-review (writing-plans checklist)

- **Spec coverage:**
  - REQ-ACF-01…06, -05a, -11, -12a/b/c and -13 → T3 (+ T3-C) and T4.
  - -07/-08/-10 → T4 XAML, T1 B3/M, T7 line 5.
  - -14 → T2 and T4.
  - -15…18 → T5, T6, T7.
  - -19/-24 → T4 review (+ T3-C under C) and T1/T7.
  - -20/-21/-28 → existing tests, unchanged (T5 Step 7).
  - -22/-26 → T5 test + T4 Step 5.
  - -23/-25/-27 → T1 B5, T7.
  
  No AC is without an owner.
- **Placeholders:** none. The conditional choices (B vs C, `RequestDelay` type, B3 icon finding) are resolved by named T1 outputs, with a rule for each branch.
- **Type consistency:** `FieldMode {Editable, Locked, ReadOnly}`, `PickRoute {None, Select}`, `IMenuOptionSource.SearchAsync(string, CancellationToken)`, `NameOptionSource`, `SearchPersonsCoreAsync`, `FocusEditor()`, `BottomSupportingText` are used identically in T3, T3-C, T4, T5 and T6.
