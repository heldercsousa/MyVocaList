---
id: apply-the-unit-of-work-pattern-to-queue-and-event-entities-deferred
title: Apply the unit-of-work pattern to Queue and Event entities (deferred)
status: 💡 Pending
target: 2026-08-04
section: DevCycleCraft
parent: read-model-notracking-guidelines
goal: Queue and Event code is excluded from the unit-of-work rollout pending their own full refactor, so they keep using the session-lifetime context and stay exposed to the tracking-conflict defect.
gate: Starts only once the pattern is established in the guides; the six embedded repository saves live here.
kind: change
---

# Apply the unit-of-work pattern to Queue and Event entities (deferred)

Queue and Event code is excluded from the unit-of-work rollout pending their own full refactor, so they keep using the session-lifetime context and stay exposed to the tracking-conflict defect.

