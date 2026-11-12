# Design: AutocompleteDXField, the shared autocomplete field (pick-lock via X, centralised behaviour)

> **Status: approved by Helder 2026-09-25; shipped and closed 2026-11-11 (T7 run 6 PASS, Rebuild Test 0-1 divergence fixed). Shipped spec: immutable; later changes go in a dated `changes/` folder beside it. Post-approval changes: `spec-changelog.md`.** Rulings: requirements § Helder rulings.
> Requirements: `requirements.md` (REQ-ACF-*). Every current-behaviour claim cites `file:line` on
> `develop`. DevExpress version pinned from `Directory.Packages.props`:
> **DevExpress.Maui.Editors 25.2.4** (central package management). MAUI Controls 10.0.41.
> **Scope:** the component + the `PersonFormPage` migration. `SongFormPage` is built on the component
> by the song-form title-first item, not here.

## 1. Architecture

Affected layers: **UI only**, plus one new constant in Domain. Services are unchanged.

```
PersonFormPage.xaml ── <textfields:AutocompleteDXField …bindings only…/>
                              │  owns: items-request glue, pick routing, pick-lock + X,
                              │  blur forwarding, SelectedItem reset, menu-open, debounce +
                              │  threshold, search on/off, option template, icon styling
                              ▼
                      dxe:AutoCompleteEdit (DX 25.2.4, hosted inside the component)
                              │
PersonFormViewModel ◄─────────┘  commands + properties (existing) + NameOptionSource (new)
        │
IPersonService.SearchPersonsStartsWithAsync   (unchanged)
```

- **Business logic stays in the ViewModel/Service** (REQ-ACF-12). The component forwards to commands.
- **Pick-lock is component-owned** and on by default. Ruling Q3 ("always on") was reversed on
  2026-11-05: `IsPickLockEnabled = false` turns it off for a field holding the principal entity's
  own data (the Person form, REQ-ACF-29). While it is enabled, `IsLocked` is a TwoWay
  BindableProperty: the component sets it `true` on a pick and `false` on X. A consumer may bind it,
  to read it or to set it programmatically (hydration), or leave it unbound.
- **No create concept** (Helder ruling Q1). Unpicked text is the consumer's business on Save.
- **Pure decision logic** lives in a static, UI-free class, unit-testable at Level A. The
  code-behind only adapts DX events to it.

## 2. Component name, location and files

### Decision: `UI/Components/TextFields/AutocompleteDXField` (Helder rulings Q4 + name, 2026-09-24)
**Chosen approach:** component name **`AutocompleteDXField`** (Helder's naming ruling), in a new
subfolder `MyVocaList/UI/Components/TextFields/`, following the MD3 component-category convention of
its siblings (`AppBars/`, `Lists/`, `Sheets/`, `States/`, `Toolbars/`). It implements the MD3/Material
**exposed dropdown menu** pattern (a text field with a menu of options under it; Material Components
Android `docs/components/Menu.md`), which is how it cross-references to MD3 documentation.
**Alternatives considered:**
- `AutocompleteDXField` (this spec's earlier proposal): replaced by Helder's ruling. "Autocomplete…"
  says what the field does, and "DX" marks it as built on DevExpress, distinct from the existing
  `AutocompleteField`.
- `Menus/` folder: rejected in review. It files what looks and behaves like a text field under menus.
- `AutocompleteField`: taken by the frozen family (§ 2.1).
**Reversibility:** Easily reversible while there is one consumer.
**Rationale:** Helder's ruling (name) and Q4 (location).

### Constitutional exception: component name (proposed text for `.claude/exception-registry.md`)
> **Constraint:** CLAUDE.md § Constitutional Constraints, "MD3 terminology": component names must
> use official MD3 terminology.
> **Exception:** the shared DevExpress autocomplete component is named `AutocompleteDXField`, not an
> MD3 term.
> **Why:** (1) MD3 has no "autocomplete" component; the closest pattern is the exposed dropdown menu
> (a text field + menu), which the component implements and names in its XML doc for
> cross-reference. (2) Project precedent: the existing `AutocompleteField` family already uses
> "Autocomplete". (3) Helder's naming ruling, 2026-09-24: "Autocomplete…" says what it does, and
> "DX" signals it is built on DevExpress, next to `AutocompleteField`.
> **Scope:** this component name only (class, files, `AutocompleteDXFieldLogic`,
> `AutocompleteDXFieldLogicTests`). Its BindableProperty names still follow MD3/DX/MAUI terms where
> they exist (`LabelText`, `PlaceholderText`, `IsReadOnly`, `IsDropDownOpen`, `HasError`, `ErrorText`).
> **Approved by:** Helder, 2026-09-24. **Filed by:** the orchestrator.

### 2.1 The frozen `UI/Components/AutocompleteField/` family

Left as is, per Helder (2026-09-24): this item does not touch, delete or reference it. REQ-DXAC-11
(retain it as reference) stands.

### Files (new)

| File | Role |
|------|------|
| `MyVocaList/UI/Components/TextFields/AutocompleteDXField.xaml` | `ContentView` hosting one `dxe:AutoCompleteEdit` + the option `ItemTemplate` (Headline + optional SupportingText, as in `PersonFormPage.xaml:32-42`). |
| `MyVocaList/UI/Components/TextFields/AutocompleteDXField.xaml.cs` | BindableProperties, DX event adapters. Thin: every decision calls `AutocompleteDXFieldLogic`. |
| `MyVocaList/UI/Components/TextFields/AutocompleteDXFieldLogic.cs` | `internal static` pure functions (§ 4). |
| `MyVocaList/UI/Models/IMenuOptionSource.cs` | Consumer search contract + `MenuOptionSource` delegate adapter (§ 3.2). |
| `MyVocaList.Tests/Unit/Components/AutocompleteDXFieldLogicTests.cs` | Level A tests. |

Conventions followed: the BindableProperty pattern with `…Property` statics and CLR wrappers, bound
via `Source={x:Reference self}` (`UI/Components/AppBars/SearchBar.xaml.cs:17-35`, `SearchBar.xaml:22-42`).
The test project references the MAUI head (`MyVocaList.Tests.csproj:33-34`), and new files in
`Unit/Components/` compile (only six named frozen tests are removed, `:38-39`).

## 3. Interfaces

### 3.1 Component surface (BindableProperties)

| Property | Type | Mode | Default | Purpose / REQ |
|----------|------|------|---------|---------------|
| `LabelText` / `PlaceholderText` | `string` | OneWay | `""` | Label and placeholder. |
| `Text` | `string` | **TwoWay** | `""` | Editor text (Person: `PersonName`). |
| `IsLocked` | `bool` | **TwoWay** | `false` | Picked state. Set `true` by the component on a pick and `false` on X; a consumer may also set it (REQ-ACF-01/02/06). Honoured only while `IsPickLockEnabled` is `true`. |
| `IsPickLockEnabled` | `bool` | OneWay | `true` | Per-field pick-lock switch (REQ-ACF-29). `false` → `Mode` is never `Locked`; a pick never writes `IsLocked`; a consumer-set `IsLocked` has no effect. propertyChanged re-runs `ApplyMode`. Person: `False`. Song Artist: default. |
| `IsReadOnly` | `bool` | OneWay | `false` | Page-set read-only state: text shown; no typing, no X, no search, no menu, no blur forwarding. **Wins over `IsLocked` and `IsSearchEnabled`** (REQ-ACF-12b). Hides the inherited name on purpose: `ContentView` has no `IsReadOnly`, and the name matches DX `EditBase.IsReadOnly` and MAUI `InputView.IsReadOnly`. Implemented by setting the inner editor's `IsReadOnly=true`, `ClearIconVisibility=Never` and `IsEndIconVisible=false`. |
| `SupportingText` | `string` | OneWay | `""` | Generic text under the field (REQ-ACF-12c). Passed to the DX editor's own `HelpText` (documented `EditBase.HelpText`, "assistive label" below the field, Context7 DX MAUI, 2026-09-25), so no extra Label is needed. The component sets `HelpText = BottomSupportingText(HasError, SupportingText)` (§ 4), which guarantees that the error wins whatever DX does when both are set. Named after the MD3 term *supporting text*, not the MD2 "helper text" or DX's `HelpText` (CLAUDE.md MD3-terminology rule). It is unrelated to `AutocompleteSuggestion.SupportingText` (an option's second line). |
| `IsSearchEnabled` | `bool` | OneWay | `true` | Page switch: `false` → no option requests, no menu; the rest of the field is unchanged (REQ-ACF-12a). |
| `HasError` / `ErrorText` | `bool` / `string` | OneWay | `false` / `""` | Passed to DX `HasError`/`ErrorText`. |
| `IsDropDownOpen` | `bool` | **TwoWay** | `false` | The BUG-065/066 lever. Forced `false` unless `Mode == Editable` and search is on (REQ-ACF-04, -12a, -12b). |
| `OptionSource` | `IMenuOptionSource` | OneWay | `null` | The search. `null` → no requests. |
| `SelectCommand` | `ICommand` (param `AutocompleteSuggestion`) | OneWay | `null` | Pick routing (Person: `SuggestionSelectedCommand`). |
| `ClearCommand` | `ICommand` | OneWay | `null` | Optional; run after X empties and unlocks (REQ-ACF-06/09). |
| `BlurWithoutPickCommand` | `ICommand` | OneWay | `null` | Blur while unlocked, with no routed pick (Person: `ValidateNameCommand`). |

Public method: `bool FocusEditor()` focuses the inner editor, which shows the keyboard on Android
(DX `EditBase.Focus()`). It replaces `nameField.Focus()` at `PersonFormPage.xaml.cs:30` (REQ-ACF-18)
and is also used internally after X (REQ-ACF-06).

Removed by the rulings: `CreateNewCommand`, `CreateNewNoun` (Q1). `IsPickLockEnabled` was removed
by Q3 and reinstated on 2026-11-05 (above).

**Event-glue rules added after T7 run 1 (2026-11-05):**
- **Programmatic text closes the menu (REQ-ACF-24, line 9).** The component handles the DX editor's
  `TextChanged`. When `Reason != UserInput` (consumer hydration, the text DX writes after a pick), it
  sets `IsDropDownOpen = false`. Only when `Reason == ProgrammaticChange` does it also set
  `editor.SelectedItem = null`. On `ItemSelected` it leaves `SelectedItem` alone, because
  `OnSelectionChanged` routes the pick first and then resets it. That way the pick is never lost,
  whatever order DX raises `TextChanged` and `SelectionChanged` in. The implementor logs the observed
  order in the task-log. This is the Song form Artist
  pattern (`SongFormViewModel` `IsArtistDropDownOpen = false` on every programmatic assignment,
  `SongFormPage.xaml.cs` `SelectedItem = null`), and it does not depend on focus. Before coding, the
  implementor confirms the `TextChanged` event and the `AutoCompleteEditTextChangeReason` values
  on DX 25.2 (Context7). If they differ, it stops and escalates; it does not swap the mechanism.
- **Stale request after a pick (REQ-ACF-26).** The component keeps a pick counter (`int`).
  `RunSearchAsync` *(T12; was `OnItemsRequested`)* captures it when the search starts. After the await, if the
  counter has changed, it drops the result and the menu stays closed.
- **Deferred focus (REQ-ACF-18, lines 1 and 10).** `FocusEditor()` calls `editor.Focus()`. When
  `editor.Handler is null` or `Focus()` returns `false`, it retries once the editor is ready, on its
  `HandlerChanged` or `Loaded`, whichever comes first, one-shot. The page call is unchanged.

**Event-glue rules added after the emulator diagnostic run (2026-11-09, root causes RC-A/B/C in `task-log.md`):**
> **T12 (2026-11-11):** the RC-A close guard (`_lastRequestRejected`, `OnEditorPropertyChanged`) and the RC-B *request*
> guard's hook (`OnItemsRequested`) are superseded by "Event-glue rules from T12" below. `DisplayMember="Headline"`,
> `IsPickEcho`/`_textAtPick` (now checked in `RestartSearch`) and RC-C stay as written.
These replace the T9 keystroke clear and the "close before DX" assumption below.
- **RC-A: a rejected request must not leave a menu open (REQ-ACF-13c).** DX opens the drop-down itself
  after the request delegate completes, whatever it returns. The component records that the latest request
  was rejected (`_lastRequestRejected`, set in the no-request branch, cleared when a request passes the gate).
  When the editor's `IsDropDownOpen` becomes `true` while that flag is set, the component closes it again
  (dispatched, so it runs after DX's own write). Before coding, check Context7 / the decompiled DLL for a DX-supported way to
  stop the open or hide the "not found" row (e.g. a cancellable opening event). If one exists, prefer it and record which
  was used. Remove the T9 `ClearMenuOnKeystroke` path (it nulled `ItemsSource`, which renders "Not found").
- **RC-B: a pick must not trigger a new search (REQ-ACF-24, -25; F1 "after a pick").** Set
  `DisplayMember="Headline"` on the inner `AutoCompleteEdit` (the Song form pattern), so DX writes the display
  text, not `ToString()`, on `ItemSelected`. Also, defensively: after a routed pick, the component holds
  `_textAtPick` (the `Text` after `SelectCommand` runs). A request whose normalised query equals the normalised
  `_textAtPick`, with no `UserInput` keystroke since the pick, is rejected (RC-A then keeps the menu closed). The first
  real keystroke clears `_textAtPick`.
- **RC-C: hide the soft keyboard explicitly (REQ-ACF-30b).** After `Unfocus()`, hide the IME through the MAUI
  soft-input API (`HideSoftInputAsync`, if the DX editor qualifies) or the Android `InputMethodManager` on the
  handler's platform view (`#if ANDROID`). Confirm the API with Context7 or MS Learn for MAUI 10.
  > **Spec updated 2026-11-09:** T10 impl: the pick echo is `AutocompleteDXFieldLogic.IsPickEcho(query, _textAtPick)`; `_textAtPick` is set from `editor.Text` after `SelectCommand`, and a UserInput change that equals it is kept (DX reports the SelectCommand write as UserInput) — only a real edit clears it. RC-A uses the editor's `PropertyChanged` for `IsDropDownOpen` (no supported DX cancel-open API, see `task-log.md` § T10). RC-C uses `Microsoft.Maui.ITextInput.HideSoftInputAsync` when the editor qualifies, else the Android IME fallback.

**Event-glue rules added after T7 run 2 (2026-11-06):**
> **T12 (2026-11-11):** wherever these bullets name `OnItemsRequested` or DX's request, read `RestartSearch` /
> `RunSearchAsync`; the below-threshold bullet's mechanism is superseded (a rejected query clears `ItemsSource` and closes
> the menu in `RestartSearch`). The pick-drop-focus bullet stays.
- **Normalised search text (REQ-ACF-13a/b).** `OnItemsRequested` computes
  `query = AutocompleteDXFieldLogic.SearchQuery(e.Text)` once, uses it for both `ShouldRequestOptions`
  checks, and passes it to `SearchAsync`.
- **Below-threshold requests show nothing (REQ-ACF-13c).** Run 2 showed that with " a" / "  a" the
  current "no request" branch (`NoOptions` returning `null` + `CloseMenuOnUiThread()`) still lets DX
  show a menu: earlier results on the develop build, a "no results" row on the T7-prep build. The
  root cause is not yet proven. Likely: DX opens its drop-down after the request delegate completes,
  which is after our close, and a `null` result keeps the previous items or shows `NoResultsFoundText`.
  T9 step 1 proves it (decompiled DX 25.2.4 `AsyncItemsSourceProvider` / `AutoCompleteEdit`, as in the
  T4 decompiled-DLL findings), then applies the smallest fix that keeps the menu closed, for example
  return an empty list and close the menu after DX has handled the result. A fix that needs XAML
  outside the component, or a change in DX threshold semantics, is escalated to Helder first.
- **A pick drops focus (REQ-ACF-30).** At the end of `OnSelectionChanged` (after `SelectCommand`,
  the `SelectedItem` reset and the menu close), the component sets a one-shot `_pickBlurPending = true`,
  moves the cursor to the end of the text (DX 25.2 member confirmed with Context7, e.g.
  `CursorPosition`/`SelectionStart`), then calls `editor.Unfocus()`. `OnUnfocused` passes
  `isPickBlur: _pickBlurPending` to `ShouldForwardBlur` and clears the flag. If no blur follows
  (the editor was already unfocused), the flag is cleared on the next `UserInput` `TextChanged` or the
  next focus, so a later real blur is never swallowed.

**Event-glue rule added after T7 run 4 (2026-11-10):**
- **Empty results show nothing (REQ-ACF-31).** In the `RequestAsync` delegate, after the existing post-await
  checks, when `!AutocompleteDXFieldLogic.ShowsMenu(options)` the component sets `_lastRequestRejected = true`,
  calls `CloseMenuOnUiThread()` and returns the (empty or null) options, exactly as a rejected request (RC-A). The
  `OnEditorPropertyChanged` guard then closes the drop-down DX opens after the delegate completes, so the
  "no results" row never renders. A later request with options resets the flag at its start, as today.
  A DX property that only blanks the text (e.g. `NoResultsFoundText = ""`) is not enough: an empty row would still open.

**Event-glue rules from T12 (2026-11-11, T7 run 5) — they supersede the RC-A close guard and the T11 empty-result rule:**
- **Why.** Decompiled DX 25.2.4: `AsyncItemsSourceProvider.RequestItem.Run` sets `IsLoadingInProgress = true` before it
  awaits the request, and the native control shows the drop-down while loading, so a no-match search renders "Not
  found" for the length of the search. The native→MAUI `IsDropDownOpen` sync is skipped while the handler's locker is
  held, so a close after the fact can be a no-op ("Not found" stuck open on the emulator). Evidence: `task-log.md` § T7 run 5.
- **The component owns the search.** No `AsyncItemsSourceProvider`; `LoadingProgressMode="Manual"` (DX 25.2 docs via
  Context7, recorded in the spike findings: `Auto` shows a wait indicator on text change, `Manual` only when
  `IsLoadingInProgress` is set) and the component never sets `IsLoadingInProgress`. A null `OptionSource`, a locked or
  read-only field, or search disabled all fail `ShouldRequestOptions`, so they take the rejected path (REQ-ACF-04, -12a/b).
  No DX `CharacterCountThreshold` or `RequestDelay` is set anywhere (they belong to the removed provider); the minimum
  length is `ShouldRequestOptions` on the normalised query and the debounce is `RunSearchAsync`'s delay. On a `UserInput` `TextChanged` (`RestartSearch`): cancel the pending search
  (own `CancellationTokenSource`, latest search only); compute `query = SearchQuery(text)`; if the pick echo
  (`IsPickEcho`) or `!ShouldRequestOptions(...)` → clear `ItemsSource`, close the menu, stop; else keep the menu as it
  is and start `RunSearchAsync`: `Task.Delay(SearchConstants.AutocompleteDebounceMilliseconds)`, `SearchAsync(query, ct)`,
  the REQ-ACF-26 pick-count check, the `ShouldRequestOptions` re-check, then on the UI thread: `ShowsMenu(options)` →
  `ItemsSource = options; IsDropDownOpen = true`; else `ItemsSource = null; IsDropDownOpen = false`.
- **Cancel points.** A programmatic text change, a routed pick and the editor's blur cancel the pending search, so a
  late result never opens the menu on a field the user has left.
- **Removed:** `OnItemsRequested`, `NoOptions`, `RejectRequest`, `_lastRequestRejected`, the `OnEditorPropertyChanged`
  close guard (RC-A) and the T11 empty-result branch. `ShowsMenu` (§ 4) stays the decision. REQ-ACF-14 is kept: the
  debounce is the same shared constant, now applied by the component instead of DX `RequestDelay`.
  > **Spec updated 2026-11-11:** T12 impl: the blur cancel is in `OnUnfocused` and the pick cancel in `OnSelectionChanged`
  > (after `_pickCount++`). `RunSearchAsync` disposes its own CTS in `finally`; `CancelSearch` only cancels and forgets
  > the reference, so it never touches a disposed CTS. The `ItemsSource`/close write for a rejected query lives in
  > `RestartSearch`; a qualifying query leaves the menu as it is, so stale options may stay visible for the debounce
  > window until the new options replace them (the amended REQ-ACF-31 allows this).

Constants are not properties. The component reads `SearchConstants.MinimumLocalQueryLength` and
`SearchConstants.AutocompleteDebounceMilliseconds` via `x:Static`.

### 3.2 Consumer ViewModel contract

```csharp
// MyVocaList/UI/Models/IMenuOptionSource.cs
public interface IMenuOptionSource
{
    /// Returns THIS query's options (BUG-056: never read back from an observable).
    Task<IReadOnlyList<AutocompleteSuggestion>> SearchAsync(string query, CancellationToken ct);
}

public sealed class MenuOptionSource(Func<string, CancellationToken, Task<IReadOnlyList<AutocompleteSuggestion>>> search)
    : IMenuOptionSource
{
    public Task<IReadOnlyList<AutocompleteSuggestion>> SearchAsync(string query, CancellationToken ct) => search(query, ct);
}
```

The contract is a per-field object, not an interface on the ViewModel, because one ViewModel can host
several fields (the Song form will have Title and Artist). The other members are plain bindings.

**`PersonFormViewModel` changes (first consumer):**

| Member | Status |
|--------|--------|
| `PersonName`, `NameHasError`, `NameErrorText`, `SuggestionSelectedCommand`, `ValidateNameCommand` | ✅ existing, unchanged |
| `SearchPersonsCoreAsync(string term, CancellationToken ct = default)` → `IReadOnlyList<AutocompleteSuggestion>` | **new**: the body of today's `SearchPersonsAsync` (`PersonFormViewModel.cs:280-293`), changed in two ways: the threshold check uses the **trimmed** term (`term.Trim().Length < SearchConstants.MinimumLocalQueryLength`, REQ-ACF-13; today it checks raw `term.Length`, `:282`), and the method takes a `CancellationToken` and passes it to `IPersonService.SearchPersonsStartsWithAsync(term, ct: ct)` (the parameter exists, `Services/PersonService.cs:213`). It returns the mapped list (empty below the threshold). `SearchPersonsAsync` becomes `Suggestions = await SearchPersonsCoreAsync(term)`, so `SearchPersonsCommand` and its tests are unchanged. |
| `NameOptionSource` (`IMenuOptionSource`) | **new**: `new MenuOptionSource((q, ct) => SearchPersonsCoreAsync(q, ct))` (REQ-ACF-16), passing the DX request's token through. |

> **Spec updated 2026-09-25:** (plan § 0.3 item 5) the service call is
> `SearchPersonsStartsWithAsync(trimmed, cancellationToken: ct)` with `var trimmed = term.Trim();`.
> The real parameter name is `cancellationToken` (`Domain/ServicesInterfaces/IPersonService.cs:24`), not
> `ct`, and the service receives the **trimmed** term, consistent with REQ-ACF-13's trimmed threshold.
> The service already normalises its input (`Services/PersonService.cs:216`), so unpadded input is unaffected.

**Handed over to the song-form title-first item:** `SongFormViewModel.ArtistOptionSource`, a Title
option source, the artist/title lock hydration, and deleting the Song glue
(`SongFormPage.xaml.cs:38,41-113`). This item does not touch `SongFormPage` or `SongFormViewModel`.

## 4. Pure logic (`AutocompleteDXFieldLogic`), Level A testable

| Function | Rule | REQ |
|----------|------|-----|
| `FieldMode Mode(bool isReadOnly, bool isLocked, string? text, bool isPickLockEnabled = true)` | `ReadOnly` if `isReadOnly` (**read-only wins**); else `Locked` if `isPickLockEnabled && isLocked && !string.IsNullOrWhiteSpace(text)`; else `Editable`. Every other rule and the lock mechanism use this, never the raw `IsLocked`/`IsReadOnly`. "Effectively locked" below means `Mode == Locked`. | ACF-05a, -12b |
| `string BottomSupportingText(bool hasError, string? supportingText)` | `hasError ? "" : (supportingText ?? "")`. It is the value passed to DX `HelpText`; DX shows `ErrorText` while `HasError`. | ACF-12c |
| `bool ShowClearIcon(FieldMode mode, string? text)` | `mode != ReadOnly && !string.IsNullOrEmpty(text)`. | ACF-07, -12b |
| `string SearchQuery(string? text)` *(T9)* | `(text ?? "").NormalizeSearchQuery()` — the app-wide helper (`MyVocaList.Extensions/Strings/StringExtensions.cs`), never a local re-implementation. It is the text passed to `SearchAsync`. | ACF-13 (a)(b) |
| `bool ShouldRequestOptions(FieldMode mode, bool isSearchEnabled, IMenuOptionSource? source, string? query)` | `source != null && isSearchEnabled && mode == Editable && SearchQuery(query).Length >= SearchConstants.MinimumLocalQueryLength` *(T9: was `query?.Trim().Length`)*. The normalised-length check is needed because DX `CharacterCountThreshold` counts raw characters (" a" would pass it). | ACF-04, -12a, -12b, -13 |
| `bool ShowsMenu(IReadOnlyList<AutocompleteSuggestion>? options)` *(T11)* | `options is { Count: > 0 }`. When false, the request is treated like a rejected one (RC-A close guard), so DX never shows its "no results" row. | ACF-31 |
| `PickRoute RoutePick(object? selectedItem)` | `Select` if `selectedItem is AutocompleteSuggestion`; else `None`. | ACF-11b |
| `bool LockAfterPick(PickRoute route, bool isPickLockEnabled = true)` | `route == Select && isPickLockEnabled`. | ACF-01, -29 |
| `bool ShouldForwardBlur(object? selectedItem, FieldMode mode, bool isPickBlur = false)` | `!isPickBlur && selectedItem is null && mode == Editable`. *(T9: `isPickBlur` is true for the one blur the component causes itself after a pick, REQ-ACF-30c.)* | ACF-11c, -12b, -17, -30 |
| `string? TextToRestore(AutoCompleteEditTextChangeReason reason, FieldMode mode, string lockedText)` | *Only if mechanism C is accepted (§ 5).* `lockedText` when `reason == UserInput && mode == Locked`; else `null`. (Read-only always uses DX `IsReadOnly`, whichever mechanism is chosen.) | ACF-01 |

The `isPickLockEnabled` parameters (2026-11-05, REQ-ACF-29) default to `true`, so every existing call
and test is unchanged. The component's `CurrentMode` and `OnSelectionChanged` pass the property's value.

The code-behind maps DX events to these and calls `ICommand.CanExecute`/`Execute`. The options
passed to DX are exactly the `IMenuOptionSource.SearchAsync` result, after
`ct.ThrowIfCancellationRequested()` (BUG-056, REQ-ACF-22/26). That glue has no branching logic
worth isolating: REQ-ACF-22 is proven at the VM seam (T5) plus code review. REQ-ACF-19 (the component
never writes `Text` on blur) and REQ-ACF-24 (programmatic text never searches, a DX behaviour) are
proven by code review and on device (T1/T7), not by unit tests.

## 5. Lock mechanism (the crux)

### 5.1 Evidence gathered (2026-09-24)

| Source | Status | Finding |
|--------|--------|---------|
| Context7 `/websites/devexpress_maui` | **Available.** Docs are not version-tagged; the site documents the current release (25.2). | `EditBase` exposes `EndIcon`, `IsEndIconVisible`, `EndIconColor`, `EndIconClicked`, `EndIconCommand` ("can act as a custom button"). Clear icon: `ClearIcon`, `ClearIconVisibility` (`Auto` = only when not empty; `Always` = "disabled when the editor is empty"; `Never`), `ClearIconColor`, `ClearIconClicked`, `ClearIconCommand`. `AutoCompleteEdit` has `SubmitIcon`, `SubmitIconVisibility`, `SubmitIconColor`, `Submitted`/`SubmitCommand` (the magnifier). `AutoCompleteEditTextChangedEventArgs.Reason` ∈ {`UserInput`, `ProgrammaticChange`, `ItemSelected`}. **No `AllowUserInput`** on `AutoCompleteEdit` (full member list). Only HTML Edit documents the split: *"AllowUserInput=false disables the text input area but allows toolbar use, while IsReadOnly=true disables editing in both"*. No doc states whether `IsReadOnly` disables the End icon. |
| `devexpress-maui` MCP (`search_maui_demo_app_code`, `search_maui_demo_app_docs`) | **Connected but returns empty** for `AutoCompleteEdit`, `EndIconCommand`, `SubmitIconVisibility` and a docs query. The same empty index was recorded on 2026-07-19 (DX-AC design § API-pinning gate). This is "tool returned empty", not "tool unavailable". No demo-app evidence exists either way. | none |
| `docs.devexpress.com` direct fetch | HTTP 403 (WebFetch and MCP generic fetch). | none |
| Decompiled `DevExpress.Maui.Editors.dll` 25.2.4, net10.0-android (ilspycmd, scratchpad only) | Local evidence, same method as the BUG-065/066 IL evidence. | (1) `IsReadOnly` is forwarded to the **native** Android editor: `EditBaseHandler.UpdateReadOnly` sets `native.ReadOnly = Element.IsReadOnly`. (2) The MAUI-side `SendEndIconClicked` / `SendClearIconClicked` have **no** `IsReadOnly` check (`EditBase.SendEndIconClicked` → event + `EndIconCommand`). The native end-icon listener is attached only when a handler exists (`EditBaseHandler`: `SetEndIconClickedListener(HasEndIconClickedHandler ? listener : null)`). (3) The native editor is a Java library (`DXEditors.a.dll` binding), so ReadOnly → icon enablement is decided in Java and cannot be read here. The binding exposes **both** `DisabledClearIconColor` and `DisabledEndIconColor`, so the end icon *has* a disabled state; whether ReadOnly triggers it is unknown. (4) `SubmitIconVisibility` defaults to `IconVisibility.Auto` (enum `Never=0, Always=1, Auto=2`; `AutoCompleteEdit.SubmitIconVisibilityProperty` default `(IconVisibility)2`). So the magnifier is a **DX default**, not project-added: `SongFormPage.xaml:28-44` sets no icon. (5) `EditBase.ShowKeyboardOnFocus` is `protected virtual` and get-only, and is consulted only by programmatic `Focus()` (`if (… && !IsReadOnly && ShowKeyboardOnFocus)`). |
| On-device history | Established. | `IsReadOnly=true` → built-in clear icon disabled (BUG-081, Helder 2026-09-23). `IsEnabled=false` → clear icon dead + disabled styling (BUG-054b, `cb78f3e4`). Without `IsReadOnly`, the clear icon works (BUG-081 fix, 634/634 + Helder's device check 2026-09-24). |

**Conclusion:** no *documented* property gives "text read-only, clear icon live" on `AutoCompleteEdit`
25.2.4. Two candidates are plausible, and both hinge on native runtime behaviour that the docs do not
state. **An on-device spike is required** (tasks.md T1).

### Decision: lock mechanism (spike-gated)
**Chosen approach (primary, B): "read-only + End-icon X".** While `Mode == Locked` (§ 4):
`IsReadOnly = true`, `ClearIconVisibility = Never`, `EndIcon = close_outlined`
(`Resources/Images/close_outlined.svg` exists), `IsEndIconVisible = true`,
`EndIconColor = OnSurfaceVariant`, and `EndIconClicked` → the X handling in § 6. While unlocked:
`IsReadOnly = false`, `IsEndIconVisible = false`, and the built-in clear icon is used
(`ClearIconVisibility = Auto`, `ClearIconColor` per REQ-ACF-08; `ClearIconClicked` → the unlocked-X handling in § 6).
**Why primary:** it is true read-only. There is no keyboard, no cursor, no text-change events and no
item requests to suppress, so REQ-ACF-01 holds by construction, with the fewest moving parts. The End
icon is documented as "a custom button", and the MAUI layer has no read-only check on its click path
(IL finding 2).
**Known risk of B: BUG-069.** B brings `IsReadOnly = true` back. BUG-069 (the menu opening on
edit-load and reopening after a pick) stopped reproducing after BUG-081 removed `IsReadOnly`
(`Docs/Management/LEDGER.md`, INLINE-AC row, "Update 2026-09-24", which calls it "likely a side effect
of dropping `IsReadOnly`"). So B passes T1 only if BUG-069 still does not reproduce under B
(criterion B5). If it does reproduce, B fails and the decision table applies.

**Secondary (C): "soft lock". Not an automatic fallback.** C is a *visible compromise*: the field
can briefly take focus, and the soft keyboard can appear. Helder must see the T1 measurements and
explicitly accept C under Q5 before it is built. `IsReadOnly` stays `false`, so the
built-in clear icon is live (proven on device after BUG-081). While locked:
(a) `TextChanged` with `Reason == UserInput` → restore `lockedText` (`TextToRestore`, documented
`Reason` enum);
(b) a keystroke → no search (`ShouldRequestOptions`, T12: in `RestartSearch`), and `IsDropDownOpen = false`;
(c) `Focused` → `Unfocus()` so the soft keyboard is dismissed.
The spike measures its weaknesses against the thresholds in tasks.md T1 (C1–C5): typed characters
visible in recorded frames, keyboard visible time, paste, long-press selection, IME composition, and
whether tapping X focuses the editor first (in which case (c) could swallow the tap).

**Spike outcome → decision table**

| B passes (B1–B6) | C meets C1–C5 | Build with |
|----------|----------|------------|
| ✅ | any | **B** |
| ❌ | ✅ | **Escalate (Q5)**: build **C** only if Helder accepts it, after seeing the measurements. |
| ❌ | ❌ | **Stop and escalate to Helder (Q5).** Options to present: (i) accept C with its measured artefact; (ii) picked state rendered as a read-only DX `TextEdit` with a separate DX "Change" icon button (a second control, so REQ-ACF-07/08 wording changes); (iii) keep today's post-BUG-081 behaviour (type-over allowed, blur restores). The agent does not pick one. |

**Alternatives rejected:**
- **`IsReadOnly` + built-in clear icon (A):** disables X. Proven on device (BUG-081).
- **`IsEnabled = false`:** kills X and applies disabled styling to the whole field. Proven on device (BUG-054b).
- **MAUI `InputTransparent = true`:** passes *all* touches through the view, including icon taps (MAUI
  semantics), so X would be dead.
- **`ShowKeyboardOnFocus` override:** protected, get-only, and affects only programmatic `Focus()`,
  not a user tap (IL finding 5). It needs a DX subclass and is undocumented.
- **Transparent tap-shield overlay over the text area in a wrapper Grid:** couples the layout to DX's
  internal icon geometry (icon indent/spacing/theme), breaks with font scaling and RTL, and hides the
  field from accessibility.
- **Platform-handler hack on the native editor** (e.g. Android `KeyListener = null`): the native
  editor is DX's Java implementation (`DXEditors.a.dll`). This is undocumented internal surface,
  per-platform, and would break on DX upgrades.
- **`dxe:ComboBoxEdit` (the non-editable exposed dropdown menu):** the display is non-editable by
  design and `ClearIconVisibility` is supported. But there is no evidence of async server-side search
  (its filter works over `ItemsSource`), so it would need a heavier migration of BUG-051/056/065 glue.
  Revisit only if B and C both fail.
- **DX `InputChipGroup` / MD3 input chip for the picked value:** changes the visual pattern Helder
  asked for (a text field with X), and chips carry multi-value semantics.

**Reversibility:** Easily reversible (internal to the component; the consumer contract is identical
for B and C).

### 5.2 X styling (REQ-ACF-08)

MD3 outlined-text-field trailing-icon tokens, verified 2026-09-24 from Material Web's token source
(`tokens/versions/v0_192/_md-comp-outlined-text-field.scss`, because m3.material.io is not fetchable
as text): `trailing-icon-color = on-surface-variant`, focus/hover also `on-surface-variant`,
`error-trailing-icon-color = error`, `disabled-trailing-icon-color = on-surface` at
`disabled-trailing-icon-opacity = 0.38`.
The component sets `ClearIconColor` (and `EndIconColor` under B) to `{StaticResource OnSurfaceVariant}`,
switching to `{StaticResource Error}` via a trigger on `HasError`. Both tokens exist
(`Resources/Styles/MaterialColors.xaml:156,167`). There is precedent in `SearchBar.xaml:36`
(`ClearIconColor="{StaticResource OnSurfaceVariant}"`). The disabled look can only appear if the
editor is read-only/disabled while the built-in clear icon shows, and mechanism B/C prevents that by
construction. Removing the magnifier (§ 5.3) also removes the same-colour neighbour that made X read
as inert.

### 5.3 Submit (magnifier) icon (REQ-ACF-10)

It is a DX default (`SubmitIconVisibility` defaults to `Auto`, IL finding 4). It is not in the project
XAML (`SongFormPage.xaml:28-44`, `PersonFormPage.xaml:19-31`) or in the implicit style (`MaterialStyles.xaml:74-80`). The component
sets `SubmitIconVisibility="Never"`. Nothing in the app handles `Submitted`/`SubmitCommand` (census:
0 hits), so no behaviour is lost.



## 6. Interaction flows

```
UNLOCKED, search enabled, typing:
  keystroke (UserInput) ─► RestartSearch: cancel pending search; query = SearchQuery(text)
     ─► IsPickEcho or ShouldRequestOptions = false ─► ItemsSource = null, menu closed
     ─► else (menu left as is) ─► RunSearchAsync: delay AutocompleteDebounceMilliseconds ─► SearchAsync(query, ct)
        ─► ct / pick-count / ShouldRequestOptions re-check ─► ShowsMenu? yes ─► ItemsSource = options, menu open
                                                                         no  ─► ItemsSource = null, menu closed
  (T12: no DX AsyncItemsSourceProvider, no loading indicator; LoadingProgressMode="Manual", IsLoadingInProgress never set)

READ-ONLY (IsReadOnly=true; wins over IsLocked/IsSearchEnabled):
  text shown ─► DX IsReadOnly=true, no clear/end icon ─► tap/keys/paste: nothing ─► no search (ShouldRequestOptions = false), no menu ─► blur: nothing

SEARCH DISABLED (IsSearchEnabled=false):
  keystroke ─► RestartSearch ─► ShouldRequestOptions = false ─► ItemsSource = null; IsDropDownOpen=false

PICK:
  SelectionChanged ─► RoutePick = Select ─► pickCounter++ (stale in-flight results discarded)
     ─► LockAfterPick(route, IsPickLockEnabled)? yes ─► IsLocked = true (TwoWay → consumer, if bound)
                                                 no  ─► IsLocked untouched (REQ-ACF-29)
     ─► SelectCommand(option) ─► SelectedItem = null (BUG-061) ─► IsDropDownOpen = false
     ─► lock mechanism applied (B), only if locked
  DX then writes the option text (TextChanged, Reason ≠ UserInput) ─► IsDropDownOpen = false, SelectedItem = null
  (Person, IsPickLockEnabled=False: SuggestionSelectedCommand navigates away with a replacing route;
   the field never locks, and the new page is editable.)

PROGRAMMATIC TEXT (any Reason ≠ UserInput, e.g. edit-mode hydration):
  ─► no search (RestartSearch runs on UserInput only; pending search cancelled) ─► IsDropDownOpen = false, SelectedItem = null (REQ-ACF-24, Song Artist pattern)

BLUR (T12):
  ─► pending search cancelled, so a late result never opens the menu on a field the user has left

LOCKED:
  tap text / long-press / keys / paste ─► B: nothing (read-only)  |  C: reverted, Unfocus
  blur             ─► ShouldForwardBlur = false ─► nothing
  keystroke        ─► RestartSearch ─► ShouldRequestOptions = false ─► nothing (REQ-ACF-04 / BUG-069)
  tap X            ─► Text = "" ─► IsLocked = false ─► ClearCommand (if bound) ─► FocusEditor()

UNLOCKED, tap X (text non-empty):
  DX clears Text (built-in clear icon) ─► ClearCommand (if bound) ─► focus stays

PROGRAMMATIC LOCK (consumer sets Text + IsLocked=true, e.g. hydration):
  programmatic text starts no search (RestartSearch runs on UserInput only; pending search cancelled)
  ─► component applies the lock (if text non-empty) ─► SelectedItem = null, IsDropDownOpen = false
```

State machine (component view; `Mode` from § 4; `IsReadOnly=true` overrides every state below with ReadOnly, and clearing it returns to the state implied by `IsLocked`/text):

```
Unlocked-Empty     --type (trimmed ≥ 2, search on)--> Unlocked-Searching
Unlocked-Searching --pick, pick-lock enabled--> Locked
Unlocked-Searching --pick, pick-lock disabled--> Unlocked-Text (menu closed)
Locked             --IsPickLockEnabled set false--> Unlocked-Text (Mode = Editable: REQ-ACF-29d)
Unlocked-*         --blur (no pick)--> Unlocked-Text (BlurWithoutPickCommand)
Unlocked-*         --X--> Unlocked-Empty (focus kept)
Locked             --X--> Unlocked-Empty (focused, keyboard up)
Locked             --any other user input--> Locked (no change)
(any, text ≠ "")   --consumer sets IsLocked=true, pick-lock enabled--> Locked
Locked             --consumer empties Text--> Unlocked-Empty (Mode = Editable: REQ-ACF-05a)
```

## 7. Invariants and postconditions

- **INV-1:** `Mode == Locked` ⇒ the editor text is unchanged by user input, the menu is closed,
  and no search call is issued.
- **INV-1a:** the field is never *locked* while its text is empty (REQ-ACF-05a). A page-set `IsReadOnly` is exempt.
- **INV-1b:** `IsReadOnly` ⇒ `Mode == ReadOnly` whatever `IsLocked`/`IsSearchEnabled` say: no X, no search, no menu, no blur forwarding (REQ-ACF-12b).
- **INV-2:** X is visible ⇔ the text is non-empty and `Mode != ReadOnly`; X is never rendered disabled while visible.
- **INV-3:** after any routed pick, DX `SelectedItem == null`.
- **INV-4:** `IsSearchEnabled == false` ⇒ 0 search calls and the menu closed.
- **INV-5:** no consumer page code-behind handles DX autocomplete events for a field hosted by the component.
- **INV-6:** `IsPickLockEnabled == false` ⇒ `Mode != Locked` in every state, and the component never writes `IsLocked` (REQ-ACF-29).
- **INV-7:** after any text change with `Reason != UserInput`, the menu is closed and `SelectedItem == null` (REQ-ACF-24).
- **Post (X on locked):** text empty, `IsLocked == false`, consumer clear command run, editor focused.

## 8. Shared constants

### Decision: debounce constant lives in `SearchConstants` (300 ms shared: ruled; location: proposed, Q10)
**Chosen approach:** add `public const int AutocompleteDebounceMilliseconds = 300;` to
`Domain/Constants/SearchConstants.cs`, with an XML doc citing this spec and REQ-DXAC-07.
**Alternatives considered:** a UI-layer constants class (the layering is purer, but it splits
search-tuning numbers across two homes, contrary to Helder's 2026-09-21 "no magic numbers anywhere"
ruling recorded at `SearchConstants.cs:23-25`); a component default value (hidden, and not shareable
with the list search).
**Reversibility:** Easily reversible.
**Rationale:** `SearchConstants` is already the single home for search thresholds consumed by UI
(`PersonFormViewModel.cs:282`) and Services, and the Domain project is referenced by the UI. 300 ms
keeps today's shipped value (`SongFormPage.xaml:41`, `PersonFormPage.xaml:28`), so there is no
behaviour change.

Threshold: the component uses `SearchConstants.MinimumLocalQueryLength` (2), measured on trimmed text
(Helder ruling Q2). `PersonFormPage.xaml:29` already uses `2`, but as a literal; this item replaces it.
The Song form's divergent `CharacterCountThreshold="1"` (`SongFormPage.xaml:42`) disappears when the
song-form item rebuilds that page on the component.


## 9. DI / registration impact

None. The component is instantiated from XAML and is not DI-registered; `MauiProgram.cs` is not
touched. `MenuOptionSource` is created by the ViewModel. The two suggestion services
(`Services/ArtistSuggestionService.cs`, `Services/SongSuggestionService.cs`) stay unregistered on
purpose; the song-form item may wrap them in a `MenuOptionSource`.

## 10. Test strategy (testing.md risk tiers)

| Level | What | Where |
|-------|------|-------|
| **A** | `AutocompleteDXFieldLogic`: every row of § 4 with all branches (read-only × locked × search on/off, with read-only winning; locked/unlocked, locked + empty/whitespace text, `ShowClearIcon`, search on/off, null source, trimmed length 1 vs 2 incl. " a", pick routing, blur forwarding, `Reason` × locked if C; **REQ-ACF-29:** `Mode(…, isPickLockEnabled: false)` → `Editable` for locked + text, `ReadOnly` still wins, and `LockAfterPick(Select, false)` → `false`). Each test tagged `// [AC] REQ-ACF-NN`. | `MyVocaList.Tests/Unit/Components/AutocompleteDXFieldLogicTests.cs` |
| **A** | `PersonFormViewModel.NameOptionSource.SearchAsync` returns exactly `SearchPersonsCoreAsync`'s list (REQ-ACF-16); `SearchPersonsCoreAsync` returns empty below the threshold, on the **trimmed** term (" a"), without calling the service; the token is passed to the service. | `PersonFormViewModelTests.cs` |
| **B** | `SearchConstants.AutocompleteDebounceMilliseconds == 300`. | `SongSuggestionThresholdTests.cs` or new `SearchConstantsTests.cs` |
| **C** | Component XAML shell, BindableProperty plumbing, Person page swap: build + manual E2E; the no-test decision recorded in the task-log. | — |
| **Regression** | All existing `PersonFormViewModelTests` and `PersonFormViewModelBug044Tests` stay green unchanged (REQ-ACF-20/21/28). | full `dotnet test` |
| **Spike (T1)** | Lock mechanism, X enablement, BUG-069 (B5). | throwaway page (tasks.md T1) |
| **Manual E2E (T7, Helder, Android)** | REQ-ACF-01…10, -13, -15, -17, -18, -23, -24, -25, -26, -27, -29 (Person never locks). Pick-lock-enabled behaviour (REQ-ACF-01/05/06) is re-verified on device by the song-form item. REQ-ACF-12a has no consumer that turns search off in this item: it is covered at Level A here and on device by the song-form item (Title in edit mode). | task-log |

Not testable at the VM seam: read-only rendering, X enablement and colour, keyboard behaviour, the
missing submit icon, `SelectedItem` highlight, and menu timing. T1 and T7 cover these.

## 11. Governance

- **New component; its first consumer is `PersonFormPage` → not governed**
  (`component-safety-gate.md` § Relationship to other rules: governance begins at the second consumer).
- **The second consumer is the new Song form** (song-form title-first item, Title and Artist fields).
  That item must add `AutocompleteDXField` to the governed table in `.claude/library/component-safety-gate.md`
  **in the same commit** as the second binding. From then on every component change is a four-gate
  task. Two fields on one page count as one consumer page; the rule counts pages/views.
- The song-form item's known needs are already in § 3.1: `IsSearchEnabled` (Title search off in
  edit mode) and `IsReadOnly` (an API-sourced song's Title in edit mode), both added before the
  second consumer binds. **Version in search results** (its README, third round, decision 3) needs
  no template change: the consumer puts the Version into `AutocompleteSuggestion.SupportingText`,
  which the option template already shows as its second line. Any other new surface is a component
  change under the four gates.
  > **Spec updated 2026-09-26:** the song-form item's seventh-round ruling reverses this: in edit mode Title is Editable with `IsSearchEnabled = false` for every song; the song form binds no `IsReadOnly`. `IsReadOnly` stays in the component for future use.
- `PersonFormPage.xaml` edits follow the incremental XAML rule (one file → build → fix → next).

## 12. Census (Python `os.walk` + substring, 2026-09-24; excludes `bin/obj/.git/Docs/.claude`)


`grep` was not used for counts (the rtk proxy has dropped matches before). Scope: `*.cs`, `*.xaml`,
`*.csproj`, `*.props`. Raw hit counts include comments.

| Token | Raw hits | Live usages (excluding comments and the frozen family) |
|-------|---------:|--------------------------------------------------------|
| `AutoCompleteEdit` | 22 | 2 controls: `SongFormPage.xaml:28`, `PersonFormPage.xaml:19`; 1 implicit style: `MaterialStyles.xaml:74` |
| `AsyncItemsSourceProvider` | 7 | 2: `SongFormPage.xaml:41`, `PersonFormPage.xaml:28` |
| `RequestDelay` | 4 | 2 literals `"300"`: `SongFormPage.xaml:41`, `PersonFormPage.xaml:28` |
| `CharacterCountThreshold` | 2 | `SongFormPage.xaml:42` = **"1"** (divergent), `PersonFormPage.xaml:29` = "2" (literal) |
| `ItemsRequested` handlers | 4 | 2: `SongFormPage.xaml.cs:45`, `PersonFormPage.xaml.cs:37` |
| `ClearIconClicked` | 2 | 1: `SongFormPage.xaml:39` → `.xaml.cs:112` |
| `IsDropDownOpen` | 3 | 1 binding: `SongFormPage.xaml:35` |
| `ClearIconVisibility` | 4 | `SongFormPage.xaml:36`; `SearchAppBar.xaml:39`, `SearchBar.xaml:35` (not autocomplete) |
| `FilteredItemsSourceProvider`, `EndIcon`, `SubmitIcon*` | 0 | none |
| `AutocompleteSuggestion` | 45 | model `Contracts/Models/AutocompleteSuggestion.cs:9`; used by both pages and VMs (`SongFormViewModel.cs:53,135,165,298,308,317`, `PersonFormViewModel.cs:37,77,83,289,295`) |
| `AutocompleteField` / `AutocompleteMobileField` | 53 / 18 | Frozen family: 8 files in `UI/Components/AutocompleteField/` excluded from compilation (`MyVocaList.csproj:172-173`), plus 6 excluded tests (`MyVocaList.Tests.csproj:38-39`). Live references: one doc comment (`AutocompleteSuggestion.cs:4`). |
| `*SuggestionService` | 26 | `ArtistSuggestionService`/`SongSuggestionService` + interfaces (`Services/`), tested, **unregistered on purpose**, pre-built for future form autocomplete. Keep. |
| `MinimumLocalQueryLength` | 19 | Services guard (`ArtistService.cs:179`, `PersonService.cs:201,218`, both suggestion services); `PersonFormViewModel.cs:282`; tests |


**Rollout (revised per Helder's rulings)**

| Consumer | Today | This item | Owner / follow-up |
|----------|-------|-----------|-------------------|
| `PersonFormPage` Full Name | DX `AutoCompleteEdit` + glue (`PersonFormPage.xaml:19-43`, `.xaml.cs:33-62`); the pick navigates to the existing person (`PersonFormViewModel.cs:295-313`) | **Migrated (first consumer)** | — |
| `SongFormPage` Artist (+ new Title) | DX `AutoCompleteEdit` + glue (`SongFormPage.xaml:28-79`, `.xaml.cs:38,41-113`) | **No** | Song-form title-first item builds the new form on the component (second consumer → governance). |
| `ArtistFormPage` duplicate suggestions | BindableLayout list (`ArtistFormPage.xaml:46-51`) | No | Candidate. |
| `ArtistPickerPage`, `SongPickerPage`, `YouTubeSearchPage` | Navigate-away pickers | No | Replaced or deleted by the song-form item. |
| Future queue-entry form (Venue/Artist/Singer) | not built | No | Intended consumer. |
| Frozen `UI/Components/AutocompleteField/` | Excluded from build | No | Left as is, per Helder. |
| List-page search (`CrudListViewModelBase.cs:238`, `Task.Delay(400)`) | literal | No | Candidate for a shared constant. |

## 13. Notes and resolved conflicts

1. **BUG-069 is fixed and must stay fixed.** Helder confirmed on device on 2026-09-24, after the
   BUG-081 merge (`2d70ac73`), that it no longer reproduces (`Docs/Management/LEDGER.md`, INLINE-AC row,
   "Update 2026-09-24"; likely a side effect of dropping `IsReadOnly`). Mechanism B brings
   `IsReadOnly` back, so T1 has criterion B5.
2. **BUG-070 is removed, not reworded:** with no create row, the Song form's "Search and select an
   artist from the list" error goes away in the song-form item (create on Save). Nothing in this item.
3. **Locked-and-empty** (`SongFormViewModel.cs:469-477`): neutralised for every consumer by
   `Mode()` (REQ-ACF-05a).
4. **REQ-ACREATE-02/03/10** are superseded by the song-form item's create-on-save
   (requirements § Supersedes). The frozen `AutocompleteField` family is left as is, per Helder
   (REQ-DXAC-11 stands).
5. **Validation messages as VM literals** (Form Validation Standard deviation): pre-existing, out of scope.
