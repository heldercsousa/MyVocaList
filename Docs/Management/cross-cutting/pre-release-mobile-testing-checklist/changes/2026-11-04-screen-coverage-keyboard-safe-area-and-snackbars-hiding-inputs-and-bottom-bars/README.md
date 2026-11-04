---
id: screen-coverage-keyboard-safe-area-and-snackbars-hiding-inputs-and-bottom-bars
title: Screen coverage — keyboard, safe area and snackbars hiding inputs and bottom bars
status: 💡 Pending
target: 2026-11-04
section: DevCycleCraft
parent: pre-release-mobile-testing-checklist
goal: Make sure the keyboard, system bars and success/error snackbars never hide what the user is typing or the list bottom bar's buttons; test every page and settle the SafeAreaEdges rule.
gate: "Later, not now — Helder: test every page on device first; the always-Container SafeAreaEdges rule is suspected wrong."
kind: change
---

# Screen coverage — keyboard, safe area and snackbars hiding inputs and bottom bars

Make sure the keyboard, system bars and success/error snackbars never hide what the user is typing or the list bottom bar's buttons; test every page and settle the SafeAreaEdges rule.


## Helder's notes (2026-11-04) — reminder detail, nothing decided or scheduled

**1. Snackbar covers the CRUD list's bottom floating bar.** The success/error message shown after a form
save appears on top of the list's bottom floating toolbar and blocks its buttons until it disappears.
Need a way to avoid that blocking (e.g. raise the snackbar above the bar, or dismiss it on tap).

**2. On-screen keyboard hiding entries** — "a big UX pitfall". If the keyboard covers the entry being
typed in, the user cannot see their text and must collapse the keyboard to check it, then reopen it to
fix it ("an UX nightmare"). **Every page must be tested manually on device** for this.

**3. Keyboard hiding other UI.** An entry near the top that opens the keyboard can still hide other
elements lower down, e.g. a list's bottom floating toolbar or its search results, forcing the user to
collapse the keyboard to see them. Applies whatever `SafeAreaEdges` value the page uses.

**4. `SafeAreaEdges` rule.** In MAUI 10 each page chooses what to stay clear of: `"Container"` = system
bars/notch only (the keyboard may cover the page); `"All"` = system bars **and** the keyboard (the page
shrinks so the focused field stays visible) — as understood 2026-11-04, unverified on device.
- CLAUDE.md § Constitutional Constraints says every `ContentPage` uses `"Container"`. Helder believes
  this rule is **a mistake** for form pages; "All"/"Container" were never actually tested.
- State on 2026-11-04 (census of `MyVocaList/**/*.xaml`): 11 files use `"Container"`; 3 forms use
  `"All"` — `ArtistFormPage`, `PersonFormPage`, `VenueFormPage` (probably by accident, per Helder).
  `SongFormPage` was `"All"` until review fix `04ef869b` (2026-06-09) changed it to `"Container"`.
- **Interim decision: keep `"All"` on those 3 forms** and test them. Logged in
  `.claude/exception-registry.md` (2026-11-04) so the deviation is not silent.
- When settled: amend the CLAUDE.md rule through the `amend:` procedure, and decide whether
  `SongFormPage` goes back to `"All"`.

Not urgent: there will be many chances to observe this while testing. No deep dive until then.
