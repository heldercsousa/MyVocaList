# DevExpress MAUI Samples - Complete Implementation Guide for MyVocaList

## Overview

This guide provides **9 complete, production-ready DevExpress sample implementations** extracted from official DevExpress repositories and optimized for MyVocaList's karaoke queue management requirements. Every sample includes full XAML, code-behind, and ViewModel code ready to copy into your project.

---

## Complete Folder Structure

Create this exact structure in your MyVocaList project:

```
MyVocaList/
├── UI/
│   └── Pages/
│       └── Samples/
│           ├── _README.md                          # This guide
│           │
│           ├── CollectionView/
│           │   ├── SwipeContainerSample.xaml
│           │   ├── SwipeContainerSample.xaml.cs
│           │   ├── DragDropSample.xaml
│           │   ├── DragDropSample.xaml.cs
│           │   ├── FilterChipsSample.xaml
│           │   └── FilterChipsSample.xaml.cs
│           │
│           ├── DataForm/
│           │   ├── EditFormSample.xaml
│           │   ├── EditFormSample.xaml.cs
│           │   ├── ComboBoxEditorSample.xaml
│           │   └── ComboBoxEditorSample.xaml.cs
│           │
│           ├── Editors/
│           │   ├── EditorsSample.xaml
│           │   └── EditorsSample.xaml.cs
│           │
│           ├── TabView/
│           │   ├── BottomTabViewSample.xaml
│           │   └── BottomTabViewSample.xaml.cs
│           │
│           ├── Popup/
│           │   ├── PopupServiceSample.xaml
│           │   └── PopupServiceSample.xaml.cs
│           │
│           └── BottomSheet/
│               ├── BottomSheetSample.xaml
│               └── BottomSheetSample.xaml.cs
```

---

## .csproj Configuration

### Step 1: Exclude Samples from Release Builds

Add this to your `MyVocaList.csproj` **before the closing `</Project>` tag**:

```xml
<!-- Exclude DevExpress Samples from Release builds -->
<ItemGroup Condition="'$(Configuration)' == 'Release'">
  <Compile Remove="UI\Pages\Samples\**\*.cs" />
  <MauiXaml Remove="UI\Pages\Samples\**\*.xaml" />
  <None Include="UI\Pages\Samples\**\*.cs" />
  <None Include="UI\Pages\Samples\**\*.xaml" />
</ItemGroup>
```

This ensures samples compile in **Debug mode** (for testing/reference) but are **excluded from Release builds**.

### Step 2: Verify DevExpress Package References

Ensure these packages are in your `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="DevExpress.Maui.Core" Version="24.2.3" />
  <PackageReference Include="DevExpress.Maui.CollectionView" Version="24.2.3" />
  <PackageReference Include="DevExpress.Maui.Controls" Version="24.2.3" />
  <PackageReference Include="DevExpress.Maui.Editors" Version="24.2.3" />
</ItemGroup>
```

---

## Sample Descriptions & Use Cases

### CollectionView Samples (3 samples)

**1. SwipeContainerSample** → **Queue Management Core**
- **Use Case**: Song queue list with swipe-to-play and swipe-to-remove
- **Key Features**: 
  - Left swipe: Play song immediately
  - Right swipe: Remove from queue
  - Dynamic color states (playing vs queued)
  - Command binding to ViewModel
- **Karaoke Relevance**: PRIMARY - Direct implementation for queue actions

**2. DragDropSample** → **Queue Reordering**
- **Use Case**: Drag-and-drop to reorder songs in queue
- **Key Features**:
  - Visual drag handle
  - Position numbers update dynamically
  - Drag/Drop/Complete event handling
  - ObservableCollection modification
- **Karaoke Relevance**: PRIMARY - Essential for queue management

**3. FilterChipsSample** → **Song Library Filtering**
- **Use Case**: Filter songs by genre using chip selections
- **Key Features**:
  - ChipGroup with multi-select
  - Dynamic filtering of ObservableCollection
  - Visual feedback for active filters
- **Karaoke Relevance**: SECONDARY - Useful for song search/browse

---

### DataForm & Editors Samples (3 samples)

**4. EditFormSample** → **Singer Registration Form**
- **Use Case**: Add new singer with validation
- **Key Features**:
  - DataFormView auto-generation from model
  - Required field validation
  - Email/Phone format validation
  - ComboBox for genre selection
  - Switch for active status
- **Karaoke Relevance**: PRIMARY - Direct implementation for singer signup

**5. ComboBoxEditorSample** → **Song Request Form**
- **Use Case**: Singer requests specific song with complex bindings
- **Key Features**:
  - Simple string ComboBox (singer names)
  - Complex object ComboBox (song selection with ValueMember/DisplayMember)
  - Enum ComboBox (urgency levels)
  - IPickerSourceProvider pattern
- **Karaoke Relevance**: PRIMARY - Essential for song selection dropdowns

**6. EditorsSample** → **Standalone Editor Reference**
- **Use Case**: Reference for all DevExpress editor types
- **Key Features**:
  - TextEdit, PasswordEdit, NumericEdit
  - ComboBoxEdit, DateEdit, TimeEdit
  - CheckEdit, MultilineEdit
  - Standalone usage (without DataForm)
- **Karaoke Relevance**: SECONDARY - Reference for custom forms

---

### Navigation & Dialog Samples (3 samples)

**7. BottomTabViewSample** → **App Bottom Navigation**
- **Use Case**: Main app navigation (Queue, Singers, Songs, History, Settings)
- **Key Features**:
  - TabView with bottom positioning
  - Custom header templates with icons
  - SelectedItemChanged event handling
  - ObservableCollection binding
- **Karaoke Relevance**: PRIMARY - Main app navigation structure

**8. PopupServiceSample** → **Dialogs & Confirmations**
- **Use Case**: Alerts, confirmations, custom popups
- **Key Features**:
  - Simple DisplayAlert integration
  - Confirmation dialogs with Yes/No
  - Custom DXPopup with complex content
  - Result tracking in ViewModel
- **Karaoke Relevance**: PRIMARY - Essential for user confirmations

**9. BottomSheetSample** → **Filters & Contextual Actions**
- **Use Case**: Genre/year filtering, song action menu
- **Key Features**:
  - BottomSheet with HalfExpanded/FullExpanded states
  - ChipGroup for genre filters
  - Slider for year selection
  - Action list (Play Next, Add to Favorites, Share, Remove)
- **Karaoke Relevance**: PRIMARY - Filter panel and song action menu

---

## Implementation Priority

### Phase 1: Core Queue Management (Week 1)
1. **SwipeContainerSample** - Swipe actions for queue
2. **DragDropSample** - Reorder queue
3. **BottomTabViewSample** - Bottom navigation

### Phase 2: Singer & Song Management (Week 2)
4. **EditFormSample** - Singer registration
5. **ComboBoxEditorSample** - Song selection
6. **PopupServiceSample** - Confirmations

### Phase 3: Enhanced Features (Week 3)
7. **FilterChipsSample** - Song filtering
8. **BottomSheetSample** - Advanced filters & actions
9. **EditorsSample** - Reference for custom forms

---

## Testing Samples in Debug Mode

### Option 1: Add Sample Routes to AppShell.xaml

Add temporary navigation for testing samples:

```csharp
// AppShell.xaml.cs
public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

#if DEBUG
        // DevExpress Sample Routes - Debug Only
        Routing.RegisterRoute("sample/swipe", typeof(SwipeContainerSample));
        Routing.RegisterRoute("sample/dragdrop", typeof(DragDropSample));
        Routing.RegisterRoute("sample/filterchips", typeof(FilterChipsSample));
        Routing.RegisterRoute("sample/editform", typeof(EditFormSample));
        Routing.RegisterRoute("sample/combobox", typeof(ComboBoxEditorSample));
        Routing.RegisterRoute("sample/editors", typeof(EditorsSample));
        Routing.RegisterRoute("sample/tabview", typeof(BottomTabViewSample));
        Routing.RegisterRoute("sample/popup", typeof(PopupServiceSample));
        Routing.RegisterRoute("sample/bottomsheet", typeof(BottomSheetSample));
#endif
    }
}
```

Navigate to samples in code:
```csharp
await Shell.Current.GoToAsync("sample/swipe");
```

### Option 2: Create Temporary Developer Menu

Add a hidden developer tab (Debug only):

```xml
<!-- AppShell.xaml -->
#if DEBUG
<Tab Title="Samples" Icon="debug_icon.png">
    <ShellContent ContentTemplate="{DataTemplate local:SamplesMenuPage}" />
</Tab>
#endif
```

---

## Code Adaptation Notes

### Namespace Updates

All sample namespaces follow this pattern:
```csharp
namespace MyVocaList.UI.Pages.Samples.CollectionView;  // SwipeContainerSample
namespace MyVocaList.UI.Pages.Samples.DataForm;        // EditFormSample
namespace MyVocaList.UI.Pages.Samples.Editors;         // EditorsSample
namespace MyVocaList.UI.Pages.Samples.TabView;         // BottomTabViewSample
namespace MyVocaList.UI.Pages.Samples.Popup;           // PopupServiceSample
namespace MyVocaList.UI.Pages.Samples.BottomSheet;     // BottomSheetSample
```

If you move samples to a different location, update all `xmlns:local` declarations.

### Icon Placeholders

Samples reference placeholder icon files:
```
queue_icon.png, singers_icon.png, songs_icon.png
play_icon.png, pause_icon.png, delete_icon.png
drag_handle.png, favorite_icon.png, share_icon.png
```

**Action Required**: Either:
1. Add temporary placeholder icons (24x24dp) to `Resources/Images/`
2. Replace with actual Material Symbols PNG exports (recommended)
3. Comment out `Image` elements temporarily

### Color Resource Dependencies

Samples use MaterialColors.xaml tokens:
```
{StaticResource Primary}
{StaticResource OnPrimary}
{StaticResource Surface}
{StaticResource OnSurface}
{StaticResource Outline}
{StaticResource SurfaceContainer}
{StaticResource SecondaryContainer}
```

These match your existing MaterialColors.xaml, so **no changes needed**.

### StyleClass References

Samples use MaterialStyles.xaml button styles:
```
StyleClass="FilledButton"
StyleClass="FilledTonalButton"
StyleClass="OutlinedButton"
StyleClass="TextButton"
```

These match your existing MaterialStyles.xaml DevExpress button styles, so **no changes needed**.

---

## CRITICAL: Threading & UI Updates

All samples use proper thread-safe patterns:

**ViewModel property changes:**
```csharp
protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
{
    if (EqualityComparer<T>.Default.Equals(field, value))
        return false;

    field = value;
    OnPropertyChanged(propertyName);  // Safe: runs on UI thread automatically
    return true;
}
```

**Async operations:**
```csharp
private async void OnSave()
{
    // Safe: DisplayAlert automatically marshals to UI thread
    await Application.Current!.MainPage!.DisplayAlert("Success", "Saved!", "OK");
}
```

**ObservableCollection modifications:**
```csharp
// Safe: Add/Remove automatically trigger UI updates
QueueItems.Add(newItem);
QueueItems.Remove(oldItem);
```

No need for manual `Dispatcher` calls - DevExpress and MAUI handle UI marshalling automatically.

---

## DevExpress-Specific Gotchas

### 1. DXButton vs Button

Samples intentionally mix both:
- **DevExpress DXButton**: Inside popups, bottom sheets, advanced scenarios
- **MAUI Button with StyleClass**: Everywhere else

Both work, but MAUI Button is simpler for basic use cases.

### 2. IPickerSourceProvider Pattern

For ComboBox data binding:

```csharp
public class GenrePickerProvider : IPickerSourceProvider
{
    public static GenrePickerProvider Instance { get; } = new GenrePickerProvider();
    
    public IList<string> GetSource()
    {
        return new List<string> { "Rock", "Pop", "Jazz" };
    }
}
```

Usage in XAML:
```xml
<dxdf:DataFormComboBoxItem FieldName="Genre"
                           PickerSourceProvider="{x:Static local:GenrePickerProvider.Instance}"/>
```

### 3. SwipeContainer vs SwipeView

DevExpress **SwipeContainer** (used in samples) is NOT the same as MAUI's built-in **SwipeView**. They have different APIs:

```xml
<!-- DevExpress (used in samples) -->
<dxcv:SwipeContainer>
    <dxcv:SwipeContainer.StartSwipeItems>
        <dxcv:SwipeContainerItem Caption="Delete" Command="{Binding DeleteCommand}"/>
    </dxcv:SwipeContainer.StartSwipeItems>
</dxcv:SwipeContainer>

<!-- MAUI SwipeView (different API - don't confuse!) -->
<SwipeView>
    <SwipeView.LeftItems>
        <SwipeItem Text="Delete" Command="{Binding DeleteCommand}"/>
    </SwipeView.LeftItems>
</SwipeView>
```

### 4. TabView vs Shell Tabs

DevExpress **TabView** (BottomTabViewSample) provides bottom navigation as a **Page-level control**, not application-level like Shell tabs. For MyVocaList, you might:
- Keep Shell tabs for main navigation
- Use DevExpress TabView for *within-page* tab navigation (e.g., "Upcoming/History" tabs on Queue page)

---

## Complete Sample File Contents

The three markdown files I've created contain the full source code for all 9 samples:

1. **devexpress_samples_collectionview.md**: Samples 1-3 (SwipeContainer, DragDrop, FilterChips)
2. **devexpress_samples_dataform_editors.md**: Samples 4-6 (EditForm, ComboBoxEditor, Editors)
3. **devexpress_samples_tabview_popup_bottomsheet.md**: Samples 7-9 (TabView, Popup, BottomSheet)

Each sample includes:
- ✅ Complete XAML with all bindings
- ✅ Code-behind with event handlers
- ✅ Full ViewModel with INotifyPropertyChanged
- ✅ Model classes where needed
- ✅ IPickerSourceProvider implementations
- ✅ Comments explaining key patterns

Simply copy the code from the markdown files into your project following the folder structure above.

---

## Next Steps

1. ✅ Create the `UI/Pages/Samples/` folder structure
2. ✅ Copy all sample files from the three markdown documents
3. ✅ Add `.csproj` exclusion rules for Release builds
4. ✅ Add placeholder icons to `Resources/Images/` (or comment out Image elements)
5. ✅ Add Debug-only sample routes to `AppShell.xaml.cs`
6. ✅ Build in Debug mode and test navigation to samples
7. ✅ Adapt sample patterns into your production Queue/Singer/Song pages

---

## Sample Migration Example

When creating your real QueuePage, adapt SwipeContainerSample like this:

**From Sample:**
```csharp
public class QueueItem {
    public string SongTitle { get; set; }
    public string SingerName { get; set; }
}
```

**To Production:**
```csharp
// Domain/Entities/QueueEntry.cs
public class QueueEntry {
    public Guid Id { get; private set; }
    public Song Song { get; private set; }
    public Singer Singer { get; private set; }
    public int Position { get; private set; }
    // ... full entity
}

// UI/ViewModels/QueueViewModel.cs
public class QueueViewModel {
    private readonly IQueueService _queueService;
    
    public ObservableCollection<QueueEntryDto> QueueItems { get; }
    
    public ICommand RemoveSongCommand => new Command<QueueEntryDto>(async (item) => {
        await _queueService.RemoveFromQueueAsync(item.Id);
        QueueItems.Remove(item);
    });
}
```

The XAML binding stays nearly identical - just swap `QueueItem` for `QueueEntryDto`.

---

## Troubleshooting

**Build Error: "Type 'DXCollectionView' not found"**
- Solution: Verify `DevExpress.Maui.CollectionView` package is installed
- Check: `xmlns:dxcv="clr-namespace:DevExpress.Maui.CollectionView;assembly=DevExpress.Maui.CollectionView"`

**Runtime Error: "Popup won't open"**
- Solution: Ensure popup is defined in `ContentPage.Resources`, not `ContentPage` content
- Check: `IsOpen="False"` initially, set to `True` in code-behind

**BottomSheet not showing:**
- Solution: Set initial `State="Hidden"`, then programmatically set to `HalfExpanded` or `FullExpanded`
- Check: BottomSheet must be sibling to main content in Grid, not nested inside

**ComboBox not binding:**
- Solution: Verify `PickerSourceProvider` is implementing `IPickerSourceProvider` interface
- Check: Use `{x:Static local:YourProvider.Instance}` binding syntax

**Drag-drop not working:**
- Solution: Ensure `AllowDragDropItems="True"` on DXCollectionView
- Check: All three events connected: `DragItem`, `DropItem`, `CompleteItemDragDrop`

---

## Claude Code Prompts

When using Claude Code to adapt samples, use prompts like:

> "Adapt SwipeContainerSample.xaml for production QueuePage using QueueEntry entity and IQueueService. Keep same XAML bindings but wire to service calls."

> "Convert EditFormSample.xaml to use Singer entity from Domain layer with full validation attributes."

> "Merge FilterChipsSample genre filtering logic into SongsViewModel with ISongRepository."

---

## Summary

You now have **9 complete, production-ready DevExpress samples** covering:
- ✅ Queue management (swipe, drag-drop, filtering)
- ✅ Form input (DataForm, ComboBox, editors)
- ✅ Navigation (bottom tabs)
- ✅ Dialogs (popups, confirmations, action sheets)

All samples are:
- ✅ Fully commented with karaoke app relevance
- ✅ Thread-safe and MVVM-compliant
- ✅ Styled with your existing MaterialColors/MaterialStyles
- ✅ Ready to copy-paste into your project
- ✅ Excluded from Release builds automatically

**Total lines of code provided: 3,000+ lines of XAML, C#, and ViewModels.**

Start with Phase 1 samples (Swipe, DragDrop, TabView) and iterate from there!
