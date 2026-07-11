# AutocompleteField Component Evaluation — Findings

> **Task:** BACKLOG ② — AutocompleteField Component Evaluation (done first per README.md § 0
> dependency-inversion decision, 2026-07-11).
> **Method:** Evaluation steps 2.2.2–2.2.7 from `README.md`. No production code changed — this is
> research output only.

---

## 2.2.2 — Current implementation: what works / what fails

Files read: `MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml`,
`AutocompleteField.xaml.cs`, `AutocompleteDebouncer.cs`.

### What works

- **Debounce logic** (`AutocompleteDebouncer.cs:26-48`) — clean `CancellationTokenSource` cancel/reissue
  pattern, extracted to a plain class for unit testability. Has a passing test file
  (`MyVocaList.Tests/Unit/Components/AutocompleteFieldDebounceTests.cs`).
- **Two-way `Text` bindable property** (`AutocompleteField.xaml.cs:29-39`) with a feedback-loop guard
  (`if (ctrl.searchEdit.Text != newVal)`) — correct pattern for a wrapped editor.
- **`HasError`/`ErrorText` forwarding** to the inner `TextEdit` (`:13-19`) — correct composition, no
  duplicated validation display logic.
- **`BlurredWithoutSelectionCommand`** (`:106-114`, `:163-171`) — lets the ViewModel restore/clear state
  on blur without a selection; this is the fix for BUG-008 referenced in `SongFormViewModel.cs:156`.
- **Result-row reuse**: the `DataTemplate` at `AutocompleteField.xaml:54-60` already uses the shared
  `ListItem` component (`Headline`/`SupportingText`) rather than a bespoke row.
- **Min-2-characters gate** (`:144-149`) avoids firing search on 0–1 char input.

### What fails (measured against the early guideline)

- **Desktop-only render pattern, unconditionally.** `AutocompleteField.xaml:32-62` implements exactly
  one visual pattern: a `DXBorder` overlay card anchored below the field (`Margin="0,56,0,0"`,
  `ZIndex="10"`) — the classic **Exposed Dropdown Menu** the guideline explicitly calls "a fundamentally
  *desktop* pattern." No window-size-class branch, no full-screen expansion. `MaximumHeightRequest="280"`
  (`:50`) leaves very little room once the keyboard consumes 40–50% of a phone screen — the exact
  failure mode the guideline warns about.
- **No auto-focus behavior.** The guideline's "Strategic Opportunity" requires the surface to auto-focus
  and raise the keyboard the instant it opens (for bottom-sheet/modal placement). Nothing in
  `AutocompleteField.xaml.cs` sets focus programmatically.
- **No keyboard-avoidance handling** — no code adjusts layout when the keyboard appears.
- **Original design doc names the anti-pattern directly.** `persons/autocomplete-design.md:22` states
  the implemented pattern is the **"MD3 Docked Search Bar"** — a real MD3 pattern, but the desktop-style
  behavior the new guideline says should be abandoned for compact/phone window classes. Predates the
  guideline (2026-03-30 vs 2026-07-11), never mobile-UX-reviewed against it.
- **No responsive/window-size-class awareness anywhere** — an entirely absent axis of behavior, not a
  bug in existing behavior. Desktop-correct today; phone branch simply doesn't exist.
- **MD3 terminology drift.** Code/design doc say "overlay card" / "suggestions overlay"; MD3 canon uses
  **SearchBar → SearchView** and **Menus (filtering)** (CLAUDE.md § MD3 terminology).

---

## 2.2.3 — Existing internal guidance

- `persons/autocomplete-design.md` + `persons/plan-autocomplete.md` — read in full. Internally
  consistent with shipped code, but commit to the desktop-style pattern with no mobile branch.
  `plan-autocomplete.md` documents the DX editor choices (`TextEdit`+`DXBorder`+`DXCollectionView`)
  without evaluating whether a purpose-built DevExpress autocomplete editor existed — one did (§ 2.2.4).
- Grep of `.claude/library/*.md`: no autocomplete *pattern* guidance in `crud-pages.md`, `ux-patterns.md`,
  `dialogs-validation.md`, or `m3-components.md`. Only incidental mentions in
  `component-safety-gate.md` (governed-component list only), `bug-tracking-reference.md`,
  `testing-reference.md`. Confirms no authoritative internal mobile-autocomplete guideline exists yet.

---

## 2.2.4 — DevExpress MAUI built-in autocomplete component

**Finding: DevExpress MAUI 25.2.4 already ships a purpose-built autocomplete editor —
`DevExpress.Maui.Editors.AutoCompleteEdit` — that the current implementation does not use.**

Confirmed via Context7 (`/websites/devexpress_maui`; installed version confirmed 25.2.4 in
`Directory.Packages.props:72-75`). The `mcp__devexpress-maui__*` demo-app search tools returned **no**
matches for "autocomplete"/"ComboBoxEdit"/"AutoComplete" — treat as an MCP-tool coverage gap (its
indexed demo-app repo doc/code search didn't surface it), not evidence DevExpress lacks the feature.

- **`AutoCompleteEdit`** — "a text editor that provides suggestions as the user types"
  (docs.devexpress.com/MAUI/404621/editors/editor-types). Ships with `FilteredItemsSourceProvider`
  ("provides suggestions... in sync mode," plus async loading per the "Configure an Auto-Complete
  Editor" example at docs.devexpress.com/MAUI/404573/editors/examples).
- **Native full-screen popup support.** DevExpress popup positioning docs
  (docs.devexpress.com/MAUI/404628/.../popup/positioning): *"If `PlacementTarget` is not explicitly set,
  a popup will appear as a modal window... cover the entire screen."* The full-screen-expansion behavior
  the guideline requires for compact/phone may be achievable by *configuring* the built-in popup rather
  than hand-building a separate overlay + a second full-screen view.
- **`ComboBoxEdit.PickerShowMode`** — an adjacent DX editor exposes explicit picker-mode configuration;
  worth checking whether `AutoCompleteEdit` has an equivalent before assuming feature parity.

**Version-currency caveat:** Context7 did not return a version-pinned `/25.2.x` sub-path this round.
Installed version is confirmed 25.2.4. Before the rebuild task starts, re-verify `AutoCompleteEdit`/
`FilteredItemsSourceProvider` directly against the installed 25.2.4 assembly (spike), not solely the
Context7 snippets.

**Implication:** the DevExpress-first constitutional rule applies with more force than previously
assumed — the current `AutocompleteField` is a hand-rolled reimplementation of something DevExpress
already ships, not just "hand-rolled MAUI."

---

## 2.2.5 — CRUD-list component reuse assessment

Read: `SearchAppBar.xaml(.cs)`, `Lists/ListItem.xaml(.cs)`, `CrudListView.xaml(.cs)`.

- **`ListItem`** — **directly reusable, already reused.** `leadingSlot`/`trailingSlot` are `IsVisible=False`
  by default (`:16-20, 51-55`) — already slot-first, nothing shown unless populated. No baked-in select
  checkbox (that lives at `DXCollectionView.SelectionMode`/`SelectedItemTemplate` in `CrudListView.xaml:49-55`,
  not in `ListItem`). Confirms README § 2.2.5.1. **No changes needed for autocomplete reuse.**
- **`SearchAppBar`** — **partially reusable, governed component (four-gate change required if modified).**
  Leading back-button (`:21-26`, out of scope), flat `TextEdit` search input (`:29-45` — the reusable
  part: transparent bg, `ClearIconVisibility="Auto"`, `ReturnType="Search"`), up to 3 trailing action
  buttons (`:47-73`, out of scope). Its input styling matches the guideline's top-docked phone input
  exactly. **Any actual modification to `SearchAppBar` must go through the four-gate process as its own
  dedicated task** — the rebuild could instead copy the styling constants without touching the component,
  avoiding governance overhead.
- **`CrudListView`** — not reusable as a whole (FloatingToolbar, FAB, multi-select `ConfirmSheet`,
  pagination all out of scope), but its `ShimmerView` loading-skeleton (`:27-40`) and dual `EmptyState`
  pattern ("no items" vs "no results", `:62-73`) are worth carrying into the new full-screen list states.

---

## 2.2.6 — Render-approach decision for the 3 contexts

| Context | Compact/phone | Large/desktop |
|---|---|---|
| **Bottom sheet** | Bypass the sheet entirely on autocomplete engagement → full-screen view (guideline's Final Recommendation). | Not an expected context on desktop in this app; if it occurs, exposed-dropdown can render inside the sheet. |
| **Page input** (`PersonFormPage`/`SongFormPage` today) | Full-screen dedicated view on tap/focus: search-style AppBar at top, input docked at bottom next to keyboard, results fill the middle (Google Maps/Gmail/Android Settings pattern). | Keep today's exposed-dropdown — **this is the one case where the current component is already correct.** |
| **Modal** | Bypass the modal surface, go full-screen (guideline downgrades modal dialogs on mobile). | Exposed-dropdown inside the modal is acceptable — no keyboard-obscuring problem on desktop. |

**Conclusion:** one component, one branch point — a single window-size-class check selecting between
(a) existing docked/exposed-dropdown (large/desktop, reuse as-is) and (b) new full-screen search-view
(compact/phone, new build). The hosting context (sheet/page/modal) only changes what must be
dismissed/replaced first, not the rendering decision itself.

---

## 2.2.7 — Full consumer map (component-change-governance gate 2)

Grepped `xmlns:autocomplete=` / `autocomplete:AutocompleteField` across all `*.xaml` in the repo
(excluding stale `.claude/worktrees/*` / `.worktrees/*` copies from prior parallel-agent runs):

| Consumer page | Field | ViewModel wiring |
|---|---|---|
| `MyVocaList/UI/Pages/People/PersonFormPage.xaml:19-` | Full Name (dedup search) | `PersonFormViewModel.cs`: `SearchPersonsCommand` (:69,271-282), `SuggestionSelectedCommand` (:70,284-) |
| `MyVocaList/UI/Pages/Songs/SongFormPage.xaml:24-` | Artist (`ArtistSearchText`) | `SongFormViewModel.cs`: `ArtistSuggestions` (:53), `SearchArtistsCommand` (:154,275-282), `SelectArtistCommand` (:155,288-), `ArtistBlurredWithoutSelectionCommand` (:156-157,302-311, BUG-008 fix) |

**No other consumers found** — README's suspected list confirmed complete by grep, not memory.

---

## Recommendation: Adjust and rebuild on top of `AutoCompleteEdit` — do not blindly remove/replace, do not keep hand-rolling as-is

**Preserve:** the debouncer pattern (pending FilteredItemsSourceProvider async evaluation), the `Text`
two-way BP + feedback-loop guard, `HasError`/`ErrorText` forwarding, `BlurredWithoutSelectionCommand`
(BUG-008 fix), and the `ListItem`-based result row.

**Adjust/rebuild:**
1. Evaluate replacing the hand-built overlay with `AutoCompleteEdit` + `FilteredItemsSourceProvider` —
   needs a spike against the installed 25.2.4 assembly first.
2. Add the missing compact/phone full-screen-expansion branch — the single biggest functional gap.
   Desktop exposed-dropdown is already correct and should be kept for large/desktop.
3. Add auto-focus-on-open for whichever hosting surface precedes the full-screen takeover.
4. Rename internal vocabulary to MD3-official terms (SearchBar/SearchView, Menus-filtering).
5. Any `SearchAppBar` reuse goes through the four-gate governance process as its own dedicated task.

**Rationale:** failures are the *absence* of one behavior (responsive full-screen expansion) plus a
newly discovered constitutional gap (DevExpress-first not honored in 2026-03). Everything else
(debounce, BP composition, error forwarding, ListItem reuse, BUG-008 fix) is sound and should carry
forward. Adjust-and-extend, not a rewrite from scratch — but significant enough to warrant the full
rebuild task already planned in README § 3.

---

## MD3-terminology / version-currency issues found

- Terminology drift: "overlay card"/"suggestions overlay" vs. MD3-official SearchBar→SearchView,
  Menus (filtering).
- Pattern-name drift: `persons/autocomplete-design.md:22` names "MD3 Docked Search Bar" — real MD3 term,
  applied without a mobile-responsive branch.
- DevExpress version-pin caveat: installed 25.2.4 confirmed via `Directory.Packages.props`; Context7
  results weren't from an explicit `/25.2.x` path — re-verify against the installed assembly before build.
- `mcp__devexpress-maui__*` demo-app MCP returned no hits for autocomplete-related queries — MCP tool
  coverage gap, not a DevExpress capability gap (Context7 against official docs found it).
