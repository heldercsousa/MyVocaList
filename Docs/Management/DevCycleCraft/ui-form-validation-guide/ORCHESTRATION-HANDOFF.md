# Form Validation — Orchestration Handoff (resume point)

> **Single source of truth to resume the "Form validation" feature.** A fresh session should read ONLY this file + the 5 BACKLOG rows (168–173) to continue — do not re-read the whole feature history. Supersedes the RESUME POINT in `Docs/Management/pure-skipping-firefly.md`.
>
> Last updated: 2026-07-01. Reason for handoff: orchestrator context grew to ~235k and account usage limits were hit repeatedly; clearing to a fresh session.

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

### 💡 Tasks 03–05 — Singer(Person) → Songs → Artists — PENDING
- Each: Sonnet subagent in a worktree off `origin/develop` (must contain the Venue merge first). Copy the **Reference Pattern** below. TDD, per-task Opus review, merge, then next.
- **Person/Singer form files** (task 03): `MyVocaList/UI/Pages/People/PersonFormPage.xaml(.cs)`, `MyVocaList/UI/ViewModels/PersonFormViewModel.cs`, `Services/PersonService.cs` (`ValidateNameInput`, `ValidateBirthday`, `ValidateEmail`). Multi-field (name + birthday date + email). **Remove the fragile `SetInlineError` substring routing** — use per-field HasError/ErrorText.

---

## REFERENCE PATTERN (Venue) — 03–05 copy this per field
1. **Service owns validation:** `(bool isValid, string message) Validate<Field>Input(...)` — no rules in the VM.
2. **VM per field:** `_<field>Dirty` flag; `[RelayCommand] Validate<Field>()` returns early if not dirty else validates; `On<Field>Changed` sets dirty and, if `!<Field>HasError` returns, else re-validates to clear/keep; shared `Apply<Field>Validation` maps tuple → `<Field>HasError`/`<Field>ErrorText`. Save re-runs all validators (safety net for uniqueness/DB).
3. **XAML:** `dxe:TextEdit`/`dxe:DateEdit` with `HasError`/`ErrorText` bindings + `Unfocused="On<Field>Unfocused"` — inline only, **no dialog/summary/snackbar/wall-of-red**.
4. **Code-behind:** `private VM ViewModel => (VM)BindingContext;` and `On<Field>Unfocused(...) => ViewModel.Validate<Field>Command.Execute(null);`
5. Multi-field: repeat the block once per field (own dirty flag / command / HasError / ErrorText). Don't route one message to a field by substring.
6. **Edit-mode pre-population must NOT mark a field dirty** (Opus review note, Task 02). `[QueryProperty]`/edit-mode assignment fires `On<Field>Changed`, which would set `_<field>Dirty = true` before the user touches the field. Benign for Venue (an existing name is valid) but the multi-field forms (Person/Songs/Artists) pre-fill fields that *could* be invalid → premature error flash on first blur. Guard pre-population so it does not set the dirty flag (e.g. suppress the dirty-set during initial load / edit-mode hydration).

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

## Efficient continuation prompt (paste into the fresh session)
> Resume the Form validation orchestration. Read ONLY `Docs/Management/DevCycleCraft/ui-form-validation-guide/ORCHESTRATION-HANDOFF.md` and BACKLOG rows 168–173 — do not re-read feature history. You are the orchestrator (shell/git only; never read source; delegate all code to subagents; each subagent in its own worktree with the STEP 0 fetch+reset guard). Continue exactly at: (1) dispatch a fresh **Opus** reviewer for the Venue branch `worktree-agent-a26c6ffb2f045a11d` (`9c51d4ba`); on APPROVE, merge it to `develop` and push. (2) Then apply the Reference Pattern (in the handoff) to the remaining forms **in BACKLOG order — Singer(Person) → Songs → Artists** — one **Sonnet** subagent each, worktree off `origin/develop`, TDD, mandatory Opus review, merge before the next. Update BACKLOG statuses and this handoff after each. Respect the Helder gates and gotchas listed in the handoff. Be autonomous; all authorizations granted.
