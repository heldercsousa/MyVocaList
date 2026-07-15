# DevExpress MAUI Component Patterns — Shell Navigation Form Page

> Section file split from `devexpress-patterns.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `devexpress-patterns.md`.

## Shell Navigation Form Page — confirmed in VenueFormPage.xaml

For Add/Edit forms that require keyboard input: use a **dedicated Shell navigation page** instead of a BottomSheet.
This avoids BottomSheet/keyboard conflicts and keyboard avoidance is handled automatically by `SafeAreaEdges="All"` + `ScrollView`.

### XAML
```xml
<ContentPage SafeAreaEdges="All"
             x:DataType="vm:MyFormViewModel"
             Title="{Binding PageTitle}">
    <ScrollView>
        <VerticalStackLayout Padding="24" Spacing="16">
            <dxe:TextEdit Text="{Binding FieldValue, Mode=TwoWay}"
                          LabelText="Field Label"
                          HasError="{Binding FieldHasError}"
                          ErrorText="{Binding FieldErrorText}"
                          BoxMode="Outlined"
                          FocusedBorderColor="{StaticResource Primary}"
                          BorderColor="{StaticResource Outline}"
                          BackgroundColor="{StaticResource SurfaceContainerHighest}"
                          TextColor="{StaticResource OnSurface}" />

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
    </ScrollView>
</ContentPage>
```

### ViewModel
```csharp
[QueryProperty(nameof(EntityId), "entityId")]
[QueryProperty(nameof(EntityName), "entityName")]
public partial class MyFormViewModel : ViewModelBase
{
    [ObservableProperty] private int? _entityId;
    [ObservableProperty] private string _entityName = string.Empty;
    [ObservableProperty] private bool _fieldHasError;
    [ObservableProperty] private string _fieldErrorText = string.Empty;
    [ObservableProperty] private bool _isBusy;

    public bool IsEditMode => EntityId.HasValue;
    public string PageTitle => IsEditMode ? "Edit X" : "New X";

    // Both commands navigate back
    private Task CancelAsync() => Shell.Current.GoToAsync("..");
    private async Task SaveAsync() { ... await Shell.Current.GoToAsync(".."); }
}
```

### Navigation (from list page)
```csharp
// Add
await Shell.Current.GoToAsync(Routes.MyForm);

// Edit — pass ID and current value via query string
await Shell.Current.GoToAsync($"{Routes.MyForm}?entityId={item.Id}&entityName={Uri.EscapeDataString(item.Name)}");
```

Register in `AppShell.xaml.cs`:
```csharp
Routing.RegisterRoute(Routes.MyForm, typeof(MyFormPage));
```

### Code-behind (focus first field on appear)
```csharp
protected override void OnAppearing()
{
    base.OnAppearing();
    nameEdit.Focus();
}
```
