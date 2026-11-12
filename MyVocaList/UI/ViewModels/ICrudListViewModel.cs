namespace MyVocaList.UI.ViewModels;

public interface ICrudListViewModel : System.ComponentModel.INotifyPropertyChanged
{
    // Navigation icon / command (context-aware: hamburger for root, back for pushed pages)
    string AppBarNavigationIcon { get; set; }
    ICommand AppBarNavigationCommand { get; set; }

    // Search / scroll state
    bool IsScrolled { get; set; }
    bool IsEmptyNoResults { get; }

    // Loading state
    bool IsInitialLoading { get; }
    bool IsRefreshing { get; set; }
    IAsyncRelayCommand RefreshCommand { get; }
    bool HasMoreItems { get; }
    IRelayCommand LoadMoreCommand { get; }

    // Selection state
    int SelectedCount { get; }
    bool IsAllSelected { get; }
    bool CanDeleteSelected { get; }
    bool IsSelectionMode { get; }

    // Gesture commands (REQ-CLSG-01..07)
    IAsyncRelayCommand<object> ItemTapCommand { get; }
    IRelayCommand<object> ItemLongPressCommand { get; }
    IRelayCommand<object> ToggleItemSelectionCommand { get; }

    // Item actions bottom sheet (REQ-CLSG-06..08)
    BottomSheetState ItemActionsSheetState { get; set; }
    IAsyncRelayCommand EditItemCommand { get; }
    IRelayCommand DeleteItemCommand { get; }
    IRelayCommand DismissItemActionsCommand { get; }

    // Toolbar commands
    IRelayCommand SelectAllCommand { get; }
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

    // Back handling (REQ-CLSG-12): true = consumed
    bool HandleBack();
}
