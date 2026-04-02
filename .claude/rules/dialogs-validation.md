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
