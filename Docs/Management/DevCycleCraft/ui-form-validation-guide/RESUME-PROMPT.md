# Form Validation — Fresh-Session Resume Prompt

> Paste the block below into a fresh Claude Code session to continue the Form Validation orchestration at **Task 04 (Songs)**. Tasks 01 (guide), 02 (Venue), 03 (Person) are DONE + merged to `develop`.
>
> **develop tip after Task 03:** `11ce501` (later docs commit `373dfb1`).
> **Before resuming:** Helder is manually testing the Venue + Person forms on the emulator. If a defect is found, fix it (bug-fix pattern) BEFORE starting Task 04.

---

## Paste-ready prompt

> Resume the Form validation orchestration. Read ONLY `Docs/Management/DevCycleCraft/ui-form-validation-guide/ORCHESTRATION-HANDOFF.md` and BACKLOG rows 168–173 — do not re-read feature history. You are the orchestrator (shell/git only; never read `.cs`/`.xaml`; delegate all code to subagents; each subagent in its own worktree with the STEP 0 fetch+reset guard).
>
> Tasks 02 (Venue) and 03 (Person) are DONE + merged (develop tip `11ce501`). Continue at **Task 04 (Songs)**:
> 1. **Single-writer pre-check (mandatory):** confirm BUG-020 `SongFormViewModel` fix (`3b2cb75`) is in `origin/develop`, and `git status` on develop shows no uncommitted SongForm changes. Only then dispatch.
> 2. Dispatch ONE **Sonnet** subagent in a worktree off `origin/develop`, applying the **Reference Pattern** (handoff points 1–8, incl. the proven `_isHydrating` edit-mode dirty-guard and the `ApplyAsyncFailureAsync` async-failure routing) to `SongFormPage`/`SongFormPage.xaml.cs`/`SongFormViewModel` + `SongService` validators (add to the Service only if a validator is missing). TDD; new `SongFormViewModelTests`; task-log + `.sln` registration; count `[Fact]` methods exactly (truthful test evidence).
> 3. On subagent return, dispatch a fresh **Opus** reviewer against the branch. On `VERDICT: APPROVE`, merge to `develop` and push. Only then start **Task 05 (Artists)** — same flow on `ArtistFormPage`/`ArtistFormViewModel`.
> 4. Update BACKLOG rows 172/173 and the handoff (STATUS, ledger, resume prompt) after each merge.
> Respect the Helder gates (birthday no-year entry is OUT OF SCOPE; emulator E2E is a manual Helder gate) and the gotchas in the handoff. Be autonomous; all authorizations granted.

---

## Session-2 context notes (why we stopped)

- Stopped after Task 03 merged, at Helder's request, due to high token usage (~170k) — not a blocker.
- Both Task 02 and Task 03 passed a fresh-Opus review before merge. Verdicts + non-blocking notes are recorded in the handoff STATUS and Reference Pattern (points 6–8).
- `.claude/settings.json` / `.claude/changed-files.txt` may show as modified on `develop` — these are edited by a **parallel terminal session** also on `develop`, not by this orchestration. Do not commit or revert them.
- Worktrees for the merged Venue and Person branches are safe to delete (see handoff ledger).

## Helder gates still open (do not block Task 04/05)
- **Birthday day/month-only entry mechanism** (Person) — DateEdit has no masked no-year entry; emulator decision pending. Validation wiring is done; the entry mechanism is untouched.
- **Integer / R10** — requirements doc `01-ui-form-validation-guide.md` Integer section is a `<TODO>` stub; needs Helder to complete the spec.
- **Emulator E2E** of Venue + Person forms — manual Helder verification.
