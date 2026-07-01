# Form Validation — Fresh-Session Resume Prompt

> Paste the block below into a fresh Claude Code session to continue the Form Validation orchestration at **Task 04 (Songs) — IN PROGRESS, NOT COMMITTED.** Tasks 01 (guide), 02 (Venue), 03 (Person) are DONE + merged to `develop`.
>
> **develop tip (session 3 start):** `ad042dd`.
> **Task 04 state:** a Sonnet subagent (id `aa495a45f830848ae`) implemented Songs-form validation in worktree `.claude/worktrees/agent-aa495a45f830848ae` (branch `worktree-agent-aa495a45f830848ae`), but the work is **uncommitted** — 7 modified files sitting in that worktree's working tree. The branch HEAD itself is still at `ad042dd` (no new commit). The subagent was resumed once to finish the exit checklist (build/test/task-log/`.sln`/commit/push) but its final status is unknown as of this handoff — do not assume it finished.
> **Before resuming:** do NOT dispatch a brand-new subagent for Task 04 — first inspect the existing worktree and either resume the existing agent or hand a subagent that same worktree to finish from. See full detail in `ORCHESTRATION-HANDOFF.md`.

---

## Paste-ready prompt

> Resume the Form validation orchestration. Read ONLY `Docs/Management/DevCycleCraft/ui-form-validation-guide/ORCHESTRATION-HANDOFF.md` and BACKLOG row 174 — do not re-read feature history. You are the orchestrator (shell/git only; never read `.cs`/`.xaml`; delegate all code to subagents).
>
> Tasks 02 (Venue) and 03 (Person) are DONE + merged. **Task 04 (Songs) is IN PROGRESS and NOT COMMITTED — resume it, do not restart it:**
> 1. Inspect worktree `.claude/worktrees/agent-aa495a45f830848ae` (branch `worktree-agent-aa495a45f830848ae`): `git -C .claude/worktrees/agent-aa495a45f830848ae status --porcelain` and `git -C .claude/worktrees/agent-aa495a45f830848ae log --oneline -3`. If 7 files are still modified and uncommitted and no new commit exists, the work is exactly where session 3 left it.
> 2. Reuse that same worktree — do not create a new one. Dispatch a subagent (or resume agent id `aa495a45f830848ae` if still addressable) to: review the existing diff against the Reference Pattern (`ORCHESTRATION-HANDOFF.md` points 1–8), fix anything that deviates, then complete the subagent exit checklist (`dotnet build` 0 errors, `dotnet test` 0 failures with exact counts, post-edit re-read, task-log at `Docs/Management/BusinessFeatures/artists-songs/form-validation-task-log.md`, `.sln` registration of that file under the existing `artists-songs` SolutionFolder — no new GUID needed, commit, push).
> 3. On completion, dispatch a fresh **Opus** reviewer against the branch. On `VERDICT: APPROVE`, merge to `develop` and push. Only then start **Task 05 (Artists)** — same flow on `ArtistFormPage`/`ArtistFormViewModel`.
> 4. Update BACKLOG row 174 and the handoff (STATUS, ledger, resume prompt) after the merge.
> Respect the Helder gates (birthday no-year entry is OUT OF SCOPE; emulator E2E is a manual Helder gate) and the gotchas in the handoff. Be autonomous; all authorizations granted.

---

## Session-3 context notes (why we stopped)

- Helder asked to stop mid-Task-04 to start a fresh session, without advancing to Task 05.
- Single-writer pre-check for Task 04 was completed and passed before dispatch (BUG-020 fix confirmed in `origin/develop`; `develop` working tree was clean).
- The dispatched subagent's own report said it was "waiting for the build to finish" on its first stop, then was resumed with explicit instructions to finish the exit checklist. Whether that resume completed is unverified — check the worktree state directly first (git status/log), don't trust the last known report.
- Worktrees for the merged Venue and Person branches are still safe to delete (see handoff ledger) — the Songs worktree is NOT safe to delete, it holds uncommitted work.

## Helder gates still open (do not block Task 04/05)
- **Birthday day/month-only entry mechanism** (Person) — DateEdit has no masked no-year entry; emulator decision pending. Validation wiring is done; the entry mechanism is untouched.
- **Integer / R10** — requirements doc `01-ui-form-validation-guide.md` Integer section is a `<TODO>` stub; needs Helder to complete the spec.
- **Emulator E2E** of Venue + Person forms — manual Helder verification.
