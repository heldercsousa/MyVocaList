# Autocomplete Component Rebuild — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give the existing `AutocompleteField` a phone-only full-screen mode (`AutocompleteMobileField`) while leaving its desktop/tablet exposed-dropdown behavior untouched, per `requirements.md` (README.md § 3 scope).

**Architecture:** `AutocompleteField` gains one new decision point — on focus, check an injected `IDeviceInfo.Idiom`. On `DeviceIdiom.Phone` it pushes a new full-screen `AutocompleteMobileField` `ContentPage` modally (via `Shell.Current.Navigation.PushModalAsync`), binding the new page's `Text`/`Suggestions` back to itself so the existing debounce → `SearchRequestedCommand` → `Suggestions` → row-tap → `SuggestionSelectedCommand` pipeline is reused unmodified. On `Desktop`/`Tablet` the existing `overlayCard` `DXBorder` is unchanged.

**Tech Stack:** .NET MAUI 10 (net10.0-android/ios), C# 13, DevExpress MAUI 25.2.4 (`dxe:TextEdit`, `dxcv:DXCollectionView`, `dx:DXButton`), xunit + Moq (test project already references `MyVocaList.csproj`).

## Global Constraints

- English only in code/comments/logs (CLAUDE.md § Constitutional Constraints).
- SafeAreaEdges: new `ContentPage` must set `SafeAreaEdges="Container"` explicitly (.NET MAUI 10 breaking-change default is `None`).
- Incremental edits: edit one component (XAML+code-behind pair) at a time, build, fix, then move to the next.
- MD3 terminology: "Search Bar" (docked field) / "Search View" (phone full-screen takeover) / "Menu" (desktop filtered-dropdown) — never "overlay card"/"suggestions overlay" in new code/comments (AC-9).
- No DevExpress `AutoCompleteEdit` or related provider types anywhere in this change (AC-8; `.claude/exception-registry.md` 2026-07-11 entry).
- No changes to `SearchAppBar`, `CrudListView`, or `ListItem` themselves — only copy literal visual constants (AC-7, requirements.md Out of scope).
- No changes to `PersonFormPage`/`SongFormPage`, or any ViewModel — out of scope for this task (requirements.md).
- `AutocompleteField`'s existing public bindable properties (`Text`, `LabelText`, `Placeholder`, `HasError`, `ErrorText`, `Suggestions`, `DebounceDelay`, `SearchRequestedCommand`, `SuggestionSelectedCommand`, `BlurredWithoutSelectionCommand`) must not change name, type, or default (design.md Gate 2 — zero consumer changes required).
- Idiom detection goes through an injected/resolved `IDeviceInfo`, never a bare `DeviceInfo.Current.Idiom` call inline in branching logic (requirements.md Validation rules) — resolved here via a small DI-free static helper (see Task 1) plus a settable seam on the component (see Task 3), since `AutocompleteField` has no constructor-injection path (it's instantiated by the XAML compiler in consumer pages, confirmed by its current parameterless constructor).

---

## File Structure

| File | Responsibility |
|---|---|
| `MyVocaList/UI/Components/AutocompleteField/AutocompleteWindowClass.cs` **(new)** | Pure, MAUI-runtime-free static helper: `IsCompactWindow(IDeviceInfo)`. Mirrors `AutocompleteDebouncer`'s "extracted for unit testability" pattern — this is what makes the idiom branch unit-testable without instantiating a `ContentView` in xunit (this repo has no headless MAUI test harness). |
| `MyVocaList/UI/Components/AutocompleteField/AutocompleteMobileField.xaml` **(new)** | Full-screen phone `ContentPage`: Search Bar row (back button + `TextEdit` styled from copied `SearchAppBar` constants) + Search View (`DXCollectionView` of `ListItem` rows). No commands/bindable-to-ViewModel wiring of its own beyond `Text`/`Placeholder`/`Suggestions` — purely a rendering surface driven by the host field. |
| `MyVocaList/UI/Components/AutocompleteField/AutocompleteMobileField.xaml.cs` **(new)** | Bindable properties `Text`/`Placeholder`/`Suggestions`; `SuggestionTapped`/`Cancelled` events; auto-focus in `OnAppearing`; hardware-back-button override. |
| `MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml.cs` **(modify)** | Adds `DeviceInfo` (internal settable, DI-resolved by default) + `IsCompactWindow` (delegates to `AutocompleteWindowClass`); branches `OnSearchEditFocused` to construct + push `AutocompleteMobileField` on Phone; new private methods to wire/unwire the modal's events and bindings; guards `OnSearchEditUnfocused` against firing `BlurredWithoutSelectionCommand` during the programmatic unfocus that precedes the modal push. |
| `MyVocaList.Tests/Unit/Components/AutocompleteWindowClassTests.cs` **(new)** | Unit tests for `AutocompleteWindowClass.IsCompactWindow` across `DeviceIdiom.Phone`/`Desktop`/`Tablet`, using `Mock<IDeviceInfo>` exactly like `FeedbackServiceTests`. |

---

### Task 1: `AutocompleteWindowClass` — pure idiom-branch helper (TDD)

**Files:**
- Create: `MyVocaList/UI/Components/AutocompleteField/AutocompleteWindowClass.cs`
- Test: `MyVocaList.Tests/Unit/Components/AutocompleteWindowClassTests.cs`

**Interfaces:**
- Produces: `internal static class AutocompleteWindowClass { internal static bool IsCompactWindow(IDeviceInfo deviceInfo); }` — consumed by Task 3's `AutocompleteField.IsCompactWindow` property.

- [ ] **Step 1: Write the failing tests**

```csharp
// MyVocaList.Tests/Unit/Components/AutocompleteWindowClassTests.cs
using Microsoft.Maui.Devices;
using MyVocaList.UI.Components.AutocompleteField;

namespace MyVocaList.Tests.Unit.Components;

public class AutocompleteWindowClassTests
{
    [Fact]
    public void IsCompactWindow_PhoneIdiom_ReturnsTrue()
    {
        var deviceInfoMock = new Mock<IDeviceInfo>();
        deviceInfoMock.Setup(d => d.Idiom).Returns(DeviceIdiom.Phone);

        var result = AutocompleteWindowClass.IsCompactWindow(deviceInfoMock.Object);

        Assert.True(result);
    }

    [Fact]
    public void IsCompactWindow_DesktopIdiom_ReturnsFalse()
    {
        var deviceInfoMock = new Mock<IDeviceInfo>();
        deviceInfoMock.Setup(d => d.Idiom).Returns(DeviceIdiom.Desktop);

        var result = AutocompleteWindowClass.IsCompactWindow(deviceInfoMock.Object);

        Assert.False(result);
    }

    [Fact]
    public void IsCompactWindow_TabletIdiom_ReturnsFalse()
    {
        var deviceInfoMock = new Mock<IDeviceInfo>();
        deviceInfoMock.Setup(d => d.Idiom).Returns(DeviceIdiom.Tablet);

        var result = AutocompleteWindowClass.IsCompactWindow(deviceInfoMock.Object);

        Assert.False(result);
    }

    [Fact]
    public void IsCompactWindow_NullDeviceInfo_ReturnsFalse()
    {
        var result = AutocompleteWindowClass.IsCompactWindow(null);

        Assert.False(result);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test MyVocaList.Tests --filter AutocompleteWindowClassTests`
Expected: FAIL — compile error, `AutocompleteWindowClass` does not exist.

- [ ] **Step 3: Write minimal implementation**

```csharp
// MyVocaList/UI/Components/AutocompleteField/AutocompleteWindowClass.cs
using Microsoft.Maui.Devices;

namespace MyVocaList.UI.Components.AutocompleteField;

/// <summary>
/// Pure idiom-branch check — extracted for unit testability (no MAUI runtime dependency),
/// mirroring <see cref="AutocompleteDebouncer"/>'s extraction rationale.
/// </summary>
internal static class AutocompleteWindowClass
{
    internal static bool IsCompactWindow(IDeviceInfo deviceInfo) =>
        deviceInfo?.Idiom == DeviceIdiom.Phone;
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test MyVocaList.Tests --filter AutocompleteWindowClassTests`
Expected: PASS (4 tests).

- [ ] **Step 5: Commit**

```bash
git add MyVocaList/UI/Components/AutocompleteField/AutocompleteWindowClass.cs MyVocaList.Tests/Unit/Components/AutocompleteWindowClassTests.cs
git commit -m "feat: add AutocompleteWindowClass idiom-branch helper for phone autocomplete"
```

---

### Task 2: `AutocompleteMobileField` — new full-screen phone component

**Files:**
- Create: `MyVocaList/UI/Components/AutocompleteField/AutocompleteMobileField.xaml`
- Create: `MyVocaList/UI/Components/AutocompleteField/AutocompleteMobileField.xaml.cs`

**Interfaces:**
- Consumes: `MyVocaList.Contracts.Models.AutocompleteSuggestion` (existing, has `Headline`/`SupportingText`); `MyVocaList.UI.Components.Lists.ListItem` (existing, `Headline`/`SupportingText` bindables, confirmed in explore report); DevExpress `dx:DXButton` `Style="{StaticResource NavigationIconButton}"` and `dxcv:DXCollectionView` (both already used by `SearchAppBar`/`AutocompleteField`).
- Produces: `public partial class AutocompleteMobileField : ContentPage` with:
  - `public string Text { get; set; }` (two-way bindable, default `""`)
  - `public string Placeholder { get; set; }` (bindable, default `""`)
  - `public IEnumerable<AutocompleteSuggestion> Suggestions { get; set; }` (bindable)
  - `public event EventHandler<AutocompleteSuggestion> SuggestionTapped;`
  - `public event EventHandler Cancelled;`
  These are consumed by Task 3's `AutocompleteField`.

- [ ] **Step 1: Create the XAML**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:dx="http://schemas.devexpress.com/maui"
    xmlns:dxe="clr-namespace:DevExpress.Maui.Editors;assembly=DevExpress.Maui.Editors"
    xmlns:dxcv="clr-namespace:DevExpress.Maui.CollectionView;assembly=DevExpress.Maui.CollectionView"
    xmlns:lists="clr-namespace:MyVocaList.UI.Components.Lists"
    xmlns:models="clr-namespace:MyVocaList.Contracts.Models;assembly=MyVocaList.Contracts"
    x:Class="MyVocaList.UI.Components.AutocompleteField.AutocompleteMobileField"
    x:Name="self"
    SafeAreaEdges="Container"
    BackgroundColor="{StaticResource Surface}">

    <Grid RowDefinitions="Auto,*">

        <!-- MD3 Search Bar row: back button + input, visual constants copied from SearchAppBar -->
        <Grid Grid.Row="0"
              ColumnDefinitions="Auto,*"
              ColumnSpacing="0"
              HeightRequest="64"
              BackgroundColor="{StaticResource Surface}"
              Padding="4,0">

            <dx:DXButton Grid.Column="0"
                         Icon="arrow_back_outlined"
                         Style="{StaticResource NavigationIconButton}"
                         SemanticProperties.Description="Back"
                         Clicked="OnBackButtonClicked" />

            <dxe:TextEdit x:Name="searchEdit"
                          Grid.Column="1"
                          Text="{Binding Text, Source={x:Reference self}, Mode=TwoWay}"
                          PlaceholderText="{Binding Placeholder, Source={x:Reference self}}"
                          BoxMode="Outlined"
                          BorderColor="Transparent"
                          FocusedBorderColor="Transparent"
                          BackgroundColor="Transparent"
                          TextColor="{StaticResource OnSurface}"
                          PlaceholderColor="{StaticResource OnSurfaceVariant}"
                          ClearIconVisibility="Auto"
                          ClearIconColor="{StaticResource OnSurfaceVariant}"
                          Keyboard="Text"
                          ReturnType="Search"
                          VerticalOptions="Center" />
        </Grid>

        <!-- MD3 Search View: results list, ListItem rows reused from CRUD-list pattern -->
        <dxcv:DXCollectionView
            x:Name="suggestionsView"
            Grid.Row="1"
            ItemsSource="{Binding Suggestions, Source={x:Reference self}}"
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

    </Grid>
</ContentPage>
```

- [ ] **Step 2: Create the code-behind**

```csharp
// MyVocaList/UI/Components/AutocompleteField/AutocompleteMobileField.xaml.cs
namespace MyVocaList.UI.Components.AutocompleteField;

public partial class AutocompleteMobileField : ContentPage
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(AutocompleteMobileField), string.Empty,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(AutocompleteMobileField), string.Empty);

    public static readonly BindableProperty SuggestionsProperty =
        BindableProperty.Create(nameof(Suggestions), typeof(IEnumerable<AutocompleteSuggestion>),
            typeof(AutocompleteMobileField), null);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public IEnumerable<AutocompleteSuggestion> Suggestions
    {
        get => (IEnumerable<AutocompleteSuggestion>)GetValue(SuggestionsProperty);
        set => SetValue(SuggestionsProperty, value);
    }

    /// <summary>Raised when the user taps a suggestion row in the Search View.</summary>
    public event EventHandler<AutocompleteSuggestion> SuggestionTapped;

    /// <summary>Raised when the user backs out (button or hardware back) without selecting a suggestion.</summary>
    public event EventHandler Cancelled;

    public AutocompleteMobileField()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        searchEdit.Focus();
    }

    private void OnBackButtonClicked(object sender, EventArgs e)
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    protected override bool OnBackButtonPressed()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private void OnSuggestionTapped(object sender, CollectionViewGestureEventArgs e)
    {
        if (e.Item is not AutocompleteSuggestion suggestion) return;
        SuggestionTapped?.Invoke(this, suggestion);
    }
}
```

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`
Expected: 0 errors. (Level C per design.md § 5 — MAUI page rendering isn't practically unit-tested in this project; no automated test for this file. Manual E2E deferred to README.md § 4, the consumer-wiring task.)

- [ ] **Step 4: Commit**

```bash
git add MyVocaList/UI/Components/AutocompleteField/AutocompleteMobileField.xaml MyVocaList/UI/Components/AutocompleteField/AutocompleteMobileField.xaml.cs
git commit -m "feat: add AutocompleteMobileField full-screen phone Search View component"
```

---

### Task 3: Wire `AutocompleteField` to branch on idiom

**Files:**
- Modify: `MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml.cs`

**Interfaces:**
- Consumes: `AutocompleteWindowClass.IsCompactWindow(IDeviceInfo)` (Task 1); `AutocompleteMobileField` with `Text`/`Placeholder`/`Suggestions` bindables and `SuggestionTapped`/`Cancelled` events (Task 2).
- Produces: `internal IDeviceInfo DeviceInfo { get; set; }` (settable seam for tests) and `internal bool IsCompactWindow { get; }` on `AutocompleteField` — no new public API; existing public bindable properties (`Text`, `Suggestions`, `SearchRequestedCommand`, `SuggestionSelectedCommand`, `HasError`, `ErrorText`, `BlurredWithoutSelectionCommand`, `LabelText`, `Placeholder`, `DebounceDelay`) unchanged in name/type/default.

- [ ] **Step 1: Add the `using` statements and `DeviceInfo`/`IsCompactWindow` seam**

Edit `AutocompleteField.xaml.cs`. No new `using` statements are needed — `Microsoft.Maui`, `Microsoft.Maui.Devices`, and `Microsoft.Extensions.DependencyInjection` are already MAUI-SDK-generated global usings for this project (confirmed via `obj/Debug/net10.0-android/MyVocaList.GlobalUsings.g.cs`).

Inside the class, add after the existing `// ── Private state ──` fields (after `_isTappingSuggestion`):

```csharp
    private bool _isShowingMobileField;

    /// <summary>
    /// Resolved via the app's DI container by default (same singleton MauiProgram.cs registers
    /// for <c>IDeviceInfo</c>); settable so tests can inject a mock without a MAUI runtime.
    /// AutocompleteField has no constructor-injection path — it's instantiated by the compiled
    /// XAML of consumer pages — so this service-locator seam is the pragmatic equivalent.
    /// </summary>
    internal IDeviceInfo DeviceInfo { get; set; }

    internal bool IsCompactWindow => AutocompleteWindowClass.IsCompactWindow(DeviceInfo);
```

And change the constructor from:

```csharp
    public AutocompleteField()
    {
        InitializeComponent();
    }
```

to:

```csharp
    public AutocompleteField()
    {
        InitializeComponent();
        DeviceInfo = IPlatformApplication.Current?.Services.GetService<IDeviceInfo>()
            ?? Microsoft.Maui.Devices.DeviceInfo.Current;
    }
```

- [ ] **Step 2: Build to verify it still compiles with no behavior change yet**

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`
Expected: 0 errors.

- [ ] **Step 3: Branch `OnSearchEditFocused` and add the modal-push/unwind methods**

Replace:

```csharp
    private void OnSearchEditFocused(object sender, FocusEventArgs e)
    {
        var list = Suggestions?.ToList();
        if (list?.Count > 0)
            overlayCard.IsVisible = true;
    }
```

with:

```csharp
    private async void OnSearchEditFocused(object sender, FocusEventArgs e)
    {
        if (IsCompactWindow)
        {
            await ShowMobileFieldAsync();
            return;
        }

        var list = Suggestions?.ToList();
        if (list?.Count > 0)
            overlayCard.IsVisible = true;
    }

    private async Task ShowMobileFieldAsync()
    {
        _isShowingMobileField = true;
        searchEdit.Unfocus();

        var mobileField = new AutocompleteMobileField
        {
            Placeholder = Placeholder
        };
        mobileField.SetBinding(AutocompleteMobileField.TextProperty,
            new Binding(nameof(Text), BindingMode.TwoWay, source: this));
        mobileField.SetBinding(AutocompleteMobileField.SuggestionsProperty,
            new Binding(nameof(Suggestions), source: this));

        mobileField.SuggestionTapped += OnMobileFieldSuggestionTapped;
        mobileField.Cancelled += OnMobileFieldCancelled;

        await Shell.Current.Navigation.PushModalAsync(mobileField);
    }

    private async void OnMobileFieldSuggestionTapped(object sender, AutocompleteSuggestion suggestion)
    {
        var mobileField = (AutocompleteMobileField)sender;
        mobileField.SuggestionTapped -= OnMobileFieldSuggestionTapped;
        mobileField.Cancelled -= OnMobileFieldCancelled;

        SuggestionSelectedCommand?.Execute(suggestion);
        _isShowingMobileField = false;
        await Shell.Current.Navigation.PopModalAsync();
    }

    private async void OnMobileFieldCancelled(object sender, EventArgs e)
    {
        var mobileField = (AutocompleteMobileField)sender;
        mobileField.SuggestionTapped -= OnMobileFieldSuggestionTapped;
        mobileField.Cancelled -= OnMobileFieldCancelled;

        BlurredWithoutSelectionCommand?.Execute(null);
        _isShowingMobileField = false;
        await Shell.Current.Navigation.PopModalAsync();
    }
```

- [ ] **Step 4: Guard `OnSearchEditUnfocused` against the programmatic unfocus that precedes the modal push**

Replace:

```csharp
    private async void OnSearchEditUnfocused(object sender, FocusEventArgs e)
    {
        await Task.Yield();
        if (!_isTappingSuggestion)
        {
            overlayCard.IsVisible = false;
            BlurredWithoutSelectionCommand?.Execute(null);
        }
    }
```

with:

```csharp
    private async void OnSearchEditUnfocused(object sender, FocusEventArgs e)
    {
        await Task.Yield();
        if (!_isTappingSuggestion && !_isShowingMobileField)
        {
            overlayCard.IsVisible = false;
            BlurredWithoutSelectionCommand?.Execute(null);
        }
    }
```

- [ ] **Step 5: Build**

Run: `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android`
Expected: 0 errors.

- [ ] **Step 6: Run the full existing test suite to confirm no regression**

Run: `dotnet test MyVocaList.Tests --filter "FullyQualifiedName~AutocompleteFieldDebounceTests|FullyQualifiedName~AutocompleteWindowClassTests|FullyQualifiedName~FeedbackServiceTests"`
Expected: PASS — all pre-existing `AutocompleteFieldDebounceTests` (AC-10, debounce unchanged) and `FeedbackServiceTests` (unaffected) plus Task 1's `AutocompleteWindowClassTests` still green.

- [ ] **Step 7: Commit**

```bash
git add MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml.cs
git commit -m "feat: branch AutocompleteField to push AutocompleteMobileField on phone idiom"
```

---

### Task 4: Task-log, AC traceability matrix, BACKLOG update

**Files:**
- Modify: `Docs/Management/DevCycleCraft/autocomplete-component/task-log.md` (create if absent)
- Modify: `Docs/Management/BACKLOG.md` (status update for this nested task under ②)

**Interfaces:** none (documentation only).

- [ ] **Step 1: Write the task-log entry**

Append to `Docs/Management/DevCycleCraft/autocomplete-component/task-log.md`:

```markdown
## Task: Build AutocompleteMobileField (README.md § 3)

**Status:** To Review

### Changed files
- `MyVocaList/UI/Components/AutocompleteField/AutocompleteWindowClass.cs` (new)
- `MyVocaList/UI/Components/AutocompleteField/AutocompleteMobileField.xaml` (new)
- `MyVocaList/UI/Components/AutocompleteField/AutocompleteMobileField.xaml.cs` (new)
- `MyVocaList/UI/Components/AutocompleteField/AutocompleteField.xaml.cs` (modified — idiom branch)
- `MyVocaList.Tests/Unit/Components/AutocompleteWindowClassTests.cs` (new)

### AC traceability matrix

| AC ID | Criterion | Implementation location | Test method |
|---|---|---|---|
| AC-1 | Phone render pushes full-screen modal | `AutocompleteField.xaml.cs` `OnSearchEditFocused`/`ShowMobileFieldAsync` | Manual E2E (deferred to README.md § 4 consumer wiring) |
| AC-2 | Desktop/Tablet render unchanged | `AutocompleteField.xaml.cs` `OnSearchEditFocused` (existing branch untouched) | `AutocompleteWindowClassTests.IsCompactWindow_DesktopIdiom_ReturnsFalse` / `_TabletIdiom_ReturnsFalse` |
| AC-3 | Auto-focus + keyboard on OnAppearing | `AutocompleteMobileField.xaml.cs` `OnAppearing` | Manual E2E (deferred) |
| AC-4 | Data flow parity (SearchRequestedCommand/Suggestions) | `AutocompleteField.xaml.cs` `ShowMobileFieldAsync` two-way `Text` + one-way `Suggestions` bindings | Manual E2E (deferred) — relies on existing `OnTextChanged` debounce path |
| AC-5 | Selection invokes SuggestionSelectedCommand + pops modal | `AutocompleteField.xaml.cs` `OnMobileFieldSuggestionTapped` | Manual E2E (deferred) |
| AC-6 | Cancel-without-selection invokes BlurredWithoutSelectionCommand | `AutocompleteField.xaml.cs` `OnMobileFieldCancelled` + `AutocompleteMobileField.xaml.cs` `OnBackButtonPressed`/`OnBackButtonClicked` | Manual E2E (deferred) |
| AC-7 | No SearchAppBar dependency, constants copied | `AutocompleteMobileField.xaml` (literal constants, no `SearchAppBar` reference) | Code review |
| AC-8 | No DevExpress AutoCompleteEdit | `AutocompleteMobileField.xaml`/`.xaml.cs` (uses `dxe:TextEdit`, not `AutoCompleteEdit`) | Code review |
| AC-9 | MD3 terminology | Search Bar/Search View comments in `AutocompleteMobileField.xaml` | Code review |
| AC-10 | Existing behavior preserved (debounce, Text feedback guard, HasError/ErrorText, ListItem rows) | Unchanged: `AutocompleteDebouncer.cs`, `TextProperty` propertyChanged guard, `HasErrorProperty`/`ErrorTextProperty` | `AutocompleteFieldDebounceTests` (pre-existing, re-run green) |
| Validation rule | IDeviceInfo injected, never static `DeviceInfo.Current.Idiom` inline | `AutocompleteWindowClass.IsCompactWindow(IDeviceInfo)` + `AutocompleteField.DeviceInfo` seam | `AutocompleteWindowClassTests` (all 4 cases) |

### Verification evidence
- `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-android` — 0 errors (paste actual output when run).
- `dotnet test MyVocaList.Tests --filter "FullyQualifiedName~AutocompleteFieldDebounceTests|FullyQualifiedName~AutocompleteWindowClassTests|FullyQualifiedName~FeedbackServiceTests"` — all pass (paste actual output when run).

### Manual E2E — deferred
AC-1, AC-3, AC-4, AC-5, AC-6 require a real phone-idiom render, which needs a consumer wired up (README.md § 4, out of scope for this task per design.md § 5). To be executed as part of that later task on an Android phone emulator, per design.md § 6 Gate 3 per-consumer risk table.
```

- [ ] **Step 2: Update BACKLOG.md status**

Edit `Docs/Management/BACKLOG.md` — find the row for BACKLOG ② nested task "Build the new MD3-compliant autocomplete component" and change its status marker to 🟡 (In Progress → ready for review) or ✅ once Task-log review is closed by Helder, per this project's status vocabulary (💡→📋→🗺️→🟢→🟡→✅).

- [ ] **Step 3: Add a dated correction note to design.md**

`design.md § 1`/`§ 2` states `IDeviceInfo` would be "injected the same way FeedbackService already does it — DI singleton... mockable in tests exactly like FeedbackServiceTests." This turned out to be inaccurate: `AutocompleteField` is a `ContentView` instantiated by the XAML compiler in consumer pages (parameterless constructor, no DI resolution path), unlike `FeedbackService` which is DI-resolved. Per the SDD Invariant (spec is immutable history — append a dated note rather than rewriting), add directly below the `## 1. Architecture` heading in `design.md`:

```markdown
> **Design updated 2026-07-11:** `IDeviceInfo` is not constructor-injected as originally stated —
> `AutocompleteField` has no DI resolution path (it's instantiated by the XAML compiler in consumer
> pages, confirmed by its parameterless constructor). Implemented instead via a service-locator seam
> (`IPlatformApplication.Current.Services.GetService<IDeviceInfo>()`, defaulted in the constructor,
> overridable via an internal settable `DeviceInfo` property) plus a MAUI-runtime-free static helper
> `AutocompleteWindowClass.IsCompactWindow(IDeviceInfo)` for unit testability — mirroring the existing
> `AutocompleteDebouncer` extraction pattern in the same folder. See `plan.md` Task 1/Task 3.
```

- [ ] **Step 4: Register the new task-log.md in MyVocaList.sln**

Per `constraints-registry.md § Visual Studio Solution` (HARD GATE — every file created in `Docs/` must be reflected in `MyVocaList.sln` in the same commit): open `MyVocaList.sln`, find the `ProjectSection(SolutionItems)` for the Solution Folder matching `Docs/Management/DevCycleCraft/autocomplete-component/` (it already lists `README.md`, `requirements.md`, `design.md` — add the new entry alongside them), and add:

```
Docs\Management\DevCycleCraft\autocomplete-component\task-log.md = Docs\Management\DevCycleCraft\autocomplete-component\task-log.md
```

- [ ] **Step 5: Commit**

```bash
git add Docs/Management/DevCycleCraft/autocomplete-component/task-log.md Docs/Management/DevCycleCraft/autocomplete-component/design.md Docs/Management/BACKLOG.md MyVocaList.sln
git commit -m "docs: task-log + BACKLOG status for AutocompleteMobileField build; design.md DI correction"
```

---

## Self-Review Notes

- **Spec coverage:** AC-1..AC-10 and the one Validation rule are each mapped to a task/file/test above (see Task 4 matrix). Out-of-scope items (PersonFormPage/SongFormPage wiring, SearchAppBar/CrudListView/ListItem changes, ux-patterns.md update, width-breakpoint detection) are not touched by any task.
- **Design deviation flagged inline, not silently made:** design.md loosely referenced `ItemsSource`/`SearchCommand`/`SelectedItemCommand` — the plan uses the actual existing names `Suggestions`/`SearchRequestedCommand`/`SuggestionSelectedCommand` (confirmed by reading the real file). This is a naming correction, not a behavior change — Gate 2's "no consumer changes" holds.
- **Design deviation flagged inline #2:** design.md said `IDeviceInfo` would be "injected the same way FeedbackService already does it — DI singleton, constructor-injected." `AutocompleteField` has no constructor-injection path (XAML-instantiated by consumer pages), so Task 3 uses a service-locator seam (`IPlatformApplication.Current.Services`) with an internal settable property for tests — functionally equivalent for the stated intent (mockable, no scattered static `DeviceInfo.Current.Idiom` calls) but not literally constructor DI. Flagged here for Helder's plan review rather than silently implemented.
- **Type consistency:** `AutocompleteMobileField.Text`/`Placeholder`/`Suggestions` property names and types match what Task 3's `ShowMobileFieldAsync` binds against; `SuggestionTapped`/`Cancelled` event signatures match Task 3's handler signatures exactly.
