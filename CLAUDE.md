# CLAUDE.md - MyVocaList

## App
Karaoke queue management with round-based progression. .NET MAUI 9.0 (net9.0-android).

## Language
Code, comments, logs, UI text: **English only**

## Translation
**CRITICAL**: All text in the codebase must be in English. When creating or updating any code:
- Translate any non-English text (comments, strings, logs, UI labels, error messages) to English immediately
- This applies to ALL files: XAML, C#, configuration files, documentation
- No exceptions: even placeholder text, test data, and temporary strings must be in English

## Comments
- **Only**: for method and property. Exist when member name isn´t enough to understand WHAT it does.
- **Never**: code inside method bodies.
- **Must**: Be formatted whenever it contributes to ease of reading.

- ### Comment text
- **Must**: say WHAT. Be brief. Updated whenever needed.
- **Can´t**: have any symbol. Sau HOW or WHY.

## Architecture
```
Domain → Contracts → Services → Infrastructure → View
(Entities)  (DTOs)    (Logic)    (EF+SQLite)    (MAUI)
```
- Business logic **only** in Services
- Interface + Implementation in **same folder**
- DTO defined as record.
- Prefer type composition over base type inheritance 
- MAUI page´s code **must** follow the code patterns of the DesignSystem folder pages.

## DDD Patterns
| Pattern | Implementation |
|---------|----------------|
| Aggregates/Entities | Base classes |
| Value Objects | Records |
| Domain Events | MediatR notifications |
| CQRS | Command/Query handlers |
| Repository | EF Core 9 + SQLite |

## TDD
- Test-first: Domain + Services
- Stack: xUnit, FluentAssertions, NSubstitute

## Error Handling
- **Avoid**: try-catch, `Debug.WriteLine`, `Console.WriteLine`
- **Use**: Serilog via `ILogger<T>`
- **Use**: Guard pattern for validation

```csharp
// ✅ Correct
Guard.AgainstNullOrWhiteSpace(name, nameof(name));

// ❌ Wrong
if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException();
```

## UI Thread Safety - CRITICAL RULE

**MANDATORY**: All UI operations MUST execute on the platform's native UI thread. Violation causes freezes, frame skips, and crashes.

### Golden Rules

1. **NEVER block the UI thread** - No `Task.Wait()`, `.Result`, or synchronous I/O
2. **ALWAYS marshal UI updates** - Use `Dispatcher` for cross-thread UI access
3. **NEVER modify ObservableCollection from background threads**
4. **ALWAYS use `async Task`** - Never `async void` (except event handlers)

### Required Pattern for UI Updates

```csharp
// CORRECT - Safe UI update from any thread
Application.Current?.Dispatcher.Dispatch(() =>
{
    myLabel.Text = "Updated";
    MyCollection.Add(newItem);
});

// CORRECT - Async version when awaiting is needed
await Application.Current.Dispatcher.DispatchAsync(async () =>
{
    await SomeAsyncUiWork();
});
```

### Forbidden Patterns

```csharp
// WRONG - Blocks UI thread, causes freezes
var result = SomeAsyncMethod().Result;
var data = SomeAsyncMethod().GetAwaiter().GetResult();
Task.WaitAll(tasks);

// WRONG - Cross-thread UI access without marshaling
await Task.Run(() => 
{
    myLabel.Text = "Crash!"; // UI update from background thread
});

// WRONG - MainThread has Windows issues
MainThread.BeginInvokeOnMainThread(() => { }); // Avoid - use Dispatcher instead
```

### Background Work Pattern

```csharp
// Heavy computation on background, results marshaled to UI
var data = await Task.Run(() => HeavyComputation());

Application.Current?.Dispatcher.Dispatch(() =>
{
    Items.Clear();
    foreach (var item in data)
        Items.Add(item);
});
```

### ViewModel Base Helper

All ViewModels should use this helper for safe UI updates:

```csharp
protected void RunOnUiThread(Action action)
{
    if (Application.Current?.Dispatcher.IsDispatchRequired == true)
        Application.Current.Dispatcher.Dispatch(action);
    else
        action();
}

protected Task RunOnUiThreadAsync(Func<Task> asyncAction)
{
    if (Application.Current?.Dispatcher.IsDispatchRequired == true)
        return Application.Current.Dispatcher.DispatchAsync(asyncAction);
    else
        return asyncAction();
}
```

### Why NOT MainThread.BeginInvokeOnMainThread

- Known issues on Windows: "Unable to find main thread" from background threads
- `Application.Current.Dispatcher` works consistently across Android, iOS, Windows
- Dispatcher is always available after app initialization

### Frame Skip Diagnosis

If experiencing frame skips:
1. Check for synchronous database/file operations on UI thread
2. Check for large collection updates without batching
3. Check for complex layout calculations during scrolling
4. Use `await Task.Yield()` to break up long UI operations

## Git Commits
```
<type>: <summary>

- detail 1
- detail 2

Co-Authored-By: Claude <noreply@anthropic.com>
```
Types: `feat:`, `fix:`, `refactor:`, `docs:`, `perf:`, `test:`

## Changelog
- Location: `Docs/Changelog/changelog.md`
- Format: `- **MM/dd/yyyy** - Type - Description`
- Types: Enhancement | Fix
- **Update after every completed task**

## Workflow
**CRITICAL**: After completing any task, ALWAYS follow this sequence:
1. Update `Docs/Changelog/changelog.md` with the changes
2. Create a git commit with all changes
3. Push the commit to remote repository
4. Never skip these steps - they are mandatory for every task completion

## Stack
```
MediatR, FluentValidation, Serilog, EF Core 9, SQLite
UraniumUI 2.14 (Material Design 3)
HorusSoftware.Maui.MaterialDesignControls 10.0 (MD3 Components)
```

## XAML Styling - Material Design 3 Compliance

### Strict Rules
**NEVER use inline styles.** All styling must follow the DesignSystem folder pages approaches.

### Forbidden Inline Properties
```xml
<!-- ❌ WRONG - Never use these inline -->
<VerticalStackLayout Spacing="16">
<HorizontalStackLayout Margin="10,20,10,20">
<Button Padding="8" CornerRadius="12">
<Image WidthRequest="32" HeightRequest="32">
<Grid ColumnSpacing="10" RowSpacing="5">
<Label FontSize="14" TextColor="Red">
```

### Correct MD3 Approach
```xml
<!-- ✅ CORRECT - Use StyleClass or StaticResource -->
<VerticalStackLayout>
<Button StyleClass="FilledButton">
<Label StyleClass="Body.Medium">
<Frame Style="{StaticResource ElevatedCard}">
```

### MD3 Component Guidelines
- **Buttons**: Use `StyleClass="FilledButton"`, `"FilledTonalButton"`, `"OutlinedButton"`, `"TextButton"`
- **Typography**: Use `StyleClass="Headline.Large"`, `"Title.Medium"`, `"Body.Medium"`, etc.
- **Containers**: Use `Style="{StaticResource ElevatedCard}"`, `"{StaticResource OutlinedCard}"`
- **Layouts**: Use VerticalStackLayout, HorizontalStackLayout, FlexLayout without inline spacing

### Buttons with Material Symbols Icons
Use `FontImageSource` in `Button.ImageSource` property:
```xml
<Button Text="Home" StyleClass="FilledButton">
    <Button.ImageSource>
        <FontImageSource FontFamily="MaterialOutlined" Glyph="{x:Static m:MaterialOutlined.Home}" />
    </Button.ImageSource>
</Button>
```
- FontFamily: `MaterialOutlined` (verified working in UraniumUI.Icons.MaterialSymbols 2.10.0)
- Namespace: `xmlns:m="clr-namespace:UraniumUI.Icons.MaterialSymbols;assembly=UraniumUI.Icons.MaterialSymbols"`
- Note: Other variants (MaterialSharp, MaterialRound, MaterialFilled) may not exist in all package versions

### Required References
- UraniumUI Docs: https://enisn-projects.io/docs/en/uranium/latest/
- Material Design 3: https://m3.material.io/components/
- Must work smoothly on Android, iOS, and Windows

### Exception
BoxView dividers may use `HeightRequest="1"` for 1px structural height only.