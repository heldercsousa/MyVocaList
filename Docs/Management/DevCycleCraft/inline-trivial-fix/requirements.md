# Inline Trivial Fix (ITF) Lane — Requirements

> **Status:** 📋 Spec — revision 2 (spec-reviewer FAIL on rev 1; B1/B2 resolved below). Awaiting Helder approval.
> **BACKLOG row:** `Dev Cycle Craft` / 2026-07-12 — *Evaluate guideline update — allow inline trivial-task execution to save tokens*.
> **Type:** Rule amendment (`workflow.md` Rule 2 + Rule 3, `orchestrator.md § Orchestrator Read-Scope`) + hook enforcement.

## Vocabulary

| Term | Definition |
|------|------------|
| **ITF lane** | The bounded path by which the orchestrator applies a code fix directly, without dispatching an implementor subagent. |
| **Declaration** | The marker file `.itf-active` written at the root of the worktree where the edit will occur, plus the matching one-line `task-log.md` entry. It is the orchestrator's explicit opt-in and the sole activator of Guard 3. |
| **Fully diagnosed** | The root cause, the exact target file, and the exact target line(s) are all already recorded — in a prior subagent report, a `task-log.md` entry, or a BACKLOG row — **before** the orchestrator opens the file. If answering "where is the defect?" would require a grep, a second file, or reading the file to find out, the fix is not fully diagnosed. |
| **Governed component** | A custom UI component with 2+ consumers, per the list in `component-safety-gate.md`. |
| **Changed lines** | The guard's upper-bound count, not a true diff: `Write` → lines in `content`; `Edit` → `max(lines(old_string), lines(new_string))`; `MultiEdit` → sum over edits. |

## Problem

`workflow.md` Rule 2 requires **all** coding to be done by subagents, and `orchestrator.md § Orchestrator Read-Scope` forbids the orchestrator from reading source files. Together they mean a one-line fix — already fully diagnosed — must be delegated.

| Path | Approximate cost for a one-line fix |
|------|-------------------------------------|
| Dispatch a subagent | briefing (~1–2k tokens) + subagent cold-start context (~15–25k) + exploration + report + orchestrator re-reads report ≈ **25–35k tokens**, 2–4 minutes |
| Orchestrator edits inline | ~500 tokens (diagnosis context already loaded) |

The rule is correct in the general case and mis-calibrated at the bottom of the change-size distribution. **BUG-050** (`SelectArtist` never sets `IsArtistLocked = true`) is the canonical instance.

## Goal

Introduce the **Inline Trivial Fix (ITF) lane**: a narrow, explicitly-declared exception permitting the orchestrator to apply a fully-diagnosed, size-bounded fix directly, with the size and path bounds mechanically enforced by a hook **for the duration of the declaration**.

### Enforcement model — stated honestly

The ITF lane is **opt-in**. Guard 3 is inert until a declaration exists, and activates only for the declared file.

- **Once declared:** C1/C3/C4/C5 are hook-enforced. The orchestrator cannot exceed the bounds it opted into — including by mistake or optimism mid-edit. This is where the mechanical value lies.
- **Undeclared:** the orchestrator has no ITF authorization at all, and inline code editing remains forbidden by Rule 2 and the read-scope rule exactly as today — **prose-enforced, as it already is**.
- **Multi-declaration chaining** (writing successive declarations to touch 2+ files in one logical fix) is a **prose violation, not a mechanically blocked one**. It is detectable after the fact: each declaration produces a task-log line, and the commit trailer records the true file count.

This is deliberately weaker than "impossible to abuse". A determined agent that skips the declaration is a rule-compliance problem of the kind the project already lives with everywhere else; solving it would require reliable actor detection, which is not available (see `design.md § Actor detection`). What ITF adds is that the *cooperative* path is bounded, measurable, and auditable — strictly better than the status quo, in which no inline fix is permitted or measured at all.

## Non-goals / Out of scope

- Relaxing any `[Unamendable]` constitutional constraint. Unaffected.
- Relaxing the worktree HARD RULE. ITF changes **who types**, never **where**.
- Relaxing `component-change-governance.md` four gates or `bug-tracking.md` regression-test requirements.
- General orchestrator code exploration. Read-scope is narrowed to one declared file, not suspended.
- Constraining implementor subagents in any way.
- Detecting or preventing undeclared inline editing (explicitly out of scope — see Enforcement model).

## User stories

**US-1 — As the orchestrator**, when I have already been told the exact file and line of a one-line defect, I want to apply the fix myself so a 30-second change does not cost 30k tokens and a subagent round-trip.

**US-2 — As Helder**, I want an orchestrator that has entered the ITF lane to be mechanically prevented from exceeding its declared bounds, and I want every entry into the lane to leave an audit trail, so that lane usage is measurable even where it is not preventable.

**US-3 — As Helder**, I want every ITF commit to be greppable, so I can audit the real size distribution after ~20 uses and re-calibrate the bounds with evidence.

## Eligibility conditions (ALL must hold)

Any single miss → dispatch an implementor. No partial qualification.

| # | Condition | Enforcement |
|---|-----------|-------------|
| **C0** | A **declaration** exists in the worktree where the edit occurs, naming this file | Precondition — activates Guard 3 |
| **C1** | Exactly **1 file**, **≤ 5 changed lines** (as counted by the guard's upper-bound rule — see Vocabulary) | Hook, once declared |
| **C2** | Fix is **fully diagnosed** (see Vocabulary) | Declaration — auditable, not mechanical |
| **C3** | Target is **not** `.xaml` / `.xaml.cs` | Hook, once declared |
| **C4** | Target is **not** a governed component | Hook, once declared |
| **C5** | Target is **not** in the sequential-only registry (`MauiProgram.cs`, `AppShell.xaml(.cs)`, `AppDbContext.cs`, `*Migration.cs`, `GlobalUsings.cs`, `Directory.Build.props`, any `tasks.md`) | Hook, once declared |
| **C6** | Severity ≤ **Major**, **and** no regression test is mandatory per `bug-tracking.md` — see note below | Declaration — auditable |
| **C7** | Edit occurs in a **worktree on a task branch** | Hook (existing Guard 1) |
| **C8** | `dotnet build` (0 errors) + affected tests green **before** commit | Existing exit checklist |

### Note on C6 — the lane is mostly for non-test-bearing fixes

Rev 1 wrote C6 as "the mandatory regression test is written first and is itself ITF-sized". That is near-circular: a new test file is a *second* file (fails C1), and a new test method in an existing file usually exceeds 5 lines. **The honest consequence, now stated plainly: any bug requiring a mandatory regression test is dispatched, not ITF'd.** Per `bug-tracking.md` that means Critical always dispatches, and Major dispatches wherever testable (Service/ViewModel/Repository).

The lane's real population is therefore: Minor bugs, UI-only Major bugs whose verification is a documented manual E2E, and non-bug trivia (log-message typos, a wrong constant, a missing null guard already covered by an existing test). **BUG-050 remains ITF-eligible only if its verification is manual-E2E**; if a ViewModel unit test is mandated, it dispatches. Helder to confirm this narrowing is intended — it materially shrinks the lane versus rev 1.

### Worked classification examples

| Case | Verdict | Reason |
|------|---------|--------|
| Typo in a log message string | **ITF** | 1 file, 1 line, no test mandated |
| BUG-050 — `SelectArtist` omits `IsArtistLocked = true` | **ITF only if** manual-E2E verification | Otherwise C6 mandates a unit test → dispatch |
| BUG-051 — stale autocomplete results | **Dispatch** | Race + cancellation; multi-line, design-bearing (fails C1, C2) |
| BUG-034 — character counter duplicates | **Dispatch** | Shared counter = governed component (fails C4) |
| Adding a DI registration | **Dispatch** | `MauiProgram.cs` is sequential-only (fails C5) |
| Any Critical bug | **Dispatch** | C6 — mandatory regression test |

## Declaration lifecycle

| # | Transition | Actor | Trigger |
|---|-----------|-------|---------|
| 1 | absent → declared | Orchestrator | Writes `<worktree>/.itf-active` **and** the `task-log.md` line, before opening the file |
| 2 | declared → active | Guard 3 | First `PreToolUse` on the declared file |
| 3 | active → consumed | **Orchestrator** — deletes the marker as the final step of the ITF commit, before `/sln-commit` returns | Fix committed |
| 4 | any → expired | Guard 3 | `declared_at` older than 30 minutes; an expired declaration is treated as absent |
| 5 | any → abandoned | Orchestrator | Fix turns out to exceed bounds → delete the marker, dispatch an implementor |

Deletion in state 3 is the orchestrator's responsibility and is a completion gate for an ITF commit; the 30-minute expiry (state 4) is the safety net for a session that dies mid-fix.

**Known gap, accepted:** state 3 has no mechanical backstop inside the expiry window. If the orchestrator forgets to delete the marker, a second edit to the *same declared file* within 30 minutes is permitted without a new declaration. The blast radius is bounded by C1 (still one file, still ≤ 5 lines) and by the expiry, so this is accepted rather than engineered around. A marker older than its commit is a review finding, not a mechanical block.

## Acceptance criteria

- **AC-ITF-01** — Given an active declaration and a change of 1 file / ≤ 5 lines meeting C2–C8, when the orchestrator edits the declared file, then the edit is **permitted** (guard exits 0).
- **AC-ITF-02** — Given an active declaration, when an edit targets a file other than the declared one, then the hook blocks with a message naming C1 and instructing dispatch.
- **AC-ITF-03** — Given an active declaration, when the edit's changed-line count exceeds 5, then the hook blocks naming C1.
- **AC-ITF-04** — Given an active declaration naming a `.xaml` / `.xaml.cs` file, when the edit is attempted, then the hook blocks naming C3.
- **AC-ITF-05** — Given an active declaration naming a governed component, when the edit is attempted, then the hook blocks naming C4 and pointing at `component-change-governance.md`.
- **AC-ITF-06** — Given an active declaration naming a sequential-only registry file, when the edit is attempted, then the hook blocks naming C5.
- **AC-ITF-07** — Given the checkout is on `develop` or `main`, when an ITF edit is attempted, then the existing Guard 1 blocks it (ITF grants no worktree exemption).
- **AC-ITF-08** — Given an ITF fix is committed, when `git log --grep "Lane: ITF"` is run, then the commit appears with its file and line count in the trailer. *(Automated: shell assertion in the test suite.)*
- **AC-ITF-09** — Given the hook encounters an internal error, a malformed declaration, or unreadable JSON, when it evaluates any ITF condition, then it exits 0 (fail-open) and never breaks the workflow.
- **AC-ITF-10** — Given **no** declaration exists in the editing worktree, when any agent (orchestrator or implementor) edits a code file, then Guard 3 exits 0 and imposes no ITF bound. *(Combined with the worktree-local declaration path, this is what keeps implementors unconstrained.)*
- **AC-ITF-12** — Given an ITF commit, when its `Lane: ITF (N files, N lines)` trailer is read, then N is understood as **orchestrator-self-reported and not machine-verified**. Chaining detection relies primarily on the per-declaration `task-log.md` lines, which are written before each edit and cannot be retroactively consolidated; the trailer is a convenience index for the calibration review, not evidence.
- **AC-ITF-11** — Given a declaration whose `declared_at` is older than 30 minutes, when an edit to the declared file is attempted, then the guard treats the declaration as absent (exits 0, no ITF authorization).

## Calibration review

After ~20 commits carrying `Lane: ITF`, Helder reviews the observed line-count distribution via `git log --grep "Lane: ITF"` and decides whether C1's 5-line bound tightens or loosens, and whether C6's near-total exclusion of test-bearing bugs should be revisited. Evidence-driven, consistent with the project's other calibrations.

## Governance

Amendment to two `[HARD RULE]`s, not to any `[Unamendable]` constitutional constraint — Helder can approve. Per `CLAUDE.md § Amending These Rules`: rationale ✅, backward-compatibility note (no application code affected; rules + one hook script only) ✅, `amend:` commit prefix ✅, changelog entry with old rule / new rule / effective date ✅.

**Additionally, per `CLAUDE.md § Continuous Enhancement — Authorship`:** these are `.claude/rules/` edits, so Helder must read and review the *amended rule text itself* — approving this spec is not sufficient. This is an explicit rollout step, not an implicit one.
