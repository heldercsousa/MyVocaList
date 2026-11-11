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

    protected void AttachViewModel()
        => ListViewModel.PropertyChanged += OnViewModelPropertyChanged;

    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
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

        if (ListViewModel is ICrudListViewModel vm)
        {
            // CRUD list pages are exclusively top-level menu destinations (see
            // hamburger-nav-pattern/design.md § Classification principle) → always the hamburger.
            // If a future feature ever pushes a CRUD list as a non-top-level sub-page, replace this
            // with an explicit "is my route in the top-level menu set?" check (design.md § Assumption).
            vm.AppBarNavigationIcon = "menu_outlined";
            vm.AppBarNavigationCommand = new Command(
                () => Shell.Current.FlyoutIsPresented = true);
        }
    }

    protected override bool OnBackButtonPressed()
        => ListViewModel.HandleBack() || base.OnBackButtonPressed();
}
