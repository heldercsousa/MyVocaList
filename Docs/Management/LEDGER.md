# Work Ledger — branch/status tracker (lives on develop)

> **Purpose:** single source of truth, always on `develop`, for WHERE every in-flight task/bug lives (branch, worktree) and its current phase/status. This is what makes resuming development, debugging, and merges into develop possible after any interruption. Maintained via `/sln-ledger` and at every phase transition (dispatch, phase change, merge, close). Rules: `workflow.md § Rule 2` (docs land on develop).
>
> **Row lifecycle:** add the row when work is dispatched to a branch/worktree; update `Phase/Step`, `Status`, and `Last commit` at each transition; on merge into develop set Status `merged` and move the row to the Completed section (keep last 20 for audit, prune older).

## Active

| ID | Feature / Bug | Phase / Step | Branch | Worktree | Status | Last commit | Next action |
|----|---------------|--------------|--------|----------|--------|-------------|-------------|
| DX-AC | Replace AutocompleteMobileField with DX AutoCompleteEdit | T2–T6 complete; merged to develop (verifier CONDITIONAL PASS, W1/W3 fixed, W2 = T7 watch item); 501/501 green; worktree removed; pushed | merged into develop | — | merged | e17cb4c | **Helder: (1) ✅ pushed — origin/develop = local at `e17cb4c`; (2) DECIDE the REQ-DXAC-03 conflict (VM clears artist text on blur — see task-log ⚠ ESCALATION; may mean BUG-027 is not closed by this swap); (3) run T7 on-device checklist** in task-log.md (items a–i incl. smoke 16C.1 + BUG-044/045/047 + BUG-027 re-verify), then close BACKLOG rows |
| INLINE-AC | Song artist field — correctness fixes (BUG-050/051/052 + retain-text) + inline create-new-artist | T1–T9 done + BUG-053 fixed (`8d33547`); **T10 on-device FAILED 2026-07-22** — Part A (BUG-053) ✅; 6 defects remain → **BUG-054…059** (root-caused to DX AutoCompleteEdit wiring/XAML, file:line in task-log § T10 outcome). Fix wave dispatched 2026-07-22 (implementor, sequential in worktree) | `feat/inline-artist-create` | `../MyVocaList-inline-ac` | in progress: fixing BUG-054…059 | 8d33547 | **Next session (fix wave in the SAME worktree, sequential):** order BUG-056 (search race) → BUG-055 (edit hydration + edit-save ArtistId) → XAML cluster BUG-054b/057/058 → BUG-054a code-behind → BUG-059 (cascade verify). Incremental single-file XAML edits. Then re-run T10 on-device (Helder). On all-green: apply the closeout in `changes/2026-07-21-inline-artist-create/pending-backlog-closeout.md` — ⚠️ do NOT hand-edit fenced `BACKLOG:GENERATED` rows (SPEC-EVO owns BACKLOG since 2026-07-22; use `backlog_gen.py status`), then merge+push (creds cached), remove worktree, unblock catalog. Full triage: task-log § T10 outcome. |
| ITF | Inline Trivial Fix lane — bounded orchestrator inline-edit exception | Rules amended + Guard 3 merged (verifier CONDITIONAL PASS, all findings resolved); worktree-base-check false-positive fix merged; 38/38 green on develop; worktrees removed | merged into develop | — | merged | a4c198a | Helder: observe the FIRST LIVE ITF FIX end-to-end (last open gate) — opportunistic, waits for a qualifying fix. Full resume instructions incl. how to recognise/run one: `DevCycleCraft/inline-trivial-fix/handoff.md`. Also awaiting a decision on the 4 misbased `agent-*` worktrees (1 unmerged commit each — listed in that handoff). |
| SPEC-EVO | Spec Evolution — nested `bugs/`/`changes/` folders + generated BACKLOG | **⚠️ OWNS `BACKLOG.md` + the 5 `backlog-archive/*.md` generated regions since 2026-07-22.** Phase 1 generator merged (125 tests green incl. T9e). Additive phase **complete through T10b** (on develop). **Phase 3 destructive in progress in the worktree** per the scoped exception below: T11a done (`e9396f6`), T11b next | `feature/backlog-migration` | `../mvl-backlog-migration` | in progress: T11b | e9396f6 | **Other sessions: do NOT hand-edit rows inside the `BACKLOG:GENERATED` fences — a hand-edit there is silently overwritten by `regen`, not merge-conflicted.** Change a row via its item `README.md` frontmatter or `backlog_gen.py status <ID>`; add items via `backlog_gen.py register`. Coordinate here first if you must touch a fenced row. Ownership ends when the branch merges. Write-ownership rule under definition: `spec-evolution-versioning/tasks.md` **T13d**; scoped deviation from "docs land on develop" authorized by Helder 2026-07-22 and recorded in `.claude/exception-registry.md`. |
| SC-ENH | Session Continuity enhancements (Phase 8, Tasks 11–15) | complete — verifier PASS; cleanup executed (30→8 worktrees, form-ux merged `f00543a`, 478/478) | develop (tooling scripts + docs only, no app code) | — | to review | 68e1852 | Helder: run two-terminal live demo (last open gate); optionally direct the 4 leftover agent worktrees |

## Completed (last 20)

| ID | Feature / Bug | Branch | Merged into develop | Commit |
|----|---------------|--------|---------------------|--------|

---

**Status vocabulary:** `dispatched` · `in progress` · `build failure` · `test failure` · `to review` · `blocked: <reason>` · `ready to merge` · `merged` · `abandoned: <reason>`

**Column notes:**
- **Branch** — the branch holding the changes (empty = work happened directly on develop, docs-only).
- **Worktree** — `.worktrees/<name>` path, or `—` if the worktree was already removed (branch still holds the commits).
- **Last commit** — short hash on the task branch, so a resuming session can `git log <hash>..<branch>` instantly.
- **Next action** — one line: what the next session should do (e.g. "fix failing test X", "await Helder review", "merge after wave 2").

**Resume chain (no globbing):** this ledger row → the feature's `task-log.md` `### Checkpoint` block (live step state, pinged on a ~10-min heartbeat) → read ONLY its Context manifest files. See `session-ops.md § Checkpoint Ping & Context Manifest`.
