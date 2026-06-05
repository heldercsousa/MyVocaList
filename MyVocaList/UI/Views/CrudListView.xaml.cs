using DevExpress.Maui.CollectionView;

namespace MyVocaList.UI.Views;

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

    /// <summary>Placeholder text for the SearchAppBar (informational — not rendered inside CrudListView).</summary>
    public static readonly BindableProperty SearchPlaceholderProperty =
        BindableProperty.Create(nameof(SearchPlaceholder), typeof(string),
            typeof(CrudListView), string.Empty);

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

    /// <summary>
    /// Optional Tap command for DXCollectionView. When non-null, wires the Tap event.
    /// Use for pages that navigate on item tap (e.g. SongsPage).
    /// </summary>
    public static readonly BindableProperty ItemTapCommandProperty =
        BindableProperty.Create(nameof(ItemTapCommand), typeof(System.Windows.Input.ICommand),
            typeof(CrudListView), null,
            propertyChanged: (b, _, n) => ((CrudListView)b).OnItemTapCommandChanged(n as System.Windows.Input.ICommand));

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

    public System.Windows.Input.ICommand ItemTapCommand
    {
        get => (System.Windows.Input.ICommand)GetValue(ItemTapCommandProperty);
        set => SetValue(ItemTapCommandProperty, value);
    }

    // -------------------------------------------------------------------------
    // ViewModel reference (cast from BindingContext when set)
    // -------------------------------------------------------------------------

    private MyVocaList.UI.Pages.ICrudListViewModel _viewModel;

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    public CrudListView()
    {
        InitializeComponent();
        BindingContextChanged += OnBindingContextChanged;
    }

    // -------------------------------------------------------------------------
    // BindingContext wiring
    // -------------------------------------------------------------------------

    private void OnBindingContextChanged(object sender, EventArgs e)
    {
        if (_viewModel != null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = BindingContext as MyVocaList.UI.Pages.ICrudListViewModel;

        if (_viewModel != null)
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MyVocaList.UI.Pages.ICrudListViewModel.ConfirmSheetState))
        {
            if (_viewModel.ConfirmSheetState == BottomSheetState.Hidden)
                confirmSheet.Close();
            else
                confirmSheet.Show(_viewModel.ConfirmSheetState, null);
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
        filterContentPresenter.IsVisible = hasFilter;
    }

    private void OnItemTapCommandChanged(System.Windows.Input.ICommand newCommand)
    {
        // Unwire then re-wire so multiple calls are safe
        collectionView.Tap -= OnCollectionViewItemTapped;
        if (newCommand != null)
            collectionView.Tap += OnCollectionViewItemTapped;
    }

    // -------------------------------------------------------------------------
    // DXCollectionView event handlers
    // -------------------------------------------------------------------------

    private void OnCollectionViewScrolled(object sender, DXCollectionViewScrolledEventArgs e)
    {
        if (_viewModel != null)
            _viewModel.IsScrolled = e.Offset > 0;
    }

    private void OnSelectionChanged(object sender, CollectionViewSelectionChangedEventArgs e)
    {
        var count = (collectionView.SelectedItems as System.Collections.ICollection)?.Count ?? 0;
        _viewModel?.OnSelectionChanged(count);
    }

    private void OnCollectionViewItemTapped(object sender, CollectionViewGestureEventArgs e)
    {
        ItemTapCommand?.Execute(e.Item);
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
}
