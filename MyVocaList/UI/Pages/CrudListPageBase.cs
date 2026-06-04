namespace MyVocaList.UI.Pages;

public abstract class CrudListPageBase : ContentPage
{
    protected abstract ICrudListViewModel ListViewModel { get; }

    protected event EventHandler<BottomSheetState> ConfirmSheetStateRequired;
    protected event EventHandler SelectionItemsWireUpRequired;

    protected void AttachViewModel()
        => ((System.ComponentModel.INotifyPropertyChanged)ListViewModel).PropertyChanged += OnViewModelPropertyChanged;

    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ICrudListViewModel.ConfirmSheetState))
            ConfirmSheetStateRequired?.Invoke(this, ListViewModel.ConfirmSheetState);
    }

    protected void OnConfirmSheetStateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        if (e.NewValue == BottomSheetState.Hidden &&
            ListViewModel.ConfirmSheetState != BottomSheetState.Hidden)
            ListViewModel.ConfirmSheetState = BottomSheetState.Hidden;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SelectionItemsWireUpRequired?.Invoke(this, EventArgs.Empty);
        _ = ListViewModel.InitializeAsync();
    }

    protected void OnCollectionViewScrolled(object sender, DXCollectionViewScrolledEventArgs e)
        => ListViewModel.IsScrolled = e.Offset > 0;

    protected void OnSelectionChanged(object sender, CollectionViewSelectionChangedEventArgs e)
    {
        var count = (sender as DXCollectionView)?.SelectedItems?.Cast<object>().Count() ?? 0;
        ListViewModel.OnSelectionChanged(count);
    }

    protected override bool OnBackButtonPressed()
    {
        if (ListViewModel.ConfirmSheetState != BottomSheetState.Hidden)
        {
            ListViewModel.ConfirmSheetState = BottomSheetState.Hidden;
            return true;
        }
        if (ListViewModel.IsSearchMode)
        {
            ListViewModel.CloseSearchCommand.Execute(null);
            return true;
        }
        return base.OnBackButtonPressed();
    }
}
