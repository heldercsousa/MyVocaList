using MyVocaList.UI.Components.Menus;
using SfBottomSheetState = Syncfusion.Maui.Toolkit.BottomSheet.BottomSheetState;

namespace MyVocaList.UI.Components;

/// <summary>
/// SPIKE (task/spike-syncfusion): Syncfusion copy of <see cref="CrudListView"/>. Same BindableProperties so pages
/// can switch by changing the tag. Sheet visibility uses SfBottomSheet Show()/Close(); the DevExpress
/// <c>BottomSheetState</c> enum still arrives from the unchanged ViewModels (ICrudListViewModel) and is compared
/// as-is. The Syncfusion state enum is aliased as <see cref="SfBottomSheetState"/> to avoid the name clash.
/// Gaps vs. the original (recorded in the spike report): stock CollectionView has no SelectedItemTemplate
/// (ignored here), no long-press or tap events (handled by SfEffectsView/TapGestureRecognizer in the page templates),
/// and no IsLoadMoreEnabled (load-more is governed only by the command's CanExecute).
/// </summary>
public partial class CrudListViewSf : ContentView, IMenuHost
{
    // -------------------------------------------------------------------------
    // BindableProperties (same names and types as CrudListView)
    // -------------------------------------------------------------------------

    /// <summary>Entity collection bound to CollectionView.ItemsSource.</summary>
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(System.Collections.IList),
            typeof(CrudListViewSf), null);

    /// <summary>Selected-items collection. Mirrored from CollectionView.SelectedItems in selection mode.</summary>
    public static readonly BindableProperty SelectedItemsSourceProperty =
        BindableProperty.Create(nameof(SelectedItemsSource), typeof(System.Collections.IList),
            typeof(CrudListViewSf), null);

    /// <summary>DataTemplate for rows. Spike: this copy uses ItemTemplate only (see class remarks).</summary>
    public static readonly BindableProperty ItemTemplateProperty =
        BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate),
            typeof(CrudListViewSf), null);

    /// <summary>DataTemplate for selected rows. Accepted for API parity; not applied (no stock equivalent).</summary>
    public static readonly BindableProperty SelectedItemTemplateProperty =
        BindableProperty.Create(nameof(SelectedItemTemplate), typeof(DataTemplate),
            typeof(CrudListViewSf), null);

    public static readonly BindableProperty SearchPlaceholderProperty =
        BindableProperty.Create(nameof(SearchPlaceholder), typeof(string),
            typeof(CrudListViewSf), string.Empty);

    public static readonly BindableProperty SearchTextProperty =
        BindableProperty.Create(nameof(SearchText), typeof(string),
            typeof(CrudListViewSf), string.Empty, BindingMode.TwoWay);

    public static readonly BindableProperty IsSearchBarElevatedProperty =
        BindableProperty.Create(nameof(IsSearchBarElevated), typeof(bool),
            typeof(CrudListViewSf), false);

    public static readonly BindableProperty EmptyNoItemsIllustrationProperty =
        BindableProperty.Create(nameof(EmptyNoItemsIllustration), typeof(string),
            typeof(CrudListViewSf), string.Empty);

    public static readonly BindableProperty EmptyNoItemsHeadlineProperty =
        BindableProperty.Create(nameof(EmptyNoItemsHeadline), typeof(string),
            typeof(CrudListViewSf), string.Empty);

    public static readonly BindableProperty IsEmptyNoItemsProperty =
        BindableProperty.Create(nameof(IsEmptyNoItems), typeof(bool),
            typeof(CrudListViewSf), false);

    public static readonly BindableProperty FabCommandProperty =
        BindableProperty.Create(nameof(FabCommand), typeof(System.Windows.Input.ICommand),
            typeof(CrudListViewSf), null);

    public static readonly BindableProperty FabDescriptionProperty =
        BindableProperty.Create(nameof(FabDescription), typeof(string),
            typeof(CrudListViewSf), string.Empty);

    public static readonly BindableProperty FabIconProperty =
        BindableProperty.Create(nameof(FabIcon), typeof(string),
            typeof(CrudListViewSf), "add_outlined");

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
    // ViewModel reference
    // -------------------------------------------------------------------------

    private MyVocaList.UI.ViewModels.ICrudListViewModel _viewModel;

    private bool _showConfirmWhenActionsHidden;
    private bool _showConfirmWhenMenuClosed;

    // -------------------------------------------------------------------------
    // IMenuHost
    // -------------------------------------------------------------------------

    /// <summary>The shared overflow menu this list hosts (DX Menu, kept as original).</summary>
    public Menu? HostedMenu => itemMenu;

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    public CrudListViewSf()
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
                _showConfirmWhenMenuClosed = false;
                confirmSheet.Close();
            }
            else if (itemActionsSheet.State != SfBottomSheetState.Hidden)
            {
                _showConfirmWhenActionsHidden = true;
            }
            else if (itemMenu.IsOpen)
            {
                _showConfirmWhenMenuClosed = true;
            }
            else
            {
                confirmSheet.Show();
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
                itemActionsSheet.Show();
        }
    }

    /// <summary>
    /// Maps IsSelectionMode to CollectionView.SelectionMode. Entering selection mode turns on Multiple selection;
    /// leaving it clears the selection (stock CollectionView keeps SelectedItems otherwise).
    /// </summary>
    private void ApplySelectionMode()
    {
        if (collectionView == null || _viewModel == null)
            return;

        if (_viewModel.IsSelectionMode)
        {
            collectionView.SelectionMode = Microsoft.Maui.Controls.SelectionMode.Multiple;
        }
        else
        {
            collectionView.SelectionMode = Microsoft.Maui.Controls.SelectionMode.None;
            collectionView.SelectedItems.Clear();
        }
    }

    // -------------------------------------------------------------------------
    // BindableProperty change handlers
    // -------------------------------------------------------------------------

    private void OnFilterContentChanged(View newView)
    {
        var hasFilter = newView != null;
        filterRow.Height = hasFilter ? GridLength.Auto : new GridLength(0);
        filterContentHost.Content = newView;
        filterContentHost.IsVisible = hasFilter;
    }

    // -------------------------------------------------------------------------
    // CollectionView event handlers
    // -------------------------------------------------------------------------

    private void OnCollectionViewScrolled(object sender, ItemsViewScrolledEventArgs e)
    {
        if (_viewModel != null)
            _viewModel.IsScrolled = e.VerticalOffset > 0;
    }

    /// <summary>
    /// Selection changed. Mirrors the selection into SelectedItemsSource (the ViewModel reads it) and reports
    /// the count after the dispatch, as the original does.
    /// </summary>
    private void OnSelectionChanged(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {
        if (SelectedItemsSource != null)
        {
            SelectedItemsSource.Clear();
            foreach (var item in collectionView.SelectedItems)
                SelectedItemsSource.Add(item);
        }

        Dispatcher.Dispatch(() => _viewModel?.OnSelectionChanged(collectionView.SelectedItems.Count));
    }

    // -------------------------------------------------------------------------
    // Sheet event handlers
    // -------------------------------------------------------------------------

    private void OnConfirmSheetStateChanged(object sender, Syncfusion.Maui.Toolkit.BottomSheet.StateChangedEventArgs e)
    {
        if (e.NewState == SfBottomSheetState.Hidden &&
            _viewModel != null &&
            _viewModel.ConfirmSheetState != BottomSheetState.Hidden)
        {
            _viewModel.ConfirmSheetState = BottomSheetState.Hidden;
        }
    }

    private void OnItemActionsSheetStateChanged(object sender, Syncfusion.Maui.Toolkit.BottomSheet.StateChangedEventArgs e)
    {
        if (e.NewState != SfBottomSheetState.Hidden)
            return;

        if (_viewModel != null && _viewModel.ItemActionsSheetState != BottomSheetState.Hidden)
            _viewModel.DismissItemActionsCommand.Execute(null);

        if (_showConfirmWhenActionsHidden)
        {
            _showConfirmWhenActionsHidden = false;
            if (_viewModel != null && _viewModel.ConfirmSheetState != BottomSheetState.Hidden)
                confirmSheet.Show();
        }
    }

    private void OnItemMenuClosed(object sender, EventArgs e)
    {
        if (!_showConfirmWhenMenuClosed)
            return;

        _showConfirmWhenMenuClosed = false;
        if (_viewModel != null && _viewModel.ConfirmSheetState != BottomSheetState.Hidden)
            confirmSheet.Show();
    }
}
