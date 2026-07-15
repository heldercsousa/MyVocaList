# UX Analysis — Autocomplete "No Match → Add New" Affordance

> **Task:** BACKLOG DevCycleCraft row *Autocomplete "no match → add new" UX analysis* (registered by Helder 2026-07-15).
> **Status:** Analysis complete — recommendation ready for Helder review. Analysis-only: no code changed.
> **Scope:** shared solution for every `AutocompleteField` consumer (today: PersonFormPage Full Name, SongFormPage Artist).
> **Author:** Claude (orchestrator session 2026-07-15), research-backed per sources below.

## 1. Problem statement

In the mobile full-screen Search View (`AutocompleteMobileField`), the only way to proceed when the
user does NOT want to tap a suggestion (typed a new, unmatched name) is the **back button**. "Back" as
the commit gesture for "keep my text and add a new person/artist" is an anti-affordance:

- Back universally signals *cancel/leave*, not *confirm* (Nielsen H4 — consistency and standards).
- The user gets no visibility that backing out preserves the typed text (H1 — visibility of system status).
- On a genuine no-match, the results area is blank — a dead end with no explanation or next action.

## 2. Options considered

| # | Option | Verdict |
|---|--------|---------|
| A | Keep back-only, add helper text explaining it | Rejected — documents the anti-pattern instead of fixing it (H4 still violated). |
| B | Keyboard "Done"/submit action commits the text | Keep as complement — standard IME affordance, but invisible; cannot be the only path. |
| C | **Persistent `Add "<typed text>"` action row as the last item of the suggestion list** | **Recommended** — the canonical Material "creatable autocomplete" pattern. |
| D | Confirm FAB / bottom action button in the Search View | Rejected — competes with the keyboard for bottom space; no MD3 precedent inside a search view. |
| E | No-results empty state with an "Add" button only when zero matches | Insufficient alone — the "typed name is a partial match of an existing name but still a new person" case (e.g. typing "Helder S" when "Helder Sousa" exists) would still have no Add path. Folded into C as the zero-results presentation. |

## 3. Recommendation (Option C + E, complemented by B)

**An explicit `Add "<typed text>"` action list item, always present while the typed text is a valid
candidate (≥ min search length), rendered as the LAST row of the suggestions list — and as the sole
row (with empty-state copy) when there are zero matches.**

Anatomy (MD3 list item, reuse the app's `ListItem` component — no new component):

- **Leading element:** `+` add icon (`person_add` / `library_add` variant per consumer is optional; generic `add` is acceptable).
- **Headline:** `Add "{typed text}"` (e.g. `Add "Helder Sanches"`).
- **Supporting text (zero-match case only):** "No existing singer matches" / consumer-supplied copy.
- **Behavior on tap:** dismiss the Search View exactly like today's back-without-selection path
  (`Cancelled` → `BlurredWithoutSelectionCommand`), keeping the typed text in the field. No new
  ViewModel contract is strictly required for v1 — the row makes the existing behavior *visible and
  intentional* instead of hidden behind back.
- **Back button:** unchanged (still cancels/keeps text) — the new row removes back's role as the only path.
- **Desktop exposed-dropdown variant:** same row appended to the overlay list (pattern is identical in
  MUI/Angular Material desktop autocompletes), keeping both idioms consistent.
- **IME action (complement):** keyboard submit ("Done") should behave like tapping the Add row.

### Why this is the right pattern (evidence)

1. **Creatable autocomplete is the established Material pattern:** Material UI's Autocomplete
   documents the *Creatable* free-solo mode as "a last option, for instance: `Add "YOUR SEARCH"`" —
   an explicit Add row appended to the suggestion list ([MUI Autocomplete](https://mui.com/material-ui/react-autocomplete/)).
   Angular Material autocompletes use the same idiom; Google Contacts' "Create new contact" row in
   its search flow is the canonical first-party example.
2. **MD3 empty-state guidance:** a no-results search state must never be blank — it should explain
   why and offer the next step ([Material empty states](https://m1.material.io/patterns/empty-states.html),
   carried forward in MD3 search guidance at [m3.material.io/components/search/guidelines](https://m3.material.io/components/search/guidelines)).
   The Add row *is* that next step.
3. **Nielsen heuristics:** restores H1 (the system shows what will happen), H4 (back means back),
   H5 (prevents the "typed text silently kept/lost?" ambiguity), H6 (recognition over recall).
4. **MD3 search view suggestion lists explicitly support action rows** (leading/trailing icons,
   supporting text) — see the suggestion `ListItem` usage in Google's M3 SearchBar docs
   ([developer.android.com — search bar](https://developer.android.com/develop/ui/compose/components/search-bar)).

### Touch/accessibility notes

- Row height ≥ 48dp (existing `ListItem` already complies — `ux-patterns.md § Touch Targets`).
- The Add row must be visually distinguished from data rows (icon + optionally `Primary` color on the
  headline) so it is never mistaken for an existing entity (error prevention).
- Empty state (zero matches) keeps the vertically-centered layout rule (`ux-patterns.md § Empty State Positioning`)
  for the explanatory block, with the Add row directly actionable.

## 4. Implementation constraints (for the future implementation task — NOT this task)

- `AutocompleteField`/`AutocompleteMobileField` are **governed components** — implementation requires
  its own dedicated task passing all four gates of `component-change-governance.md` (consumer map,
  per-consumer risk, Helder approval). This analysis satisfies the MD3-review input of Gate 1.
- Consumer-facing surface: likely two new optional bindable properties (`AddNewText` template /
  `AddNewCommand`) with a safe default (fall back to current behavior when unset) so SongFormPage and
  PersonFormPage adopt it independently.
- Interacts with the *shimmer/empty-state evaluation* BACKLOG row (2026-07-12) — the zero-match
  presentation should be specified once, jointly, when that evaluation runs.
- BUG-046 (whitespace normalization) affects when "zero matches" occurs — land BUG-046 first so the
  Add row isn't shown against falsely-empty results.

## 5. Sources

- https://m3.material.io/components/search/guidelines (search view layout, suggestions/results)
- https://m1.material.io/patterns/empty-states.html (empty-state guidance, carried into M3)
- https://mui.com/material-ui/react-autocomplete/ (Creatable / free-solo `Add "…"` row)
- https://developer.android.com/develop/ui/compose/components/search-bar (M3 suggestion ListItem anatomy)
- Nielsen Norman heuristics via `ux:interaction-design` skill references
