namespace MyVocaList.UI.Pages.Base;

public abstract class CrudListPageBase : ContentPage
{
    // PHASE2-INSTRUMENTATION: remove after page-load-frozen is closed.
    private readonly long _ctorTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();

    protected abstract ICrudListViewModel ListViewModel { get; }

    // PHASE2-INSTRUMENTATION: remove after page-load-frozen is closed.
    protected CrudListPageBase()
    {
        Loaded += (_, _) =>
        {
            var ms = ElapsedMs(_ctorTimestamp);
            Serilog.Log.ForContext("SourceContext", GetType().Name)
                .Information("[PageLoad] {Page} loaded={Ms}ms (ctor→Loaded)", GetType().Name, ms);
        };
    }

    // PHASE2-INSTRUMENTATION: remove after page-load-frozen is closed.
    private static long ElapsedMs(long startTimestamp)
        => (long)((System.Diagnostics.Stopwatch.GetTimestamp() - startTimestamp)
            * 1000.0 / System.Diagnostics.Stopwatch.Frequency);

    // The leading icon (hamburger, or a back arrow on a pushed sub-page) is drawn in Shell's native
    // navigation slot — the one form pages use for their back arrow — not by SmallAppBar. With a
    // Command set, Shell shows IconOverride there at any stack depth; hardware back still reaches
    // OnBackButtonPressed. Kept in code: a XAML binding on BackButtonBehavior did not refresh.
    private readonly BackButtonBehavior _navigationBehavior = new();
    private readonly Command _openDrawerCommand = new(() => Shell.Current.FlyoutIsPresented = true);

    protected void AttachViewModel()
    {
        ListViewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Before the first frame, or a pushed page flashes Shell's default back arrow.
        ApplyHamburgerNavigation();
        SyncNavigationBehavior();
        Shell.SetBackButtonBehavior(this, _navigationBehavior);
    }

    /// <summary>Gives the ViewModel the hamburger icon and the open-drawer command.</summary>
    private void ApplyHamburgerNavigation()
    {
        // CRUD list pages are top-level menu destinations (see hamburger-nav-pattern/design.md
        // § Classification principle) → the hamburger. A list shown as a pushed sub-page overrides
        // both properties after base.OnNavigatedTo (see SongsViewModel.ApplyAppBarNavigation).
        ListViewModel.AppBarNavigationIcon = "menu_outlined";
        ListViewModel.AppBarNavigationCommand = _openDrawerCommand;
    }

    /// <summary>Copies the ViewModel's navigation icon and command to the Shell-native navigation slot.</summary>
    private void SyncNavigationBehavior()
    {
        _navigationBehavior.IconOverride = string.IsNullOrEmpty(ListViewModel.AppBarNavigationIcon)
            ? null
            : ImageSource.FromFile(ListViewModel.AppBarNavigationIcon);
        _navigationBehavior.Command = ListViewModel.AppBarNavigationCommand;
    }

    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ICrudListViewModel.AppBarNavigationIcon)
            or nameof(ICrudListViewModel.AppBarNavigationCommand))
            SyncNavigationBehavior();
    }

    protected override void OnAppearing()
    {
        // PHASE2-INSTRUMENTATION: remove after page-load-frozen is closed.
        var appearingMs = ElapsedMs(_ctorTimestamp);
        Serilog.Log.ForContext("SourceContext", GetType().Name)
            .Information("[PageLoad] {Page} appearing={Ms}ms (ctor→OnAppearing)", GetType().Name, appearingMs);

        base.OnAppearing();
        _ = ListViewModel.InitializeAsync();

        // PHASE2-INSTRUMENTATION: remove after page-load-frozen is closed.
        var afterInitMs = ElapsedMs(_ctorTimestamp);
        Serilog.Log.ForContext("SourceContext", GetType().Name)
            .Information("[PageLoad] {Page} afterInitDispatch={Ms}ms (ctor→after InitializeAsync fire)", GetType().Name, afterInitMs);
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        // PHASE2-INSTRUMENTATION: remove after page-load-frozen is closed.
        var navigatedMs = ElapsedMs(_ctorTimestamp);
        Serilog.Log.ForContext("SourceContext", GetType().Name)
            .Information("[PageLoad] {Page} navigatedTo={Ms}ms (ctor→OnNavigatedTo)", GetType().Name, navigatedMs);

        ApplyHamburgerNavigation();
    }

    protected override bool OnBackButtonPressed()
        => ListViewModel.HandleBack() || base.OnBackButtonPressed();
}
