namespace MyVocaList.UI.ViewModels;

public interface ICrudListViewModel : System.ComponentModel.INotifyPropertyChanged
{
    // Navigation icon / command (context-aware: hamburger for root, back for pushed pages)
    string AppBarNavigationIcon { get; set; }
    ICommand AppBarNavigationCommand { get; set; }

    // Search / scroll state
    bool IsSearchMode { get; }
    bool IsScrolled { get; set; }
    bool IsEmptyNoResults { get; }
    IRelayCommand CloseSearchCommand { get; }

    // Loading state
    bool IsInitialLoading { get; }
    bool IsRefreshing { get; set; }
    IAsyncRelayCommand RefreshCommand { get; }
    bool HasMoreItems { get; }
    IRelayCommand LoadMoreCommand { get; }

    // Selection state
    int SelectedCount { get; }
    bool IsAllSelected { get; }
    bool CanEditSelected { get; }
    bool CanDeleteSelected { get; }

    // Toolbar commands
    IRelayCommand SelectAllCommand { get; }
    IAsyncRelayCommand EditSelectedCommand { get; }
    IRelayCommand DeleteSelectedCommand { get; }

    // Confirm bottom sheet
    BottomSheetState ConfirmSheetState { get; set; }
    string ConfirmMessage { get; }
    string ConfirmActionText { get; }
    IAsyncRelayCommand ConfirmActionCommand { get; }
    IRelayCommand DismissConfirmCommand { get; }

    // Lifecycle
    Task InitializeAsync();
    void OnSelectionChanged(int count);
}
