namespace MyVocaList.UI.Pages;

public interface ICrudListViewModel
{
    BottomSheetState ConfirmSheetState { get; set; }
    bool IsSearchMode { get; }
    bool IsScrolled { get; set; }
    IRelayCommand CloseSearchCommand { get; }
    Task InitializeAsync();
    void OnSelectionChanged(int count);
}
