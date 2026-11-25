using System.Collections.Specialized;
using MyVocaList.UI.Components.Menus;
using SfLoadMoreOption = Syncfusion.Maui.ListView.LoadMoreOption;
using SfSelectionMode = Syncfusion.Maui.ListView.SelectionMode;

namespace MyVocaList.UI.Components;

/// <summary>
/// Syncfusion copy of <see cref="CrudListView"/>: the shared structural ContentView for CRUD list pages. Hosts the
/// loading skeleton, the <c>SfListView</c> inside an <c>SfPullToRefresh</c>, the empty states, the floating toolbar,
/// the FAB, the two bottom sheets and the shared overflow <see cref="MenuSf"/>. Pages pass entity-specific DataTemplates
/// and commands via BindableProperties; shared commands are bound directly via the ICrudListViewModel BindingContext.
/// </summary>
public partial class CrudListViewSf : ContentView, IMenuHostSf
{
    private const string SkeletonAnimationName = "CrudListViewSfSkeletonPulse";
    private const uint SkeletonPulseLengthMs = 1400;
    private const double SkeletonOpacityHigh = 1.0;
    private const double SkeletonOpacityLow = 0.4;

    // -------------------------------------------------------------------------
    // BindableProperties (same names, types and defaults as CrudListView)
    // -------------------------------------------------------------------------

    /// <summary>Entity collection bound to SfListView.ItemsSource.</summary>
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(System.Collections.IList),
            typeof(CrudListViewSf), null);

    /// <summary>
    /// Selected-items collection. SfListView.SelectedItems is get-only, so it is mirrored both ways in code-behind.
    /// </summary>
    public static readonly BindableProperty SelectedItemsSourceProperty =
        BindableProperty.Create(nameof(SelectedItemsSource), typeof(System.Collections.IList),
            typeof(CrudListViewSf), null,
            propertyChanged: (b, _, n) => ((CrudListViewSf)b).OnSelectedItemsSourceChanged(n as System.Collections.IList));

    /// <summary>DataTemplate for unselected rows.</summary>
    public static readonly BindableProperty ItemTemplateProperty =
        BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate),
            typeof(CrudListViewSf), null);

    /// <summary>DataTemplate for selected rows.</summary>
    public static readonly BindableProperty SelectedItemTemplateProperty =
        BindableProperty.Create(nameof(SelectedItemTemplate), typeof(DataTemplate),
            typeof(CrudListViewSf), null);

    /// <summary>Placeholder text for the internal persistent SearchBar.</summary>
    public static readonly BindableProperty SearchPlaceholderProperty =
        BindableProperty.Create(nameof(SearchPlaceholder), typeof(string),
            typeof(CrudListViewSf), string.Empty);

    /// <summary>
    /// Search query text, propagated TwoWay to the internal SearchBar. Pages bind this to the
    /// ViewModel's SearchText so the existing debounce pipeline drives list filtering.
    /// </summary>
    public static readonly BindableProperty SearchTextProperty =
        BindableProperty.Create(nameof(SearchText), typeof(string),
            typeof(CrudListViewSf), string.Empty, BindingMode.TwoWay);

    /// <summary>
    /// liftOnScroll elevation for the internal SearchBar (SurfaceContainerLow → SurfaceContainer).
    /// Pages bind this to the ViewModel's IsScrolled.
    /// </summary>
    public static readonly BindableProperty IsSearchBarElevatedProperty =
        BindableProperty.Create(nameof(IsSearchBarElevated), typeof(bool),
            typeof(CrudListViewSf), false);

    /// <summary>Icon name for the "no items" EmptyState illustration.</summary>
    public static readonly BindableProperty EmptyNoItemsIllustrationProperty =
        BindableProperty.Create(nameof(EmptyNoItemsIllustration), typeof(string),
            typeof(CrudListViewSf), string.Empty);

    /// <summary>Headline for the "no items" EmptyState (e.g. "No venue registered").</summary>
    public static readonly BindableProperty EmptyNoItemsHeadlineProperty =
        BindableProperty.Create(nameof(EmptyNoItemsHeadline), typeof(string),
            typeof(CrudListViewSf), string.Empty);

    /// <summary>
    /// Whether the "no items" empty state is visible. Bound to the entity-specific VM bool property
    /// (e.g. IsEmptyNoVenues), whose name differs per VM.
    /// </summary>
    public static readonly BindableProperty IsEmptyNoItemsProperty =
        BindableProperty.Create(nameof(IsEmptyNoItems), typeof(bool),
            typeof(CrudListViewSf), false);

    /// <summary>Command for the FAB (e.g. AddVenueCommand).</summary>
    public static readonly BindableProperty FabCommandProperty =
        BindableProperty.Create(nameof(FabCommand), typeof(System.Windows.Input.ICommand),
            typeof(CrudListViewSf), null);

    /// <summary>SemanticProperties.Description for the FAB.</summary>
    public static readonly BindableProperty FabDescriptionProperty =
        BindableProperty.Create(nameof(FabDescription), typeof(string),
            typeof(CrudListViewSf), string.Empty);

    /// <summary>Icon name for the FAB. Defaults to "add_outlined".</summary>
    public static readonly BindableProperty FabIconProperty =
        BindableProperty.Create(nameof(FabIcon), typeof(string),
            typeof(CrudListViewSf), "add_outlined");

    /// <summary>
    /// Optional view shown above the list (e.g. a filter chip group).
    /// When set, the filter row becomes Auto height and the view is displayed.
    /// </summary>
    public static readonly BindableProperty FilterContentProperty =
        BindableProperty.Create(nameof(FilterContent), typeof(View),
            typeof(CrudListViewSf), null,
            propertyChanged: (b, _, n) => ((CrudListViewSf)b).OnFilterContentChanged(n as View));

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

    /// <summary>The selected-items collection currently observed for changes (mirrored into SfListView.SelectedItems).</summary>
    private INotifyCollectionChanged _observedSelection;

    /// <summary>Re-entrancy guard for the two-way SelectedItems mirror.</summary>
    private bool _syncingSelection;

    // -------------------------------------------------------------------------
    // IMenuHostSf
    // -------------------------------------------------------------------------

    /// <summary>The shared overflow menu this list hosts (REQ-OVFM-20).</summary>
    public MenuSf? HostedMenu => itemMenu;

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    public CrudListViewSf()
    {
        InitializeComponent();
        ((Syncfusion.Maui.Core.IParallaxView)listView).Scrolling += OnListViewScrolling;
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
            ApplyLoadMoreOption();
            ApplySkeletonPulse();
        }
    }

    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MyVocaList.UI.ViewModels.ICrudListViewModel.IsSelectionMode):
                RunOnView(ApplySelectionMode);
                break;
            case nameof(MyVocaList.UI.ViewModels.ICrudListViewModel.HasMoreItems):
                RunOnView(ApplyLoadMoreOption);
                break;
            case nameof(MyVocaList.UI.ViewModels.ICrudListViewModel.IsInitialLoading):
                RunOnView(ApplySkeletonPulse);
                break;
        }
    }

    /// <summary>Runs <paramref name="action"/> inline on the UI thread, or dispatches it there from a background thread.</summary>
    private void RunOnView(Action action)
    {
        if (Dispatcher.IsDispatchRequired)
            Dispatcher.Dispatch(action);
        else
            action();
    }

    /// <summary>
    /// Maps the ViewModel's IsSelectionMode to the SfListView selection mode, then mirrors the ViewModel selection
    /// into the list so the SelectedItemTemplate reflects the VM state after the switch.
    /// </summary>
    private void ApplySelectionMode()
    {
        if (listView == null || _viewModel == null)
            return;

        var mode = _viewModel.IsSelectionMode ? SfSelectionMode.Multiple : SfSelectionMode.None;
        if (listView.SelectionMode != mode)
        {
            // Switching to None clears the list selection; that must not write back into the ViewModel.
            _syncingSelection = true;
            try
            {
                listView.SelectionMode = mode;
            }
            finally
            {
                _syncingSelection = false;
            }
        }

        SyncListSelectionFromSource();
    }

    /// <summary>Load-more on scroll only while the ViewModel has more pages (DX: IsLoadMoreEnabled="{Binding HasMoreItems}").</summary>
    private void ApplyLoadMoreOption()
    {
        if (listView == null)
            return;

        listView.LoadMoreOption = _viewModel?.HasMoreItems == true
            ? SfLoadMoreOption.AutoOnScroll
            : SfLoadMoreOption.None;
    }

    /// <summary>Starts the skeleton opacity pulse while the initial load runs; stops it otherwise.</summary>
    private void ApplySkeletonPulse()
    {
        if (skeletonBones == null)
            return;

        this.AbortAnimation(SkeletonAnimationName);
        skeletonBones.Opacity = SkeletonOpacityHigh;

        if (_viewModel?.IsInitialLoading != true)
            return;

        var pulse = new Animation
        {
            { 0, 0.5, new Animation(v => skeletonBones.Opacity = v, SkeletonOpacityHigh, SkeletonOpacityLow) },
            { 0.5, 1, new Animation(v => skeletonBones.Opacity = v, SkeletonOpacityLow, SkeletonOpacityHigh) },
        };
        pulse.Commit(this, SkeletonAnimationName, length: SkeletonPulseLengthMs,
            finished: (_, _) => skeletonBones.Opacity = SkeletonOpacityHigh,
            repeat: () => _viewModel?.IsInitialLoading == true);
    }

    // -------------------------------------------------------------------------
    // BindableProperty change handlers
    // -------------------------------------------------------------------------

    private void OnSelectedItemsSourceChanged(System.Collections.IList newValue)
    {
        if (_observedSelection != null)
            _observedSelection.CollectionChanged -= OnSelectedItemsSourceCollectionChanged;

        _observedSelection = newValue as INotifyCollectionChanged;

        if (_observedSelection != null)
            _observedSelection.CollectionChanged += OnSelectedItemsSourceCollectionChanged;

        SyncListSelectionFromSource();
    }

    private void OnFilterContentChanged(View newView)
    {
        var hasFilter = newView != null;
        filterRow.Height = hasFilter ? GridLength.Auto : new GridLength(0);
        // Host the filter View directly: assigning ContentView.Content reparents the View into the visual tree.
        filterContentHost.Content = newView;
        filterContentHost.IsVisible = hasFilter;
    }

    // -------------------------------------------------------------------------
    // SelectedItems mirror (SfListView.SelectedItems is get-only)
    // -------------------------------------------------------------------------

    /// <summary>ViewModel selection changed (tap on a checkbox, select all, clear): mirror it into the list.</summary>
    private void OnSelectedItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (_syncingSelection)
            return;

        RunOnView(SyncListSelectionFromSource);
    }

    /// <summary>Replaces the list's SelectedItems with the ViewModel's selected items (only in selection mode).</summary>
    private void SyncListSelectionFromSource()
    {
        if (listView == null)
            return;

        _syncingSelection = true;
        try
        {
            var target = listView.SelectedItems;
            target.Clear();

            if (listView.SelectionMode == SfSelectionMode.None || SelectedItemsSource == null)
                return;

            foreach (var item in SelectedItemsSource)
                target.Add(item);
        }
        finally
        {
            _syncingSelection = false;
        }
    }

    /// <summary>
    /// The list selection changed by a user tap in selection mode: apply it to the ViewModel collection, then report
    /// the count. The count is read inside the dispatched action so a stale queued value cannot override the settled state.
    /// </summary>
    private void OnListViewSelectionChanged(object sender, Syncfusion.Maui.ListView.ItemSelectionChangedEventArgs e)
    {
        if (_syncingSelection)
            return;

        var source = SelectedItemsSource;
        if (source != null)
        {
            _syncingSelection = true;
            try
            {
                foreach (var removed in e.RemovedItems)
                    source.Remove(removed);

                foreach (var added in e.AddedItems)
                {
                    if (!source.Contains(added))
                        source.Add(added);
                }
            }
            finally
            {
                _syncingSelection = false;
            }
        }

        Dispatcher.Dispatch(() => _viewModel?.OnSelectionChanged(listView.SelectedItems.Count));
    }

    // -------------------------------------------------------------------------
    // SfListView event handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Scroll offset → IsScrolled (liftOnScroll). SfListView has no public Scrolling event: it implements
    /// <c>IParallaxView.Scrolling</c> explicitly, so the handler is attached in the constructor through that interface
    /// (a XAML <c>Scrolling=</c> attribute fails at runtime with XamlParseException).
    /// </summary>
    private void OnListViewScrolling(object sender, Syncfusion.Maui.Core.ParallaxScrollingEventArgs e)
    {
        if (_viewModel != null)
            _viewModel.IsScrolled = e.ScrollY > 0;
    }

    /// <summary>
    /// Tap on an item. Forwards to the ViewModel, which navigates in browse mode and is a no-op in selection mode
    /// (there SfListView toggles the row selection itself).
    /// </summary>
    private void OnItemTapped(object sender, Syncfusion.Maui.ListView.ItemTappedEventArgs e)
    {
        if (_viewModel?.ItemTapCommand is { } command && command.CanExecute(e.DataItem))
            command.Execute(e.DataItem);
    }

    /// <summary>
    /// Press-and-hold on an item. Forwards to the ViewModel, which starts selection in browse mode
    /// and opens the item actions sheet in selection mode.
    /// </summary>
    private void OnItemLongPress(object sender, Syncfusion.Maui.ListView.ItemLongPressEventArgs e)
    {
        if (_viewModel?.ItemLongPressCommand is { } command && command.CanExecute(e.DataItem))
            command.Execute(e.DataItem);
    }

    // -------------------------------------------------------------------------
    // BottomSheet event handler
    // -------------------------------------------------------------------------

    /// <summary>
    /// Item actions sheet closed (drag down ends in Collapsed, or the ViewModel hid it). The TwoWay State binding has
    /// already written Hidden back; DismissItemActionsCommand also clears the item-actions target, as CrudListView does
    /// on a swipe-dismiss. The command is idempotent, so a close the ViewModel started is unaffected.
    /// </summary>
    private void OnItemActionsSheetStateChanged(object sender, Syncfusion.Maui.Toolkit.BottomSheet.StateChangedEventArgs e)
    {
        var closed = e.NewState is Syncfusion.Maui.Toolkit.BottomSheet.BottomSheetState.Hidden
            or Syncfusion.Maui.Toolkit.BottomSheet.BottomSheetState.Collapsed;
        var wasOpen = e.OldState is not Syncfusion.Maui.Toolkit.BottomSheet.BottomSheetState.Hidden
            and not Syncfusion.Maui.Toolkit.BottomSheet.BottomSheetState.Collapsed;

        if (closed && wasOpen)
            _viewModel?.DismissItemActionsCommand.Execute(null);
    }
}
