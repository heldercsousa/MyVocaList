---
id: BUG-079
title: DbLoadGate removal dropped its _currentPage serialisation role
status: 💡 Pending
severity: Major
target: 2026-09-19
section: BusinessFeatures
parent: read-model-notracking-guidelines
goal: Gate removal accounted for only one of the gate's two documented responsibilities; LoadFirstPageAsync can now reset _currentPage while a LoadMoreAsync is in flight, risking a duplicated or skipped page.
gate: Helder rules whether this is a real regression; if so a guard is added and a regression test proves the interleaving.
kind: bug
---

# DbLoadGate removal dropped its _currentPage serialisation role

Gate removal accounted for only one of the gate's two documented responsibilities; LoadFirstPageAsync can now reset _currentPage while a LoadMoreAsync is in flight, risking a duplicated or skipped page.


## How this was found

Raised by the **final independent verifier** on the READ-SCOPE change, reviewing task 8.1
(`DbLoadGate` removal). It is explicitly labelled **unproven, not demonstrated wrong** — no failing
test exists yet. Registered proactively (`bug-tracking.md`: register before fixing) so the analysis is
not lost, and because it is a **newly introduced** risk rather than a pre-existing one.

## The defect

`DbLoadGate` had **two** responsibilities. The removal commit accounted for one of them.

The deleted comment block documented the second explicitly:

> *"Read the page number AFTER the gate: a first-page load (search/refresh) holding the gate may reset
> `_currentPage` before this load-more runs."*

Wave 8's evidence covers only the `Task.Run` offloads (R3, `page-load-frozen`) — which did correctly
survive. The serialisation role was not carried forward.

After removal, in `MyVocaList/UI/ViewModels/CrudListViewModelBase.cs`:

- `_isLoading` guards `LoadMoreAsync` **against itself only**.
- `LoadFirstPageAsync` has **no `_isLoading` guard at all**, and writes `_currentPage = 1`.
- So a debounced search or a refresh can interleave with an in-flight load-more, producing a
  **duplicated or skipped page**.

Previously the gate serialised the two paths, so nothing else had to.

## Why it is NOT a spec violation by the implementor

`requirements.md` REQ-UOW-29/47/48 describe the gate purely as a captive-`DbContext` workaround and
mandate its removal once every read is scoped. **The spec never records the gate's second, paging-
serialisation responsibility** — only the code comment did. The implementor followed the spec as
written and produced the evidence the spec asked for.

This is therefore a **spec gap**, which `workflow.md` says must be escalated rather than improvised:

> *Spec incomplete → stop and clarify with Helder; do not improvise.*

No guard was invented, and no concurrency fix was attempted, deliberately.

## Why it is filed Major rather than Minor

`CrudListViewModelBase` is the shared base for **every** CRUD list in the app, so the exposure is
broad, and the user-visible symptom (a row duplicated or a page silently skipped while scrolling and
searching) has no workaround beyond re-triggering the search. Filed **Major** so it gets a folder to
hold this analysis. **If Helder judges the interleaving impractical in the real UI, downgrade to
Minor** — the folder is then a mechanical validation error and must be removed
(`bug-tracking.md`: a `severity: Minor` folder aborts regeneration).

## What Helder needs to decide

1. **Is the interleaving reachable in practice?** `LoadFirstPageAsync` runs from a debounced search and
   from pull-to-refresh; whether either can fire while a load-more is genuinely in flight is a UI-
   timing question the verifier could not settle from source alone.
2. **If yes** — where does the guard belong? Options: extend `_isLoading` to cover
   `LoadFirstPageAsync`; re-read `_currentPage` after the await in `LoadMoreAsync`; or a cancellation
   token on the superseded load. This is an architecture decision, not an implementation detail.
3. **Either way**, `requirements.md` should record the gate's second responsibility so this cannot be
   lost again — the fact that it lived only in a code comment is the root cause here.

## Regression test requirement

Per `bug-tracking.md`, a **Major** bug's regression test is MANDATORY where testable. This is
ViewModel-level and therefore testable: force `LoadFirstPageAsync` to reset `_currentPage` while a
`LoadMoreAsync` is parked mid-await, and assert the resulting page sequence. Red before, green after.

> **Do not fix this by restoring `DbLoadGate`.** The gate's captive-`DbContext` rationale is genuinely
> dead — every read is now scoped, proven by task 7.2's clean tree-wide census and two independent
> verifiers. Only the paging-serialisation role needs a replacement, and it should be solved directly.
