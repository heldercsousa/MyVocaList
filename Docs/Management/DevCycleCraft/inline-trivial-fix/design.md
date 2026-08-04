# Inline Trivial Fix (ITF) Lane — Design

> Companion to `requirements.md` in this folder. Rule amendment + hook enforcement; no application code is touched.

## Architecture overview

Three artifacts change. Nothing in `MyVocaList*/**` is modified.

| Artifact | Change |
|----------|--------|
| `.claude/rules/workflow.md` § Rule 2 | Add the ITF lane as a bounded exception to "all coding is done by subagents" |
| `.claude/agents/orchestrator.md` § Orchestrator Read-Scope | Add the single-file scoped-read exception to the deny-list |
| `.claude/scripts/constitutional-guard.py` | Add **Guard 3 — ITF bounds**, evaluating C1/C3/C4/C5 mechanically |

`Docs/Changelog/changelog.md` gains an entry; `MyVocaList.sln` needs the two new `Docs/` files registered (constraints-registry HARD GATE).

## Decision record

**D1 — Mechanical bounds where possible, declared bounds elsewhere.** "Tiny" is self-assessed by the agent seeking permission; every agent rates its own fix tiny. Only conditions a hook can count (file count, line count, path membership) are load-bearing, and they bind **once the orchestrator has declared**. C2 and C6 are judgement calls handled by *declaration* — written to the task-log **before** the edit — making over-reach auditable rather than invisible.

**D1a — The lane is opt-in, and the spec says so `[rev 2, spec-reviewer B1]`.** Guard 3 is inert without a declaration. Rev 1 claimed the bounds made abuse "impossible"; that was false — an orchestrator that never declares is simply in the pre-amendment state, governed by prose. Rev 2 restates the model honestly (`requirements.md § Enforcement model`): the hook bounds a *declared* ITF; prose bounds the undeclared case, exactly as today. The alternative — Guard 3 blocking every undeclared orchestrator `.cs` edit — requires reliable actor detection, which is not available (see below), and was rejected rather than faked. **Multi-declaration chaining** (successive declarations to touch 2+ files) is likewise a prose violation, detectable after the fact via the task-log lines and the commit trailer's true file count, not a mechanically blocked one.

**D2 — Narrow read-scope rather than suspend it.** The read-scope rule's stated rationale (`orchestrator.md` line 27) is that reading source burns coordination context and causes drift into implementer work. Reading exactly one already-identified file for ≤ 5 lines does not produce either effect. Grepping and neighbour-reading do, so they stay forbidden. This keeps the rule's purpose intact while removing the specific friction.

**D3 — Reuse `constitutional-guard.py`, do not add a second hook.** The guard already runs on `PreToolUse` for Write/Edit against `_CODE_SUFFIXES`, already parses the same payload shape, and already establishes the fail-open + exit-2 conventions. A third guard function is ~40 lines inside a proven harness; a new hook script would duplicate payload parsing and add a second failure surface.

**D4 — Orchestrator-only lane (AC-ITF-10).** Implementor subagents must not inherit ITF bounds — a 5-line cap on implementors would break normal feature work. The guard must therefore distinguish *who is editing*. See "Actor detection" below; this is the design's one genuine technical risk.

**D5 — `Lane:` commit trailer over a separate ledger.** Git log is already the durable audit surface and needs no maintenance. A trailer is greppable, survives rebases, and costs one line. A separate ITF registry file would need its own upkeep rule and would drift.

## Actor detection (the hard part)

Guard 3 must apply only when the **orchestrator/main agent** is editing. A `PreToolUse` hook payload does not reliably identify the acting agent type. Three options:

| Option | Mechanism | Assessment |
|--------|-----------|------------|
| **A — Declaration file** | Orchestrator writes `.claude/.itf-active` (JSON: bug id, file path, expected lines, timestamp) before editing; Guard 3 activates only when the file exists and names the target path; the file is consumed/cleared on commit or expires after 30 min | **Recommended.** Explicit, inspectable, no reliance on undocumented payload fields. Doubles as the C2 declaration. Failure mode is safe: no file → guard inert → the pre-existing rules apply unchanged |
| B — Payload agent field | Read an agent-type field from the hook JSON | Rejected — field presence/name is not contractually guaranteed and would silently fail-open |
| C — Session-id registry | Reuse `lease_lib.py` session identity | Rejected — leases identify sessions, not agent roles; the orchestrator and its subagents can share a session |

**Chosen: Option A, with the marker scoped to the worktree.** The declaration file *is* the opt-in. Without it the orchestrator has no ITF authorization, and the ordinary Rule 2 / read-scope rules apply unchanged; with it, Guard 3 enforces C1/C3/C4/C5 against the declared target.

**Marker location `[rev 2, spec-reviewer B2]`:** the marker lives at the **root of the worktree in which the edit occurs** — `<worktree>/.itf-active` — never at the repo root. Rev 1 placed it at `.claude/.itf-active`, which made it a repo-global switch: while it existed, Guard 3's "declared file != this file" branch would block *any* agent editing in that checkout, including an implementor subagent — precisely what AC-ITF-10 forbids. Scoping the marker to the worktree means an implementor working in its own worktree never sees a declaration and is never constrained (AC-ITF-10 holds by construction).

**Residual risk, stated:** an implementor that edits inside the *same* checkout the orchestrator declared in would still be caught by C1. Rule 2's worktree HARD RULE makes this configuration illegitimate anyway (every implementation task gets its own worktree), so the residual case is a pre-existing rule violation, not a new failure mode introduced here. Guard 3 does not detect actors and does not claim to.

### Declaration file shape

```json
{
  "id": "BUG-050",
  "file": "MyVocaList/ViewModels/SongFormViewModel.cs",
  "expected_lines": 1,
  "declared_at": "2026-07-21T14:32:00Z"
}
```

`expected_lines` is **audit-only** — Guard 3 checks the actual changed-line count against the constant 5, never against this field. It exists so a declaration that predicted 1 line but produced 5 is visible in review. Guard 3 must not read it.

`file` is repo-relative, forward slashes. Expiry 30 minutes; an expired declaration is treated as absent (AC-ITF-11).

### Declaration lifecycle

Full state table with per-transition actor: `requirements.md § Declaration lifecycle`. The load-bearing point for implementation: **the orchestrator deletes the marker as the final step of the ITF commit** (state 3), and the 30-minute expiry is only the safety net for a session that dies mid-fix. A stale marker must never authorize a later edit.

## Guard 3 — control flow

```
PreToolUse (Write | Edit | MultiEdit) on a *.cs file
  └─ Guard 1 (branch) ────────── on develop/main? → BLOCK (existing, C7)
  └─ Guard 2 (native dialogs) ── introduces DisplayAlert(? → BLOCK (existing)
  └─ Guard 3 (ITF bounds) ── read <worktree-of-file>/.itf-active
        ├─ absent or expired ──────────────── exit 0 (guard inert)
        ├─ declared file != this file ─────── BLOCK (C1: one file per declaration)
        ├─ suffix in (.xaml, .xaml.cs) ────── BLOCK (C3)
        ├─ path in governed-component list ── BLOCK (C4)
        ├─ path in sequential-only registry ─ BLOCK (C5)
        ├─ changed lines > 5 ──────────────── BLOCK (C1)
        └─ else ───────────────────────────── exit 0 (permit)
```

Ordering matters: cheapest and most-specific checks first, and Guards 1–2 keep priority so an ITF declaration can never smuggle a native dialog or a develop-branch edit past them.

### Changed-line counting

- `Write` → count lines in `content` (whole-file replace; almost always fails C1 for an existing file, which is correct — a full rewrite is not an ITF).
- `Edit` → `max(lines(old_string), lines(new_string))`, which upper-bounds added+modified+deleted.
- `MultiEdit` → sum across `edits`.

Upper-bounding rather than diffing exactly is deliberate: it errs toward blocking, and the failure mode of a wrongly-blocked fix (dispatch a subagent — the status quo) is far cheaper than a wrongly-permitted one.

### Path lists

Governed components (C4) and the sequential-only registry (C5) are already enumerated in `component-safety-gate.md` and `workflow.md` Rule 2 respectively. Guard 3 holds them as module-level constants with a comment pointing at the authoritative rules file, matching how `_FORBIDDEN` documents its source. Duplication is accepted — a hook cannot parse prose reliably — and the constants carry a "keep in sync with" note.

### Error handling

Fail-open on every exception (`return 0`), matching the existing guard's design note. A malformed or unreadable declaration file is treated as absent, never as authorization.

## Interaction with existing rules

| Rule | Interaction |
|------|-------------|
| `workflow.md` Rule 2 worktree HARD RULE | **Unchanged.** Guard 1 still fires; ITF grants no exemption (AC-ITF-07) |
| `workflow.md` Rule 3 commit-after-every-task | **Unchanged.** ITF commits use the Bug Fix Pattern message plus the `Lane:` trailer |
| `bug-tracking.md` regression tests | **Unchanged.** C6 defers to it; if the mandatory test is not itself ITF-sized, the whole fix is dispatched |
| `component-change-governance.md` four gates | **Unchanged.** C4 makes governed components categorically ineligible |
| `testing.md` risk tiers | **Unchanged.** A Level-A fix still needs its test; C6 routes it |
| Constitutional `[Unamendable]` constraints | **Unaffected.** Guard 2 continues to run ahead of Guard 3 |

## Testing strategy

Python tests beside the existing suites (`.claude/scripts/backlog/tests/`, `.claude/scripts/lease/tests/` establish the pattern) in `.claude/scripts/tests/test_constitutional_guard.py`:

| Test | AC |
|------|-----|
| Declaration absent → exit 0 (no ITF bound on any agent) | AC-ITF-10 |
| Declaration expired (> 30 min) → exit 0 | AC-ITF-11 |
| Declaration in worktree A, edit in worktree B → exit 0 | AC-ITF-10 / B2 |
| 1 file, 3 lines, plain `.cs` → exit 0 | AC-ITF-01 |
| Edit to a file other than the declared one → exit 2, message names C1 | AC-ITF-02 |
| 9-line `new_string` → exit 2, names C1 | AC-ITF-03 |
| `.xaml` target → exit 2, names C3 | AC-ITF-04 |
| Governed-component path → exit 2, names C4 | AC-ITF-05 |
| `MauiProgram.cs` → exit 2, names C5 | AC-ITF-06 |
| On develop → Guard 1 fires first | AC-ITF-07 |
| Malformed JSON declaration → exit 0 | AC-ITF-09 |

Guard-3 tests are Level A per `testing.md` (this is enforcement logic whose failure is silent and user-facing at the process level).

**AC-ITF-08** is automated as a shell assertion (`git log --grep "Lane: ITF"` against a fixture commit) rather than left to observation — rev 2, spec-reviewer suggestion 2. **AC-ITF-10** is automated by the two declaration-scope rows above.

Manual observation on the first real ITF fix remains as end-to-end confirmation, and is the point at which Helder sees the lane working in practice.

## Rollout

1. Helder approves this spec — including the C6 narrowing (`requirements.md § Note on C6`), which materially shrinks the lane versus rev 1.
2. **Helder reads and reviews the amended rule text itself** in `proposed-diffs.md` — required by `CLAUDE.md § Continuous Enhancement — Authorship` for any `.claude/rules/` edit. Approving the spec does not satisfy this; it is a separate, explicit gate.
3. Apply the four `amend:` diffs + changelog entry + `.gitignore` + `.sln` registration, in one `amend:` commit on develop.
4. Implement Guard 3 + tests (subagent task, in a worktree — Guard 3 is not itself ITF-eligible).
5. First live use, observed end-to-end by Helder. Subject depends on the C6 decision: a log-message-typo-class fix if C6 excludes test-bearing bugs; BUG-050 if its verification is manual-E2E.
6. Calibration review after ~20 `Lane: ITF` commits.

## Risks

| Risk | Mitigation |
|------|------------|
| Actor detection is declaration-based, so an agent could declare ITF for work that fails C2/C6 | Declaration is written to `task-log.md` *and* the marker file — both are reviewable; the `Lane:` trailer surfaces it in git history |
| Path constants drift from the authoritative rules files | "Keep in sync with" comment + the calibration review is a natural checkpoint |
| Scope creep: ITF becomes the default path | The 5-line cap plus the audit trailer make erosion measurable; calibration review is the correction mechanism |
