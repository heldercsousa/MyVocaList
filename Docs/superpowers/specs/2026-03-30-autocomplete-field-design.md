# AutocompleteField Component — Design Spec

**Date:** 2026-03-30
**Last updated:** 2026-03-31
**Status:** Approved — pending implementation plan. Prerequisite: Styles & Structure must be implemented first (EmptyState, ConfirmSheet, ListItemLeadingMonogram, named styles all in place before this component is built).

---

## Problem

Multiple features need "type to search existing records → see a filtered overlay list → tap to act":
- **Person form:** type a name → see matching singers → tap → navigate to edit (dedup detection)
- **Queue enqueue (future):** type a name → see matching singers → tap → enqueue that person
- **Other entities (future):** venues, songs, or any searchable record

Without a shared component, each usage site reimplements the same UI (TextEdit + debounce + overlay card + result list), diverging over time and accumulating bugs in parallel.

---

## Solution

A reusable `AutocompleteField` ContentView implementing the **MD3 Docked Search Bar** pattern: a text input that, when results exist, shows a contained card below it with filtered results rendered as MD3 list items. The component is entity-agnostic — it operates on `AutocompleteSuggestion` records that any caller can project their entity onto.

---

## Contract

### AutocompleteSuggestion

```csharp
// Contracts/Models/AutocompleteSuggestion.cs
public record AutocompleteSuggestion(string Headline, string SupportingText, object Data);
```

- `Headline` — primary display text (e.g. person's full name, song title)
- `SupportingText` — optional secondary line (e.g. email/birthday, artist name); `null` or empty = 1-line row
- `Data` — the original entity, passed back through `SuggestionSelectedCommand` for caller to handle

Lives in **Contracts** (not MAUI) — pure data shape usable by ViewModels and potentially other layers.

---

## Component API

**File:** `MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml` + `.xaml.cs`
**Namespace:** `xmlns:autocomplete="clr-namespace:MyVocaList.UI.Components.AutocompleteField"`

### BindableProperties

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| `LabelText` | `string` | `""` | Forwarded to inner `TextEdit.LabelText` |
| `Placeholder` | `string` | `""` | Forwarded to inner `TextEdit.PlaceholderText` |
| `HasError` | `bool` | `false` | Forwarded to inner `TextEdit.HasError` |
| `ErrorText` | `string` | `""` | Forwarded to inner `TextEdit.ErrorText` |
| `Suggestions` | `IEnumerable<AutocompleteSuggestion>` | `null` | Drives overlay list; `null` or empty = overlay hidden |
| `DebounceDelay` | `int` | `300` | Milliseconds before `SearchRequestedCommand` fires after typing stops |
| `SearchRequestedCommand` | `ICommand` | `null` | Fired with current text (`string`) after debounce elapses |
| `SuggestionSelectedCommand` | `ICommand` | `null` | Fired with tapped `AutocompleteSuggestion` |

### Derived (internal, no BP needed)
- `HasSuggestions` — `Suggestions?.Any() == true` — drives overlay `IsVisible`

---

## Anatomy

```
AutocompleteField (ContentView)
└── Grid (single cell, ZIndex overlay pattern)
     ├── Row 0: TextEdit
     │     BoxMode=Outlined, standard DX TextEdit styling
     │     TextChanged → restarts debounce CancellationTokenSource
     │
     └── Row 0 + RowSpan overflow: DXBorder card  [overlay — does NOT push content below]
           BackgroundColor=SurfaceContainerHigh
           CornerRadius=12
           Shadow=Level2 (BoxShadow)
           IsVisible=HasSuggestions
           ZIndex=10
           └── DXCollectionView
                 MaximumHeightRequest=(5 × 56dp = 280dp)
                 IsScrollEnabled=True (scrolls if > 5 results)
                 └── DataTemplate → ListItem
                       Headline=suggestion.Headline
                       SupportingText=suggestion.SupportingText (nil = 1-line)
                       Tap → SuggestionSelectedCommand.Execute(suggestion)
```

The card overlays content below the field — achieved by placing both the TextEdit and the card in the same `Grid` row. The card uses `VerticalOptions=Start` + `Margin.Top` equal to the TextEdit height (~56dp) so it appears anchored just below the field without disturbing the layout flow.

---

## Behavior

| Trigger | Action |
|---------|--------|
| User types | Cancel previous debounce `CancellationTokenSource`; start new one with `DebounceDelay` ms |
| Debounce elapses | `SearchRequestedCommand.Execute(currentText)` |
| `Suggestions` set by caller | `HasSuggestions` recalculated → overlay appears |
| User taps a row | `SuggestionSelectedCommand.Execute(suggestion)` |
| Text length drops below 2 chars | Component clears `Suggestions` internally and hides overlay — `SearchRequestedCommand` is not fired |
| Field loses focus | Overlay hides (300ms delay to allow tap to register first) |

**Debounce implementation:** `CancellationTokenSource` + `Task.Delay` inside `TextEdit.TextChanged` handler in code-behind. No timer object — consistent with existing debounce pattern in `VenuesViewModel.TriggerSearchDebounce`.

---

## Usage Examples

### Person form (dedup detection)

```xml
<autocomplete:AutocompleteField
    LabelText="Full Name"
    Placeholder="First and last name"
    HasError="{Binding NameHasError}"
    ErrorText="{Binding NameErrorText}"
    Suggestions="{Binding Suggestions}"
    SearchRequestedCommand="{Binding SearchPersonsCommand}"
    SuggestionSelectedCommand="{Binding SuggestionSelectedCommand}" />
```

```csharp
// ViewModel — no debounce logic, no timer
[RelayCommand]
async Task SearchPersonsAsync(string term)
{
    var results = await _personService.SearchPersonsStartsWithAsync(term, 5);
    Suggestions = results.Select(p =>
        new AutocompleteSuggestion(p.FullName, p.GetDisplayIdentifier(), p));
}

[RelayCommand]
void SuggestionSelected(AutocompleteSuggestion s)
{
    var person = (Person)s.Data;
    // Navigate to edit form for that person
    Shell.Current.GoToAsync($"{Routes.PersonForm}?personId={person.Id}&...");
}
```

### Queue enqueue (future)

```xml
<autocomplete:AutocompleteField
    LabelText="Singer"
    Placeholder="Search by name or email"
    Suggestions="{Binding SingerSuggestions}"
    DebounceDelay="400"
    SearchRequestedCommand="{Binding SearchSingersCommand}"
    SuggestionSelectedCommand="{Binding EnqueueSingerCommand}" />
```

Same component. Different delay. Different tap action. Zero duplication.

---

## MD3 Compliance

| Spec | Value | Rationale |
|------|-------|-----------|
| Pattern | Docked Search Bar | Compact search + results in contained surface; does not go full-screen |
| Result rows | `ListItem` (existing component) | Results carry metadata (name + disambiguator) → list items, not menu items |
| Result surface | `SurfaceContainerHigh`, `CornerRadius=12` | Medium shape per MD3; elevated above page surface |
| Elevation | Level 2 shadow | Conveys temporariness; visible via tint in dark mode |
| Max visible rows | 5 (280dp) | Avoids overwhelming the form; scrollable if more results |
| Row height | 56dp (1-line) / 72dp (2-line) | Standard MD3 list item heights |

---

## What the Component Does NOT Do

- Does not own the search query. Caller owns `SearchText` if it needs it (e.g. for form state).
- Does not validate the typed text. Caller owns validation (`HasError` / `ErrorText` BPs are just forwarded to the inner `TextEdit`).
- Does not know what entity it is searching. `AutocompleteSuggestion.Data` is `object` — the caller casts it.
- Does not navigate. `SuggestionSelectedCommand` fires; the caller decides the action.

---

## Files

| File | Purpose |
|------|---------|
| `Contracts/Models/AutocompleteSuggestion.cs` | Shared data shape |
| `MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml` | Layout: TextEdit + overlay card + DXCollectionView |
| `MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml.cs` | Debounce logic, `HasSuggestions` computation, focus/blur overlay guard |

---

## Impact on Person CRUD spec

`PersonFormPage` no longer has a custom suggestion overlay — it uses `AutocompleteField` instead. The `PersonsPage` search (in `SearchAppBar`) is unaffected — that is a full-page list search, not an inline autocomplete.

`PersonFormViewModel` changes:
- Remove `Suggestions` collection + `HasSuggestions` + suggestion debounce
- Add `SearchPersonsCommand(string term)` — responds to already-debounced event
- Add `SuggestionSelectedCommand(AutocompleteSuggestion s)` — handles tap action

`Docs/specs/persons/design.md` and `tasks.md` should be updated to reference `AutocompleteField` instead of the inline overlay description.
