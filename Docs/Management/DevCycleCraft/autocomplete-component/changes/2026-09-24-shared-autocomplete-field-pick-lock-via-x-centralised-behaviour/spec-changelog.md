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
