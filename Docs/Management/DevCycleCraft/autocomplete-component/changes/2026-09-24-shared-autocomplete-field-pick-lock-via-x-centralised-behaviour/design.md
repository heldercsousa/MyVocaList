# Design: shared exposed dropdown menu field (pick-lock via X, centralised behaviour)

> **Status: DRAFT for Helder's review (2026-09-24).** Requirements: `requirements.md` (REQ-ACF-*).
> Every current-behaviour claim below cites `file:line` on `develop` as of 2e8a8d63.
> DevExpress version pinned from `Directory.Packages.props`: **DevExpress.Maui.Editors 25.2.4**
> (the `.csproj` uses central package management). MAUI Controls 10.0.41.

## 1. Architecture

Affected layers: **UI only**, plus one new constant in Domain. Services are unchanged.

```
SongFormPage.xaml ── <menus:ExposedDropdownMenu …bindings only…/>
                             │  (owns: items-request glue, create-new row, pick routing,
                             │   blur/clear forwarding, SelectedItem reset, menu-open,
                             │   debounce + threshold, lock mechanism, icon styling)
                             ▼
                     dxe:AutoCompleteEdit (DX 25.2.4, hosted inside the component)
                             │
SongFormViewModel ◄──────────┘  commands + properties (unchanged members, plus ArtistOptionSource)
        │
IArtistService.SearchArtistsByNameAsync / CreateArtistAsync   (unchanged)
```

- **Business logic stays in the ViewModel/Service.** The component forwards to commands only
  (REQ-ACF-12). The lock flag is consumer-owned (`SongFormViewModel.IsArtistLocked`, `:52`) and bound
  into the component OneWay.
- **Pure decision logic is extracted** into a static, UI-free class so it is unit-testable at
  Level A without a DX handler. The code-behind only adapts DX events to it.

## 2. Component name, location and files

### Decision: component name `ExposedDropdownMenu`
**Chosen approach:** `ExposedDropdownMenu`, under a new MD3-category folder
`MyVocaList/UI/Components/Menus/`.
**Alternatives considered:**
- `AutocompleteField`: rejected. The name is taken by the frozen family
  (`UI/Components/AutocompleteField/`, `README-FROZEN.md`), which is still listed in
  `component-safety-gate.md`'s governed table. Reusing it would make the governance list and grep
  census ambiguous. "Autocomplete" is also not an MD3 component name.
- `FormAutocompleteField` (the name floated in the DX-AC design § Wrapper promotion trigger): rejected
  because it is not MD3 terminology (CLAUDE.md § Constitutional Constraints, "MD3 terminology").
- `SearchBar`-derived: rejected. The MD3 search bar is a top-level search surface, not a form field.
**Reversibility:** Easily reversible (a rename while there is one consumer).
**Rationale:** MD3 has no "autocomplete" component. The Material pattern for "a text field with a
menu of options under it, where the text field shows the current selection" is the **exposed
dropdown menu**, and its *editable* variant lets the user type to filter. Verified 2026-09-24 against
the Material Components Android `docs/components/Menu.md`: *"Exposed dropdown menus display the
currently selected menu item above a list of options"*, implemented as *"an AutoCompleteTextView within
a TextInputLayout"*; the non-editable variant disables user input. That is exactly this field: editable
while searching, non-editable once picked.
**Caveats (Q4):** (a) this evidence is Material Components Android documentation, not m3.material.io,
which CLAUDE.md names as the MD3 source. m3.material.io renders client-side and returned only its
page title to WebFetch on 2026-09-24 (`/components/menus/guidelines`, `/components/text-fields/specs`).
(b) `Menus/` files a component that looks like a text field under the menus category. Helder confirms
the name and folder, or picks an alternative (e.g. `TextFields/`).

### Files (new)

| File | Role |
|------|------|
| `MyVocaList/UI/Components/Menus/ExposedDropdownMenu.xaml` | `ContentView` hosting one `dxe:AutoCompleteEdit` + the option `ItemTemplate` (moved from `SongFormPage.xaml:45-78`). |
| `MyVocaList/UI/Components/Menus/ExposedDropdownMenu.xaml.cs` | BindableProperties, DX event adapters. Thin: every decision calls `ExposedDropdownMenuLogic`. |
| `MyVocaList/UI/Components/Menus/ExposedDropdownMenuLogic.cs` | `internal static` pure functions (§ 4). No MAUI/DX types in signatures except `AutocompleteSuggestion`. |
| `MyVocaList/UI/Models/IMenuOptionSource.cs` | Consumer search contract (§ 3.2) + `MenuOptionSource` delegate adapter. |
| `MyVocaList.Tests/Unit/Components/ExposedDropdownMenuLogicTests.cs` | Level A tests. |

Existing conventions followed: BindableProperty pattern with `…Property` statics and CLR wrappers,
bound in XAML via `Source={x:Reference self}` (`UI/Components/AppBars/SearchBar.xaml.cs:17-35`,
`SearchBar.xaml:22-42`). The MD3 category folder mirrors `AppBars/`, `Lists/`, `Sheets/`, `States/`,
`Toolbars/`. The test project already references the MAUI head for component tests
(`MyVocaList.Tests/MyVocaList.Tests.csproj:33-34`). New files in `Unit/Components/` are compiled,
because only six named frozen tests are removed (`:38-39`).

## 3. Interfaces

### 3.1 Component surface (BindableProperties)

| Property | Type | Mode | Default | Purpose / REQ |
|----------|------|------|---------|---------------|
| `LabelText` | `string` | OneWay | `""` | Floating label ("Artist"). |
| `PlaceholderText` | `string` | OneWay | `""` | "Search artists..." (REQ-ACF-06). |
| `Text` | `string` | **TwoWay** | `""` | The editor text (→ `ArtistSearchText`). |
| `IsLocked` | `bool` | OneWay | `false` | Picked state, consumer-owned (→ `IsArtistLocked`). Drives the lock mechanism (§ 5) and REQ-ACF-04. |
| `IsPickLockEnabled` | `bool` | OneWay | `true` | App-wide default ON (README point 1). A consumer whose pick does not keep a value (e.g. `PersonFormPage` dedup navigation) may set `false`. Q3. |
| `HasError` / `ErrorText` | `bool` / `string` | OneWay | `false` / `""` | Passed to DX `HasError`/`ErrorText` (REQ-DXAC-05 kept). |
| `IsDropDownOpen` | `bool` | **TwoWay** | `false` | → `IsArtistDropDownOpen` (BUG-065/066 lever, `SongFormViewModel.cs:56-61`). The component also forces `false` while locked (REQ-ACF-04). |
| `OptionSource` | `IMenuOptionSource` | OneWay | `null` | Search delegate. `null` means no options are requested. |
| `SelectCommand` | `ICommand` (param `AutocompleteSuggestion`) | OneWay | `null` | Real-option pick (→ `SelectArtistCommand`). |
| `CreateNewCommand` | `ICommand` (param `string` raw text) | OneWay | `null` | **Opt-in**: when non-null, the create-new row is appended (REQ-ACF-11b) (→ `CreateArtistInlineCommand`). |
| `CreateNewNoun` | `string` | OneWay | `"item"` | Renders `Add "«text»" as a new «noun»` ("artist"). |
| `ClearCommand` | `ICommand` | OneWay | `null` | X (→ `ClearArtistCommand`, source-generated from `[RelayCommand] ClearArtist`, `SongFormViewModel.cs:398-399`). |
| `BlurWithoutPickCommand` | `ICommand` | OneWay | `null` | Blur while unlocked with no routed pick (→ `ArtistBlurredWithoutSelectionCommand`). |

Constants are not properties. The component reads `SearchConstants.MinimumLocalQueryLength` and
`SearchConstants.AutocompleteDebounceMilliseconds` via `x:Static` (REQ-ACF-13/14). A future
remote-search consumer would add a `QueryTier` property then (YAGNI now).

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

Why a per-field source object and not an interface on the ViewModel: one ViewModel can host several
fields (the queue-entry form will have Venue, Artist and Singer), so the contract must be per field.
The other members are plain bindings, because they are already field-prefixed properties and
commands.

`SongFormViewModel` must expose (✅ = exists today, unchanged):

| Member | Status |
|--------|--------|
| `ArtistSearchText`, `IsArtistLocked`, `ArtistHasError`, `ArtistErrorText`, `IsArtistDropDownOpen` | ✅ `:49-61` |
| `SelectArtistCommand`, `CreateArtistInlineCommand`, `ArtistBlurredWithoutSelectionCommand` | ✅ `:164-169` |
| `ClearArtistCommand` | ✅ generated from `:398-399` |
| `ArtistOptionSource` (`IMenuOptionSource`) | **new**: `new MenuOptionSource((q, ct) => SearchArtistsCoreAsync(q))`, created in the ctor. `SearchArtistsCoreAsync` (`:298-315`) is unchanged. The token is honoured by the component after the await (current behaviour, `SongFormPage.xaml.cs:60`). |
| Blur/Save message copy | **changes** per REQ-ACF-15 once Q1 is answered (`:378`, `:570`). |

## 4. Pure logic (`ExposedDropdownMenuLogic`), Level A testable

| Function | Rule | REQ |
|----------|------|-----|
| `bool EffectiveLocked(bool isLocked, bool isPickLockEnabled, string? text)` | `isPickLockEnabled && isLocked && !string.IsNullOrWhiteSpace(text)`. **Every other function and the lock mechanism use this value, never the raw `IsLocked`.** It enforces REQ-ACF-05a (locked-and-empty behaves as unlocked, so the field never becomes read-only with no X). | ACF-05a |
| `bool ShouldRequestOptions(bool effectiveLocked, IMenuOptionSource? source)` | `false` if `source is null` or `effectiveLocked`. | ACF-04 |
| `IReadOnlyList<AutocompleteSuggestion> BuildOptions(IReadOnlyList<AutocompleteSuggestion> matches, string query, bool effectiveLocked, string? pickedText, bool createEnabled)` | Returns the matches in order. Appends one create-new row `new AutocompleteSuggestion(query, "", query) { IsCreateNew = true }` last iff `createEnabled && (query?.Trim().Length ?? 0) >= SearchConstants.MinimumLocalQueryLength && !effectiveLocked && !string.Equals(query, pickedText, StringComparison.Ordinal)`. The **trimmed-length** gate closes the whitespace-padding hole: DX `CharacterCountThreshold` counts raw characters, so " a" would otherwise offer "a". The input is the search's **return value** (BUG-056). | ACF-13, -16, -21, -22 |
| `PickRoute RoutePick(object? selectedItem)` | `None` if not an `AutocompleteSuggestion`; `CreateNew` if `IsCreateNew`; else `Select`. | ACF-11d |
| `bool ShouldForwardBlur(object? selectedItem, bool effectiveLocked)` | `selectedItem is null && !effectiveLocked`. A locked field has nothing to validate. | ACF-11e |
| `string? TextToRestore(AutoCompleteEditTextChangeReason reason, bool effectiveLocked, string lockedText)` | *Only used if mechanism C is accepted (§ 5).* Returns `lockedText` when `reason == UserInput && effectiveLocked`; otherwise `null`. | ACF-01 |

The code-behind maps DX events to these functions and then calls `ICommand.CanExecute`/`Execute`.
The BUG-049-style re-entry guard (`CanExecute` before `Execute` for create) is kept
(`SongFormPage.xaml.cs:88-89`).

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
**Chosen approach (primary, B): "read-only + End-icon X".** While `EffectiveLocked` (§ 4):
`IsReadOnly = true`, `ClearIconVisibility = Never`, `EndIcon = close_outlined`
(`Resources/Images/close_outlined.svg` exists), `IsEndIconVisible = true`,
`EndIconColor = OnSurfaceVariant`, and `EndIconClicked` → `ClearCommand`. While unlocked:
`IsReadOnly = false`, `IsEndIconVisible = false`, and the built-in clear icon is used
(`ClearIconVisibility = Auto`, `ClearIconColor` per REQ-ACF-08).
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
(b) `ItemsRequested` → no request (`ShouldRequestOptions`), and `IsDropDownOpen = false`;
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
XAML (`SongFormPage.xaml:28-44`) or in the implicit style (`MaterialStyles.xaml:74-80`). The component
sets `SubmitIconVisibility="Never"`. Nothing in the app handles `Submitted`/`SubmitCommand` (census:
0 hits), so no behaviour is lost.

## 6. Interaction flows

```
UNLOCKED, typing:
  keystroke ─► DX debounce (AutocompleteDebounceMilliseconds) ─► ItemsRequested
     ─► ShouldRequestOptions? no ─► (nothing; menu stays closed)
                             yes ─► OptionSource.SearchAsync(text, ct) ─► ct check
                                  ─► BuildOptions(matches, text, …) ─► menu shows options [+ create-new]
  < MinimumLocalQueryLength chars ─► DX requests nothing (CharacterCountThreshold)

PICK (option or create-new):
  SelectionChanged ─► RoutePick ─► Select: SelectCommand(option)  |  CreateNew: CreateNewCommand(text) (CanExecute guard)
     ─► SelectedItem = null (BUG-061)
     ─► VM LockArtist (SongFormViewModel.cs:328-338): text = name, IsArtistDropDownOpen = false, IsArtistLocked = true
     ─► component sees IsLocked=true ─► applies lock mechanism (B or C), forces IsDropDownOpen=false,
        SelectedItem = null again

LOCKED:
  tap on text      ─► B: nothing (read-only)  |  C: Unfocus; any UserInput change reverted
  blur             ─► ShouldForwardBlur = false ─► nothing
  ItemsRequested   ─► ShouldRequestOptions = false ─► nothing (REQ-ACF-04 / BUG-069)
  tap X            ─► ClearCommand ─► VM ClearArtist (:399-409) ─► IsLocked=false
                   ─► component removes lock, then Focus() (REQ-ACF-06; DX Focus() shows the keyboard
                      when not read-only, EditBase.Focus IL)

EDIT-MODE LOAD:
  VM LoadSong/InitializeArtistField set text + IsArtistLocked=true + IsArtistDropDownOpen=false
  (:415-425, :469-477) ─► programmatic text: DX fires no ItemsRequested (BUG-065/066 IL evidence)
  ─► component in locked state from first bind; SelectedItem = null on IsLocked→true
     (replaces SongFormPage.xaml.cs:38)
```

State machine (field, from the component's view):

```
Unlocked-Empty  --type (≥ min chars)--> Unlocked-Searching
Unlocked-Searching --pick option / create succeeds--> Locked
Unlocked-Searching --blur--> Unlocked-Text (+ error per REQ-ACF-15)
Unlocked-Text   --type--> Unlocked-Searching
Unlocked-*      --X (text non-empty)--> Unlocked-Empty (focused)
Locked          --X--> Unlocked-Empty (focused)
Locked          --any other input--> Locked (no change)
(any)           --VM sets IsLocked=true (hydration/picker)--> Locked
```

## 7. Invariants and postconditions

- **INV-1:** `EffectiveLocked` ⇒ the editor text equals the value the VM set, the menu is closed,
  and no search call is issued.
- **INV-1a:** the field is never read-only while its text is empty (REQ-ACF-05a, enforced by
  `EffectiveLocked` in the component, independent of the consumer VM).
- **INV-2:** X is visible ⇔ the text is non-empty, and X is never rendered in the disabled style
  while visible.
- **INV-3:** After any routed pick, DX `SelectedItem == null`.
- **INV-4:** No page code-behind references DX autocomplete events for a field hosted by this
  component.
- **Post (X):** VM selection is cleared (REQ-ACREATE-15 semantics), the field is unlocked, empty and
  focused.

## 8. Shared constants

### Decision: debounce constant lives in `SearchConstants` (proposed, Helder confirms: Q10)
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

Threshold: the component uses `SearchConstants.MinimumLocalQueryLength` (2). This changes the Song
Artist field from `1` (`SongFormPage.xaml:42`). `PersonFormPage.xaml:29` already uses `2` but as a
literal; that is fixed when it migrates.

## 9. DI / registration impact

None. The component is instantiated from XAML and is not DI-registered. `MauiProgram.cs` is not
touched (a sequential-only file, spared). `MenuOptionSource` is created by the ViewModel. The two
suggestion services (`Services/ArtistSuggestionService.cs`, `Services/SongSuggestionService.cs`)
stay unregistered on purpose; a future consumer wraps them in a `MenuOptionSource`.

## 10. Test strategy (testing.md risk tiers)

| Level | What | Where |
|-------|------|-------|
| **A** | `ExposedDropdownMenuLogic`: every row of § 4, with all branches (locked/unlocked, pick-lock on/off, null source, whitespace query, query == picked text, create enabled/disabled, `Reason` × locked). Each test carries `// [AC] REQ-ACF-NN`. | `MyVocaList.Tests/Unit/Components/ExposedDropdownMenuLogicTests.cs` |
| **A** | `SongFormViewModel.ArtistOptionSource.SearchAsync` returns `SearchArtistsCoreAsync`'s list (BUG-056 contract); REQ-ACF-15 copy on blur and Save (after Q1: the existing message assertions are updated **with the spec** by the Tester, never by the Builder). | `SongFormViewModelTests.cs` |
| **B** | `SearchConstants.AutocompleteDebounceMilliseconds == 300` (matches the existing constant-value tests, e.g. `SongSuggestionThresholdTests.cs:55`). | the existing threshold test class, or a new `SearchConstantsTests.cs` |
| **C** | Component XAML shell, BindableProperty plumbing, page XAML swap: build + manual E2E. The no-test decision is recorded in the task-log. | — |
| **Regression** | All existing `SongFormViewModelTests` artist tests stay green unchanged (list in requirements § G), except the REQ-ACF-15 message assertions. | full `dotnet test` |
| **Manual E2E (device, Helder)** | T7 checklist: REQ-ACF-01…10, -13, -16, -20, -23, -25, -27. Android only. | task-log |

What cannot be tested at the VM seam: the actual read-only rendering, X enablement and colour, the
keyboard behaviour, the submit icon's absence, DX `SelectedItem` highlight, and menu reopen timing.
These are DX rendering concerns, covered by T1 (spike) and T7 (E2E).

## 11. Governance

- **New component, one consumer → not governed** (`component-safety-gate.md` § Relationship to other
  rules: "Adding a brand-new component … is NOT governed … Governance begins at the second consumer").
- The moment a second page binds (expected: `PersonFormPage`), add `ExposedDropdownMenu` to the
  governed table in `.claude/library/component-safety-gate.md` **in the same commit**, and that
  migration is a four-gate component task.
- `SongFormPage.xaml` edits follow the incremental XAML rule: one file → build → fix → next.
- `SearchConstants.cs` is not on the sequential-only registry. It is edited by one task only.

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

**Rollout sizing**

| Consumer | Today | This item | Follow-up |
|----------|-------|-----------|-----------|
| `SongFormPage` Artist | DX `AutoCompleteEdit` + ~70 lines of glue (`SongFormPage.xaml:28-79`, `.xaml.cs:38,41-113`) | **Migrated** | — |
| `PersonFormPage` Full Name | DX `AutoCompleteEdit` + glue (`PersonFormPage.xaml:19-43`, `.xaml.cs:33-62`); pick navigates to the existing person (`PersonFormViewModel.cs:295-313`) | No | Migrate with `IsPickLockEnabled=false`? (Q3). Second consumer → governance starts. |
| `ArtistFormPage` duplicate suggestions | BindableLayout list under the name field (`ArtistFormPage.xaml:46-51`) | No | Candidate. |
| `ArtistPickerPage`, `SongPickerPage`, `YouTubeSearchPage` | Navigate-away pickers | No | Out of scope (likely deleted). |
| Future queue-entry form (Venue/Artist/Singer) | not built | No | Intended consumer (DX-AC design § Wrapper promotion trigger). |
| List-page search (`CrudListViewModelBase.cs:238`, `Task.Delay(400)`) | list search debounce literal | No | Candidate for a shared constant (not autocomplete). |

## 13. Conflicts and notes found while drafting

1. **BUG-069 is fixed and must stay fixed; mechanism B puts that at risk.** Helder confirmed on
   device on 2026-09-24, after the BUG-081 merge (`2d70ac73`), that BUG-069 no longer reproduces
   (`Docs/Management/LEDGER.md`, INLINE-AC row, "Update 2026-09-24"; likely a side effect of dropping
   `IsReadOnly`). BUG-081's README "Related" note is the older 2026-09-23 observation. BUG-069 never
   had a generated row, so there is nothing to close. Because B re-introduces `IsReadOnly`, T1 has
   criterion B5 (REQ-ACF-25). BUG-070 still reproduces and is owned here (REQ-ACF-15).
6. **Song-picker path vs "X is the only way":** `ResolveAndLockArtistAsync` (`SongFormViewModel.cs:516-538`)
   re-locks to a different artist, or unlocks (`:537`), without X. Left open as Q6.
7. **Locked-and-empty:** edit-mode load sets `IsArtistLocked = true` even when `song.ArtistId == 0`
   (`SongFormViewModel.cs:469-477`). Neutralised in the component by `EffectiveLocked` (REQ-ACF-05a).
2. **REQ-ACREATE-02/10 vs REQ-UOW-51:** "≥1 non-whitespace char" conflicts with the shared minimum of
   2 (Q2).
3. **REQ-ACREATE-15's type-over branch vs README point 1:** superseded (requirements § Supersedes).
4. **BUG-081 fix rationale vs README point 1:** the fix's reasoning ("literally non-editable was an
   over-restriction") is reversed by Helder's new requirement. The fix's outcome (a live X) stays.
5. **Validation-message location:** the artist messages are VM literals (`SongFormViewModel.cs:378,
   569-570`), while the Form Validation Standard puts messages in services. This is pre-existing and
   out of scope.
