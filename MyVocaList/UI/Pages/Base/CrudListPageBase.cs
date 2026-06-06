namespace MyVocaList.UI.Pages.Base;

public abstract class CrudListPageBase : ContentPage
{
    protected abstract ICrudListViewModel ListViewModel { get; }

    [Obsolete("Replaced by CrudListView internal wiring. Will be deleted in Step 7e after all pages migrate.")]
    protected event EventHandler<BottomSheetState> ConfirmSheetStateRequired;

    [Obsolete("Replaced by CrudListView internal wiring. Will be deleted in Step 7e after all pages migrate.")]
    protected event EventHandler SelectionItemsWireUpRequired;

    protected void AttachViewModel()
        => ListViewModel.PropertyChanged += OnViewModelPropertyChanged;

#pragma warning disable CS0618 // Raising obsolete events intentionally during Step 7b-7e migration period
    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ICrudListViewModel.ConfirmSheetState))
            ConfirmSheetStateRequired?.Invoke(this, ListViewModel.ConfirmSheetState);
    }
#pragma warning restore CS0618

    protected void OnConfirmSheetStateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        if (e.NewValue == BottomSheetState.Hidden &&
            ListViewModel.ConfirmSheetState != BottomSheetState.Hidden)
            ListViewModel.ConfirmSheetState = BottomSheetState.Hidden;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
#pragma warning disable CS0618 // Raising obsolete event intentionally during Step 7b-7e migration period
        SelectionItemsWireUpRequired?.Invoke(this, EventArgs.Empty);
#pragma warning restore CS0618
        _ = ListViewModel.InitializeAsync();
    }

    protected void OnCollectionViewScrolled(object sender, DXCollectionViewScrolledEventArgs e)
        => ListViewModel.IsScrolled = e.Offset > 0;

    protected void OnSelectionChanged(object sender, CollectionViewSelectionChangedEventArgs e)
    {
        var count = ((sender as DXCollectionView)?.SelectedItems as System.Collections.ICollection)?.Count ?? 0;
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
