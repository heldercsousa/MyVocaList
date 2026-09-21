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

## Helder's ruling (2026-09-21) — CONFIRMED REAL, and the fix direction is decided

> *"LoadFirstPageAsync must work pretty similar to LoadMoreAsync; I suppose the distinction between
> them is minimal. So, in general, they're the same thing. LoadFirstPageAsync is supposed to be loaded
> in the Load Page moment, triggering the 'loading' component (DevExpress has a component that isn't
> labeled as 'loading' but has this exact goal) until the records are finally retrieved from DB."*

This answers **both** open questions at once.

### 1. Reachability — settled

The bug is **not** downgraded to Minor. The folder stays. The two methods are the same operation over
the same paging state, so they were always capable of interleaving; only `DbLoadGate` was stopping it.

### 2. The fix — symmetry, not a bespoke lock

`LoadFirstPageAsync` adopts the **same pattern** as `LoadMoreAsync`: it takes the loading guard
(`_isLoading`, or whatever the symmetric implementation names it) and surfaces a **loading indicator**
for the duration of the fetch.

The race closes as a **byproduct**: once both paths honour the same guard, a first-page load cannot
reset `_currentPage` while a load-more is mid-flight, because it cannot start. **No new lock, no
cancellation token, no re-read-after-await is needed** — the three options originally offered are
superseded by "make the asymmetry go away", which is the better fix because it removes the cause rather
than defending against the effect.

> **Why the asymmetry existed at all:** `LoadMoreAsync` guards itself with `_isLoading`;
> `LoadFirstPageAsync` never needed a guard because `DbLoadGate` was serialising everything anyway. The
> gate was masking a missing guard. Removing the gate exposed it — which is the honest reading of this
> bug: READ-SCOPE did not *create* the defect, it **revealed** a pre-existing asymmetry the gate had
> been hiding.

### 3. Scope beyond the race — a user-visible improvement

The ruling adds a requirement the bug alone would not have: the first-page load must show a loading
indicator "until the records are finally retrieved from DB". Today a page load has no visible busy
state. So this is **not purely a bug fix** — it carries a UI behaviour change.

### Governance consequence `[HARD RULE]`

`CrudListView` is a **governed component** (`component-change-governance.md`) — a custom component with
many consumers. Adding a loading indicator to it requires **all four gates before any edit**:

1. Dedicated task + explicit MD3 review against m3.material.io.
2. **Consumer map** — every `<local:CrudListView` and every `CrudListViewModelBase`-derived ViewModel,
   produced by a Python walk (never `grep` — it has returned false zeroes in this repo).
3. **Per-consumer risk assessment** — one line per consumer: what could break, and the verification step.
4. **Helder's approval recorded** before implementation begins.

Helder has approved the *direction*; the four gates still have to be run, and the MD3 review and
per-consumer risk table do not yet exist. This may **not** be bundled into a bug-fix commit
(`component-change-governance.md`: no bundling, `[HARD RULE]`).

### Regression test — still mandatory

Severity remains **Major**, so `bug-tracking.md` requires a regression test seen to FAIL before and PASS
after: force `LoadFirstPageAsync` while a `LoadMoreAsync` is parked mid-await, and assert the resulting
page sequence. The symmetry fix must be proven to close the interleaving, not merely to look tidier.

### Spec debt to clear in the same work

`requirements.md` must record the paging-serialisation responsibility. Its absence is the root cause:
the role lived only in a code comment, so the gate's removal dropped it silently and no reviewer caught
it. See the parent spec's lifted Phase 4.7 note for the same lesson.
