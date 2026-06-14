# Session Continuity — Task Leasing & Auto-Resume Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give Claude Code sessions a self-arbitrating lease mechanism — a heartbeat-maintained per-session claim file plus a freshness/reclaim helper — so two sessions never collide on the same work unit and an interrupted unit auto-resumes after a usage-window reset, with no Helder arbitration.

**Architecture:** A `PostToolUse`/`Stop` command hook atomically writes a per-session claim file (`.claude/leases/<session_id>.json`) holding `owner`, `pid`, `last_active`, `resume_pointer`. A separate, **unit-testable** freshness helper classifies any claim `fresh|stale` from the two-fact model (`last_active` within `LEASE_TTL_SECONDS=1800` **OR** `pid` alive on this host), treats corrupt claims as stale, and enforces single-winner concurrent reclaim via re-read. `workflow.md` Rules 4/7/8 are amended to consult these claims. An in-session scheduled wakeup reads the resume pointer and continues. All artifacts are shell/Python scripts + JSON config + Markdown rule edits — **no MAUI/C# app code**.

**Tech Stack:** POSIX sh / Python 3 (hook scripts must run on the dev host via the same `python -c` pattern already used in `.claude/settings.json`), JSON (claim files + settings.json), Markdown (`workflow.md`), `pytest` for the freshness-classification unit tests, Claude Code hooks (`PostToolUse`, `Stop`, `SessionStart`) + scheduled wakeup (`/loop`).

---

## Source-of-Truth Decisions (locked 2026-06-14, confirmed by Helder)

- `LEASE_TTL_SECONDS=1800` (30 min) — single source, defined once in the lease library, read by both writer and checker.
- Claim file path: `.claude/leases/<session_id>.json` — gitignored, one file per session.
- Heartbeat keys off the **parent** `session_id` even when `agent_id`/`agent_type` is present (AC-3.4).
- Two-fact freshness: `now - last_active < TTL` **OR** `pid` alive on this host.
- Corrupt/half-written claim → treated as absent/stale (AC-2.5).
- Concurrent reclaim → write-then-re-read single-winner (AC-2.4 / INV-3).
- Auto-resume scope: IN-SESSION usage-window reset only; fully-closed terminal is OUT of scope (findings.md § self-scheduled re-entry).

## File Structure

| File | Responsibility | Status |
|------|----------------|--------|
| `.claude/scripts/lease/lease_lib.py` | Pure logic library: `LEASE_TTL_SECONDS` constant, `classify(claim_dict, now, pid_alive_fn) -> "fresh"|"stale"`, `parse_claim(raw_text) -> dict|None`, `pid_alive(pid) -> bool`. No hook I/O — fully unit-testable. | Create |
| `.claude/scripts/lease/heartbeat.py` | Hook entry point: reads hook JSON from stdin, resolves parent `session_id`, atomically writes/updates `.claude/leases/<session_id>.json` (tmp+rename). Imports `lease_lib`. | Create |
| `.claude/scripts/lease/reclaim.py` | CLI helper a session runs at session-start / collision-check: classifies a target claim, performs write-then-re-read single-winner reclaim, prints `fresh|reclaimed|lost`. Imports `lease_lib`. | Create |
| `.claude/scripts/lease/resume.py` | In-session scheduled-wakeup entry: reads own claim's `resume_pointer` + `tasks.md` + last commit, prints the exact continuation instruction. Imports `lease_lib`. | Create |
| `MyVocaList.Tests.Tooling/test_lease_lib.py` (or `.claude/scripts/lease/tests/test_lease_lib.py`) | pytest unit tests for `lease_lib.classify` / `parse_claim` / `pid_alive` covering AC-1.1, AC-1.2, AC-2.1, AC-2.2, AC-2.5. | Create |
| `.claude/settings.json` | Register `heartbeat.py` under `PostToolUse` (all tools) and `Stop`; via `update-config` skill. | Modify |
| `.gitignore` | Add `.claude/leases/` so claim files are never committed. | Modify |
| `.claude/rules/workflow.md` | Rule 4 (`[~]` reclaim semantics), Rule 7 (session-start claim/reclaim step), Rule 8 (liveness in collision check). **WRITE-PROTECTED — see Risk R1; Helder manual `amend:` commit.** | Modify (handoff) |
| `Docs/Management/DevCycleCraft/session-continuity-leasing/tasks.md` | Ordered task tracker. | Create (this plan's companion) |

## Dependency Ordering (DRY-Onion adapted to infra)

```
Phase 1 (innermost): lease_lib.py — pure logic, no I/O          [no deps]
Phase 2:             unit tests for lease_lib                    [P, depends on Phase 1 interface]
Phase 3:             heartbeat.py + reclaim.py + resume.py       [depend on lease_lib]
Phase 4:             config wiring (settings.json + .gitignore)  [depends on heartbeat.py]
Phase 5:             workflow.md rule edits                      [HANDOFF — write-protected]
Phase 6:             in-session auto-resume wiring               [depends on resume.py + Phase 4]
Phase 7 (outermost): two-terminal demo verification             [depends on all]
```

---

## Risks & Handoffs

- **R1 — `.claude/rules/` is WRITE-PROTECTED (HIGH / handoff).** `.claude/settings.json` `permissions.deny` contains `Edit(.claude/rules/*.md)` and `Write(.claude/rules/*.md)`. CLAUDE.md § Amending These Rules requires an `amend:` commit prefix + changelog entry for any `workflow.md` change. **A subagent CANNOT edit `workflow.md`.** Phase 5 is therefore a **Helder manual handoff**: the implementing wave produces the exact proposed diff text for Rules 4/7/8 as a review artifact (e.g. appended to tasks.md or a scratch note), and Helder applies it himself with an `amend:` commit + changelog entry. Do not assume the subagent can complete Phase 5.
- **R2 — `pid` reuse / cross-host (MEDIUM).** A recycled PID on the same host could read as "alive" falsely. Mitigation: `pid` is only a *fast-reclaim accelerator*; the TTL heartbeat remains the authoritative cross-host-safe signal. Out-of-scope per requirements.md (cross-host). Keep `pid_alive` conservative: on any uncertainty (cannot determine), return `False` so the unit ages out via TTL rather than being held forever.
- **R3 — Hook portability (MEDIUM).** Hooks run on the dev host shell. The repo already uses `python -c "..."` inline hooks (see existing `PostToolUse`), so Python 3 is assumed available. Keep `heartbeat.py` dependency-free (stdlib only: `json`, `os`, `sys`, `tempfile`, `datetime`).
- **R4 — Heartbeat overhead (LOW).** `PostToolUse` fires after every tool call; the write must be cheap (single small JSON, atomic rename). No network, no git calls in the heartbeat path.
- **R5 — Test runner availability (LOW).** Unit tests are Python/pytest, not the .NET `MyVocaList.Tests` xUnit suite. If pytest is not desired in the toolchain, the same cases can be authored as `python -m unittest` (stdlib, no extra dependency). Plan defaults to stdlib `unittest` to avoid adding a dependency — see Task 2.
- **Deferred to Helder:** Phase 5 (workflow.md edits) entirely. Also: whether the freshness helper should additionally key the BACKLOG `🟡 In Progress` feature-scope claim (requirements.md lists feature-scope as a work-unit type, but the claim-file mechanism is session-scoped; this plan implements the session/claim-file layer and the `[~]` step scope, and flags feature-scope BACKLOG claiming as a follow-up for Helder).

---

## AC Traceability

| AC | Covered by Task(s) |
|----|--------------------|
| AC-1.1 fresh within TTL → skip | T1 (`classify`), T2 (test), T9 (demo) |
| AC-1.2 dead TTL but live pid → fresh | T1, T2 |
| AC-1.3 blocked → pick next per Rule 4 | T6 (reclaim CLI returns `fresh`), T8 (Rule 4 edit), T9 |
| AC-2.1 stale (old + dead pid) → reclaimable | T1, T2 |
| AC-2.2 dead pid before TTL → fast reclaim | T1, T2 |
| AC-2.3 reclaim overwrites owner/pid/last_active | T6 |
| AC-2.4 concurrent reclaim single winner (re-read) | T6, T9 |
| AC-2.5 corrupt claim → stale | T1 (`parse_claim`), T2 |
| AC-3.1 tool call → heartbeat updates last_active | T4, T7 (config) |
| AC-3.2 interruption → last_active stops advancing | T4 (no timer), T9 |
| AC-3.3 no background timer / manual ping | T4 |
| AC-3.4 subagent heartbeat keys PARENT session_id | T4, T9 |
| AC-4.1 in-session wakeup auto-resumes | T5, T10 |
| AC-4.2 reclaim reads resume pointer + tasks.md + last commit | T5, T6 |
| AC-4.3 resume pointer written on claim/progress | T4, T5 |
| INV-1..INV-4 | T1/T6 (classification + single-winner), T4 (heartbeat side-effect only) |
| Demo Statement | T9 (two-terminal) + T10 (auto-resume) |

---

## Task Reference

The bite-sized, checkboxed task steps live in the companion file **`tasks.md`** (same folder), using the project's structured task entry format (Produces / Consumes / Risk / Files owned / Demo / Review lane) grouped into the phases above. Execute `tasks.md` top-to-bottom; respect `[P]` (parallelizable) vs `[SEQUENTIAL]` markers and the single-writer rule for `.claude/settings.json`.

## Self-Review notes

- **Spec coverage:** every AC maps to a task (table above); INV-1..4 covered by T1/T4/T6.
- **Out-of-scope honored:** no cross-host, no daemon, no fully-closed-terminal resume (T10 scoped to in-session wakeup).
- **Write-protection:** workflow.md edits are NOT performed by a subagent — flagged as Helder handoff (R1, Phase 5/T8).
- **Type consistency:** the function names `classify`, `parse_claim`, `pid_alive`, and the constant `LEASE_TTL_SECONDS` are used identically across all tasks and consuming scripts.
