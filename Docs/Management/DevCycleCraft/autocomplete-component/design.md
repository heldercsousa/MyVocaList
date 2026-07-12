# Autocomplete Component Rebuild — Design

> Approved by Helder 2026-07-11. Covers README.md § 3 only (component build); § 4 (apply to a consumer)
> is a separate, later task. See `requirements.md` for ACs this design must satisfy.

---

## 1. Architecture

> **Design updated 2026-07-11:** `IDeviceInfo` is not constructor-injected as originally stated —
> `AutocompleteField` has no DI resolution path (it's instantiated by the XAML compiler in consumer
> pages, confirmed by its parameterless constructor). Implemented instead via a service-locator seam
> (`IPlatformApplication.Current.Services.GetService<IDeviceInfo>()`, defaulted in the constructor,
> overridable via an internal settable `DeviceInfo` property) plus a MAUI-runtime-free static helper
> `AutocompleteWindowClass.IsCompactWindow(IDeviceInfo)` for unit testability — mirroring the existing
> `AutocompleteDebouncer` extraction pattern in the same folder. See `plan.md` Task 1/Task 3.

`AutocompleteField` (the existing, desktop-correct component) gets exactly one new decision point: on
focus/tap, check the injected `IDeviceInfo.Idiom`.

- **`DeviceIdiom.Phone`** → push a new **`AutocompleteMobileField`** modally (full-screen), passing
  through the existing `ItemsSource`, `SearchCommand`, `SelectedItemCommand` bindable properties and the
  current `Text` as the initial query. On row tap or cancel, the page pops itself.
- **`DeviceIdiom.Desktop` / `DeviceIdiom.Tablet`** → unchanged: today's `DXBorder` exposed-dropdown
  overlay stays exactly as-is (`AutocompleteField.xaml:32-62`).

**Naming:** the new full-screen component is named **`AutocompleteMobileField`** — deliberately mirroring
`AutocompleteField`'s name (desktop-default component ↔ its phone-specific counterpart), rather than a
generic `AutocompleteSearchPage` name.

No new interface is introduced (rejected the `IAutocompleteSource<T>` alternative — no current consumer
needs the extra abstraction). No `SearchAppBar` reuse (avoids a second governed-component gate). No
DevExpress `AutoCompleteEdit` (per the logged exception in `.claude/exception-registry.md`).
`IDeviceInfo` is injected the same way `FeedbackService` already does it — DI singleton
(`MauiProgram.cs:163`), mockable in tests exactly like `FeedbackServiceTests`.

## 2. Components

| Component | Change |
|---|---|
| `AutocompleteField.xaml.cs` | Add an idiom check (`_deviceInfo.Idiom == DeviceIdiom.Phone`) in the existing focus/tap handler; branch to `PushModalAsync(new AutocompleteMobileField(...))` on Phone instead of showing the overlay. Existing bindable properties (`Text`, `ItemsSource`, `SearchCommand`, `SelectedItemCommand`, `HasError`/`ErrorText`, `BlurredWithoutSelectionCommand`) are unchanged. |
| `AutocompleteMobileField.xaml(.cs)` **(new)** | Top row: back/cancel `ImageButton` + `dx:TextEdit` styled with `SearchAppBar`'s visual constants (transparent background, `ClearIconVisibility="Auto"`, `ReturnType="Search"`) — copied literals, not a component reference. Auto-focuses the input in `OnAppearing`. Middle: `CollectionView` of `ListItem` rows bound to the same `ItemsSource`; reuses the `ShimmerView`/dual-`EmptyState` ("no items" vs. "no results") pattern from `CrudListView.xaml:27-40,62-73` for loading/empty states. Row tap → invoke `SelectedItemCommand`, then `PopModalAsync()`. |
| `AutocompleteDebouncer.cs` | Unchanged — reused as-is by the host `AutocompleteField`; `AutocompleteMobileField` never debounces independently, it only renders what already flows through `SearchCommand`. |

> **Design updated 2026-07-11:** The shimmer/empty-state pattern described above was NOT implemented in
> the phone `AutocompleteMobileField` build (Task 2) — the shipped `AutocompleteMobileField.xaml` uses a
> bare `DXCollectionView` with no loading shimmer and no empty-state view. This is a scope deferral, not
> a design rejection: the component has no consumer yet (README.md § 4's consumer-wiring task is
> out of scope for this branch), so loading/empty behavior cannot be meaningfully demonstrated or tested
> until a real consumer drives it. **Must be addressed as part of the consumer-wiring task** — either
> implement the `ShimmerView`/dual-`EmptyState` pattern as originally specified, or make and document a
> deliberate decision not to. Flagged by the final whole-branch code review.

## 3. Data flow

Same one-way-in/one-way-out flow that exists today, just re-hosted on phone:

```
User types → AutocompleteField.Text (two-way BP) → debouncer → SearchCommand (host VM)
   → host VM populates ItemsSource → (phone: shown in AutocompleteMobileField;
                                       desktop/tablet: shown in existing overlay, unchanged)
Row tap → SelectedItemCommand (host VM) → [phone: PopModalAsync also fires]
```

No new state lives in the ViewModels; `PersonFormViewModel` / `SongFormViewModel` are untouched by this
task (they are only exercised, unmodified, by whichever consumer picks this component up next).

## 4. Error handling

`HasError` / `ErrorText` forwarding stays on `AutocompleteField` itself — the field remains present
under/behind the modal on phone, so its own error UI is still the single source of truth.
`BlurredWithoutSelectionCommand` (BUG-008 fix) fires on modal cancel/back on phone, matching today's
blur-without-selection semantics exactly (AC-6).

## 5. Testing

- `AutocompleteDebouncer` — unchanged, existing tests (`AutocompleteFieldDebounceTests.cs`) untouched.
  Level A, already covered.
- New idiom-branch logic — a small, pure `IsCompactWindow` check off the injected `IDeviceInfo` →
  unit-testable with `Mock<IDeviceInfo>` (same pattern as `FeedbackServiceTests.cs:15,61-62`). **Level A**
  given it's a user-facing behavior fork (testing.md risk tiers).
- `AutocompleteMobileField` UI rendering itself — MAUI page rendering isn't practically unit-tested in
  this project. **Level C / manual** — covered by the per-consumer manual E2E steps below, to be executed
  once this component is wired into a real consumer (README.md § 4).

## 6. Governance — component-change-governance gates

`AutocompleteField` is a governed component (2+ consumers). All four gates for this change:

### Gate 1 — dedicated task + MD3 review

This design doc is the dedicated task. **MD3 review (m3.material.io):** the full-screen phone view maps
to MD3's **Search View**, expanded from a **Search Bar** — top input row, results list beneath, no dialog
chrome, no title bar. The desktop/tablet path keeps the existing Menu-style filtered-dropdown pattern
(`AutocompleteField`'s current overlay is close to MD3's Menu pattern applied to a text field). Both
branches use MD3-official vocabulary in code/comments going forward — **Search Bar / Search View / Menu
(filtering)** — not "overlay card" / "suggestions overlay" (AC-9).

### Gate 2 — consumer map

Already established in `findings.md § 2.2.7` (confirmed by grep, not memory):

| Consumer | Field |
|---|---|
| `PersonFormPage.xaml:19-` | Full Name (dedup search) |
| `SongFormPage.xaml:24-` | Artist (`ArtistSearchText`) |

No other consumers exist. This task changes `AutocompleteField`'s internals but not its public bindable
properties, so both consumers require no XAML/binding changes of their own — only re-verification.

### Gate 3 — per-consumer risk assessment

| Consumer | What could break | Verification |
|---|---|---|
| `PersonFormPage` (Full Name field) | Dedup-search suggestions currently render inline (desktop-style overlay); on a phone they now appear full-screen via `AutocompleteMobileField` — a visible behavior change, though the data/command wiring (`SearchPersonsCommand`, `SuggestionSelectedCommand`) is untouched. | Manual E2E on an Android phone emulator: type a partial name, confirm the suggestion list appears full-screen and selecting a row populates the field identically to today's overlay behavior. |
| `SongFormPage` (Artist field, `ArtistSearchText`) | Same rendering change as above; additionally must confirm `ArtistBlurredWithoutSelectionCommand` (BUG-008 fix, `SongFormViewModel.cs:156-157,302-311`) still fires correctly when the user backs out of the full-screen view without selecting. | Manual E2E: open the artist search, back out without selecting, confirm the field clears/restores per BUG-008 behavior; repeat the flow with an actual selection to confirm normal-path parity. |

### Gate 4 — Helder approval

Recorded: Helder approved this design 2026-07-11, with one required change (component named
`AutocompleteMobileField`, not `AutocompleteSearchPage`) — applied above.

## 7. Out of scope (see requirements.md)

Wiring into `PersonFormPage`/`SongFormPage`, any change to `SearchAppBar`/`CrudListView`/`ListItem`
themselves, the `ux-patterns.md`/`m3-components.md` guideline update, and Windows-style width-breakpoint
detection are all out of scope for this task.
