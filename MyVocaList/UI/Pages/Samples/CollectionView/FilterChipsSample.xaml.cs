using DevExpress.Maui.Editors;

namespace MyVocaList.UI.Pages.Samples.CollectionView;

public partial class FilterChipsSample : ContentPage
{
    private FilterChipsSampleViewModel ViewModel => (FilterChipsSampleViewModel)BindingContext;

    public FilterChipsSample()
    {
        InitializeComponent();
    }

    private void OnGenreSelectionChanged(object? sender, EventArgs e)
    {
        if (sender is FilterChipGroup chipGroup && chipGroup.SelectedItems.Count > 0)
        {
            var selectedGenres = chipGroup.SelectedItems.OfType<GenreFilter>().Select(g => g.Name).ToList();
            if (selectedGenres.Any())
            {
                ViewModel.ApplyGenreFilter(selectedGenres.First());
            }
        }
        else
        {
            ViewModel.ApplyGenreFilter("All");
        }
    }
}