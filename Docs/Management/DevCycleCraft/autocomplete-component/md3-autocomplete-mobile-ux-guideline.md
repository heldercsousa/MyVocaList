# MD3 Autocomplete — Mobile UX Guideline (Early / Expected-Behavior Reference)

> **Status:** Early guideline — registered *as is* to anchor the expected behavior for the
> **AutocompleteField Component Evaluation** task (see `README.md` step 2.2.1).
> **Source:** Research by Gemini 3 Pro, gathered from the official Material Design 3 documentation
> (m3.material.io). Captured 2026-07-11.
> **Not yet the authoritative internal guideline.** The authoritative version lives in
> `.claude/library/ux-patterns.md` and is only written *after* the proven-concept work
> (evaluation → new component → first application) validates it — see `README.md` step 5.
> This file is the input to that process, not its output.

---

## Part 1: The Single-Input Form Architecture

**Objective:** Minimize user friction for a single data-point entry while maintaining context.

### Analysis of Form Placement Options

The instinct to avoid a dedicated page is correct. The cognitive and interactive cost of navigating
to a completely new screen, waiting for the transition, and finding a "back" button is
disproportionately high for entering a single piece of data.

However, before defaulting to a bottom sheet, consider the full spectrum of mobile UI patterns:

- **Dedicated Page**
  - *Verdict:* Strongly discouraged.
  - *Implication:* High friction; breaks user flow and context. Only justified if that single input
    requires immense cognitive load or precedes a highly complex flow.

- **Modal Dialog (MD3 Basic Dialog)**
  - *Verdict:* Acceptable, but ergonomically flawed.
  - *Implication:* Dialogs center on the screen. On modern, tall smartphones, they force the user to
    adjust their grip to reach the input. Furthermore, when the virtual keyboard appears, it can
    obscure the dialog or force an awkward layout shift.

- **Bottom Sheet (Modal Bottom Sheet)**
  - *Verdict:* Highly effective, with necessary caveats.
  - *Implication:* Highly ergonomic for one-handed use (thumbs operate near the bottom of the screen)
    and maintains visual context by dimming the background rather than replacing it.
  - *The Risk:* When the virtual keyboard is triggered, the bottom sheet must dynamically slide up. If
    not handled seamlessly by the framework, it creates a jumpy, squished UI.
  - *Strategic Opportunity:* To truly achieve "low friction," the bottom sheet must be programmed to
    automatically focus the input field and trigger the keyboard the moment it opens. If the user has
    to tap a button to open the sheet, and then tap again to focus the input, the UX has failed to
    minimize effort.

- **Inline Editing (The Counterpoint)**
  - *Verdict:* The lowest possible friction.
  - *Implication:* Instead of opening a new surface entirely, consider an inline text field that simply
    expands upon tapping an "Add" icon directly on the page. This eliminates surface transitions
    completely.

---

## Part 2: Autocomplete Patterns on Mobile (MD3)

**Objective:** Implement a filtering/selection mechanism that survives the severe spatial constraints
of mobile devices.

### Context & MD3 Coverage

Material Design 3 defines autocomplete primarily through its **Menus** (specifically the "Filtering"
pattern) and the **SearchBar** component.

### The Mobile UX Problem

A classic "Exposed Dropdown Menu" (where the input sits on the page and a menu drops down below it) is
fundamentally a *desktop* pattern. On a mobile device, when the user taps an input field, the virtual
keyboard consumes roughly **40% to 50%** of the screen from the bottom up.

If the input is located in the middle of the screen, the space remaining for the "dropdown" is often
just enough for 1 or 2 items. The user is forced to painstakingly scroll through a tiny, cramped
window. This is highly frustrating and prone to misclicks.

### The MD3 Benchmark Solution

To make autocomplete mobile-friendly, standard dropdowns should be abandoned in favor of the
**Search Bar to Search View (Full-Screen Expansion)** pattern.

- **The Trigger:** The user interacts with a standard input or SearchBar on the page.
- **The Transition:** Upon tapping the input, the component morphs into a full-screen view (or a modal
  bottom sheet that expands to the maximum available screen height).
- **The Layout:** The input field docks to the absolute top of the screen. The keyboard is anchored to
  the bottom. The entire space between them is dedicated exclusively to the autocomplete list.

### Real-World Benchmarks

This exact MD3 interaction pattern is executed in these standard apps:

- **Google Maps (Mobile):** Tap the "Search here" bar. It immediately abandons the map view and opens
  a dedicated full-screen list for recent and autocomplete suggestions. The keyboard takes the bottom,
  the list takes the rest.
- **Gmail (Mobile):** Tapping the search bar at the top transitions the UI into a full-screen
  history/autocomplete view.
- **Android OS Settings:** Searching for a specific device setting uses this identical full-screen
  expansion.

---

## Final Recommendation

For a standard single-input entry, utilize an **auto-focusing Modal Bottom Sheet**. However, if that
single input relies on autocomplete, **bypass the bottom sheet entirely and use a component that
expands into a full-screen view.** This guarantees the user has the necessary visual real estate to
evaluate their options without fighting the virtual keyboard.

---

## Responsive nuance to encode (from BACKLOG row ①)

The rule is **responsive by window size class**, not phone-only:

- **Large / desktop window class** → keep the desktop-like exposed-dropdown autocomplete (it must
  remain as-is on desktop screens).
- **Compact / phone window class** → full-screen dedicated view: entire page used + search AppBar +
  the filter/input term docked at the very bottom of the screen (next to the keyboard) + the rest of
  the screen area lists the search results.

MD3 currency to re-check inline against m3.material.io during the evaluation: **SearchBar → SearchView**
and **Menus (filtering)**.
