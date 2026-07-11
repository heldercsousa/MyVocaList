# Autocomplete Component Rebuild — Requirements

> **Task:** README.md § 3 — "Build the new MD3-compliant autocomplete component" (nested under BACKLOG ②).
> **Scope:** component only. Wiring into `PersonFormPage`/`SongFormPage` is the next task (README.md § 4)
> and is explicitly out of scope here.
> **Predecessor decisions this requirements doc builds on:**
> - `findings.md` — evaluation of the current `AutocompleteField` (what to keep vs. fix).
> - `README.md § 2a` — DevExpress `AutoCompleteEdit` route rejected; extend the hand-rolled component.

---

## User stories

1. **As a phone user filling `PersonFormPage`'s Full Name field**, when I start typing, I want the
   suggestion list to take over the full screen (not a cramped overlay that the keyboard covers), so I
   can read and select a suggestion comfortably.
2. **As a tablet/desktop user**, I want the existing exposed-dropdown behavior to stay exactly as it is
   today — it already works correctly for that window class.
3. **As a user who opens the full-screen suggestion view and changes my mind**, I want to cancel/back out
   without a selection and have the field behave exactly as it does today on blur-without-selection
   (BUG-008 fix preserved).

## Acceptance criteria

- **AC-1 (Phone render):** On a device where `IDeviceInfo.Idiom == DeviceIdiom.Phone`, focusing/tapping
  the field's input pushes a full-screen modal (`AutocompleteMobileField`) instead of showing the
  existing overlay.
- **AC-2 (Desktop/Tablet render unchanged):** On `DeviceIdiom.Desktop` or `DeviceIdiom.Tablet`, the
  existing `DXBorder` exposed-dropdown overlay renders exactly as before this change — no regression.
- **AC-3 (Auto-focus):** `AutocompleteMobileField` auto-focuses its input and raises the keyboard
  immediately on `OnAppearing`.
- **AC-4 (Data flow parity):** Typing in `AutocompleteMobileField`'s input drives the same
  `SearchCommand` the host `AutocompleteField` already exposes; results populate from the same
  `ItemsSource`; no new ViewModel-side state is introduced.
- **AC-5 (Selection):** Tapping a result row in `AutocompleteMobileField` invokes the existing
  `SelectedItemCommand`, then pops the modal — identical net effect to selecting a row in the desktop
  overlay today.
- **AC-6 (Cancel-without-selection parity):** Backing out of `AutocompleteMobileField` without a
  selection invokes `BlurredWithoutSelectionCommand`, preserving BUG-008 behavior for both consumers.
- **AC-7 (No `SearchAppBar` dependency):** `AutocompleteMobileField`'s input row is styled to visually
  match `SearchAppBar` (transparent background, `ClearIconVisibility=Auto`, `ReturnType=Search`) by
  copying those constants — it does not reference or modify `SearchAppBar` itself (avoids triggering the
  four-gate governance process on a second governed component).
- **AC-8 (No DevExpress `AutoCompleteEdit`):** The rebuild does not introduce `DevExpress.Maui.Editors
  .AutoCompleteEdit` or any related provider type, per the logged exception in
  `.claude/exception-registry.md` (2026-07-11 entry).
- **AC-9 (MD3 terminology):** Code, XAML element names, and comments use MD3-official vocabulary —
  **Search Bar** (the field as docked) → **Search View** (the expanded phone takeover) — not "overlay
  card" / "suggestions overlay".
- **AC-10 (Existing behavior preserved):** Debounce timing (`AutocompleteDebouncer.cs`), the two-way
  `Text` bindable property + feedback-loop guard, `HasError`/`ErrorText` forwarding, and `ListItem`-based
  result rows are unchanged and continue to pass their existing tests.

## Validation rules

- Idiom detection must go through an injected `IDeviceInfo` (constructor/DI), never a static
  `DeviceInfo.Current.Idiom` call inside the component — matches the existing `IDeviceInfo` DI
  convention (`FeedbackService`) and keeps the branch unit-testable.

## Out of scope

- Wiring `AutocompleteMobileField` into `PersonFormPage` or `SongFormPage` (README.md § 4, next task).
- Any change to `SearchAppBar`, `CrudListView`, or `ListItem` themselves — only visual constants and the
  existing `ShimmerView`/dual-`EmptyState` pattern are referenced/copied, not the components.
- Updating `.claude/library/ux-patterns.md` / `m3-components.md` (README.md § 5, comes after this and the
  first real application).
- Width-breakpoint-based responsive detection (Windows/WinUI3 is out of scope per CLAUDE.md Stack — no
  target currently needs it; `DeviceIdiom` is sufficient for Android/iOS).
