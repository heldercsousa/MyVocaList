# DevExpress MAUI Component Patterns — TextEdit + DateEdit (Editors)

> Section file split from `devexpress-patterns.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `devexpress-patterns.md`.

## TextEdit (Editors) — confirmed in codebase

```xml
<dxe:TextEdit Text="{Binding FieldValue, Mode=TwoWay}"
              LabelText="Field Label"
              PlaceholderText="Enter value"
              BoxMode="Outlined"
              FocusedBorderColor="{StaticResource Primary}"
              BorderColor="{StaticResource Outline}"
              BackgroundColor="{StaticResource SurfaceContainerHighest}"
              TextColor="{StaticResource OnSurface}"
              MaxCharacterCount="30"
              HasError="{Binding FieldHasError}"
              ErrorText="{Binding FieldErrorText}" />
```

Removed properties (DevExpress 25.1.3+):
- `BoxCornerRadius` — removed, do not use

**Field-level blur validation (confirmed DX 25.2.x, 2026-07-01):** `TextEdit` inherits the MAUI `Unfocused`
event (from `VisualElement`). Wire blur validation by subscribing to `Unfocused` in the page code-behind and
invoking a ViewModel `Validate<Field>Command`. This is the confirmed blur hook for the app's standalone-editor
forms. See the full timing/wiring rules in `dialogs-validation.md § Form Validation Standard`.
```xml
<dxe:TextEdit ... Unfocused="OnFieldUnfocused" />
```
```csharp
private void OnFieldUnfocused(object sender, FocusEventArgs e) => ViewModel.ValidateFieldCommand.Execute(null);
```

Search bar inside a rounded container:
```xml
<dx:DXBorder BackgroundColor="{StaticResource SurfaceContainer}"
             CornerRadius="28" Padding="4" Margin="16,8">
    <dxe:TextEdit Text="{Binding SearchText, Mode=TwoWay}"
                  PlaceholderText="Search..."
                  StartIcon="search_outlined"
                  StartIconColor="{StaticResource OnSurfaceVariant}"
                  BoxMode="Outlined"
                  BorderColor="Transparent"
                  FocusedBorderColor="Transparent"
                  BackgroundColor="Transparent"
                  ClearIconVisibility="Auto"
                  ClearIconColor="{StaticResource OnSurfaceVariant}" />
</dx:DXBorder>
```

## DateEdit (Editors) — masked dates

Per the quick-reference substitution table, `DatePicker` → `dxe:DateEdit`. Two date-entry patterns exist —
pick per the field's needs (full timing/masking rules in `dialogs-validation.md § Form Validation Standard`):

**Full date via picker (built-in validity):** `dxe:DateEdit` binds a full `DateTime` (`Date` property) and
renders a customizable picker; the picker cannot produce an out-of-range date, so it satisfies "reuse a
specialized validator" (no hand-rolled month/day/year checks needed). Format is display-only via
`DisplayFormat`; `MinDate`/`MaxDate` bound the range.
```xml
<dxe:DateEdit Date="{Binding Birthday, Mode=TwoWay}"
              LabelText="Date"
              DisplayFormat="{}{0:MM/dd/yyyy}"
              MinDate="{Binding MinBirthday}"
              MaxDate="{Binding MaxBirthday}"
              HasError="{Binding BirthdayHasError}"
              ErrorText="{Binding BirthdayErrorText}" />
```

**Masked keyboard entry:** `dxe:TextEdit` with `Mask` (separators are display-only, never persisted).
```xml
<dxe:TextEdit Text="{Binding BirthdayText, Mode=TwoWay}" Mask="00/00/0000" MaskPlaceholderChar="_" />
```

> **OPEN — Helder gate: day/month-only birthday (no year).** `dxe:DateEdit` has **no** masked no-year
> text-entry mode (its value is always a full `DateTime`). Candidate paths pending a Helder emulator decision:
> (1) masked `dxe:TextEdit` (`Mask="00/00"`) + service `ValidateBirthdayInput`; or (2) `dxe:DateEdit` + a
> fixed sentinel year + `DisplayFormat="{}{0:MM/dd}"`. See `dialogs-validation.md § Masked inputs — dates`.

Date input/display format is **locale-dependent** (English `MM/dd/yyyy`, pt-BR `dd/MM/yyyy`, Japanese TBD) —
do not hard-code it. Localization is currently disabled; see `theme-locale.md § Locale`.

---
