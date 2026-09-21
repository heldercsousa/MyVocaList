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

## Investigation findings + Helder's design decisions (2026-09-21)

### The loading component already exists — this is EXTENDING a pattern, not introducing one

`CrudListView.xaml:37-38` already wraps the `DXCollectionView` in a **`dx:ShimmerView`** bound
`IsLoading="{Binding IsInitialLoading}"`, with six `SkeletonBone`-styled rows sized to match `ListItem`
(`MaterialStyles.xaml:231-232`). It is shared by all four CRUD pages. Three other pages
(`ArtistPickerPage`, `SongPickerPage`, `YouTubeSearchPage`) use the same shimmer against a plain
`IsLoading`.

Confirmed against DevExpress docs for the pinned **25.2.4** (`Directory.Packages.props:52-58`):
`ShimmerView` is `DevExpress.Maui.Controls.ShimmerView`, with the `IsLoading` and `LoadingView`
properties this code uses. `dx:` maps to `http://schemas.devexpress.com/maui`.

> **Governance correction.** An earlier note in this session claimed the four gates of
> `component-change-governance.md` would be required. **They are not engaged** if the work changes only
> `CrudListViewModelBase` and leaves `CrudListView.xaml` untouched — the component, the binding and the
> skeleton already exist. Should the implementation end up editing `CrudListView.xaml` after all, the
> four gates apply in full and this correction is void.

### The precise gap

`IsInitialLoading` is set **only** by `InitializeAsync` (`CrudListViewModelBase.cs:107,110`), and only
on the "never loaded before" branch. `LoadFirstPageAsync`, `ReloadAsync` (`:178`) and `RefreshAsync`
(`:170-175`) all bypass it.

And the asymmetry Helder predicted is exact: **`LoadFirstPageAsync` never reads or writes `_isLoading`
at all.** The field (`:12`, `volatile`) is touched only by `LoadMoreAsync` — read `:182`, set `:188`,
cleared `:222` in `finally`. `_currentPage` is written `:122` (first page) and `:206` (load-more, on
success only), and read `:193`.

### Decision 1 — shimmer on **every** `LoadFirstPageAsync` call

Helder chose the literal reading of his ruling over the narrower options:

| Path | Shimmer |
|------|---------|
| `InitializeAsync` (page load) | yes |
| `ReloadAsync` (filter change) | yes |
| search debounce (`:240`) | **yes** |
| `RefreshAsync` (pull-to-refresh) | **yes** |

Plus the `_isLoading` guard on all four paths, which is what closes this bug.

> **Two consequences were shown to Helder in the option preview and accepted deliberately** — they are
> NOT oversights, and a reviewer must not "fix" them without asking:
> 1. the skeleton will **flash during search typing**, once per debounce;
> 2. pull-to-refresh will show **two indicators** — the shimmer plus `DXCollectionView`'s own
>    `IsRefreshing` spinner.
>
> If either reads badly on device, that is a **follow-up UX decision for Helder**, not a licence to
> silently narrow the scope back.

**Open design question for the implementor:** whether to drive the existing `IsInitialLoading` flag
from all four paths (its name then becomes misleading — it is no longer "initial") or to introduce a
correctly-named flag and rebind `CrudListView.xaml`. **The second option edits the governed component
and therefore triggers the four gates.** Prefer reusing the existing flag and renaming only if the name
becomes actively wrong; either way `CrudListViewModelBaseTests.cs:82-107` already asserts
`IsInitialLoading` transitions and must stay green.

### Decision 2 — centralise the result cap as a constant

A second inconsistency surfaced in the same call chain:
`PersonService.SearchPersonsStartsWithAsync` defaults to `maxResults = 3` (`:213`) while
`ArtistService.SearchArtistsByNameAsync` (`:172`) and `PersonService.SearchPersonsAsync` (`:196`) use
`5` — and `PersonFormViewModel.cs:287` passes an explicit `5`, **silently overriding the service's own
default of 3**.

Helder's ruling: add a `SearchConstants` result-cap constant (value **5**), use it at all three service
defaults, and drop the redundant call-site override. Consistent with "no magic numbers anywhere".
