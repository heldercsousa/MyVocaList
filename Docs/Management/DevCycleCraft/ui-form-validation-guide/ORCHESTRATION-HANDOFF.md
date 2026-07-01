# Form Validation — Orchestration Handoff (resume point)

> **Single source of truth to resume the "Form validation" feature.** A fresh session should read ONLY this file + the 5 BACKLOG rows (168–173) to continue — do not re-read the whole feature history. Supersedes the RESUME POINT in `Docs/Management/pure-skipping-firefly.md`.
>
> Last updated: 2026-07-01 (session 3). Progress this session: Task 04 (Songs) implementation **STARTED but NOT COMMITTED, NOT REVIEWED, NOT MERGED** — see STATUS below for exact resume point. Reason for handoff: Helder requested a stop-and-document (new session incoming); do NOT advance to Task 05 until Task 04 is finished, reviewed, and merged.

## Goal
Establish ONE form-validation standard in the project's internal guidelines, then apply it to every app form — Venue (reference) first, then the rest **in BACKLOG order: Venues → Singer(Person) → Songs → Artists.** Same pattern replicated to each; each obeys the updated guidelines.

## Orchestration rules (unchanged — carry forward)
- Autonomous; all authorizations pre-granted to orchestrator + subagents.
- Orchestrator does shell/git only, **never reads/writes `.cs`/`.xaml` source** — delegate all code work to subagents.
- Every implementation subagent runs in its **own git worktree** (harness `isolation:"worktree"`). Sequential forms — one at a time; merge to `develop` before the next.
- **Per-task review is mandatory** (fresh Opus reviewer after each form) before merge.
- Models: guide + Venue reference = **Opus** (done); Singer/Songs/Artists = **Sonnet**; all reviews = **Opus**.

---

## STATUS

### ✅ Task 01 — Form validation guide — DONE, MERGED to `develop`
- `develop` tip after merge: **`256e1fb`** (pushed to origin).
- Guideline files edited (all in `.claude/library/`): `dialogs-validation.md` (**primary — `## Form Validation Standard`**), `crud-pages.md`, `devexpress-patterns.md`, `theme-locale.md`, `ux-patterns.md`.
- Feature docs: `Docs/Management/DevCycleCraft/ui-form-validation-guide/` → `01-ui-form-validation-guide.md` (requirements, has a `<TODO>` for Integer), `plan.md`, `task-log.md`, this handoff. `.sln` folder GUID = `{FA1234BC-...-000000000033}`.
- Flow used: Opus plan → fresh-Opus review (APPROVE + 3 fixes: confirm DX blur mechanism up-front, consult UX skill, verify `.sln` GUID) → Opus implement.

### ✅ Task 02 — Venues form — DONE, MERGED to `develop`
- `develop` tip after merge: **`5c669f5`** (pushed to origin). Merge commit `5c669f5`; branch tip was `ed0d57e` (adds the test-count correction on top of `9c51d4ba`).
- Opus review verdict: **APPROVE** after one required fix — the task-log's Verification evidence claimed 8 VenueFormViewModel tests / 20 local total; actual is **7** tests / **19** local (12 VenueService + 7 VenueForm). Full suite re-run confirmed **368/368**. Code, XAML, code-behind, and the Reference Pattern were rated exemplary and safe to replicate — no source change needed.
- Files changed (6): `MyVocaList/UI/ViewModels/VenueFormViewModel.cs`, `MyVocaList/UI/Pages/Venues/VenueFormPage.xaml`, `…/VenueFormPage.xaml.cs`, `MyVocaList.Tests/Unit/ViewModels/VenueFormViewModelTests.cs` (NEW), `Docs/Management/BusinessFeatures/venues/form-validation-task-log.md` (NEW), `MyVocaList.sln`. `Services/VenueService.cs` intentionally unchanged (already returns the standard tuple).
- **Helder gate remaining:** emulator E2E of the Venue form (manual — subagents can't run the emulator).

### ✅ Task 03 — Singer(Person) form — DONE, MERGED to `develop`
- Merge commit **`11ce501`** (pushed). Branch `worktree-agent-afd1b94bef4c77862` tip `f3428e1`. First **multi-field** form (name + birthday + email).
- Opus review verdict: **APPROVE** — all six Reference-Pattern points verified per field; truthful test count (30 total in `PersonFormViewModelTests`, 18 net-new; suite 386/386); no native dialogs; birthday no-year mechanism untouched (Helder gate).
- `PersonService.ValidateNameInput/ValidateBirthday/ValidateEmail` already existed with full coverage — reused unchanged (no service edit).
- Edit-mode dirty-guard: `_isHydrating` (default true) + `CompleteHydration()` called from `PersonFormPage.OnAppearing` (Shell applies `[QueryProperty]` before `OnAppearing`); each `On<Field>Changed` early-returns while hydrating. ViewModels are Transient so back-nav gets a fresh hydration cycle.
- `SetInlineError` fragile substring routing **removed** (grep-confirmed no remaining consumers) → replaced by `ApplyAsyncFailureAsync`: the one field-attributable async failure (email uniqueness) routes to the email field; non-attributable failures ("Singer not found") go to the snackbar.
- Files changed (7): `PersonFormViewModel.cs`, `PersonFormPage.xaml(.cs)`, `PersonFormViewModelTests.cs` (NEW), `persons/form-validation-task-log.md` (NEW), `MyVocaList.sln`, `BACKLOG.md`.
- **Helder gate remaining:** emulator E2E of the Person form.

### 🟡 Task 04 — Songs form — IN PROGRESS, **NOT COMMITTED / NOT REVIEWED / NOT MERGED — RESUME HERE**

- **Single-writer pre-check already done and PASSED (2026-07-01, session 3):** confirmed BUG-020 `SongFormViewModel` fix (`3b2cb75`) is present in `origin/develop`; `develop` working tree was clean (`rtk proxy git status --porcelain` empty; `rtk proxy git diff --stat -- '*SongForm*'` empty) before dispatch. Safe to resume without re-checking, unless a new parallel-session commit has landed on `SongFormViewModel.cs`/`SongService.cs` since — re-verify if in doubt.
- **`.sln` GUID check done:** `artists-songs` SolutionFolder already exists (GUID `{C141C5C9-833C-4A26-...}`, ~line 161 of `MyVocaList.sln`). The new `form-validation-task-log.md` just needs a line added to its existing `ProjectSection(SolutionItems)` — **no new GUID needed** (next free GUID in the `FA1234BC-0001-4000-8000-...` sequence would be `...035` if one is ever needed, but it isn't for this task).
- **Subagent dispatched:** ONE Sonnet subagent, `isolation:"worktree"`, based off `origin/develop`. Agent ID `aa495a45f830848ae`.
  - **Worktree path:** `C:\Users\helde\source\repos\MyVocaList\.claude\worktrees\agent-aa495a45f830848ae`
  - **Branch:** `worktree-agent-aa495a45f830848ae`
  - **Branch HEAD as of this handoff:** `ad042dd` (== `develop` tip at dispatch time) — **the agent's Songs-form work is NOT YET COMMITTED to this branch.**
  - **Uncommitted working-tree changes present in that worktree** (confirmed via `git status --porcelain` in the worktree, session 3):
    ```
    M Domain/ServicesInterfaces/ISongService.cs
    M MyVocaList.Tests/Unit/Services/SongServiceTests.cs
    M MyVocaList.Tests/Unit/ViewModels/SongFormViewModelTests.cs
    M MyVocaList/UI/Pages/Songs/SongFormPage.xaml
    M MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs
    M MyVocaList/UI/ViewModels/SongFormViewModel.cs
    M Services/SongService.cs
    ```
  - The agent had reported (mid-task, before first stop) that it was "waiting for the build to finish." It was resumed once (session 3) with instructions to finish the full exit checklist (build/test confirmation, post-edit re-read, task-log at `Docs/Management/BusinessFeatures/artists-songs/form-validation-task-log.md`, `.sln` registration, commit, push). **That resume may still be running in the background, or it may have stopped again without completing — status unknown as of this handoff.** No commit, no task-log file, and no `.sln` edit had landed as of the last check.
- **NEXT ACTION for the fresh session:**
  1. First check whether agent `aa495a45f830848ae` is still alive / has produced new output (or just re-check the worktree directly: `git -C .claude/worktrees/agent-aa495a45f830848ae status --porcelain` and `git -C .claude/worktrees/agent-aa495a45f830848ae log --oneline -3`).
  2. If the working tree in that worktree still shows the 7 modified files above and no new commit: **do not start a new subagent from scratch** — either resume agent `aa495a45f830848ae` (if the harness still has it addressable) or dispatch a fresh subagent instructed to `cd` into the **existing worktree** (do not create a new one) and finish the exit checklist against the existing uncommitted changes — inspect the diff first to confirm it actually followed the Reference Pattern before treating it as reusable.
  3. Once committed and pushed: dispatch a fresh **Opus** reviewer against the branch (see Orchestration rules). Only on `VERDICT: APPROVE`, merge to `develop`, push, update this handoff + BACKLOG row 174.
  4. **Do NOT start Task 05 (Artists) until Task 04 is merged.**

---

## REFERENCE PATTERN (Venue) — 03–05 copy this per field
1. **Service owns validation:** `(bool isValid, string message) Validate<Field>Input(...)` — no rules in the VM.
2. **VM per field:** `_<field>Dirty` flag; `[RelayCommand] Validate<Field>()` returns early if not dirty else validates; `On<Field>Changed` sets dirty and, if `!<Field>HasError` returns, else re-validates to clear/keep; shared `Apply<Field>Validation` maps tuple → `<Field>HasError`/`<Field>ErrorText`. Save re-runs all validators (safety net for uniqueness/DB).
3. **XAML:** `dxe:TextEdit`/`dxe:DateEdit` with `HasError`/`ErrorText` bindings + `Unfocused="On<Field>Unfocused"` — inline only, **no dialog/summary/snackbar/wall-of-red**.
4. **Code-behind:** `private VM ViewModel => (VM)BindingContext;` and `On<Field>Unfocused(...) => ViewModel.Validate<Field>Command.Execute(null);`
5. Multi-field: repeat the block once per field (own dirty flag / command / HasError / ErrorText). Don't route one message to a field by substring.
6. **Edit-mode pre-population must NOT mark a field dirty** (Opus review note, Task 02). `[QueryProperty]`/edit-mode assignment fires `On<Field>Changed`, which would set `_<field>Dirty = true` before the user touches the field. Benign for Venue (an existing name is valid) but the multi-field forms (Person/Songs/Artists) pre-fill fields that *could* be invalid → premature error flash on first blur. Guard pre-population so it does not set the dirty flag.
   - **Proven implementation (Task 03 Person):** `private bool _isHydrating = true;` + `public void CompleteHydration() => _isHydrating = false;` called from `<Form>Page.OnAppearing` (Shell applies `[QueryProperty]` values before `OnAppearing`). Each `On<Field>Changed` does `if (_isHydrating) return;` before marking dirty. Never re-arm `_isHydrating` to true — ViewModels are Transient, so back-nav yields a fresh instance. **Copy this verbatim for Songs/Artists.**
7. **Async-failure routing (replaces removed `SetInlineError`)** (Task 03). Do NOT route service failure messages across fields by substring. Route the one known field-attributable async failure (e.g. uniqueness) to that field's HasError/ErrorText; send any non-field-attributable failure to the **snackbar**, not a guessed field. (Task 03 used `ApplyAsyncFailureAsync` with a single narrow `Contains` for the uniqueness message.)
8. **Testability improvement to propagate (Task 03):** in `SaveAsync`, use `Shell.Current?.GoToAsync("..") ?? Task.CompletedTask` (null-safe) so the success-path can be unit-tested (verifies the service call) — better than the Venue reference which cannot.

## Helder gates (do NOT block form work; log them)
- **DateEdit day/month-only birthday (Person)** — OPEN: DateEdit has no masked no-year entry; two candidate paths documented in `dialogs-validation.md`. Needs emulator decision.
- **Integer / R10** — the requirements doc `01-ui-form-validation-guide.md` Integer section is `<TODO>`; shipped as a spec-incomplete stub. Needs Helder to complete the spec.
- **Emulator E2E** of each form is a manual Helder gate (subagents can't run the emulator).

## Gotchas learned (carry forward — save the next session time)
- **Worktrees branch off `origin/develop`, which can LAG local `develop`.** Every subagent's STEP 0 must: verify `.claude/library/dialogs-validation.md` has `## Form Validation Standard`; if missing → `git fetch origin && git reset --hard origin/develop`, re-verify. (Venue subagent hit this and self-corrected.)
- **`.sln` GUID collisions:** the `FA1234BC-...` sequence in `constraints-registry.md` says "0014" but is STALE — highest actually used is `...033` (this guide's folder). **Next free = `...034`.** Always grep the real max before adding a folder.
- **RTK proxy compresses git output** (e.g. `git status` → "ok"). Use `rtk proxy git <cmd>` for raw output when the real result matters.
- **A parallel session works directly on `develop`** (it committed BUG-020 `SongFormViewModel` fix `3b2cb75`). **Task 04 (Songs) edits `SongFormViewModel` — check for uncommitted SongForm changes on develop before dispatching, to avoid a single-writer collision.**
- **Merging divergent worktree branches:** when a branch base has diverged, prefer surgical integration (`git checkout <branch> -- <disjoint files>` + re-apply `.sln` additions on current develop) over a full `git merge` that would conflict on `.sln`/hotspots. (Used for Task 01.)

## Branches / worktrees ledger
| Purpose | Branch | Commit | State | Cleanup |
|--------|--------|--------|-------|---------|
| 1A draft plan | `worktree-agent-a284a1b6d18225d42` | `6a525e4` | superseded (plan finalized in 01 merge) | safe to delete |
| 1C void (limit-killed) | `worktree-agent-a9d55a7c4aac000f1` | `efcc492` | void, no useful work | delete |
| 1C guideline impl | `worktree-agent-aaae95a5d8a6c56ec` | `c1814652` | content merged to develop `256e1fb` | safe to delete |
| Venue form | `worktree-agent-a26c6ffb2f045a11d` | `ed0d57e` | merged to develop `5c669f5` | safe to delete |
| Person form | `worktree-agent-afd1b94bef4c77862` | `f3428e1` | merged to develop `11ce501` | safe to delete |
| **Songs form (Task 04, IN PROGRESS)** | `worktree-agent-aa495a45f830848ae` | branch HEAD `ad042dd` (no new commit yet) | **uncommitted changes present — do NOT delete.** 7 modified files (see STATUS above), not committed, not pushed, not reviewed | **KEEP — resume from here** |

## Efficient continuation prompt (paste into the fresh session)
> Resume the Form validation orchestration. Read ONLY `Docs/Management/DevCycleCraft/ui-form-validation-guide/ORCHESTRATION-HANDOFF.md` and BACKLOG row 174 — do not re-read feature history. You are the orchestrator (shell/git only; never read source; delegate all code to subagents). **Tasks 02 (Venue) and 03 (Person) are DONE + merged. Task 04 (Songs) is IN PROGRESS, NOT COMMITTED — do NOT dispatch a fresh subagent from scratch.** First inspect the existing worktree at `.claude/worktrees/agent-aa495a45f830848ae` (branch `worktree-agent-aa495a45f830848ae`) — it has 7 uncommitted modified files (listed in the STATUS section above) implementing Songs-form validation. Check whether agent `aa495a45f830848ae` is still resumable; if not, dispatch a subagent that reuses that same worktree (do not create a new one) to review the existing diff against the Reference Pattern, finish the exit checklist (build/test, task-log, `.sln` registration, commit, push). Only then dispatch a fresh **Opus** reviewer, and only on `VERDICT: APPROVE` merge to `develop`. Do NOT start Task 05 (Artists) until Task 04 is merged. Update BACKLOG row 174 and this handoff after the merge. Respect the Helder gates and gotchas. Be autonomous; all authorizations granted.
