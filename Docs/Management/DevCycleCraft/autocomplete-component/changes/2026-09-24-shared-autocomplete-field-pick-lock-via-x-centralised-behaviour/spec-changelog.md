# Spec changelog: AutocompleteDXField (pick-lock via X, centralised behaviour)

Post-approval changes (spec approved by Helder on 2026-09-25). The spec is not shipped, so each change
is made in place and listed here.

## 2026-11-05: T7 run 1 outcome, Q3 reversed

- **Trigger:** T7 on-device gate run 1 (Helder, Android). Lines 1, 9 and 10 failed, line 8 was
  accepted as designed, and line 4 is moot. Details: `task-log.md` § T7.
- **Ruling:** Q3 ("pick-lock on everywhere, no opt-out") is reversed. A field that holds the
  principal entity's own data (Person Full Name) must never lock; foreign-entity fields (Song Artist)
  keep the lock.
- **requirements.md:**
  - New REQ-ACF-29 (`IsPickLockEnabled`).
  - REQ-ACF-01 and -02 qualified by pick-lock enabled; REQ-ACF-15 says the Person form never locks.
  - REQ-ACF-18 adds deferred focus. REQ-ACF-24 says programmatic text closes the menu and resets
    `SelectedItem`. REQ-ACF-26 adds stale-request discard after a pick.
  - Goal, US-1, the domain vocabulary and the failure modes updated.
  - § Helder rulings — 2026-11-05 added.
- **design.md:**
  - § 1 pick-lock bullet updated.
  - § 3.1: the `IsPickLockEnabled` row added, the `IsLocked` row updated, and the event-glue rules
    added (`TextChanged` reason, pick counter, deferred focus).
  - § 4: `Mode` and `LockAfterPick` gain the parameter.
  - § 6 flows and state machine, § 7 INV-6/INV-7, § 10 test strategy.
- **tasks.md:** T7 run 1 note, and the new T8 (fixes + opt-out).
- **plan.md:** note on the "pick-lock always on" constraint.
- 2026-11-05 (later): spec re-review PASS; should-fixes applied (REQ-ACF-24 ItemSelected vs ProgrammaticChange split, -29(c) read-only exception, -18 traceability row, T7 line 4 reworded, T8 owns its task-log section).

## 2026-11-06: T7 run 2 outcome

- **Trigger:** T7 run 2 (Helder, Android). Line 2 failed (" a" / "  a" shows a menu) and line 9
  failed (after a pick the field keeps focus, the keyboard stays up, the cursor is at 0). Details:
  `task-log.md` § T7, "Run 2".
- **Rulings:** search text uses the app-wide `NormalizeSearchQuery`; line 8 confirmed as the
  app-wide pattern; after a pick the field drops focus (Helder chose option (b)).
- **requirements.md:** REQ-ACF-13 amended (a/b/c); new REQ-ACF-30; § Helder rulings — 2026-11-06;
  traceability rows.
- **design.md:** § 3.1 event-glue rules after T7 run 2; § 4 `SearchQuery`, `ShouldRequestOptions`,
  `ShouldForwardBlur(…, isPickBlur)`.
- **tasks.md:** T7 run 2 note and the new T9.

## 2026-11-09: T7 run 3 outcome, emulator root causes

- **Trigger:** T7 run 3: line 2 FAIL, line 9 partial. An emulator diagnostic run proved RC-A/B/C (`task-log.md`).
- **requirements.md:** § Helder rulings — 2026-11-09 (F1 "after a pick" half fixed here; line 9 cursor PASS).
- **design.md:** § 3.1 event-glue rules after the emulator diagnostic run (they supersede the T9 keystroke clear).
- **tasks.md:** T7 run 3 note and the new T10.

## 2026-11-10: T7 run 4 outcome, no "no results" row

- **Trigger:** T7 run 4: lines 2-4 PASS; "aa" / "aaa" with no match show DX's "Not found" row.
- **Ruling:** the row must never show, in any context (Helder). Scope: component + Person form; `SongFormPage` excluded.
- **requirements.md:** new REQ-ACF-31; § Helder rulings — 2026-11-10; traceability row.
- **design.md:** § 3.1 event-glue rule after T7 run 4; § 4 `ShowsMenu`.
- **tasks.md:** T7 run 4 note and the new T11.
