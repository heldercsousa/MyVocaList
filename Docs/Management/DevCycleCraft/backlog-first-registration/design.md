# Design — BACKLOG-first Registration Enforcement

> Authoritative design input: `analysis-pipeline/09-final-consolidated-plan.md`. This file is the
> SDD design artifact derived from it. Trace tags `(R1-n)`/`(R2-…)` point to the source review item.
> Language rule: English only (CLAUDE.md § Constitutional Constraints).

---

## 1. Architecture overview

The feature ships in **layers**, smallest blast radius first:

```
D (rule strengthening)        ── ships unconditionally  (proposed diff + direct session-ops edit)
C (review/checklist backstop) ── ships unconditionally  (.claude/commands/review.md lane note)
A (Stop-hook advisory)        ── ships unconditionally  (fail-open, classifier-driven, non-blocking)
B (PostToolUse interception)  ── ships ONLY if Phase 1 spike passes
```

No hard-blocking anywhere. No mtime baseline anywhere.

### Why device memory is the real target (corrected rationale, R1-1)
Device auto-memory (`~/.claude/projects/<project>/memory/`, ~16 live files — snapshot at design
time) is the genuinely team-invisible surface: not git-tracked, not in `changed-files.txt`. The earlier
"in-repo `memory-bank/` is an empty stub already covered" rationale was **false and removed** —
`.claude/memory-bank/MEMORY.md` is git-tracked (~2410 bytes), and `changed-files.txt` only records
in-session Edit/Write paths, so it would not capture a harness-injected memory file anyway. The
device-only scope is re-derived from the true fact: the device dir is the only team-invisible home.

---

## 2. Components & interfaces

### 2.1 `backlog_lib.py` (pure logic — Level A, full TDD)
Mirrors `lease_lib.py` (pure functions, no I/O, fixture-testable).

```python
def classify_memory_change(filename: str, line_or_diff: str) -> str:
    """Classify a single changed memory line.
    Returns 'exempt' or 'candidate' — never raises, never a third state.
    Line/content-level: a new-work line inside MEMORY.md is a 'candidate' even though
    MEMORY.md is otherwise auto-captured (INV-3). Applies the documented precedence rule
    when an exempt marker and a new-work verb co-occur on one line."""

def should_remind(classified_changes: list[str],
                  backlog_changed_this_session: bool) -> tuple[bool, str]:
    """Return (should_remind, message).
    (False, _) whenever backlog_changed_this_session is True, regardless of candidates.
    (True, reminder) iff >=1 change is 'candidate' AND backlog not changed this session."""
```

**Precedence rule (R2-precedence) — documented contract:**
When a line carries BOTH an exempt marker (e.g. `project_*` "NEXT:" prefix) AND a new-work verb
(`implement`, `add`, `build`, `create`, `fix`, `investigate` …):
- If the new-work verb targets a **noun not already tracked** → **candidate** (new-work signal wins).
- If the line is a pure continuation of an already-tracked item ("NEXT: continue Phase 16C") → **exempt**.
The discriminator is the presence of a *new* work-item noun, not merely the verb. Adversarial tests
(AC-13) pin this: "NEXT: implement X" where X is new → candidate; "NEXT: run smoke test for <tracked>"
→ exempt.

### 2.2 `orphan_check.py` (thin fail-open Stop wrapper — Level B, deterministic tests)
```python
# Pseudocode contract
def main(device_memory_dir: str | None = None) -> int:
    try:
        path = resolve_device_dir(device_memory_dir)   # PARAMETERIZED — fixture-testable (R1-5/R2)
        changed = enumerate_changed_memory_files(path)  # signal per spike, else reviewer-supplied
        classified = [classify_memory_change(f, line) for f, line in changed]
        remind, msg = should_remind(classified, backlog_changed_this_session())
        if remind:
            print(msg)                                  # advisory ONLY — never blocks
        return 0
    except Exception:
        return 0                                        # INV-1 fail-open (R1)
```
- Device-dir path is **injected/parameterized**, never hardcoded-mangled → unit-testable against a fixture dir.
- Always `return 0`. No `sys.exit(non-zero)` path exists.

```python
def backlog_changed_this_session() -> bool:
    """True iff BACKLOG.md changed at any point THIS SESSION — committed OR working-tree.
    MUST NOT use a bare `git diff HEAD` (R1-7): that misses BACKLOG edits already committed
    in-session by the auto-commit hook. Detect across the whole session window (e.g. compare
    against the session-start ref, OR union the working-tree diff with in-session commit history)."""
```
This helper carries the §7 suppression-window correctness point — its contract is pinned here so the
Builder cannot regress to a working-diff-only check.

### 2.3 `tests/test_backlog_lib.py` + `orphan_check` tests
`tests/` subdir to match the lease precedent (R2-test-path). Coverage:
- 4 exempt categories → exempt (AC-4)
- new-work line → candidate (AC-4)
- adversarial precedence cases — "NEXT: implement X" (AC-13)
- backlog-already-changed → no reminder (AC-5)
- empty / garbage input → exempt, no crash (AC-6)
- path-resolution / enumeration against fixture dir + fail-open (AC-12)

### 2.4 `settings.json` wiring
- Add `orphan_check.py` as a **NEW command-type entry under the existing `Stop` key**, mirroring the
  `heartbeat.py` command entry — NOT woven into the Stop agent-prompt (R1-3). Non-blocking.
- **No new top-level key** → SessionStart expected-keys check unchanged (AC-10, INV-2).
- (spike-pass only) add a memory-write buffer command hook under the existing `PostToolUse` key.

---

## 3. Data flow

```
session work ──► (maybe) memory write(s)
                       │
            ┌──────────┴───────────┐
            │  Phase 1 spike PASS  │  Phase 1 spike FAIL
            ▼                      ▼
  PostToolUse buffer        no live capture →
  records memory writes     advisory uses spike-confirmed signal,
            │               else reviewer-supplied list
            └──────────┬───────────┘
                       ▼
                 session end (Stop hook)
                       ▼
        orphan_check.py: enumerate changed memory
                       ▼
        classify each line  ──► candidate? AND backlog NOT changed?
                       ▼ yes
                 print advisory (never blocks)  ── exit 0 always
```

---

## 4. The spike (Phase 1 — gates Option B only)

**[SPIKE] Is a device-scoped auto-memory write observable by ANY Claude Code hook, AND is the device
dir path deterministically resolvable?**
- Time-box: **60 min, hard stop.**
- **Lead with path-determinism** (R1-5/R2): no repo precedent resolves the out-of-tree mangled path —
  all lease scripts resolve only `CLAUDE_PROJECT_DIR`. Option-B event-observability is secondary.
- Method: throwaway logging hook on candidate events + scratch log; trigger a memory write; inspect.
- **Success** → Option B viable (build PostToolUse buffer).
- **Failure** → Option B DEAD; ship D + C + advisory-A; advisory operates on the spike-confirmed
  signal or is **reviewer-driven**; **do NOT** fall back to an mtime baseline.
- Artifact: `findings.md`. Mirrors the Session-Continuity AC-5 spike.

> **§4 Spike outcome (resolved 2026-06-24 — AC-8 cleared).** Both questions passed; full evidence in
> `findings.md`. **Path determinism: DETERMINISTIC** — `orphan_check.py` resolves the project memory
> dir via `git rev-parse --git-common-dir` → strip a trailing `/.git` → mangle `[:/\\]→-` →
> `~/.claude/projects/<mangled>/memory/`. It must NOT use `cwd` or `$CLAUDE_PROJECT_DIR` (the latter was
> observed unset; the former yields the worktree-mangled trap path
> `…--claude-worktrees-backlog-first-registration`). On any `git rev-parse` failure it fails open
> (`return 0`). **Hook observability: OBSERVABLE** — the existing PostToolUse `Edit|Write` group already
> logs memory-dir writes to `.claude/changed-files.txt` (verified: 29 logged lines), matched by the
> substring `projects/<mangled>/memory/` (paths are logged cwd-relative, so substring — not absolute —
> match is required). **Option B is VIABLE**: the Stop check reads the session-scoped
> `changed-files.txt` for candidate memory writes — no separate buffer and no mtime baseline needed.
> Posture stays advisory/fail-open: warn only, always exit 0.

---

## 5. Files to create / change

### A. Directly editable (NOT deny-listed) — implement in-session
| File | Purpose |
|------|---------|
| `requirements.md` / `design.md` / `tasks.md` / `plan.md` / `findings.md` / `task-log.md` | Spec set |
| `proposed-diffs.md` | Write-protected rule diffs for Helder (see B) |
| `.claude/scripts/backlog/backlog_lib.py` | Pure `classify_memory_change` + `should_remind` |
| `.claude/scripts/backlog/orphan_check.py` | Thin fail-open Stop wrapper, parameterized device path |
| `.claude/scripts/backlog/tests/test_backlog_lib.py` | Classifier + wrapper tests |
| `.claude/settings.json` | Stop command entry (+ spike-pass PostToolUse buffer) |
| `.claude/library/session-ops.md` | Device memory as 6th tier (+ Authorship review) |
| `MyVocaList.sln` | Auto-register spec `.md`; **manual** register `.py` files |

### B. Write-protected — proposed diffs only (`proposed-diffs.md`), applied by Helder
| File | Handling |
|------|----------|
| `.claude/rules/workflow.md` | Rule 1 upgrade + Rule 2 exit-checklist line + hook-table row → Helder `amend:` + changelog triple. Authorship: Helder reads/edits (R1-8). |
| `CLAUDE.md` | Recommend NO change (600-line budget); Helder-reserved one-line pointer only. |
| `Docs/Changelog/changelog.md` | The `amend:` triple lives in `proposed-diffs.md` until Helder applies it; agent must NOT pre-write (the `TaskCompleted`/`Stop` hooks already auto-touch it and would collide). |

---

## 6. `.sln` registration detail (R1-4, R2-Corr1)
- Spec `.md` files: `sync-docs-to-sln.ps1` auto-registers on Write (DevCycleCraft prefix →
  `{0C4BA720-…}`). Files go in the **existing** `backlog-first-registration` solution folder
  (`{FA1234BC-0001-4000-8000-000000000029}`), flat (no dedicated subfolder — Helder gate e default).
- `.claude/scripts/backlog/*.py`: the sync hook only handles `Docs\` paths on Write, so these are
  **NOT auto-covered** → an explicit **manual** `.sln` task registers them.
- **Per-file verification is an explicit gate** (the sync hook is Write-only + self-skips): verify
  every new file (`.md` AND `.py`) actually appears in `.sln` before close (AC-9).

---

## 7. Known documented limitation (R2 / R1-7)
The advisory inherits Stop STEP 5's **coarse correlation**: a session that updates BACKLOG for
feature X while writing a memory-only orphan for feature Y will **suppress** the reminder (BACKLOG
"changed at all"). This is defensible (matches existing STEP 5) but is a deliberate limitation, not a
bug. Additionally, `should_remind`'s suppression window must be specified precisely against the
auto-commit hook: **`git diff HEAD` will NOT see already-committed in-session BACKLOG edits** —
`backlog_changed_this_session()` must detect BACKLOG changes across the whole session (committed or
working-tree), not just the current working diff.

---

## 8. Phasing (DRY-onion) & sequencing

| Phase | Scope | Gate |
|-------|-------|------|
| **0 — Spec** | brainstorm → requirements/design/tasks → spec-reviewer → Helder approval; BACKLOG `💡→📋→🗺️→🟢`; register + verify spec `.md` in `.sln` | Helder approval |
| **1 — Spike** | throwaway only → `findings.md` → update `design.md` | gates Phase 4 B-branch + path-resolution |
| **2 — Rule/def diffs** | workflow.md proposed-diff (Helder-gated); session-ops.md direct edit + Authorship review | innermost, no code |
| **3 — Pure logic** | Tester→Builder, Level A full TDD: line-level classifier + precedence + adversarial tests, red→green | — |
| **4 — Tooling + hook wiring** | `orphan_check.py`; command-type Stop entry; (spike-pass) PostToolUse buffer; manual `.sln` for `.py`; verify expected-keys unchanged | **gated on posture ✅ (ratified)**; SEQUENTIAL — `settings.json` single-writer |
| **5 — Backstop + close** | apply `.claude/commands/review.md` lane note SEPARATELY from / after the workflow.md `amend:`; verification; session-end ritual; BACKLOG → `✅ Done` only after Helder applies the `amend:` | — |

**Sequencing:** Phase 3 before Phase 4; Phase 1 gates only Phase 4's B-branch; Phase 2's workflow.md
change is Helder-gated/independent.
**Single-writer hotspots:** `.claude/settings.json` (Phase 4 sequential), `tasks.md`, `MyVocaList.sln`.

---

## 9. Orchestration constraints (this feature)
- **Orchestrator never reads `.cs`/`.xaml`** and delegates all code/script inspection + edits to
  subagents (one task per subagent). Spec `.md` authoring is the orchestrator's own job.
- `workflow.md` + `CLAUDE.md` are **deny-listed** — rule changes are proposed diffs only.
- `session-ops.md` is directly editable but needs Helder **Authorship** review.
- `.claude/scripts/backlog/*.py` need **manual** `.sln` registration.
- The Stop hook gets a **command-type entry**, not an agent-prompt weave.
