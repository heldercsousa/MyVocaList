---
id: BUG-074
title: Pre-commit test gate blocks the RED-first commit that bug-tracking mandates
status: 💡 Pending
severity: Major
target: 2026-08-04
section: DevCycleCraft
parent: hooks-redesign
goal: The pre-commit hook refuses any commit containing a failing test, but a Critical bug's regression test must be committed before its fix. The two rules cannot both be obeyed.
gate: Found during UOW Phase 0; Helder authorised the no-verify bypass for RED commits as an interim workaround.
kind: bug
---

# Pre-commit test gate blocks the RED-first commit that bug-tracking mandates

The pre-commit hook refuses any commit containing a failing test, but a Critical bug's regression test must be committed before its fix. The two rules cannot both be obeyed.

## The conflict

Two project rules are individually correct and jointly unsatisfiable.

| Rule | Source | What it demands |
|------|--------|-----------------|
| Failing-test-first | `.claude/rules/bug-tracking.md § Regression-test requirement per severity` `[HARD RULE]` | For a **Critical** bug: *"MANDATORY — failing test first (Red), then fix (Green). No exceptions."* A fix is not complete until its regression test has been **seen** to fail before and pass after. |
| Pre-commit test gate | `.claude/settings.json` pre-commit hook | Runs `dotnet build` + `dotnet test` whenever a `.cs` file is staged and **aborts the commit if any test fails**. |
| No hook bypass | `CLAUDE.md § Harness` / commit discipline | *"Never skip hooks (`--no-verify`) … unless the user has explicitly asked for it."* |

A RED-first commit **is by definition a commit whose test suite is failing**. The gate cannot
distinguish "this commit deliberately adds a failing test that encodes a not-yet-fixed defect" from
"this commit broke something." So the only ways to satisfy the gate are to (a) bypass it, (b) not
commit the RED test separately at all, or (c) weaken the test until it passes — and (c) is silent
spec deletion (`testing.md § Builder Must Not Modify Tests`).

## How it surfaced

UOW Phase 0 (2026-08-04), whose entire deliverable is failing tests proving BUG-068/BUG-071 exists
before the unit-of-work refactor removes it. Two separate implementor subagents independently hit the
gate and both resorted to `git commit --no-verify`:

- `1963af4b` — `test(uow): RED — BUG-068 tracking conflict on create->read->update (REQ-UOW-03)`
- `756ed4a3` — `test(uow): RED — REQ-UOW-22/24 nested chain is not atomic (partial state survives)`

Both bypasses were **retroactively authorised by Helder 2026-08-04** as the interim workaround, and
both agents disclosed the bypass rather than hiding it. That disclosure is the correct behaviour and
should stay correct — the point of this item is that they should not have needed the bypass.

## Why the interim workaround is not good enough

`--no-verify` disables the gate **wholesale**, not selectively. A RED-first commit made this way also
skips the build check and every other pre-commit guard, including the constitutional guard. An agent
that has been told "use `--no-verify` for RED commits" has been handed a general-purpose escape hatch
for exactly the situation where discipline matters most. The bypass is also indistinguishable in
`git log` from a careless one unless the commit body says so.

## Proposed direction (not yet decided)

Teach the gate to permit precisely the RED case and nothing else. Sketch:

1. The gate parses the staged diff for **newly added** test methods carrying an `// [AC] REQ-…` tag
   (the traceability marker already mandated by `testing.md § Acceptance Criteria Traceability`).
2. It re-runs the suite and collects the failing test IDs.
3. The commit is allowed **iff every failing test is one of the newly-added `[AC]`-tagged tests** —
   i.e. nothing that previously passed is now failing. Any pre-existing test that broke still aborts.
4. The commit message must carry a `Red: <REQ-ID>[, …]` trailer naming the ACs, so `git log --grep "^Red:"`
   audits every RED commit, and a later GREEN commit can be matched against it.

This keeps the build check and every other guard active, removes the need for `--no-verify` entirely,
and makes "a failing test was committed on purpose" a *machine-checkable, greppable* fact instead of
a prose convention.

**Open question for the fix:** whether the gate should also verify the RED test's failure *reason*
(both `bug-tracking.md` and the UOW plan require that a test fail for its stated reason, since a test
that goes red for the wrong reason is not evidence). That is likely beyond a pre-commit hook's remit
and may belong in the task-log/review lane instead.

## Files to change when this is picked up

- `.claude/settings.json` — the pre-commit hook definition
- the hook script it invokes
- `.claude/rules/bug-tracking.md` — document the `Red:` trailer as part of the RED-first workflow
- `.claude/rules/workflow.md § Rule 3` — add the trailer to the Bug Fix Pattern commit template

Amending the two rules files follows `CLAUDE.md § Amending These Rules` (`amend:` prefix + changelog).

