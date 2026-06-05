using System.Collections;
using System.ComponentModel;
using MyVocaList.UI.Pages;

namespace MyVocaList.UI.Views;

/// <summary>
/// Shared ContentView that encapsulates the standard CRUD list body:
/// shimmer skeleton, DXCollectionView, empty states, FloatingToolbar + FAB,
/// and confirm-delete BottomSheet.
/// The BindingContext flows from the host page (the ViewModel).
/// Per-entity configuration is supplied via BindableProperties.
/// </summary>
public partial class CrudListView : ContentView
{
    // === BindableProperties ===

    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(CrudListView));

    public static readonly BindableProperty SelectedItemsSourceProperty =
        BindableProperty.Create(nameof(SelectedItemsSource), typeof(IList), typeof(CrudListView),
            propertyChanged: OnSelectedItemsSourceChanged);

    public static readonly BindableProperty IsEmptyNoItemsProperty =
        BindableProperty.Create(nameof(IsEmptyNoItems), typeof(bool), typeof(CrudListView), false);

    public static readonly BindableProperty SearchPlaceholderProperty =
        BindableProperty.Create(nameof(SearchPlaceholder), typeof(string), typeof(CrudListView), string.Empty);

    public static readonly BindableProperty EmptyNoItemsIllustrationProperty =
        BindableProperty.Create(nameof(EmptyNoItemsIllustration), typeof(string), typeof(CrudListView), string.Empty);

    public static readonly BindableProperty EmptyNoItemsHeadlineProperty =
        BindableProperty.Create(nameof(EmptyNoItemsHeadline), typeof(string), typeof(CrudListView), string.Empty);

    public static readonly BindableProperty FabCommandProperty =
        BindableProperty.Create(nameof(FabCommand), typeof(System.Windows.Input.ICommand), typeof(CrudListView));

    public static readonly BindableProperty FabDescriptionProperty =
        BindableProperty.Create(nameof(FabDescription), typeof(string), typeof(CrudListView), string.Empty);

    public static readonly BindableProperty ItemTemplateProperty =
        BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(CrudListView));

    public static readonly BindableProperty SelectedItemTemplateProperty =
        BindableProperty.Create(nameof(SelectedItemTemplate), typeof(DataTemplate), typeof(CrudListView));

    // === CLR wrappers ===

    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public IList SelectedItemsSource
    {
        get => (IList)GetValue(SelectedItemsSourceProperty);
        set => SetValue(SelectedItemsSourceProperty, value);
    }

    public bool IsEmptyNoItems
    {
        get => (bool)GetValue(IsEmptyNoItemsProperty);
        set => SetValue(IsEmptyNoItemsProperty, value);
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

    // === Constructor ===

    public CrudListView()
    {
        InitializeComponent();
    }

    // === BindingContext change — subscribe to ViewModel PropertyChanged ===

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext is ICrudListViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            // Wire up SelectedItems immediately if already set
            if (SelectedItemsSource != null)
                collectionView.SelectedItems = SelectedItemsSource;
        }
    }

    private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ICrudListViewModel.ConfirmSheetState) &&
            sender is ICrudListViewModel vm)
        {
            if (vm.ConfirmSheetState == BottomSheetState.Hidden)
                confirmSheet.Close();
            else
                confirmSheet.Show(vm.ConfirmSheetState, Application.Current?.Windows[0]?.Page);
        }
    }

    // === SelectedItemsSource change → wire DXCollectionView ===

    private static void OnSelectedItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (CrudListView)bindable;
        if (newValue is IList list)
            view.collectionView.SelectedItems = list;
    }

    // === Event handlers (delegate to ViewModel via BindingContext) ===

    private void OnCollectionViewScrolled(object sender, DXCollectionViewScrolledEventArgs e)
    {
        if (BindingContext is ICrudListViewModel vm)
            vm.IsScrolled = e.Offset > 0;
    }

    private void OnSelectionChanged(object sender, CollectionViewSelectionChangedEventArgs e)
    {
        var count = ((sender as DXCollectionView)?.SelectedItems as System.Collections.ICollection)?.Count ?? 0;
        if (BindingContext is ICrudListViewModel vm)
            vm.OnSelectionChanged(count);
    }

    private void OnConfirmSheetStateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        if (e.NewValue == BottomSheetState.Hidden &&
            BindingContext is ICrudListViewModel vm &&
            vm.ConfirmSheetState != BottomSheetState.Hidden)
        {
            vm.ConfirmSheetState = BottomSheetState.Hidden;
        }
    }
}
