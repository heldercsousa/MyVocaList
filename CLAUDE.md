# CLAUDE.md - MyVocaList

## App
Karaoke queue management with round-based progression. .NET MAUI 8.0 (net8.0-android).

## Language
Code, comments, logs: **English only**

## Translation
Translate any non-English text (comments, strings, logs) to English when encountered.

## Comments
- **Only**: classes, records, structs, methods (XML summary)
- **Never**: code inside method bodies

## Architecture
```
Domain → Contracts → Services → Infrastructure → View
(Entities)  (DTOs)    (Logic)    (EF+SQLite)    (MAUI)
```
- Business logic **only** in Services
- Interface + Implementation in **same folder**

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

## Stack
```
MediatR, FluentValidation, Serilog, EF Core 9, SQLite
UraniumUI (Material Design 3)
```

## XAML Styling - Material Design 3 Compliance

### Strict Rules
**NEVER use inline styles.** All styling must come from StyleClass or StaticResource.

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
- Available FontFamily: `MaterialOutlined`, `MaterialSharp`, `MaterialRound`
- Namespace: `xmlns:m="clr-namespace:UraniumUI.Icons.MaterialSymbols;assembly=UraniumUI.Icons.MaterialSymbols"`
- Note: There is NO `MaterialFilled` - use `MaterialSharp` for sharp-cornered icons

### Required References
- UraniumUI Docs: https://enisn-projects.io/docs/en/uranium/latest/
- Material Design 3: https://m3.material.io/components/
- Must work smoothly on Android, iOS, and Windows

### Exception
BoxView dividers may use `HeightRequest="1"` for 1px structural height only.