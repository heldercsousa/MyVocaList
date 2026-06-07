# Search Picker Pattern

> Canonical pattern for explicit-submit search pages that return a single result to the caller via `WeakReferenceMessenger`.
>
> Established by: `ArtistPickerPage` / `SongPickerPage` / `YouTubeSearchPage` (June 2026).

---

## 1. When to Use a Picker Page vs Inline Search

| Search type | Pattern | Trigger |
|-------------|---------|---------|
| External API (music DB, YouTube) | **Picker page** — explicit submit via SearchCommand | Tapping a trigger `ListItem` row on a form page |
| Local DB (venues, persons, artists, songs CRUD) | **Inline** — reactive debounced search on the same page | User types in `SearchAppBar.SearchText` |

**Decision rule:** If the search incurs cost (API quota, latency, network) or requires confirmation before result is applied, use a picker page. If the search is free, fast, and local, keep it inline.

**SearchAppBar constraint:** Never add a `SearchCommand` as a reactive trigger to `SearchAppBar`. The `Action1Command` slot is the correct binding for an explicit submit action. The `SearchText` TwoWay binding collects the query; the user taps the search icon or presses return to submit.

---

## 2. Files That Constitute a Picker Implementation

Three layers, one file each:

```
MyVocaList.Contracts/
  Messages/
    XxxPickedMessage.cs          ← result carrier (Contracts project)

MyVocaList/
  UI/
    ViewModels/
      XxxPickerViewModel.cs      ← search logic, loading state, messaging
    Pages/
      [Domain]/
        XxxPickerPage.xaml       ← XAML shell + search bar + results list
        XxxPickerPage.xaml.cs    ← minimal code-behind (InitializeComponent only)
```

Supporting infrastructure (once per app, already registered):
- `INavigationService` — `MyVocaList.UI.Services.NavigationService` singleton
- `IMessenger` — `WeakReferenceMessenger.Default` singleton
- Route constant in `MyVocaList/Navigation/Routes.cs`
- Route + DI registrations in `AppShell.xaml.cs` and `MauiProgram.cs`

---

## 3. Message Record Pattern

```csharp
// Contracts/Messages/XxxPickedMessage.cs
namespace MyVocaList.Contracts.Messages;

public sealed record XxxPickedMessage(XxxResultDto Result);
```

- `sealed record` — immutable, no subclassing
- One property: `Result` typed to the DTO the caller needs
- Namespace: `MyVocaList.Contracts.Messages`
- Assembly: `MyVocaList.Contracts` (shared across all projects)

**Existing messages:**
- `ArtistPickedMessage(MusicSearchResultDto Result)`
- `SongPickedMessage(MusicSearchResultDto Result)`
- `YouTubeVideoPickedMessage(YouTubeSearchResultDto Result)`

---

## 4. ViewModel Contract

### Constructor injection

```csharp
public sealed partial class XxxPickerViewModel : ViewModelBase, IDisposable
{
    private readonly IXxxService _service;
    private readonly IMessenger _messenger;
    private readonly INavigationService _navigation;
    private readonly ILogger<XxxPickerViewModel> _logger;

    private CancellationTokenSource _cts = new();
```

### Required ObservableProperty fields

```csharp
[ObservableProperty] private string _searchText = string.Empty;
[ObservableProperty] private bool _isLoading;
[ObservableProperty] private bool _hasResults;
[ObservableProperty] private bool _hasSearched;
[ObservableProperty] private string _emptyStateMessage = string.Empty;
```

### IsShowEmptyState computed property

```csharp
// Three partial void notifiers — one per contributing property
partial void OnIsLoadingChanged(bool value)  => OnPropertyChanged(nameof(IsShowEmptyState));
partial void OnHasResultsChanged(bool value) => OnPropertyChanged(nameof(IsShowEmptyState));
partial void OnHasSearchedChanged(bool value) => OnPropertyChanged(nameof(IsShowEmptyState));

// Computed — NOT an ObservableProperty
public bool IsShowEmptyState => HasSearched && !HasResults && !IsLoading;
```

**Rationale:** Three separate `partial void On*Changed` calls ensure `IsShowEmptyState` is recalculated any time any of its three inputs change. A single `[ObservableProperty]` would not cover the compound condition.

### Results collection

```csharp
public ObservableRangeCollection<XxxResultDto> Results { get; } = [];
```

### Commands

```csharp
[RelayCommand(AllowConcurrentExecutions = false)]
private async Task SearchAsync() { ... }

[RelayCommand]
private async Task SelectResultAsync(XxxResultDto result) { ... }

[RelayCommand]
private Task BackAsync() => _navigation.GoBackAsync();
```

`AllowConcurrentExecutions = false` on `SearchCommand` prevents double-taps from launching concurrent API calls.

### Full SearchAsync loading discipline

```csharp
[RelayCommand(AllowConcurrentExecutions = false)]
private async Task SearchAsync()
{
    if (string.IsNullOrWhiteSpace(SearchText)) return;

    // Cancel any in-flight request before starting a new one
    _cts.Cancel();
    _cts.Dispose();
    _cts = new CancellationTokenSource();
    var ct = _cts.Token;

    // Synchronous state reset — before first await
    IsLoading = true;
    HasSearched = false;
    Results.Clear();

    try
    {
        var items = await _service.SearchXxxAsync(SearchText, ct);
        Results.ReplaceRange(items);
        HasResults = Results.Count > 0;
        HasSearched = true;
        EmptyStateMessage = "No results found";
    }
    catch (OperationCanceledException)
    {
        // Silently ignored — superseded by newer search
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Search failed for query {Query}", SearchText);
        HasResults = false;
        HasSearched = true;
        EmptyStateMessage = "Search failed. Please try again.";
    }
    finally
    {
        IsLoading = false;
    }
}
```

### SelectResultAsync

```csharp
[RelayCommand]
private async Task SelectResultAsync(XxxResultDto result)
{
    _messenger.Send(new XxxPickedMessage(result));
    await _navigation.GoBackAsync();
}
```

Send before navigate — the caller registers before navigating, so the message arrives when the picker pops.

### IDisposable

```csharp
public void Dispose()
{
    try { _cts?.Cancel(); _cts?.Dispose(); }
    catch { /* ignore disposal races */ }
}
```

---

## 5. Page Structure (XAML)

### ContentPage root attributes

```xml
<ContentPage
    x:Class="MyVocaList.UI.Pages.[Domain].XxxPickerPage"
    ...
    x:DataType="vm:XxxPickerViewModel"
    BackgroundColor="{StaticResource Surface}"
    SafeAreaEdges="Container">
```

**Do NOT add `Shell.NavBarIsVisible="False"`** — the Shell nav bar hosts the `Shell.TitleView` which contains `SearchAppBar`. Hiding it would remove the search bar.

### Shell.TitleView

```xml
<Shell.TitleView>
    <appbars:SearchAppBar
        SearchText="{Binding SearchText, Mode=TwoWay}"
        BackCommand="{Binding BackCommand}"
        Action1Icon="search_outlined"
        Action1Command="{Binding SearchCommand}"
        Placeholder="Search [domain]..." />
</Shell.TitleView>
```

### Body — three-layer Grid (z-order matters)

```xml
<Grid>
    <!-- Layer 1 (bottom): Shimmer skeleton — visible while IsLoading -->
    <dx:ShimmerView IsActive="{Binding IsLoading}" IsVisible="{Binding IsLoading}">
        <VerticalStackLayout Spacing="0">
            <BoxView HeightRequest="56" Margin="16,8" CornerRadius="8"
                     Color="{StaticResource SurfaceVariant}" />
            <BoxView HeightRequest="56" Margin="16,8" CornerRadius="8"
                     Color="{StaticResource SurfaceVariant}" />
            <BoxView HeightRequest="56" Margin="16,8" CornerRadius="8"
                     Color="{StaticResource SurfaceVariant}" />
            <BoxView HeightRequest="56" Margin="16,8" CornerRadius="8"
                     Color="{StaticResource SurfaceVariant}" />
            <BoxView HeightRequest="56" Margin="16,8" CornerRadius="8"
                     Color="{StaticResource SurfaceVariant}" />
        </VerticalStackLayout>
    </dx:ShimmerView>

    <!-- Layer 2 (middle): EmptyState — visible when HasSearched && !HasResults && !IsLoading -->
    <states:EmptyState
        Illustration="search_outlined"
        Headline="{Binding EmptyStateMessage}"
        IsVisible="{Binding IsShowEmptyState}" />

    <!-- Layer 3 (top): Results list — renders above EmptyState in z-order -->
    <dx:DXCollectionView
        ItemsSource="{Binding Results}"
        IsVisible="{Binding HasResults}"
        UseRippleEffect="True">
        <dx:DXCollectionView.ItemTemplate>
            ...
        </dx:DXCollectionView.ItemTemplate>
    </dx:DXCollectionView>
</Grid>
```

**z-order rule:** Shimmer is lowest (index 0 in Grid), EmptyState is middle (index 1), DXCollectionView is highest (index 2). When `HasResults = true`, the list renders on top and visually covers EmptyState — no explicit hide needed.

### DataTemplate and TapGestureRecognizer

The `DataTemplate` must carry `x:DataType` on the DTO to enable compiled bindings. The `TapGestureRecognizer` uses `RelativeSource AncestorType` to reach the page's `BindingContext.SelectResultCommand` — a compiled binding cannot resolve a parent ViewModel from inside a DataTemplate without this.

```xml
<DataTemplate x:DataType="dto:XxxResultDto">
    <lists:ListItem Headline="{Binding PropertyName}">
        <lists:ListItem.GestureRecognizers>
            <TapGestureRecognizer
                Command="{Binding Source={RelativeSource AncestorType={x:Type ContentPage}}, Path=BindingContext.SelectResultCommand}"
                CommandParameter="{Binding .}" />
        </lists:ListItem.GestureRecognizers>
    </lists:ListItem>
</DataTemplate>
```

### ListItem variants

**Single-line** (ArtistPickerPage — headline only):
```xml
<lists:ListItem Headline="{Binding ArtistName}">
```

**Two-line** (SongPickerPage — headline + supporting text):
```xml
<lists:ListItem
    Headline="{Binding SongTitle}"
    SupportingText="{Binding ArtistName}">
```

**Leading image** (YouTubeSearchPage — thumbnail + headline + supporting):
```xml
<lists:ListItem
    Headline="{Binding Title}"
    SupportingText="{Binding ChannelName}">
    <lists:ListItem.LeadingContent>
        <lists:ListItemLeadingImage
            ImageSource="{Binding ThumbnailUrl}"
            Aspect="AspectFill" />
    </lists:ListItem.LeadingContent>
```

---

## 6. Caller Side — Register Before Navigate, Unregister After Receive

```csharp
[RelayCommand]
private async Task NavigateToXxxPickerAsync()
{
    // Register BEFORE navigating — message arrives on pop
    _messenger.Register<XxxPickedMessage>(this, (_, msg) =>
    {
        // Apply result to form fields
        XxxName = msg.Result.XxxName;
        _messenger.Unregister<XxxPickedMessage>(this);
    });
    await Shell.Current.GoToAsync(Routes.XxxPicker);
}
```

**Cancel path (back without selecting):** If the user presses Back without selecting a result, no message is sent. The registered handler stays active. On the next open of the picker, the `Register` call replaces the existing registration (WeakReferenceMessenger silently overwrites same-token registrations). This is safe and idempotent — no double-fire risk.

**Do NOT** register in the page's `OnAppearing` or the ViewModel's constructor. Register in the navigate command, immediately before `GoToAsync`.

---

## 7. DI Registration

```csharp
// MauiProgram.cs — picker pages and ViewModels are Transient (new instance per navigation)
builder.Services.AddTransient<ArtistPickerPage>();
builder.Services.AddTransient<ArtistPickerViewModel>();
builder.Services.AddTransient<SongPickerPage>();
builder.Services.AddTransient<SongPickerViewModel>();
builder.Services.AddTransient<YouTubeSearchPage>();
builder.Services.AddTransient<YouTubeSearchViewModel>();

// INavigationService — Singleton; use fully-qualified name to avoid
// ambiguity with DevExpress.Maui.Core.NavigationService
builder.Services.AddSingleton<MyVocaList.UI.Services.INavigationService,
                               MyVocaList.UI.Services.NavigationService>();

// IMessenger — Singleton; use WeakReferenceMessenger.Default as the instance
builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
// Required using: CommunityToolkit.Mvvm.Messaging
```

### Route registration

```csharp
// AppShell.xaml.cs constructor
public AppShell(AppShellViewModel vm, ...)
{
    ...
    Routing.RegisterRoute(Routes.ArtistPicker, typeof(ArtistPickerPage));
    Routing.RegisterRoute(Routes.SongPicker,   typeof(SongPickerPage));
    Routing.RegisterRoute(Routes.YouTubeSearch, typeof(YouTubeSearchPage));
}
```

---

## 8. Routes Constants

Add to `MyVocaList/Navigation/Routes.cs`:

```csharp
public const string ArtistPicker  = "artist-picker";
public const string SongPicker    = "song-picker";
public const string YouTubeSearch = "youtube-search";
```

Use these constants everywhere (`GoToAsync(Routes.ArtistPicker)`) — never hardcode route strings.

---

## 9. Unit Test Pattern

### Inject IMessenger via constructor; use new WeakReferenceMessenger() for isolation

```csharp
public class ArtistPickerViewModelTests
{
    private readonly Mock<IMusicMetadataService> _serviceMock = new();
    private readonly IMessenger _messenger = new WeakReferenceMessenger(); // NOT .Default
    private readonly Mock<INavigationService> _navMock = new();
    private readonly Mock<ILogger<ArtistPickerViewModel>> _loggerMock = new();

    private ArtistPickerViewModel CreateSut() =>
        new(_serviceMock.Object, _messenger, _navMock.Object, _loggerMock.Object);
```

**Never use `WeakReferenceMessenger.Default` in tests** — it is a global singleton and will leak state between test runs.

### Key test cases

```csharp
// Empty text — no service call
[Fact]
public async Task SearchCommand_EmptyText_DoesNotCallService()

// IsLoading = true before first await
[Fact]
public async Task SearchCommand_SetsIsLoadingTrueBeforeAwait()
// Pattern: use TaskCompletionSource to block the service mock; verify IsLoading mid-flight

// Successful search
[Fact]
public async Task SearchCommand_ValidQuery_PopulatesResultsAndSetsHasResultsTrue()

// Empty result set
[Fact]
public async Task SearchCommand_EmptyResults_SetsHasResultsFalseHasSearchedTrue()

// Exception path
[Fact]
public async Task SearchCommand_ServiceThrows_SetsHasSearchedTrueHasResultsFalse()

// Cancellation — no crash
[Fact]
public async Task SearchCommand_CancelledMidFlight_SilentlyIgnored()

// SelectResultCommand sends message
[Fact]
public async Task SelectResultCommand_SendsArtistPickedMessage()
{
    MusicSearchResultDto received = null;
    _messenger.Register<ArtistPickedMessage>(this, (_, msg) => received = msg.Result);
    var sut = CreateSut();
    var result = new MusicSearchResultDto { ArtistName = "Test" };

    await sut.SelectResultCommand.ExecuteAsync(result);

    Assert.NotNull(received);
    Assert.Equal("Test", received.ArtistName);
    _navMock.Verify(n => n.GoBackAsync(), Times.Once);
}

// BackCommand delegates to INavigationService
[Fact]
public async Task BackCommand_CallsGoBackAsync()
{
    var sut = CreateSut();
    await sut.BackCommand.ExecuteAsync(null);
    _navMock.Verify(n => n.GoBackAsync(), Times.Once);
}
```

---

## 10. Reference Implementations

Copy from these files as the canonical baseline:

| File | Purpose |
|------|---------|
| `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml` | Canonical single-line list picker XAML |
| `MyVocaList/UI/Pages/Artists/ArtistPickerPage.xaml.cs` | Minimal code-behind |
| `MyVocaList/UI/ViewModels/ArtistPickerViewModel.cs` | Canonical ViewModel with full loading discipline |
| `Contracts/Messages/ArtistPickedMessage.cs` | Canonical message record |
| `MyVocaList/UI/Pages/Songs/SongPickerPage.xaml` | Two-line ListItem variant |
| `MyVocaList/UI/Pages/Songs/YouTubeSearchPage.xaml` | Leading image ListItem variant |
| `MyVocaList/UI/ViewModels/ArtistFormViewModel.cs` | Canonical caller-side register-before-navigate pattern |
