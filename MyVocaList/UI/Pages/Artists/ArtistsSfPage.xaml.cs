namespace MyVocaList.UI.Pages.Artists;

public partial class ArtistsSfPage : CrudListPageBase
{
    private readonly ArtistsViewModel _viewModel;

    protected override ICrudListViewModel ListViewModel => _viewModel;

    public ArtistsSfPage(ArtistsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        AttachViewModel();
    }

    private readonly List<string> _roleFilters = [];

    private void OnRoleFilterSelectionChanged(object? sender, Syncfusion.Maui.Toolkit.Chips.SelectionChangedEventArgs e)
    {
        if (e.RemovedItem?.ToString() is { } removed)
            _roleFilters.Remove(removed);
        if (e.AddedItem?.ToString() is { } added && !_roleFilters.Contains(added))
            _roleFilters.Add(added);
        _viewModel.SelectedRoleFilters = _roleFilters.ToList();
    }
}
