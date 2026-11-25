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
    SheetState ItemActionsSheetState { get; set; }
    IAsyncRelayCommand EditItemCommand { get; }
    IRelayCommand DeleteItemCommand { get; }
    IRelayCommand DismissItemActionsCommand { get; }

    // Item overflow menu (browse mode, REQ-OVFM-01..08)
    bool IsItemMenuOpen { get; set; }                     // TwoWay with the item menu IsOpen
    IReadOnlyList<ItemMenuAction> ItemMenuActions { get; } // page-specific items, between Edit and Delete
    IReadOnlyList<MenuItemModel> ItemMenuItems { get; }   // Edit, page-specific items, Delete; built once (REQ-OVFM-18)
    ICommand OpenItemMenuCommand { get; }                 // parameter: the list item
    ICommand DismissItemMenuCommand { get; }
    ICommand ExecuteItemMenuActionCommand { get; }        // parameter: ItemMenuAction

    // Toolbar commands
    IRelayCommand SelectAllCommand { get; }
    IRelayCommand DeleteSelectedCommand { get; }

    // Confirm bottom sheet
    SheetState ConfirmSheetState { get; set; }
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
