# Dialogs & Validation Rules

## Non-Negotiable: No Native Dialogs
NEVER use:
- `DisplayAlert()`
- `DisplayActionSheet()`
- `DisplayPromptAsync()`

All user confirmation and input collection must use DevExpress `BottomSheet`.

---

## BottomSheet Patterns

## ConfirmSheet component

Use `ConfirmSheet` for destructive action confirmations. It wraps `dx:BottomSheet` with
standard MD3 styling and handles its own Show/Close based on the `SheetState` BindableProperty.

```xml
<sheets:ConfirmSheet x:Name="confirmSheet"
                     SheetState="{Binding ConfirmSheetState, Mode=TwoWay}"
                     Message="{Binding ConfirmMessage}"
                     ActionText="{Binding ConfirmActionText}"
                     ActionCommand="{Binding ConfirmActionCommand}"
                     DismissCommand="{Binding DismissConfirmCommand}" />
```

Namespace: `xmlns:sheets="clr-namespace:MyVocaList.UI.Components.Sheets"`

The ViewModel retains the same `ConfirmSheetState`, `ConfirmMessage`, `ConfirmActionText`,
`ConfirmActionCommand`, `DismissConfirmCommand` properties — no ViewModel changes needed.
The page code-behind no longer needs to subscribe to `PropertyChanged` for sheet management.

**⚠️ Known limitation (confirmed 2026-04-02):** `dx:BottomSheet` wrapped inside a `ContentView`
causes an ANR on initialization — `bottomSheet.Close()` hangs when called before the sheet has
ever been opened. The `ConfirmSheet` component exists in `UI/Components/Sheets/` but **must not
be used** until DevExpress resolves this. Use the inline `dx:BottomSheet` pattern below instead.

---

## When to Use BottomSheet vs. Shell Navigation Form

| Scenario | Pattern |
|----------|---------|
| Confirm / destructive action (delete, clear) | BottomSheet (stays on same page) |
| Add / Edit form with text input | Shell navigation page with `SafeAreaEdges="All"` + `ScrollView` |

Reason: BottomSheets conflict with the soft keyboard on Android — the keyboard can cover input fields inside the sheet. A dedicated navigation page with `SafeAreaEdges="All"` handles keyboard avoidance natively.

See `devexpress-patterns.md` → **Shell Navigation Form Page** for the full XAML + ViewModel pattern.

---

### Edit / Input Sheet — confirmed in VenuesPage.xaml (DEPRECATED — use Shell nav page instead)
```xml
<dx:BottomSheet x:Name="editBottomSheet"
                HalfExpandedRatio="0.4"
                AllowedState="HalfExpanded"
                IsModal="True"
                ShowGrabber="True"
                AllowDismiss="True"
                BackgroundColor="{StaticResource Surface}"
                CornerRadius="28"
                StateChanged="OnBottomSheetStateChanged">
    <VerticalStackLayout Padding="24" Spacing="16">
        <!-- Title -->
        <Label Text="{Binding BottomSheetTitle}"
               FontFamily="RobotoMedium" FontSize="24"
               TextColor="{StaticResource OnSurface}" />

        <!-- Input field with validation -->
        <dxe:TextEdit x:Name="venueNameEdit"
                      Text="{Binding EditingVenueName, Mode=TwoWay}"
                      LabelText="Field Label"
                      PlaceholderText="Enter value"
                      BoxMode="Outlined"
                      FocusedBorderColor="{StaticResource Primary}"
                      BorderColor="{StaticResource Outline}"
                      BackgroundColor="{StaticResource SurfaceContainerHighest}"
                      TextColor="{StaticResource OnSurface}"
                      HasError="{Binding FieldHasError}"
                      ErrorText="{Binding FieldErrorText}" />

        <!-- Action buttons -->
        <HorizontalStackLayout HorizontalOptions="End" Spacing="8">
            <dx:DXButton Content="Cancel"
                         Style="{StaticResource OutlinedButton}"
                         Padding="24,0"
                         Command="{Binding CancelCommand}" />
            <dx:DXButton Content="Save"
                         Style="{StaticResource FilledButton}"
                         Padding="24,0"
                         Command="{Binding SaveCommand}" />
        </HorizontalStackLayout>
    </VerticalStackLayout>
</dx:BottomSheet>
```

**ViewModel state binding:** Use `BottomSheetState` enum (`Hidden`, `HalfExpanded`, `FullExpanded`):
```xml
<!-- Bind in XAML code-behind or via StateChanged event -->
```
> Note: In VenuesPage, `BottomSheetState` is bound via code-behind's `StateChanged` event, not direct XAML binding. The ViewModel sets `BottomSheetState = BottomSheetState.HalfExpanded` and the page's event handler opens/closes the sheet.

---

### Confirm / Destructive Action Sheet — confirmed in VenuesPage.xaml
```xml
<dx:BottomSheet x:Name="confirmSheet"
                HalfExpandedRatio="0.28"
                AllowedState="HalfExpanded"
                IsModal="True"
                ShowGrabber="True"
                AllowDismiss="True"
                BackgroundColor="{StaticResource Surface}"
                CornerRadius="28"
                StateChanged="OnConfirmSheetStateChanged">
    <VerticalStackLayout>
        <Label Text="{Binding ConfirmMessage}"
               FontFamily="RobotoMedium" FontSize="16"
               TextColor="{StaticResource OnSurface}"
               HorizontalTextAlignment="Center"
               Margin="24,20" />
        <BoxView HeightRequest="1" BackgroundColor="{StaticResource OutlineVariant}" />
        <dx:DXButton Content="{Binding ConfirmActionText}"
                     BackgroundColor="Transparent"
                     TextColor="{StaticResource Error}"
                     HorizontalOptions="Fill"
                     HeightRequest="56"
                     Command="{Binding ConfirmActionCommand}" />
        <BoxView HeightRequest="1" BackgroundColor="{StaticResource OutlineVariant}" />
        <dx:DXButton Content="Cancel"
                     BackgroundColor="Transparent"
                     TextColor="{StaticResource Primary}"
                     HorizontalOptions="Fill"
                     HeightRequest="56"
                     Command="{Binding DismissConfirmCommand}" />
    </VerticalStackLayout>
</dx:BottomSheet>
```

---

## TextEdit Validation (HasError / ErrorText)

Confirmed working pattern from `VenuesPage.xaml`:

```xml
<dxe:TextEdit ...
              HasError="{Binding VenueNameHasError}"
              ErrorText="{Binding VenueNameErrorText}" />
```

ViewModel properties:
```csharp
[ObservableProperty] private bool _venueNameHasError;
[ObservableProperty] private string _venueNameErrorText = string.Empty;

// Set error
VenueNameHasError = true;
VenueNameErrorText = "Venue name is required";

// Clear error
VenueNameHasError = false;
VenueNameErrorText = string.Empty;
```

**Rule:** Never use `DisplayAlert` for validation errors. Set `HasError`/`ErrorText` inline under the field.
See **Form Validation Standard** below for the full per-field timing, wiring, and surfacing rules.

---

## Form Validation Standard

> **This is the single, canonical form-input validation standard for MyVocaList. Every form page
> (Venue, Person/Singer, Songs, Artists, and all future forms) must follow it.** The **Venue form** is the
> single-field reference; the **Person form** (name + birthday + email) is the multi-field reference that
> Songs and Artists copy. Sourced from `Docs/Management/DevCycleCraft/ui-form-validation-guide/`.
>
> Constitutional constraints this standard upholds: DevExpress-first, native-dialog ban (no
> `DisplayAlert`/summary for validation), MD3 terminology, English-only, and **business logic in Services**
> (validation rules live in `Validate<Field>Input` service methods — never in ViewModels or pages).

### Validation timing — blur first, keystroke on error, submit as safety net

The "Gold Standard" UX rule is **punish late, reward early**. Validation timing is per field and depends on
the field's state:

1. **On blur** (field `Unfocused`): if the field is **dirty** (the user has edited it), run the field's
   service validator and set `HasError`/`ErrorText`. Do **not** fire an error on a *pristine* field the user
   only tabbed through without editing — that is premature and feels aggressive. (Untouched required fields
   are caught by the Save safety net.)
2. **While a field is in error:** re-validate that field **on every keystroke** so the error clears the
   instant it becomes valid ("reward early"). On `TextChanged`, if `<Field>HasError` is currently `true`,
   re-run the validator and clear the error when it passes. Do **not** run full validation on keystroke for a
   field that is *not yet* in error (the "impatient teacher" anti-pattern).
3. **On Save:** re-run **all** field validators (final safety net) plus any cross-field / uniqueness / DB
   checks that require the service (duplicate name, email-taken, confirm-password, inventory). A Save-time
   service failure maps back to the offending field's `HasError`/`ErrorText`.

**Error messages must be specific and actionable** — say what is wrong and how to fix it (e.g. "Name must be
30 characters or fewer"), never a bare "Invalid".

#### Decision table — field type → when it validates

| Field type | Blur (dirty) | Keystroke while in error | Debounced keystroke (~500 ms) | Submit |
|------------|:---:|:---:|:---:|:---:|
| Standard field (name, email, masked date) | ✅ validate | ✅ clear-on-fix | — | ✅ safety net |
| Guidance-only (character counter, strength meter) | — | — | ✅ guidance | — |
| Availability / uniqueness (username, duplicate name) | — | — | ✅ guidance + pending indicator | ✅ authoritative check |
| Cross-field (confirm-password, inventory) | — | — | — | ✅ only |

For any async guidance/availability check, surface a **pending status indicator** while the request is in
flight (visibility of system status) and debounce ~500 ms so the server is not hit per keystroke.

#### Anti-patterns — forbidden as the primary channel

| Anti-pattern | What it is | Why forbidden |
|--------------|-----------|---------------|
| **Wall of Red** | Validating only on Submit, bouncing the user to the top with a list of errors | High cognitive load, form abandonment (R5) |
| **Impatient Teacher** | Validating every keystroke *always* (even before the field is in error) | Penalizes the user mid-entry; aggressive and distracting |
| **Native dialog / summary / snackbar for validation** | `DisplayAlert`, an error-summary banner, or a snackbar to report a field error | Bypasses theme + MD3; not field-addressed; snackbar is for non-blocking success only |

### Wiring the pattern (ViewModel + XAML)

**Blur hook — CONFIRMED (DevExpress 25.2.x, Context7 2026-07-01):** `dxe:TextEdit` and `dxe:DateEdit` inherit
the MAUI `Unfocused` event (from `VisualElement`). Subscribe to it in the page code-behind and invoke a VM
`Validate<Field>Command`. (If a form is ever migrated to `dxdf:DataFormView`, the DX-native equivalent is
`ValidationMode="LostFocus"` — the app uses standalone editors today, so `Unfocused` is the mechanism.)

**Service (business logic — required):**
```csharp
// One validator per field, returning the standard tuple (see code-principles.md § Service Return Patterns).
(bool isValid, string message) ValidateNameInput(string name);
```

**ViewModel (invokes validation, maps result to HasError/ErrorText — no business rules here):**
```csharp
[ObservableProperty] private string _name = string.Empty;
[ObservableProperty] private bool _nameHasError;
[ObservableProperty] private string _nameErrorText = string.Empty;
private bool _nameDirty;   // becomes true once the user edits the field

// Blur: validate only a dirty field
[RelayCommand]
private void ValidateName()
{
    if (!_nameDirty) return;                       // pristine field: no premature error
    var (isValid, message) = _service.ValidateNameInput(Name);
    NameHasError = !isValid;
    NameErrorText = isValid ? string.Empty : message;
}

// Keystroke: mark dirty; re-validate ONLY if already in error (reward early)
partial void OnNameChanged(string value)
{
    _nameDirty = true;
    if (!NameHasError) return;                     // not in error yet: do nothing (no impatient teacher)
    var (isValid, message) = _service.ValidateNameInput(value);
    NameHasError = !isValid;
    NameErrorText = isValid ? string.Empty : message;
}
```

**XAML (inline error binding + blur hook in code-behind):**
```xml
<dxe:TextEdit x:Name="nameEdit"
              Text="{Binding Name, Mode=TwoWay}"
              LabelText="Name"
              HasError="{Binding NameHasError}"
              ErrorText="{Binding NameErrorText}"
              Unfocused="OnNameUnfocused" />
```
```csharp
// Page code-behind — bridge the MAUI Unfocused event to the VM command
private void OnNameUnfocused(object sender, FocusEventArgs e) => ViewModel.ValidateNameCommand.Execute(null);
```

### Error surfacing — inline only

- Surface every validation error **inline, under the field**, via `dxe:TextEdit`/`dxe:DateEdit`
  `HasError` + `ErrorText`. Never a summary banner, dialog, or snackbar for validation.
- **Field-addressed errors, not substring routing.** Each field has its own `Validate<Field>Input` and its own
  `<Field>HasError`/`<Field>ErrorText`. Do **not** route a single service message to a field by substring
  matching (the Person form's `SetInlineError` substring approach is the pattern to remove, not replicate).

### Masked inputs — dates

- **Masks are mandatory and never persisted.** Separators (`/`) are applied in the UI only; the DB stores a
  date type and the value is re-formatted on display. The user manipulates only the day/month/year numbers.
- **Full dates:** use `dxe:DateEdit` (picker + `DisplayFormat`, e.g. `{0:MM/dd/yyyy}`; the picker cannot
  produce an out-of-range date, giving built-in validity and satisfying "reuse a specialized validator"), or a
  masked `dxe:TextEdit` (`Mask="00/00/0000"`) for keyboard-first entry. See `devexpress-patterns.md § DateEdit`.
- **Locale-driven format:** English `MM/dd/yyyy`, pt-BR `dd/MM/yyyy` (future), Japanese TBD. Do **not**
  hard-code a single date format. Localization is currently disabled (`useLocalization:false`, no `.resx`) —
  locale-aware masks are future work; this standard states the intent. See `theme-locale.md § Locale`.

> **OPEN — confirm on emulator (Helder gate): day/month-only birthday (no year).** MyVocaList's Person
> birthday is entered day/month only, with no year. DevExpress `dxe:DateEdit` has **no** masked no-year
> text-entry mode — its `Date` is always a full `DateTime`. Two candidate paths, decision pending Helder:
> (1) masked `dxe:TextEdit` (`Mask="00/00"`) + a service-side `ValidateBirthdayInput` that checks month/day
> numbers; or (2) `dxe:DateEdit` with a fixed sentinel year + `DisplayFormat="{0:MM/dd}"` (leans on the
> component's built-in validity). Do not implement the birthday field until this is confirmed on the emulator.

### Integer inputs

> **Spec-incomplete — escalated to Helder.** The requirements doc's Integer section ends with
> `<TODO> - complete Integer and append any` (`01-ui-form-validation-guide.md`). Do **not** author integer
> validation rules until Helder completes that section — no rules are invented here. (Traceability item R10.)

---

## Character Counter Pattern

Confirmed in `VenuesPage.xaml` — shown when input is near max length:

```xml
<Label Text="{Binding CharacterCounterText}"
       IsVisible="{Binding ShowCharacterCounter}"
       FontFamily="RobotoRegular" FontSize="12"
       HorizontalOptions="End">
    <Label.Triggers>
        <DataTrigger TargetType="Label"
                     Binding="{Binding IsCharacterCounterError}"
                     Value="True">
            <Setter Property="TextColor" Value="{StaticResource Error}" />
        </DataTrigger>
        <DataTrigger TargetType="Label"
                     Binding="{Binding IsCharacterCounterWarning}"
                     Value="True">
            <Setter Property="TextColor" Value="{StaticResource Warning}" />
        </DataTrigger>
    </Label.Triggers>
</Label>
```

Service helper (`VenueService`):
```csharp
public bool ShouldShowCharacterCounter(int currentLength) => currentLength > ShowCounterAt;
public (string text, bool isWarning, bool isError) GetCharacterCounterInfo(int currentLength);
```

---

## Snackbar (non-blocking feedback)

For success/error feedback that doesn't block the user, use `ISnackbarService`:
```csharp
await _snackbarService.ShowSuccessAsync("Venue created");
await _snackbarService.ShowErrorAsync(message);
```

Registered as `AddSingleton<ISnackbarService, SnackbarService>()`.

---

## BottomSheet State Management (Code-Behind Pattern)

The BottomSheet state is driven by ViewModel property + code-behind event wiring:

```csharp
// Page code-behind
private void OnBottomSheetStateChanged(object? sender, ValueChangedEventArgs<BottomSheetState> e)
{
    // Sync sheet state back to ViewModel when user dismisses
    if (e.NewValue == BottomSheetState.Hidden)
        ViewModel.CloseEditSheet();
}
```

ViewModel drives open/close by setting the state property; page handles sheet open/close via event.
