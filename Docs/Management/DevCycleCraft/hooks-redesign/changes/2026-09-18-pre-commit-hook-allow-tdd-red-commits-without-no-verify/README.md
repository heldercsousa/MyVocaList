---
id: pre-commit-hook-allow-tdd-red-commits-without-no-verify
title: "Pre-commit hook: allow TDD Red commits without --no-verify"
status: 💡 Pending
target: 2026-09-18
section: DevCycleCraft
parent: hooks-redesign
goal: Teach the pre-commit hook to skip the test gate when a commit is a declared TDD Red, so a deliberately failing test can be committed without bypassing the hook wholesale.
gate: Helder approves the Red-marker mechanism; a Red commit passes the hook unaided and a genuinely broken commit still fails it.
kind: change
---

# Pre-commit hook: allow TDD Red commits without --no-verify

Teach the pre-commit hook to skip the test gate when a commit is a declared TDD Red, so a deliberately failing test can be committed without bypassing the hook wholesale.


## Why this exists

The pre-commit hook runs the test suite. A TDD **Red** commit contains a test that fails *by design* —
that failing test IS the deliverable, and it is the mandatory fail-before evidence for any
Critical/Major bug fix (`bug-tracking.md § Regression-test requirement`). So the hook blocks exactly
the commit the methodology requires.

This first bit during READ-SCOPE Wave 1 (commit `5110b088`, BUG-078 Red), which used `--no-verify`
**without pre-authorisation** — disclosed in the task-log and logged in `.claude/exception-registry.md`.

## Options put to Helder (2026-09-18)

| Option | Trade-off |
|--------|-----------|
| **A. Allow `--no-verify` for Red commits only**, with a required `Lane: RED` commit trailer | Keeps Red and Green as separate commits, so fail-before evidence lives in git history. The bypass becomes a legitimate, greppable lane rather than an undisclosed act. |
| **B. No bypass — squash Red+Green into one commit** | Hook always green, but the fail-before evidence survives only in the task-log, not in git. Rejected: it destroys the discriminating artifact. |
| **C. Teach the hook to recognise the Red marker and skip the test gate itself** | Same guarantee as A with no bypass flag at all. More work up front; safest long-term. |

## Decision

**Helder authorised A now, C later (2026-09-18).** Option A is the interim policy so READ-SCOPE Wave 3
could proceed the same day; **this item is C**, the durable fix, deferred to its own task.

## Scope when picked up

- Hook reads the incoming commit message; on a `Lane: RED` trailer it skips the *test* gate only —
  the build gate and every other check still run.
- A commit without the trailer whose tests fail must still be rejected (negative test required).
- On landing, retire Option A from `workflow.md § Rule 3` and close the exception-registry entry.
