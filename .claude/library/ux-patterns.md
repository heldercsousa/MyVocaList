# UX Patterns

## Touch Targets

All interactive controls must be at least **48×48dp** (WCAG 2.5.5 / MD3).

```xml
<dx:DXButton WidthRequest="48" HeightRequest="48" CornerRadius="24" ... />
```

Icon-only FABs use 56×56 with CornerRadius=16 (MD3 standard FAB shape).

## Form Validation Timing (pointer)

Form-field validation timing follows the **Form Validation Standard** in `dialogs-validation.md § Form
Validation Standard` — validate on blur (dirty fields), keystroke-on-error to clear immediately, Save as the
safety net. Two IxD refinements (Nielsen heuristics) apply:

- **Do not fire a blur error on a pristine field** the user only tabbed through without editing (error
  prevention, H5) — validate on blur only once the field is dirty, or on Save. Firing on untouched fields is
  the "impatient teacher" anti-pattern.
- **Async guidance/availability checks** (username, duplicate name) must show a **pending status indicator**
  while in flight (visibility of system status, H1), and every error message must be **specific and
  actionable** — say what is wrong and how to fix it, never a bare "Invalid" (help users recover, H9).

The standard itself is single-sourced in `dialogs-validation.md`; this is a pointer.

## ~~MD3 Contextual Action Bar (Multi-Select Mode)~~ — RETIRED

> **This pattern is retired as of the Venues MD3 rebuild (2026-03-29).**
> The 5-column Grid contextual bar in `Shell.TitleView` is replaced by:
> - `SmallAppBar.Title` binding showing "N selected" (no separate contextual bar needed)
> - `FloatingToolbar` for page-level actions (Select All / Edit / Delete)
>
> See `.claude/rules/crud-pages.md` for the current pattern.

## Form Action Buttons vs Toolbar Action Icons

These are intentionally different — do not "unify" them:

| Context | Pattern | Reason |
|---------|---------|--------|
| Add/Edit form (Shell nav page) | Labeled buttons — `Cancel` + `Save` | User needs label clarity when entering data |
| Contextual action bar (multi-select) | Icon-only buttons (48×48) | Space is constrained; actions are reversible/confirmable |

Both are correct per MD3. Labeled toolbar actions (`Content="Select all"`) are acceptable
only when icon meaning is ambiguous.

## Multi-Select Mode Behavior

- **Enter**: Long press on an item → `EnterMultiSelectMode(item)` → item is pre-selected → haptic feedback
- **Tap in multi-select**: DXCollectionView handles selection natively — do NOT double-toggle in `OnItemTapped`
- **Tap in normal mode**: Navigate to detail/edit page
- **Select All toggle**: If all selected → deselect all but **remain in multi-select mode** (do not exit)
- **Exit**: Cancel button or selection count drops to 0 via selection event (suppress exit when programmatically clearing)
- **Swipe suppressed during multi-select**: `OnSwipeItemShowing` → `e.Cancel = true` when `IsMultiSelectMode`

```csharp
// Select All — stays in multi-select even when deselecting
private void ToggleSelectAll()
{
    if (IsAllSelected)
    {
        _suppressSelectionChangedExit = true;
        RunOnUiThread(() =>
        {
            SelectedItems.ClearRange();
            _suppressSelectionChangedExit = false;
        });
        SelectedCount = 0;
        return;
    }
    IsMultiSelectMode = true;
    _suppressSelectionChangedExit = true;
    RunOnUiThread(() =>
    {
        SelectedItems.ReplaceRange([.. Items]);
        _suppressSelectionChangedExit = false;
    });
    SelectedCount = Items.Count;
}
```

## Empty State Positioning

Empty state containers must be **vertically centered**, not top-aligned:

```xml
<VerticalStackLayout VerticalOptions="Center"
                     HorizontalOptions="Center"
                     Spacing="8" Margin="32">
    <!-- icon, heading, subtitle -->
</VerticalStackLayout>
```

Never use `VerticalOptions="Start"` for empty/error/loading placeholder states.

## Autocomplete — Responsive Full-Screen Expansion (MD3)

> Authoritative encoding of BACKLOG ① *Autocomplete Mobile UX Pattern — Full-Screen Expansion Guideline*
> (2026-07-15), written from the proven concept: `AutocompleteField` + `AutocompleteMobileField`, first
> applied on PersonFormPage. Early research input: `Docs/Management/DevCycleCraft/autocomplete-component/md3-autocomplete-mobile-ux-guideline.md`.
> MD3 currency verified 2026-07-15 against m3.material.io/components/search/guidelines.
> ⏳ Pending Helder authorship review (CLAUDE.md § Continuous Enhancement — Authorship).

**Rule — responsive by window size class, per the MD3 Search guidelines** (suggestions/results are
full-screen on compact windows, docked on larger ones):

| Window size class | Autocomplete presentation |
|-------------------|---------------------------|
| **Compact** (< 600dp width — phone portrait) | **Full-screen Search View** pushed modally: search input in an AppBar-style header, back affordance, results fill the space between input and keyboard. In this codebase: `AutocompleteMobileField`, triggered by `AutocompleteField` when `IDeviceInfo.Idiom == Phone`. |
| **Medium/Expanded** (≥ 600dp — tablet, desktop) | **Exposed-dropdown / docked overlay** below the input (results list anchored to the field). In this codebase: `AutocompleteField`'s overlay card, unchanged. |

Why: on a compact window the keyboard consumes ~40–50% of the height; a dropdown anchored mid-page
leaves room for 1–2 rows — unusable. Full-screen expansion (Google Maps / Gmail / Android Settings
search) gives the results list the whole area between input and keyboard.

Implementation invariants (proven by BUG-040…045/047 fixes — do not regress):

- **Auto-focus + keyboard raise** on Search View appear must be deferred past the push animation (`Dispatcher.DispatchDelayed`), or Android drops focus (BUG-040).
- **Reopen guard**: dismissing the Search View refocuses the underlying field — a one-shot suppression (`MobileFieldReopenGuard`) must swallow that refocus or the view reopens/duplicates (BUG-041/042).
- **Trim-safe wiring**: the dynamically-instantiated mobile view must use event-driven wiring, never reflection `SetBinding` (linker trims it in Release — BUG-043).
- **Programmatic-text guard**: ViewModel hydration of `Text` (Edit-mode load) must NOT trigger the suggestion search — only user-driven input does (BUG-047).
- Every dismissal path (in-view back, hardware back, system gesture) must raise `Cancelled` → `BlurredWithoutSelectionCommand` so typed text is preserved and validation runs.
- **No-match → add-new affordance:** an explicit `Add "<typed text>"` action row (creatable-autocomplete pattern), never back-button-only — analysis + spec input: `Docs/Management/DevCycleCraft/autocomplete-component/ux-no-match-add-new-analysis.md` (implementation pending, governed-component gates apply).

MD3 terminology: **Search bar → Search view** (m3.material.io/components/search); the docked variant
maps to MD3 *Menus — filtering* / exposed dropdown.

## Badge / Chip Pattern (event count)

Use `dx:DXBorder` + `Label` for informational badges. Show count, not just a binary flag:

```xml
<dx:DXBorder IsVisible="{Binding HasEvents}"
             BackgroundColor="{StaticResource SecondaryContainer}"
             CornerRadius="12"
             Padding="8,4">
    <Label Text="{Binding EventCount, StringFormat='{0} events'}"
           FontFamily="RobotoMedium" FontSize="11"
           TextColor="{StaticResource OnSecondaryContainer}"
           VerticalOptions="Center" />
</dx:DXBorder>
```

`HasEvents` is a derived property (`EventCount > 0`) on the DTO — not a separate DB column.
The repository counts via `v.Events.Count()` in the EF Core projection (translated to SQL COUNT).
