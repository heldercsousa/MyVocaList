namespace MyVocaList.UI.Components;

/// <summary>
/// Shared structural ContentView for CRUD list pages. Hosts the ShimmerView, DXCollectionView,
/// FloatingToolbar, FAB, empty states, and confirm BottomSheet. Pages pass entity-specific
/// DataTemplates and commands via BindableProperties; shared commands are bound directly
/// via the ICrudListViewModel BindingContext.
/// </summary>
public partial class CrudListView : ContentView
{
    // -------------------------------------------------------------------------
    // BindableProperties
    // -------------------------------------------------------------------------

    /// <summary>Entity collection bound to DXCollectionView.ItemsSource.</summary>
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(System.Collections.IList),
            typeof(CrudListView), null);

    /// <summary>Selected-items collection. Assigned to DXCollectionView.SelectedItems in code-behind.</summary>
    public static readonly BindableProperty SelectedItemsSourceProperty =
        BindableProperty.Create(nameof(SelectedItemsSource), typeof(System.Collections.IList),
            typeof(CrudListView), null,
            propertyChanged: (b, _, n) => ((CrudListView)b).OnSelectedItemsSourceChanged(n as System.Collections.IList));

    /// <summary>DataTemplate for unselected rows.</summary>
    public static readonly BindableProperty ItemTemplateProperty =
        BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate),
            typeof(CrudListView), null);

    /// <summary>DataTemplate for selected rows.</summary>
    public static readonly BindableProperty SelectedItemTemplateProperty =
        BindableProperty.Create(nameof(SelectedItemTemplate), typeof(DataTemplate),
            typeof(CrudListView), null);

    /// <summary>Placeholder text for the internal persistent SearchBar (live — rendered in the search bar row).</summary>
    public static readonly BindableProperty SearchPlaceholderProperty =
        BindableProperty.Create(nameof(SearchPlaceholder), typeof(string),
            typeof(CrudListView), string.Empty);

    /// <summary>
    /// Search query text, propagated TwoWay to the internal SearchBar. Pages bind this to the
    /// ViewModel's SearchText so the existing debounce pipeline drives list filtering.
    /// </summary>
    public static readonly BindableProperty SearchTextProperty =
        BindableProperty.Create(nameof(SearchText), typeof(string),
            typeof(CrudListView), string.Empty, BindingMode.TwoWay);

    /// <summary>
    /// liftOnScroll elevation for the internal SearchBar (SurfaceContainerLow → SurfaceContainer).
    /// Pages bind this to the ViewModel's IsScrolled.
    /// </summary>
    public static readonly BindableProperty IsSearchBarElevatedProperty =
        BindableProperty.Create(nameof(IsSearchBarElevated), typeof(bool),
            typeof(CrudListView), false);

    /// <summary>Icon name for the "no items" EmptyState illustration.</summary>
    public static readonly BindableProperty EmptyNoItemsIllustrationProperty =
        BindableProperty.Create(nameof(EmptyNoItemsIllustration), typeof(string),
            typeof(CrudListView), string.Empty);

    /// <summary>Headline for the "no items" EmptyState (e.g. "No venue registered").</summary>
    public static readonly BindableProperty EmptyNoItemsHeadlineProperty =
        BindableProperty.Create(nameof(EmptyNoItemsHeadline), typeof(string),
            typeof(CrudListView), string.Empty);

    /// <summary>
    /// Whether the "no items" empty state is visible. Bound to entity-specific VM bool property
    /// (e.g. IsEmptyNoVenues) whose name differs per VM — therefore a BindableProperty, not a
    /// direct BindingContext binding.
    /// </summary>
    public static readonly BindableProperty IsEmptyNoItemsProperty =
        BindableProperty.Create(nameof(IsEmptyNoItems), typeof(bool),
            typeof(CrudListView), false);

    /// <summary>Command for the FAB (e.g. AddVenueCommand).</summary>
    public static readonly BindableProperty FabCommandProperty =
        BindableProperty.Create(nameof(FabCommand), typeof(System.Windows.Input.ICommand),
            typeof(CrudListView), null);

    /// <summary>SemanticProperties.Description for the FAB.</summary>
    public static readonly BindableProperty FabDescriptionProperty =
        BindableProperty.Create(nameof(FabDescription), typeof(string),
            typeof(CrudListView), string.Empty);

    /// <summary>Icon name for the FAB. Defaults to "add_outlined".</summary>
    public static readonly BindableProperty FabIconProperty =
        BindableProperty.Create(nameof(FabIcon), typeof(string),
            typeof(CrudListView), "add_outlined");

    /// <summary>
    /// Optional view shown above the list (e.g. FilterChipGroup for ArtistsPage).
    /// When set, the filter row (Row 0) becomes Auto height and the view is displayed.
    /// </summary>
    public static readonly BindableProperty FilterContentProperty =
        BindableProperty.Create(nameof(FilterContent), typeof(View),
            typeof(CrudListView), null,
            propertyChanged: (b, _, n) => ((CrudListView)b).OnFilterContentChanged(n as View));

    // -------------------------------------------------------------------------
    // CLR wrappers
    // -------------------------------------------------------------------------

    public System.Collections.IList ItemsSource
    {
        get => (System.Collections.IList)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public System.Collections.IList SelectedItemsSource
    {
        get => (System.Collections.IList)GetValue(SelectedItemsSourceProperty);
        set => SetValue(SelectedItemsSourceProperty, value);
    }

    public DataTemplate ItemTemplate
    {
        get => (DataTemplate)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public DataTemplate SelectedItemTemplate
    {
        get => (DataTemplate)GetValue(SelectedItemTemplateProperty);
        set => SetValue(SelectedItemTemplateProperty, value);
    }

    public string SearchPlaceholder
    {
        get => (string)GetValue(SearchPlaceholderProperty);
        set => SetValue(SearchPlaceholderProperty, value);
    }

    public string SearchText
    {
        get => (string)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public bool IsSearchBarElevated
    {
        get => (bool)GetValue(IsSearchBarElevatedProperty);
        set => SetValue(IsSearchBarElevatedProperty, value);
    }

    public string EmptyNoItemsIllustration
    {
        get => (string)GetValue(EmptyNoItemsIllustrationProperty);
        set => SetValue(EmptyNoItemsIllustrationProperty, value);
    }

    public string EmptyNoItemsHeadline
    {
        get => (string)GetValue(EmptyNoItemsHeadlineProperty);
        set => SetValue(EmptyNoItemsHeadlineProperty, value);
    }

    public bool IsEmptyNoItems
    {
        get => (bool)GetValue(IsEmptyNoItemsProperty);
        set => SetValue(IsEmptyNoItemsProperty, value);
    }

    public System.Windows.Input.ICommand FabCommand
    {
        get => (System.Windows.Input.ICommand)GetValue(FabCommandProperty);
        set => SetValue(FabCommandProperty, value);
    }

    public string FabDescription
    {
        get => (string)GetValue(FabDescriptionProperty);
        set => SetValue(FabDescriptionProperty, value);
    }

    public string FabIcon
    {
        get => (string)GetValue(FabIconProperty);
        set => SetValue(FabIconProperty, value);
    }

    public View FilterContent
    {
        get => (View)GetValue(FilterContentProperty);
        set => SetValue(FilterContentProperty, value);
    }

    // -------------------------------------------------------------------------
    // ViewModel reference (cast from BindingContext when set)
    // -------------------------------------------------------------------------

    private MyVocaList.UI.ViewModels.ICrudListViewModel _viewModel;

    /// <summary>
    /// Set when the confirm sheet must open after the item actions sheet has hidden (sheet hand-off).
    /// </summary>
    private bool _showConfirmWhenActionsHidden;

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    public CrudListView()
    {
        // PHASE2-INSTRUMENTATION: remove after page-load-frozen is closed.
        var sw = System.Diagnostics.Stopwatch.StartNew();
        InitializeComponent();
        sw.Stop();
        Serilog.Log.ForContext("SourceContext", nameof(CrudListView))
            .Information("[PageLoad] CrudListView InitializeComponent={Ms}ms", sw.ElapsedMilliseconds);

        Loaded += (_, _) =>
            Serilog.Log.ForContext("SourceContext", nameof(CrudListView))
                .Information("[PageLoad] CrudListView loaded={Ms}ms (ctor→Loaded)", sw.ElapsedMilliseconds);
        // PHASE2-INSTRUMENTATION end

        BindingContextChanged += OnBindingContextChanged;
    }

    // -------------------------------------------------------------------------
    // BindingContext wiring
    // -------------------------------------------------------------------------

    private void OnBindingContextChanged(object sender, EventArgs e)
    {
        if (_viewModel != null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = BindingContext as MyVocaList.UI.ViewModels.ICrudListViewModel;

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            ApplySelectionMode();
        }
    }

    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MyVocaList.UI.ViewModels.ICrudListViewModel.ConfirmSheetState))
        {
            if (_viewModel.ConfirmSheetState == BottomSheetState.Hidden)
            {
                _showConfirmWhenActionsHidden = false;
                confirmSheet.Close();
            }
            else if (itemActionsSheet.State != BottomSheetState.Hidden)
            {
                // Only one sheet at a time: the confirm sheet opens once the item actions sheet has hidden.
                _showConfirmWhenActionsHidden = true;
            }
            else
            {
                confirmSheet.Show(_viewModel.ConfirmSheetState, null);
            }
        }
        else if (e.PropertyName == nameof(MyVocaList.UI.ViewModels.ICrudListViewModel.IsSelectionMode))
        {
            ApplySelectionMode();
        }
        else if (e.PropertyName == nameof(MyVocaList.UI.ViewModels.ICrudListViewModel.ItemActionsSheetState))
        {
            if (_viewModel.ItemActionsSheetState == BottomSheetState.Hidden)
                itemActionsSheet.Close();
            else
                itemActionsSheet.Show(_viewModel.ItemActionsSheetState, null);
        }
    }

    /// <summary>
    /// Maps the ViewModel's IsSelectionMode to the DXCollectionView selection mode (design I4).
    /// When entering selection mode, the view's SelectedItems is reassigned to the ViewModel collection
    /// so DX reflects the VM state after the switch.
    /// </summary>
    private void ApplySelectionMode()
    {
        if (collectionView == null || _viewModel == null)
            return;

        if (_viewModel.IsSelectionMode)
        {
            collectionView.SelectionMode = Microsoft.Maui.Controls.SelectionMode.Multiple;
            collectionView.SelectedItems = SelectedItemsSource;
        }
        else
        {
            collectionView.SelectionMode = Microsoft.Maui.Controls.SelectionMode.None;
        }
    }

    // -------------------------------------------------------------------------
    // BindableProperty change handlers
    // -------------------------------------------------------------------------

    private void OnSelectedItemsSourceChanged(System.Collections.IList newValue)
    {
        if (collectionView != null)
            collectionView.SelectedItems = newValue;
    }

    private void OnFilterContentChanged(View newView)
    {
        var hasFilter = newView != null;
        filterRow.Height = hasFilter ? GridLength.Auto : new GridLength(0);
        // Host the filter View directly. A bare ContentPresenter only presents content
        // when it lives inside a ControlTemplate (it resolves Content from the templated
        // parent); placed directly in a layout it renders nothing regardless of its
        // Content binding — which is why the filter chips never appeared after migration.
        // Assigning ContentView.Content reparents the View into the visual tree correctly.
        filterContentHost.Content = newView;
        filterContentHost.IsVisible = hasFilter;
    }

    // -------------------------------------------------------------------------
    // DXCollectionView event handlers
    // -------------------------------------------------------------------------

    private void OnCollectionViewScrolled(object sender, DXCollectionViewScrolledEventArgs e)
    {
        if (_viewModel != null)
            _viewModel.IsScrolled = e.Offset > 0;
    }

    /// <summary>
    /// DX selection changed. The count is read inside the dispatched action, not before it, so a stale
    /// queued value (for example the 0 raised by the None to Multiple switch) cannot override the settled state.
    /// </summary>
    private void OnSelectionChanged(object sender, CollectionViewSelectionChangedEventArgs e)
    {
        Dispatcher.Dispatch(() => _viewModel?.OnSelectionChanged(CurrentSelectedCount()));
    }

    private int CurrentSelectedCount() =>
        (collectionView.SelectedItems as System.Collections.ICollection)?.Count ?? 0;

    /// <summary>
    /// Tap on an item. Forwards to the ViewModel, which navigates in browse mode and is a no-op in selection mode.
    /// </summary>
    private void OnItemTap(object sender, CollectionViewGestureEventArgs e)
    {
        if (_viewModel?.ItemTapCommand is { } command && command.CanExecute(e.Item))
            command.Execute(e.Item);
    }

    /// <summary>
    /// Press-and-hold on an item. Forwards to the ViewModel, which starts selection in browse mode
    /// and opens the item actions sheet in selection mode.
    /// </summary>
    private void OnItemLongPress(object sender, CollectionViewGestureEventArgs e)
    {
        if (_viewModel?.ItemLongPressCommand is { } command && command.CanExecute(e.Item))
            command.Execute(e.Item);
    }

    // -------------------------------------------------------------------------
    // BottomSheet event handler
    // -------------------------------------------------------------------------

    private void OnConfirmSheetStateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        // Sheet closed by gesture (swipe-down) — sync back to ViewModel
        if (e.NewValue == BottomSheetState.Hidden &&
            _viewModel != null &&
            _viewModel.ConfirmSheetState != BottomSheetState.Hidden)
        {
            _viewModel.ConfirmSheetState = BottomSheetState.Hidden;
        }
    }

    /// <summary>
    /// Item actions sheet state changed. A swipe-dismiss is synced back to the ViewModel, and a pending
    /// confirm sheet is shown once the item actions sheet has hidden.
    /// </summary>
    private void OnItemActionsSheetStateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        if (e.NewValue != BottomSheetState.Hidden)
            return;

        // Sheet closed by gesture (swipe-down) — sync back to ViewModel
        if (_viewModel != null && _viewModel.ItemActionsSheetState != BottomSheetState.Hidden)
            _viewModel.DismissItemActionsCommand.Execute(null);

        if (_showConfirmWhenActionsHidden)
        {
            _showConfirmWhenActionsHidden = false;
            if (_viewModel != null && _viewModel.ConfirmSheetState != BottomSheetState.Hidden)
                confirmSheet.Show(_viewModel.ConfirmSheetState, null);
        }
    }
}
