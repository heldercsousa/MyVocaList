# AutocompleteField Component Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a reusable `AutocompleteField` ContentView (MD3 Docked Search Bar pattern) that any form page can bind to for inline entity search with debounced suggestions overlay.

**Architecture:** One shared DTO (`AutocompleteSuggestion`) lives in `Contracts/Models/`. The component (`AutocompleteField.xaml` + `.xaml.cs`) lives in `MyVocaList/UI/Components/AutocompleteField/`. The component owns debounce logic in code-behind; the caller ViewModel owns the search query and suggestion results.

**Tech Stack:** .NET MAUI 10 · DevExpress MAUI v25.2.4 (`DXCollectionView`, `TextEdit`, `DXBorder`) · CommunityToolkit.Mvvm · existing `ListItem` component · xUnit + Moq (unit test for debounce behavior)

---

## File Map

| File | Action | Responsibility |
|------|--------|----------------|
| `Contracts/Models/AutocompleteSuggestion.cs` | **Create** | Shared DTO — Headline, SupportingText, Data |
| `MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml` | **Create** | Layout: TextEdit + overlay DXBorder card + DXCollectionView |
| `MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml.cs` | **Create** | BindableProperties, debounce CTS, HasSuggestions, focus/blur guard |
| `MyVocaList/GlobalUsings.cs` | **Modify** | Add `MyVocaList.Contracts.Models` global using (if not already present) |
| `MyVocaList.Tests/Unit/Components/AutocompleteFieldDebounceTests.cs` | **Create** | Unit tests for debounce timing and `SearchRequestedCommand` firing behavior |

---

## Task 1: Add `AutocompleteSuggestion` DTO to Contracts

**Files:**
- Create: `Contracts/Models/AutocompleteSuggestion.cs`

- [ ] **Step 1: Create the file**

```csharp
namespace MyVocaList.Contracts.Models;

/// <summary>
/// A single result item surfaced by <c>AutocompleteField</c>.
/// </summary>
/// <param name="Headline">Primary display text (e.g. person's full name).</param>
/// <param name="SupportingText">Optional secondary line (e.g. email or birthday). Null or empty = 1-line row.</param>
/// <param name="Data">The original entity object. The caller casts this in <c>SuggestionSelectedCommand</c>.</param>
public record AutocompleteSuggestion(string Headline, string SupportingText, object Data);
```

- [ ] **Step 2: Build and confirm 0 errors**

```
dotnet build MyVocaList.sln -f net10.0-android
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```
git add Contracts/Models/AutocompleteSuggestion.cs
git commit -m "feat(contracts): add AutocompleteSuggestion DTO"
```

---

## Task 2: Add global using for `Contracts.Models` in MAUI project

**Files:**
- Modify: `MyVocaList/GlobalUsings.cs`

- [ ] **Step 1: Check if `MyVocaList.Contracts.Models` is already in GlobalUsings.cs**

Read `MyVocaList/GlobalUsings.cs`. If `global using MyVocaList.Contracts.Models;` is already present, skip this task.

- [ ] **Step 2: Add the global using**

In `MyVocaList/GlobalUsings.cs`, after the `global using MyVocaList.Contracts.DTOs.List;` line, add:

```csharp
global using MyVocaList.Contracts.Models;
```

- [ ] **Step 3: Build and confirm 0 errors**

```
dotnet build MyVocaList.sln -f net10.0-android
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```
git add MyVocaList/GlobalUsings.cs
git commit -m "chore(maui): add global using for Contracts.Models"
```

---

## Task 3: Create `AutocompleteField` XAML layout

**Files:**
- Create: `MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml`

> The overlay card sits in the **same Grid row** as the TextEdit, uses `VerticalOptions=Start` with `Margin.Top=56` (TextEdit height), and `ZIndex=10`. This makes it visually appear below the field without pushing down content.

- [ ] **Step 1: Create the XAML file**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentView
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:dx="http://schemas.devexpress.com/maui"
    xmlns:dxe="clr-namespace:DevExpress.Maui.Editors;assembly=DevExpress.Maui.Editors"
    xmlns:dxcv="clr-namespace:DevExpress.Maui.CollectionView;assembly=DevExpress.Maui.CollectionView"
    xmlns:lists="clr-namespace:MyVocaList.UI.Components.Lists"
    xmlns:models="clr-namespace:MyVocaList.Contracts.Models;assembly=MyVocaList.Contracts"
    x:Class="MyVocaList.UI.Components.AutocompleteField.AutocompleteField"
    x:Name="self">

    <!-- Single-row Grid: TextEdit and overlay card share row 0 -->
    <Grid RowDefinitions="Auto">

        <!-- Input field -->
        <dxe:TextEdit
            x:Name="searchEdit"
            Grid.Row="0"
            LabelText="{Binding LabelText, Source={x:Reference self}}"
            PlaceholderText="{Binding Placeholder, Source={x:Reference self}}"
            HasError="{Binding HasError, Source={x:Reference self}}"
            ErrorText="{Binding ErrorText, Source={x:Reference self}}"
            BoxMode="Outlined"
            FocusedBorderColor="{StaticResource Primary}"
            BorderColor="{StaticResource Outline}"
            BackgroundColor="{StaticResource SurfaceContainerHighest}"
            TextColor="{StaticResource OnSurface}"
            TextChanged="OnTextChanged"
            Focused="OnSearchEditFocused"
            Unfocused="OnSearchEditUnfocused" />

        <!-- Suggestions overlay card — appears below the field, overlays content -->
        <dx:DXBorder
            x:Name="overlayCard"
            Grid.Row="0"
            IsVisible="False"
            ZIndex="10"
            VerticalOptions="Start"
            Margin="0,56,0,0"
            BackgroundColor="{StaticResource SurfaceContainerHigh}"
            CornerRadius="12">
            <dx:DXBorder.Shadow>
                <Shadow Brush="{StaticResource OnSurface}"
                        Offset="0,4"
                        Radius="8"
                        Opacity="0.25" />
            </dx:DXBorder.Shadow>

            <dxcv:DXCollectionView
                x:Name="suggestionsView"
                MaximumHeightRequest="280"
                ItemSeparatorThickness="0"
                IndicatorColor="{StaticResource Primary}"
                Tap="OnSuggestionTapped">
                <dxcv:DXCollectionView.ItemTemplate>
                    <DataTemplate x:DataType="models:AutocompleteSuggestion">
                        <lists:ListItem
                            Headline="{Binding Headline}"
                            SupportingText="{Binding SupportingText}" />
                    </DataTemplate>
                </dxcv:DXCollectionView.ItemTemplate>
            </dxcv:DXCollectionView>
        </dx:DXBorder>

    </Grid>
</ContentView>
```

- [ ] **Step 2: Build and confirm 0 errors**

```
dotnet build MyVocaList.sln -f net10.0-android
```
Expected: Build succeeded, 0 errors.

---

## Task 4: Create `AutocompleteField` code-behind

**Files:**
- Create: `MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml.cs`

> The debounce pattern matches `VenuesViewModel.TriggerSearchDebounce`: CTS cancel/dispose in a try-catch, new CTS, `Task.Run` with `Task.Delay`.
>
> Focus/blur guard: on `Unfocused`, wait 300ms before hiding the overlay so a tap on a suggestion row registers before the overlay disappears.

- [ ] **Step 1: Create the code-behind file**

```csharp
namespace MyVocaList.UI.Components.AutocompleteField;

public partial class AutocompleteField : ContentView
{
    // ── BindableProperties ────────────────────────────────────────────────

    public static readonly BindableProperty LabelTextProperty =
        BindableProperty.Create(nameof(LabelText), typeof(string), typeof(AutocompleteField), "");

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(AutocompleteField), "");

    public static readonly BindableProperty HasErrorProperty =
        BindableProperty.Create(nameof(HasError), typeof(bool), typeof(AutocompleteField), false,
            propertyChanged: (b, _, n) => ((AutocompleteField)b).searchEdit.HasError = (bool)n);

    public static readonly BindableProperty ErrorTextProperty =
        BindableProperty.Create(nameof(ErrorText), typeof(string), typeof(AutocompleteField), "",
            propertyChanged: (b, _, n) => ((AutocompleteField)b).searchEdit.ErrorText = (string)n);

    public static readonly BindableProperty SuggestionsProperty =
        BindableProperty.Create(nameof(Suggestions), typeof(IEnumerable<AutocompleteSuggestion>),
            typeof(AutocompleteField), null,
            propertyChanged: (b, _, n) => ((AutocompleteField)b).OnSuggestionsChanged((IEnumerable<AutocompleteSuggestion>)n));

    public static readonly BindableProperty DebounceDelayProperty =
        BindableProperty.Create(nameof(DebounceDelay), typeof(int), typeof(AutocompleteField), 300);

    public static readonly BindableProperty SearchRequestedCommandProperty =
        BindableProperty.Create(nameof(SearchRequestedCommand), typeof(ICommand), typeof(AutocompleteField), null);

    public static readonly BindableProperty SuggestionSelectedCommandProperty =
        BindableProperty.Create(nameof(SuggestionSelectedCommand), typeof(ICommand), typeof(AutocompleteField), null);

    // ── Public properties ─────────────────────────────────────────────────

    public string LabelText
    {
        get => (string)GetValue(LabelTextProperty);
        set => SetValue(LabelTextProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public bool HasError
    {
        get => (bool)GetValue(HasErrorProperty);
        set => SetValue(HasErrorProperty, value);
    }

    public string ErrorText
    {
        get => (string)GetValue(ErrorTextProperty);
        set => SetValue(ErrorTextProperty, value);
    }

    public IEnumerable<AutocompleteSuggestion> Suggestions
    {
        get => (IEnumerable<AutocompleteSuggestion>)GetValue(SuggestionsProperty);
        set => SetValue(SuggestionsProperty, value);
    }

    public int DebounceDelay
    {
        get => (int)GetValue(DebounceDelayProperty);
        set => SetValue(DebounceDelayProperty, value);
    }

    public ICommand SearchRequestedCommand
    {
        get => (ICommand)GetValue(SearchRequestedCommandProperty);
        set => SetValue(SearchRequestedCommandProperty, value);
    }

    public ICommand SuggestionSelectedCommand
    {
        get => (ICommand)GetValue(SuggestionSelectedCommandProperty);
        set => SetValue(SuggestionSelectedCommandProperty, value);
    }

    // ── Private state ─────────────────────────────────────────────────────

    private CancellationTokenSource _debounceCts;

    // ── Constructor ───────────────────────────────────────────────────────

    public AutocompleteField()
    {
        InitializeComponent();
    }

    // ── Suggestions changed ───────────────────────────────────────────────

    private void OnSuggestionsChanged(IEnumerable<AutocompleteSuggestion> suggestions)
    {
        var list = suggestions?.ToList();
        suggestionsView.ItemsSource = list;
        overlayCard.IsVisible = list?.Count > 0;
    }

    // ── TextEdit events ───────────────────────────────────────────────────

    private void OnTextChanged(object sender, EventArgs e)
    {
        var text = searchEdit.Text ?? "";

        if (text.Length < 2)
        {
            // Clear overlay immediately — do not fire search
            Suggestions = null;
            return;
        }

        TriggerDebounce(text);
    }

    private void TriggerDebounce(string text)
    {
        try
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
        }
        catch { /* ignore disposal races */ }

        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;
        var delay = DebounceDelay;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay, token);
                if (token.IsCancellationRequested) return;
                SearchRequestedCommand?.Execute(text);
            }
            catch (OperationCanceledException) { /* ignore */ }
        }, token);
    }

    // ── Focus / blur guard ────────────────────────────────────────────────

    private void OnSearchEditFocused(object sender, FocusEventArgs e)
    {
        // Re-show overlay if suggestions are present when field re-focuses
        var list = Suggestions?.ToList();
        if (list?.Count > 0)
            overlayCard.IsVisible = true;
    }

    private async void OnSearchEditUnfocused(object sender, FocusEventArgs e)
    {
        // Delay before hiding so a suggestion tap can register first
        await Task.Delay(300);
        overlayCard.IsVisible = false;
    }

    // ── Suggestion tap ────────────────────────────────────────────────────

    private void OnSuggestionTapped(object sender, CollectionViewGestureEventArgs e)
    {
        if (e.Item is not AutocompleteSuggestion suggestion) return;
        overlayCard.IsVisible = false;
        SuggestionSelectedCommand?.Execute(suggestion);
    }
}
```

- [ ] **Step 2: Build and confirm 0 errors**

```
dotnet build MyVocaList.sln -f net10.0-android
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```
git add MyVocaList/UI/Components/AutocompleteField/
git commit -m "feat(ui): AutocompleteField component — MD3 docked search bar with debounce overlay"
```

---

## Task 5: Write unit tests for `AutocompleteField` debounce behavior

> These tests verify the debounce timing and command firing rules. Because `AutocompleteField` is a MAUI `ContentView`, we test the **debounce logic** by extracting it as an internal helper — or by testing the observable outcome via the `SearchRequestedCommand` mock.
>
> **Approach:** Use a `Mock<ICommand>` for `SearchRequestedCommand`. Drive the component via the `OnTextChanged` path by calling the internal method directly after making it `internal`. Add `[assembly: InternalsVisibleTo("MyVocaList.Tests")]` to the MAUI project.

- [ ] **Step 1: Expose internals to tests**

In `MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml.cs`, add at the top of the file (before the namespace):

```csharp
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MyVocaList.Tests")]
```

Change `TriggerDebounce` from `private` to `internal`:

```csharp
internal void TriggerDebounce(string text)
```

- [ ] **Step 2: Write the failing test class**

Create `MyVocaList.Tests/Unit/Components/AutocompleteFieldDebounceTests.cs`:

```csharp
using MyVocaList.UI.Components.AutocompleteField;

namespace MyVocaList.Tests.Unit.Components;

public class AutocompleteFieldDebounceTests
{
    [Fact]
    public async Task TriggerDebounce_AfterDelay_ExecutesSearchCommand()
    {
        var commandMock = new Mock<ICommand>();
        commandMock.Setup(c => c.CanExecute(It.IsAny<object>())).Returns(true);

        var field = new AutocompleteField
        {
            DebounceDelay = 50,
            SearchRequestedCommand = commandMock.Object
        };

        field.TriggerDebounce("jo");

        await Task.Delay(150); // wait well past debounce

        commandMock.Verify(c => c.Execute("jo"), Times.Once);
    }

    [Fact]
    public async Task TriggerDebounce_RapidCalls_ExecutesOnlyOnce()
    {
        var commandMock = new Mock<ICommand>();
        commandMock.Setup(c => c.CanExecute(It.IsAny<object>())).Returns(true);

        var field = new AutocompleteField
        {
            DebounceDelay = 100,
            SearchRequestedCommand = commandMock.Object
        };

        // Rapid typing — should cancel previous and fire only once
        field.TriggerDebounce("j");
        field.TriggerDebounce("jo");
        field.TriggerDebounce("joh");
        field.TriggerDebounce("john");

        await Task.Delay(250); // wait well past final debounce

        commandMock.Verify(c => c.Execute(It.IsAny<object>()), Times.Once);
        commandMock.Verify(c => c.Execute("john"), Times.Once);
    }

    [Fact]
    public async Task TriggerDebounce_NullCommand_DoesNotThrow()
    {
        var field = new AutocompleteField
        {
            DebounceDelay = 50,
            SearchRequestedCommand = null
        };

        var ex = await Record.ExceptionAsync(async () =>
        {
            field.TriggerDebounce("test");
            await Task.Delay(150);
        });

        Assert.Null(ex);
    }
}
```

- [ ] **Step 3: Run tests and confirm they fail (Red)**

```
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~AutocompleteFieldDebounceTests" --verbosity normal
```

Expected: 3 tests fail — `AutocompleteField` MAUI type cannot be instantiated in `net10.0` test context.

> **Note:** If `AutocompleteField` cannot be instantiated in the non-MAUI test TFM (because it inherits from `ContentView`), these tests must be marked `[Fact(Skip = "Requires MAUI runtime")]` and the debounce logic must be extracted to a testable plain class. See the fallback step below.

- [ ] **Step 4 (Fallback): Extract debounce logic to a testable class**

If tests fail due to MAUI runtime absence, create `MyVocaList/UI/Components/AutocompleteField/AutocompleteDebouncer.cs`:

```csharp
namespace MyVocaList.UI.Components.AutocompleteField;

/// <summary>
/// Plain debounce helper extracted for unit testability.
/// Owned and driven by <see cref="AutocompleteField"/> code-behind.
/// </summary>
internal sealed class AutocompleteDebouncer
{
    private CancellationTokenSource _cts;

    /// <summary>
    /// Cancels any pending debounce and starts a new one.
    /// After <paramref name="delayMs"/> elapses without interruption,
    /// <paramref name="onElapsed"/> is invoked with <paramref name="text"/>.
    /// </summary>
    internal void Trigger(string text, int delayMs, Action<string> onElapsed)
    {
        try
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
        catch { /* ignore disposal races */ }

        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delayMs, token);
                if (token.IsCancellationRequested) return;
                onElapsed(text);
            }
            catch (OperationCanceledException) { /* ignore */ }
        }, token);
    }
}
```

Update `AutocompleteField.xaml.cs` to use `AutocompleteDebouncer`:

```csharp
// Replace _debounceCts field with:
private readonly AutocompleteDebouncer _debouncer = new();

// Replace TriggerDebounce method with:
internal void TriggerDebounce(string text)
{
    _debouncer.Trigger(text, DebounceDelay, t => SearchRequestedCommand?.Execute(t));
}
```

Update `AutocompleteFieldDebounceTests.cs` to test `AutocompleteDebouncer` directly:

```csharp
using MyVocaList.UI.Components.AutocompleteField;

namespace MyVocaList.Tests.Unit.Components;

public class AutocompleteFieldDebounceTests
{
    [Fact]
    public async Task Trigger_AfterDelay_InvokesCallback()
    {
        var results = new List<string>();
        var debouncer = new AutocompleteDebouncer();

        debouncer.Trigger("jo", 50, t => results.Add(t));

        await Task.Delay(150);

        Assert.Single(results);
        Assert.Equal("jo", results[0]);
    }

    [Fact]
    public async Task Trigger_RapidCalls_OnlyLastCallbackFires()
    {
        var results = new List<string>();
        var debouncer = new AutocompleteDebouncer();

        debouncer.Trigger("j",    100, t => results.Add(t));
        debouncer.Trigger("jo",   100, t => results.Add(t));
        debouncer.Trigger("joh",  100, t => results.Add(t));
        debouncer.Trigger("john", 100, t => results.Add(t));

        await Task.Delay(250);

        Assert.Single(results);
        Assert.Equal("john", results[0]);
    }

    [Fact]
    public async Task Trigger_NullCallback_DoesNotThrow()
    {
        var debouncer = new AutocompleteDebouncer();

        var ex = await Record.ExceptionAsync(async () =>
        {
            debouncer.Trigger("test", 50, null!);
            await Task.Delay(150);
        });

        Assert.Null(ex);
    }
}
```

- [ ] **Step 5: Run tests and confirm they pass (Green)**

```
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --filter "FullyQualifiedName~AutocompleteFieldDebounceTests" --verbosity normal
```

Expected: 3 tests pass.

- [ ] **Step 6: Build MAUI project and confirm 0 errors**

```
dotnet build MyVocaList.sln -f net10.0-android
```

- [ ] **Step 7: Commit**

```
git add MyVocaList/UI/Components/AutocompleteField/AutocompleteDebouncer.cs
git add MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml.cs
git add MyVocaList.Tests/Unit/Components/AutocompleteFieldDebounceTests.cs
git commit -m "test(autocomplete): debounce unit tests + extracted AutocompleteDebouncer"
```

---

## Task 6: Add `AutocompleteField` namespace to GlobalUsings (if needed by 2+ pages)

> The component namespace `MyVocaList.UI.Components.AutocompleteField` is currently used only by `PersonFormPage`. Per `code-principles.md`: add to `GlobalUsings.cs` only when 2+ types in the project use it. Until Person CRUD is implemented, reference it per-page via the XAML `xmlns:` declaration. This task is a reminder to revisit after Person CRUD is implemented.

- [ ] **Step 1: Defer until Person CRUD is implemented**

No code change. Note in `GlobalUsings.cs` as a comment if helpful:
```csharp
// AutocompleteField namespace: add global using once 2+ pages use it (currently PersonFormPage only)
```

---

## Task 7: Update `Docs/specs/persons/` to reference `AutocompleteField`

**Files:**
- Modify: `Docs/specs/persons/design.md`
- Modify: `Docs/specs/persons/tasks.md`

The person CRUD spec was written before `AutocompleteField` existed. It describes an inline overlay in `PersonFormPage`. Update it to reflect the new component.

- [ ] **Step 1: Update `Docs/specs/persons/design.md`**

Find the section describing the suggestion overlay in `PersonFormPage`. Replace any description of a custom `DXBorder` card + `DXCollectionView` inline in the form with:

> **AutocompleteField component** (`MyVocaList/UI/Components/AutocompleteField/`) handles the TextEdit + debounce + overlay. `PersonFormViewModel` exposes `Suggestions`, `SearchPersonsCommand`, and `SuggestionSelectedCommand`. The component forwards `HasError`/`ErrorText` for validation display.

- [ ] **Step 2: Update `Docs/specs/persons/tasks.md`**

Remove any task that says "build suggestion overlay in PersonFormPage" or "add DXBorder card for results". Replace with:

> - [ ] Use `AutocompleteField` in `PersonFormPage` — bind `Suggestions`, `SearchPersonsCommand`, `SuggestionSelectedCommand`

- [ ] **Step 3: Commit**

```
git add Docs/specs/persons/design.md Docs/specs/persons/tasks.md
git commit -m "docs(persons): update spec to reference AutocompleteField instead of inline overlay"
```

---

## Self-Review

### Spec coverage check

| Spec requirement | Task |
|---|---|
| `AutocompleteSuggestion` record in Contracts | Task 1 |
| `LabelText`, `Placeholder`, `HasError`, `ErrorText` BPs | Task 4 |
| `Suggestions` BP — drives overlay visibility | Task 4 |
| `DebounceDelay` BP (default 300ms) | Task 4 |
| `SearchRequestedCommand` fired after debounce | Task 4 + 5 |
| `SuggestionSelectedCommand` fired on tap | Task 4 |
| Text < 2 chars → overlay hidden, no search fired | Task 4 (`OnTextChanged`) |
| Focus lost → overlay hides with 300ms guard | Task 4 (`OnSearchEditUnfocused`) |
| Overlay: same Grid row, ZIndex=10, Margin.Top=56 | Task 3 |
| MaximumHeightRequest=280dp (5 rows) | Task 3 |
| `ListItem` used for result rows | Task 3 |
| `SurfaceContainerHigh`, `CornerRadius=12`, Level 2 shadow | Task 3 |
| Debounce tests (rapid-fire, single fire, null guard) | Task 5 |
| Persons spec updated | Task 7 |

All spec requirements covered.

### Placeholder scan

No TBD, TODO, or "similar to" placeholders found.

### Type consistency

- `AutocompleteSuggestion` — defined in Task 1, used in Task 3 (DataTemplate), Task 4 (BPs + tap handler). Consistent.
- `AutocompleteDebouncer` — defined in Task 5 fallback, referenced in Task 5 test. Consistent.
- `CollectionViewGestureEventArgs` — DX type used in `OnSuggestionTapped`. Consistent with codebase usage in `VenuesPage`.
