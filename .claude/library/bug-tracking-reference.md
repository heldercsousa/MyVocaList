# Bug Tracking — Reference

> Extracted from `.claude/rules/bug-tracking.md` (2026-07-05, rules-file-refactoring Task 03). The rule file is now a routing table; this file holds the full detail. Discovered via the `myvocalist-coding` skill map or the rule's routing table.
> Language rule: English only — see `CLAUDE.md § Constitutional Constraints`.

`workflow.md § Bug Fix Pattern` defines how to *commit* a bug fix (commit message as spec). This file defines how bugs are *tracked* before and during that fix: where they live in BACKLOG, when they earn a task-log entry, how they are classified by severity, and what regression test each class requires.

---

## Bug ID scheme

- Bugs use a sequential ID: `BUG-001`, `BUG-002`, … (continue from the highest existing ID in BACKLOG.md — never reuse).
- The ID is assigned at registration time and used in commit subject, BACKLOG row, and task-log entry.

> Rationale: a stable ID lets the commit, the backlog row, and any regression test cross-reference one bug without ambiguity.

---

## BACKLOG nesting — bugs live under their parent feature

A bug must be registered as a nested row under the business feature it affects, not as a free-floating top-level item.

```markdown
### Artists & Songs
- BUG-010 🟡 In Progress — SongForm Save silently swallows exception (Critical)
- BUG-011 💡 Pending — Artist autocomplete does not clear on blur (Minor)
```

- A cross-cutting bug (no single parent feature) goes under a `### Cross-cutting` heading in BACKLOG.
- Register the bug BEFORE starting the fix (proactive triage — `workflow.md` Rule 1).

> Rationale: nesting keeps a feature's defect history with the feature, so its true health is visible at a glance instead of scattered across a flat list.

---

## Severity classification

| Severity | Definition | Examples |
|----------|-----------|----------|
| **Critical** | Data loss, silent data corruption, crash, or security exposure | Save swallows exception; double-tap crash; DB write lost |
| **Major** | Core feature unusable or wrong result, no workaround | Search returns wrong matches; navigation dead-ends |
| **Minor** | Cosmetic, edge-case, or has an easy workaround | Misaligned padding; autocomplete blur-clear glitch |

> Rationale: severity drives both fix priority and the regression-test requirement below — without classification both are guessed.

---

## When a bug gets a task-log entry

| Severity | Task-log entry required? | Spec artifact |
|----------|--------------------------|---------------|
| **Critical** | Yes — full entry (Changed files + Verification evidence + AC traceability if it exposed a missing AC) | Commit message as spec + new AC in `requirements.md` if a behavior gap is revealed |
| **Major** | Yes — entry with Changed files + Verification evidence | Commit message as spec |
| **Minor** | No task-log entry required; commit message is the only artifact | Commit message as spec |

> Rationale: high-severity fixes carry regression risk that future agents must be able to trace; trivial fixes do not warrant the ceremony (matches `workflow.md` Bug Fix Pattern's "no spec" allowance).

---

## Regression-test requirement per severity — HARD RULE

A bug fix is not complete until its regression test exists, has been seen to FAIL before the fix and PASS after (`testing.md § Regression tests`).

| Severity | Regression test |
|----------|-----------------|
| **Critical** | MANDATORY. Write the failing test first (Red), then fix (Green). No exceptions — a Critical fix without a regression test is incomplete. |
| **Major** | MANDATORY where the bug lives in a testable layer (Service, ViewModel, Repository). UI-only Major bugs not unit-testable: document the manual E2E verification step in the task-log instead. |
| **Minor** | Optional. Add a regression test only if the bug is likely to recur or the fix is non-obvious. |

> Rationale: Critical/Major bugs by definition slipped past existing tests — a regression test is the only proof the same defect cannot return silently.

---

## Bug fix workflow (summary)

1. **Register** — assign `BUG-NNN`, classify severity, add nested BACKLOG row under the parent feature.
2. **Regression test first** (Critical/Major) — write the failing test, confirm Red.
3. **Fix** — minimal change; confirm test Green.
4. **Commit** — use `workflow.md` Bug Fix Pattern message; subject includes `BUG-NNN`.
5. **Task-log** — Critical/Major only; include Changed files + Verification evidence.
6. **BACKLOG** — mark the bug row resolved (✅) in the same session.

> Rationale: a fixed sequence prevents the common failure of fixing the symptom and leaving the bug untracked, untested, and still "open" in the backlog.

---

> **Authorship note:** This file must be human-reviewed before it is relied upon (CLAUDE.md § Continuous Enhancement — Authorship).
