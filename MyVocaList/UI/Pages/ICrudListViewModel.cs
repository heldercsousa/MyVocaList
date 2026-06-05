namespace MyVocaList.UI.Pages;

public interface ICrudListViewModel : System.ComponentModel.INotifyPropertyChanged
{
    BottomSheetState ConfirmSheetState { get; set; }
    bool IsSearchMode { get; }
    bool IsScrolled { get; set; }
    bool IsEmptyNoResults { get; }
    IRelayCommand CloseSearchCommand { get; }
    Task InitializeAsync();
    void OnSelectionChanged(int count);
}
