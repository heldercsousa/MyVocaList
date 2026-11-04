---
id: dev-and-qa-data-tooling-seed-and-empty-app-data-on-demand
title: Dev and QA data tooling — seed and empty app data on demand
status: 💡 Pending
target: 2026-11-04
section: DevCycleCraft
parent: large-volume-data-stress-test
goal: "Post-MVP idea to discuss: give developers and QA a way to fill the app with seed data or empty it, per build or test context, so testing needs no manual data entry."
gate: Post-MVP — Helder opens the discussion; nothing is designed or built before that.
kind: change
---

# Dev and QA data tooling — seed and empty app data on demand

Post-MVP idea to discuss: give developers and QA a way to fill the app with seed data or empty it, per build or test context, so testing needs no manual data entry.


## Helder's framing (2026-11-04)

- Seed data saves developers and QA from entering many records by hand; which data is needed depends on
  the feature under test.
- QA builds (e.g. a deployment to QA) may also want an automatic seed.
- The opposite is just as useful: dev and QA sometimes need to test with **no data at all**, so
  "empty the app's data" is a first-class need alongside "seed lots of data".
- More seeds will come later — out of scope until this is discussed.
- **Post-MVP.** Not designed, not scheduled. The interim bridge for the one existing seed
  (`20260211055225_venuesSeedForTest`) is BUG-080's debug-only guard.
- Related: the parent item's 1–2-year seed for the stress test would be one consumer of such tooling.
