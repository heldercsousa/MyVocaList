using DevExpress.Maui.Controls;
using MyVocaList.UI.Collections;
using System.Collections.Concurrent;

namespace MyVocaList.Tests.Unit.ViewModels;

public class CrudListViewModelBaseTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    // Regression: page-load-frozen — fetch must not run on the UI SynchronizationContext
    public async Task InitializeAsync_WithSynchronizationContext_ExecutesFetchOffContext()
    {
        var original = SynchronizationContext.Current;
        using var uiContext = new PumpSynchronizationContext();
        SynchronizationContext contextDuringFetch = null;
        var contextCaptured = false;
        var sut = new TestCrudListViewModel(
            onFetchPage: () => { contextDuringFetch = SynchronizationContext.Current; contextCaptured = true; });

        try
        {
            SynchronizationContext.SetSynchronizationContext(uiContext);
            var initTask = sut.InitializeAsync();
            SynchronizationContext.SetSynchronizationContext(original);

            var completed = await Task.WhenAny(initTask, Task.Delay(TestTimeout));
            Assert.Same(initTask, completed); // guard: a regression must fail fast, not hang
            await initTask;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }

        Assert.True(contextCaptured);
        Assert.NotSame(uiContext, contextDuringFetch);
    }

    [Fact]
    // Regression: page-load-frozen — fetch must not run on the UI SynchronizationContext
    public async Task LoadMoreCommand_WithSynchronizationContext_ExecutesFetchOffContext()
    {
        var original = SynchronizationContext.Current;
        using var uiContext = new PumpSynchronizationContext();
        SynchronizationContext contextDuringFetch = null;
        var fetchInvoked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sut = new TestCrudListViewModel(
            onFetchMore: () =>
            {
                contextDuringFetch = SynchronizationContext.Current;
                fetchInvoked.TrySetResult();
            });

        try
        {
            SynchronizationContext.SetSynchronizationContext(uiContext);
            sut.LoadMoreCommand.Execute(null);
            SynchronizationContext.SetSynchronizationContext(original);

            var completed = await Task.WhenAny(fetchInvoked.Task, Task.Delay(TestTimeout));
            Assert.Same(fetchInvoked.Task, completed); // guard: a regression must fail fast, not hang

            // LoadMoreCommand is fire-and-forget — wait for the load to finish
            // (items land via RunOnUiThread) before tearing down the pump.
            var deadline = DateTime.UtcNow + TestTimeout;
            while (sut.ItemCount == 0 && DateTime.UtcNow < deadline)
                await Task.Delay(10);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }

        Assert.NotSame(uiContext, contextDuringFetch);
    }

    [Fact]
    // [AC] REQ-DLH-05: a later visit (second InitializeAsync) shows the shimmer until its rows apply.
    // Replaces InitializeAsync_SecondCall_DoesNotToggleIsInitialLoading (old "shimmer only on first load", replaced by REQ-DLH-05).
    public async Task InitializeAsync_SecondCall_TogglesIsInitialLoading()
    {
        var sut = new TestCrudListViewModel();

        // First call — establishes the loaded state
        await sut.InitializeAsync();
        Assert.False(sut.IsInitialLoading);

        // Capture PropertyChanged events for IsInitialLoading raised by the second call
        var isInitialLoadingChanges = new List<bool>();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ICrudListViewModel.IsInitialLoading))
                isInitialLoadingChanges.Add(sut.IsInitialLoading);
        };

        // Second call — simulates Shell-cached page revisit (OnAppearing fires again)
        await sut.InitializeAsync();

        // The revisit shows the shimmer (true) and ends it when the rows apply (false)
        Assert.Equal(new[] { true, false }, isInitialLoadingChanges);
        Assert.False(sut.IsInitialLoading);
    }

    [Fact]
    // [AC] REQ-SEARCHBAR-14: SearchText change debounces into a fetch carrying the trimmed query
    public async Task SearchText_Changed_DebouncesIntoFetchWithTrimmedQuery()
    {
        var sut = new TestCrudListViewModel();
        await sut.InitializeAsync();
        sut.ResetFetchCapture();

        sut.SearchText = "  abc  ";

        var deadline = DateTime.UtcNow + TestTimeout;
        while (sut.LastFetchQuery == null && DateTime.UtcNow < deadline)
            await Task.Delay(20);

        Assert.Equal("abc", sut.LastFetchQuery);
    }

    [Fact]
    // [AC] REQ-SEARCHBAR-14: rapid SearchText edits cancel the pending debounce — only the final query is fetched
    public async Task SearchText_RapidChanges_OnlyFetchesFinalQuery()
    {
        var sut = new TestCrudListViewModel();
        await sut.InitializeAsync();
        sut.ResetFetchCapture();

        sut.SearchText = "a";
        sut.SearchText = "ab";
        sut.SearchText = "abc";

        var deadline = DateTime.UtcNow + TestTimeout;
        while (sut.LastFetchQuery == null && DateTime.UtcNow < deadline)
            await Task.Delay(20);

        Assert.Equal("abc", sut.LastFetchQuery);
        Assert.Equal(1, sut.FetchPageCallCount);
    }

    // ── Selection gestures (CLSG) ─────────────────────────────────────────

    [Fact]
    // [AC] REQ-CLSG-01: When the user taps a list item in browse mode, the system shall navigate to that item's edit form and shall not select it.
    public async Task TapItem_InBrowseMode_NavigatesToEditAndDoesNotSelect()
    {
        var sut = new TestCrudListViewModel();

        await sut.ItemTapCommand.ExecuteAsync("item-1");

        Assert.Equal(["item-1"], sut.EditNavigations);
        Assert.Empty(sut.SelectedItemsList);
        Assert.False(sut.IsSelectionMode);
    }

    [Fact]
    // [AC] REQ-CLSG-01b: When the VM receives a tap in selection mode, the system shall not navigate.
    public async Task TapItem_InSelectionMode_DoesNotNavigate()
    {
        var sut = new TestCrudListViewModel();
        sut.ItemLongPressCommand.Execute("item-1");

        await sut.ItemTapCommand.ExecuteAsync("item-2");

        Assert.Empty(sut.EditNavigations);
        Assert.True(sut.IsSelectionMode);
    }

    [Fact]
    // [AC] REQ-CLSG-02: When the user long-presses a list item in browse mode, the system shall select exactly that item and enter selection mode (IsSelectionMode, SelectedCount == 1, the item in SelectedItems).
    public void LongPressItem_InBrowseMode_SelectsExactlyThatItemAndEntersSelectionMode()
    {
        var sut = new TestCrudListViewModel();

        sut.ItemLongPressCommand.Execute("item-1");

        Assert.True(sut.IsSelectionMode);
        Assert.Equal(1, sut.SelectedCount);
        Assert.Equal(["item-1"], sut.SelectedItemsList);
    }

    [Fact]
    // [AC] REQ-CLSG-04a: When the selected count becomes 0 through a selection change from the view (OnSelectionChanged(0)), the system shall leave selection mode.
    public void OnSelectionChangedToZero_LeavesSelectionMode()
    {
        var sut = new TestCrudListViewModel();
        sut.ItemLongPressCommand.Execute("item-1");

        sut.OnSelectionChanged(0);

        Assert.False(sut.IsSelectionMode);
    }

    [Fact]
    // [AC] REQ-CLSG-04b: When the Select all toggle clears the selection, the system shall leave selection mode.
    public async Task SelectAllToggle_WhenAllSelected_ClearsSelectionAndLeavesSelectionMode()
    {
        var sut = new TestCrudListViewModel();
        await sut.InitializeAsync();
        sut.SelectAllCommand.Execute(null);
        Assert.True(sut.IsAllSelected); // sanity: select-all selected the single loaded item

        sut.SelectAllCommand.Execute(null);

        Assert.Empty(sut.SelectedItemsList);
        Assert.False(sut.IsSelectionMode);
    }

    [Fact]
    // [AC] REQ-CLSG-04c: When a confirmed delete completes, the system shall clear the selection and leave selection mode.
    public async Task ConfirmedDeleteOfSelection_ClearsSelectionAndLeavesSelectionMode()
    {
        var sut = new TestCrudListViewModel();
        sut.ItemLongPressCommand.Execute("item-1");
        sut.ToggleItemSelectionCommand.Execute("item-2");

        sut.DeleteSelectedCommand.Execute(null);
        await sut.ConfirmActionCommand.ExecuteAsync(null);

        Assert.Equal([["item-1", "item-2"]], sut.DeletedBatches);
        Assert.Empty(sut.SelectedItemsList);
        Assert.False(sut.IsSelectionMode);
    }

    [Fact]
    // [AC] REQ-CLSG-04d: When the list reloads (refresh, search) in selection mode, the system shall clear the selection and leave selection mode.
    public async Task Reload_InSelectionMode_ClearsSelectionAndLeavesSelectionMode()
    {
        var sut = new TestCrudListViewModel();
        sut.ItemLongPressCommand.Execute("item-1");

        await sut.RefreshCommand.ExecuteAsync(null);

        Assert.Empty(sut.SelectedItemsList);
        Assert.False(sut.IsSelectionMode);
    }

    [Fact]
    // [AC] REQ-CLSG-05a: When the user taps an unselected item's trailing selection control in browse mode, the system shall select it and enter selection mode, without navigating.
    public void ToggleItemSelection_InBrowseMode_SelectsItemWithoutNavigating()
    {
        var sut = new TestCrudListViewModel();

        sut.ToggleItemSelectionCommand.Execute("item-1");

        Assert.True(sut.IsSelectionMode);
        Assert.Equal(["item-1"], sut.SelectedItemsList);
        Assert.Empty(sut.EditNavigations);
    }

    [Fact]
    // [AC] REQ-CLSG-05b: When the user taps an item's trailing selection control in selection mode, the system shall toggle that item; deselecting the last item leaves selection mode.
    public void ToggleItemSelection_InSelectionMode_TogglesItemAndLeavesModeWhenEmpty()
    {
        var sut = new TestCrudListViewModel();
        sut.ItemLongPressCommand.Execute("item-1");

        sut.ToggleItemSelectionCommand.Execute("item-2");
        Assert.Equal(["item-1", "item-2"], sut.SelectedItemsList);

        sut.ToggleItemSelectionCommand.Execute("item-1");
        Assert.Equal(["item-2"], sut.SelectedItemsList);
        Assert.True(sut.IsSelectionMode);

        sut.ToggleItemSelectionCommand.Execute("item-2");
        Assert.Empty(sut.SelectedItemsList);
        Assert.False(sut.IsSelectionMode);
    }

    [Fact]
    // [AC] REQ-CLSG-06: When the user long-presses a list item in selection mode, the system shall open the item actions sheet for that item (ItemActionsTarget == item, ItemActionsSheetState == HalfExpanded) and leave the selection unchanged.
    public void LongPressItem_InSelectionMode_OpensItemActionsAndKeepsSelection()
    {
        var sut = new TestCrudListViewModel();
        sut.ItemLongPressCommand.Execute("item-1");

        sut.ItemLongPressCommand.Execute("item-2");

        Assert.Equal("item-2", sut.ItemActionsTargetValue);
        Assert.Equal(BottomSheetState.HalfExpanded, sut.ItemActionsSheetState);
        Assert.Equal(["item-1"], sut.SelectedItemsList);
    }

    [Fact]
    // [AC] REQ-CLSG-07: When the user chooses Edit in the item actions sheet, the system shall hide the sheet, clear the target and navigate to the target item's edit form.
    public async Task EditItemAction_HidesSheetClearsTargetAndNavigatesToTarget()
    {
        var sut = new TestCrudListViewModel();
        sut.ItemLongPressCommand.Execute("item-1");
        sut.ItemLongPressCommand.Execute("item-2");

        await sut.EditItemCommand.ExecuteAsync(null);

        Assert.Equal(BottomSheetState.Hidden, sut.ItemActionsSheetState);
        Assert.Null(sut.ItemActionsTargetValue);
        Assert.Equal(["item-2"], sut.EditNavigations);
    }

    [Fact]
    // [AC] REQ-CLSG-08: When the user chooses Delete in the item actions sheet, the system shall hide the sheet and open the delete confirmation for exactly that item. Confirming shall call ExecuteDeleteAsync with only that item.
    public async Task DeleteItemAction_HidesSheetOpensConfirmForExactlyThatItem()
    {
        var sut = new TestCrudListViewModel();
        sut.ItemLongPressCommand.Execute("item-1");
        sut.ItemLongPressCommand.Execute("item-2");

        sut.DeleteItemCommand.Execute(null);

        Assert.Equal(BottomSheetState.Hidden, sut.ItemActionsSheetState);
        Assert.Equal(BottomSheetState.HalfExpanded, sut.ConfirmSheetState);

        await sut.ConfirmActionCommand.ExecuteAsync(null);

        Assert.Equal([["item-2"]], sut.DeletedBatches);
    }

    [Fact]
    // [AC] REQ-CLSG-12: When the user presses back, the system shall close the item actions sheet, else the confirm sheet, else clear the selection in selection mode, else return false. HandleBack() returns whether back was consumed.
    public void HandleBack_ConsumesLayersInOrderThenReturnsFalse()
    {
        var sut = new TestCrudListViewModel();
        sut.ItemLongPressCommand.Execute("item-1");
        sut.ItemLongPressCommand.Execute("item-2");       // opens item actions
        sut.DeleteSelectedCommand.Execute(null);          // opens confirm sheet

        Assert.True(sut.HandleBack());                    // 1: item actions
        Assert.Equal(BottomSheetState.Hidden, sut.ItemActionsSheetState);
        Assert.Equal(BottomSheetState.HalfExpanded, sut.ConfirmSheetState);
        Assert.True(sut.IsSelectionMode);

        Assert.True(sut.HandleBack());                    // 2: confirm sheet
        Assert.Equal(BottomSheetState.Hidden, sut.ConfirmSheetState);
        Assert.True(sut.IsSelectionMode);

        Assert.True(sut.HandleBack());                    // 3: selection
        Assert.Empty(sut.SelectedItemsList);
        Assert.False(sut.IsSelectionMode);

        Assert.False(sut.HandleBack());                   // 4: nothing left
    }

    [Fact]
    // [AC] REQ-CLSG-01: non-TItem parameter to ItemTapCommand is a no-op.
    public async Task TapItem_NonTItemParameter_IsNoOp()
    {
        var sut = new TestCrudListViewModel();

        await sut.ItemTapCommand.ExecuteAsync(42);

        Assert.Empty(sut.EditNavigations);
        Assert.False(sut.IsSelectionMode);
    }

    [Fact]
    // [AC] REQ-CLSG-02: non-TItem parameter to ItemLongPressCommand is a no-op.
    public void LongPressItem_NonTItemParameter_IsNoOp()
    {
        var sut = new TestCrudListViewModel();

        sut.ItemLongPressCommand.Execute(42);

        Assert.False(sut.IsSelectionMode);
        Assert.Empty(sut.SelectedItemsList);
        Assert.Equal(BottomSheetState.Hidden, sut.ItemActionsSheetState);
    }

    [Fact]
    // [AC] REQ-CLSG-05a: non-TItem parameter to ToggleItemSelectionCommand is a no-op.
    public void ToggleItemSelection_NonTItemParameter_IsNoOp()
    {
        var sut = new TestCrudListViewModel();

        sut.ToggleItemSelectionCommand.Execute(42);

        Assert.False(sut.IsSelectionMode);
        Assert.Empty(sut.SelectedItemsList);
    }

    // ── Item menu (MD3 overflow, REQ-OVFM) ────────────────────────────────

    [Fact]
    // [AC] REQ-OVFM-01: In browse mode, OpenItemMenuCommand with a list item sets ItemActionsTarget to that item and IsItemMenuOpen to true. The selection is unchanged and stays empty.
    public void OpenItemMenu_InBrowseMode_SetsTargetAndOpensMenu()
    {
        var sut = new TestCrudListViewModel();

        sut.OpenItemMenuCommand.Execute("item-1");

        Assert.True(sut.IsItemMenuOpen);
        Assert.Equal("item-1", sut.ItemActionsTargetValue);
        Assert.Empty(sut.SelectedItemsList);
        Assert.False(sut.IsSelectionMode);
    }

    [Fact]
    // [AC] REQ-OVFM-02a: OpenItemMenuCommand does nothing in selection mode.
    public void OpenItemMenu_InSelectionMode_IsNoOp()
    {
        var sut = new TestCrudListViewModel();
        sut.ItemLongPressCommand.Execute("item-1");

        sut.OpenItemMenuCommand.Execute("item-2");

        Assert.False(sut.IsItemMenuOpen);
    }

    [Fact]
    // [AC] REQ-OVFM-02b: OpenItemMenuCommand does nothing when the parameter is not a list item.
    public void OpenItemMenu_NonTItemParameter_IsNoOp()
    {
        var sut = new TestCrudListViewModel();

        sut.OpenItemMenuCommand.Execute(42);

        Assert.False(sut.IsItemMenuOpen);
    }

    [Fact]
    // [AC] REQ-OVFM-03: With the menu open, EditItemCommand closes the menu (IsItemMenuOpen false, ItemActionsTarget cleared) and navigates to the edit form of the target item.
    public async Task EditItem_FromMenu_ClosesMenuAndNavigatesToTarget()
    {
        var sut = new TestCrudListViewModel();
        sut.OpenItemMenuCommand.Execute("item-1");

        await sut.EditItemCommand.ExecuteAsync(null);

        Assert.False(sut.IsItemMenuOpen);
        Assert.Null(sut.ItemActionsTargetValue);
        Assert.Equal(["item-1"], sut.EditNavigations);
    }

    [Fact]
    // [AC] REQ-OVFM-04: With the menu open, DeleteItemCommand closes the menu and opens the delete confirmation for exactly the target item.
    public async Task DeleteItem_FromMenu_ClosesMenuAndOpensConfirmForTarget()
    {
        var sut = new TestCrudListViewModel();
        sut.OpenItemMenuCommand.Execute("item-2");

        sut.DeleteItemCommand.Execute(null);

        Assert.False(sut.IsItemMenuOpen);
        Assert.Equal(BottomSheetState.HalfExpanded, sut.ConfirmSheetState);

        await sut.ConfirmActionCommand.ExecuteAsync(null);

        Assert.Equal([["item-2"]], sut.DeletedBatches);
    }

    [Fact]
    // [AC] REQ-OVFM-05a: DismissItemMenuCommand closes the menu and clears ItemActionsTarget without navigating or deleting.
    public void DismissItemMenu_ClosesMenuWithoutSideEffects()
    {
        var sut = new TestCrudListViewModel();
        sut.OpenItemMenuCommand.Execute("item-1");

        sut.DismissItemMenuCommand.Execute(null);

        Assert.False(sut.IsItemMenuOpen);
        Assert.Null(sut.ItemActionsTargetValue);
        Assert.Empty(sut.EditNavigations);
        Assert.Equal(BottomSheetState.Hidden, sut.ConfirmSheetState);
        Assert.Empty(sut.DeletedBatches);
    }

    [Fact]
    // [AC] REQ-OVFM-05b: Setting IsItemMenuOpen = false directly clears ItemActionsTarget without navigating or deleting.
    public async Task SetIsItemMenuOpenFalse_ClearsTarget()
    {
        var sut = new TestCrudListViewModel();
        sut.OpenItemMenuCommand.Execute("item-1");

        sut.IsItemMenuOpen = false;
        await sut.EditItemCommand.ExecuteAsync(null);

        Assert.Null(sut.ItemActionsTargetValue);
        Assert.Empty(sut.EditNavigations);
    }

    [Fact]
    // [AC] REQ-OVFM-05b: Closing an already-closed menu is a no-op: no exception and no IsItemMenuOpen PropertyChanged.
    public void SetIsItemMenuOpenFalse_WhenAlreadyClosed_IsNoOp()
    {
        var sut = new TestCrudListViewModel();
        var changed = new List<string>();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName ?? string.Empty);

        var ex = Record.Exception(() => sut.IsItemMenuOpen = false);

        Assert.Null(ex);
        Assert.DoesNotContain("IsItemMenuOpen", changed);
    }

    [Fact]
    // [AC] REQ-OVFM-06: With the menu open, HandleBack() closes the menu and returns true. It does not leave selection mode and does not close any other sheet in the same call.
    public void HandleBack_WithMenuOpen_ClosesMenuAndReturnsTrue()
    {
        var sut = new TestCrudListViewModel();
        sut.OpenItemMenuCommand.Execute("item-1");

        Assert.True(sut.HandleBack());

        Assert.False(sut.IsItemMenuOpen);
        Assert.Equal(BottomSheetState.Hidden, sut.ConfirmSheetState);
        Assert.False(sut.IsSelectionMode);
    }

    [Fact]
    // [AC] REQ-OVFM-07: ItemMenuActions is empty by default.
    public void ItemMenuActions_ByDefault_IsEmpty()
    {
        var sut = new TestCrudListViewModel();

        Assert.Empty(sut.ItemMenuActions);
    }

    [Fact]
    // [AC] REQ-OVFM-07: A derived ViewModel can add page-specific menu items, and they keep the order it gives them.
    public void ItemMenuActions_DerivedVm_KeepsDeclaredOrder()
    {
        var sut = new ActionsTestCrudListViewModel();

        Assert.Equal(["First", "Second"], sut.ItemMenuActions.Select(a => a.Text));
    }

    [Fact]
    // [AC] REQ-OVFM-18: ItemMenuItems of a plain ViewModel is Edit, then Delete, with their icons and commands.
    public void ItemMenuItems_Default_IsEditThenDelete()
    {
        var sut = new TestCrudListViewModel();

        Assert.Equal(["Edit", "Delete"], sut.ItemMenuItems.Select(m => m.Text));
        Assert.Equal(["edit_outlined", "delete_outlined"], sut.ItemMenuItems.Select(m => m.Icon));
        Assert.Same(sut.EditItemCommand, sut.ItemMenuItems[0].Command);
        Assert.Same(sut.DeleteItemCommand, sut.ItemMenuItems[1].Command);
    }

    [Fact]
    // [AC] REQ-OVFM-18: ItemMenuItems puts the page-specific actions between Edit and Delete, in their given order.
    public void ItemMenuItems_WithPageActions_EditActionsDelete()
    {
        var sut = new ActionsTestCrudListViewModel();

        Assert.Equal(["Edit", "First", "Second", "Delete"], sut.ItemMenuItems.Select(m => m.Text));
    }

    [Fact]
    // [AC] REQ-OVFM-18: A page-specific ItemMenuItems entry runs ExecuteItemMenuActionCommand with its ItemMenuAction as the parameter.
    public void ItemMenuItems_PageAction_CommandIsExecuteWithActionParameter()
    {
        var sut = new ActionsTestCrudListViewModel();

        var pageItem = sut.ItemMenuItems[1];

        Assert.Same(sut.ExecuteItemMenuActionCommand, pageItem.Command);
        Assert.Same(sut.ItemMenuActions[0], pageItem.CommandParameter);
    }

    [Fact]
    // [AC] REQ-OVFM-18: ItemMenuItems is built once, so two reads return the same list instance.
    public void ItemMenuItems_IsBuiltOnce()
    {
        var sut = new ActionsTestCrudListViewModel();

        var first = sut.ItemMenuItems;
        var second = sut.ItemMenuItems;

        Assert.Same(first, second);
    }

    [Fact]
    // [AC] REQ-OVFM-08: With the menu open, ExecuteItemMenuActionCommand with a page-specific menu item closes the menu, then runs that item's action once, passing the target item.
    public void ExecuteItemMenuAction_ClosesMenuThenRunsActionOnceWithTarget()
    {
        var sut = new ActionsTestCrudListViewModel();
        sut.OpenItemMenuCommand.Execute("item-1");

        sut.ExecuteItemMenuActionCommand.Execute(sut.ItemMenuActions[0]);

        Assert.Equal([new ActionCall("First", "item-1", false)], sut.ActionCalls);
        Assert.False(sut.IsItemMenuOpen);
    }

    [Fact]
    // [AC] REQ-OVFM-08: ExecuteItemMenuActionCommand does not run a page action when no target is set (menu never opened).
    public void ExecuteItemMenuAction_WithNullTarget_DoesNotRunAction()
    {
        var sut = new ActionsTestCrudListViewModel();

        sut.ExecuteItemMenuActionCommand.Execute(sut.ItemMenuActions[0]);

        Assert.Empty(sut.ActionCalls);
    }

    [Fact]
    // [AC] REQ-OVFM-01: A second OpenItemMenuCommand with the same item keeps the menu open with the same target.
    public void OpenItemMenu_Twice_KeepsSameTargetAndMenuOpen()
    {
        var sut = new TestCrudListViewModel();

        sut.OpenItemMenuCommand.Execute("item-1");
        sut.OpenItemMenuCommand.Execute("item-1");

        Assert.True(sut.IsItemMenuOpen);
        Assert.Equal("item-1", sut.ItemActionsTargetValue);
    }

    [Fact]
    // [AC] REQ-OVFM-15: With the menu open, ItemTapCommand only closes the menu. It does not navigate.
    public async Task TapItem_WhileMenuOpen_ClosesMenuAndDoesNotNavigate()
    {
        var sut = new TestCrudListViewModel();
        sut.OpenItemMenuCommand.Execute("item-1");

        await sut.ItemTapCommand.ExecuteAsync("item-2");

        Assert.False(sut.IsItemMenuOpen);
        Assert.Empty(sut.EditNavigations);
    }

    [Fact]
    // [AC] REQ-OVFM-16: With the menu open, ItemLongPressCommand only closes the menu. It does not start selection.
    public void LongPressItem_WhileMenuOpen_ClosesMenuAndDoesNotSelect()
    {
        var sut = new TestCrudListViewModel();
        sut.OpenItemMenuCommand.Execute("item-1");

        sut.ItemLongPressCommand.Execute("item-2");

        Assert.False(sut.IsItemMenuOpen);
        Assert.False(sut.IsSelectionMode);
        Assert.Empty(sut.SelectedItemsList);
    }

    // ── Test double ───────────────────────────────────────────────────────

    private sealed class TestCrudListViewModel : CrudListViewModelBase<string>
    {
        private readonly Action _onFetchPage;
        private readonly Action _onFetchMore;
        private volatile string _lastFetchQuery;
        private int _fetchPageCallCount;

        public TestCrudListViewModel(Action onFetchPage = null, Action onFetchMore = null)
            : base(Mock.Of<ILogger>())
        {
            _onFetchPage = onFetchPage;
            _onFetchMore = onFetchMore;
        }

        public int ItemCount => Items.Count;
        public string LastFetchQuery => _lastFetchQuery;
        public int FetchPageCallCount => _fetchPageCallCount;
        public IList<string> SelectedItemsList => SelectedItems.ToList();
        public string ItemActionsTargetValue => ItemActionsTarget;
        public List<string> EditNavigations { get; } = [];
        public List<List<string>> DeletedBatches { get; } = [];

        public void ResetFetchCapture()
        {
            _lastFetchQuery = null;
            Interlocked.Exchange(ref _fetchPageCallCount, 0);
        }

        protected override ObservableRangeCollection<string> Items { get; } = [];
        protected override ObservableRangeCollection<string> SelectedItems { get; } = [];

        protected override Task<(IEnumerable<string> items, int totalCount)> FetchPageAsync(
            int page, int pageSize, string query, CancellationToken ct)
        {
            Interlocked.Increment(ref _fetchPageCallCount);
            _lastFetchQuery = query;
            _onFetchPage?.Invoke();
            return Task.FromResult<(IEnumerable<string>, int)>((["item-1"], 1));
        }

        protected override Task<(IEnumerable<string> items, int totalCount)> FetchMoreAsync(
            int page, int pageSize, string query, CancellationToken ct = default)
        {
            _onFetchMore?.Invoke();
            return Task.FromResult<(IEnumerable<string>, int)>((["item-more"], 2));
        }

        protected override Task ExecuteDeleteAsync(IEnumerable<string> items)
        {
            DeletedBatches.Add(items.ToList());
            return Task.CompletedTask;
        }

        protected override string BuildDeleteConfirmMessage(IList<string> items) => string.Empty;
        protected override Task NavigateToAddAsync() => Task.CompletedTask;

        protected override Task NavigateToEditAsync(string item)
        {
            EditNavigations.Add(item);
            return Task.CompletedTask;
        }

        protected override void RaiseEntityEmptyStateProperties() { }
    }

    private sealed record ActionCall(string Name, string Target, bool MenuOpen);

    private sealed class ActionsTestCrudListViewModel : CrudListViewModelBase<string>
    {
        public ActionsTestCrudListViewModel()
            : base(Mock.Of<ILogger>())
        {
        }

        public List<ActionCall> ActionCalls { get; } = [];

        protected override ObservableRangeCollection<string> Items { get; } = [];
        protected override ObservableRangeCollection<string> SelectedItems { get; } = [];

        protected override IReadOnlyList<ItemMenuAction> BuildItemMenuActions() =>
        [
            MenuAction("First", "first_icon", item => Record("First", item)),
            MenuAction("Second", "second_icon", item => Record("Second", item)),
        ];

        private Task Record(string name, string item)
        {
            ActionCalls.Add(new ActionCall(name, item, IsItemMenuOpen));
            return Task.CompletedTask;
        }

        protected override Task<(IEnumerable<string> items, int totalCount)> FetchPageAsync(
            int page, int pageSize, string query, CancellationToken ct)
            => Task.FromResult<(IEnumerable<string>, int)>(([], 0));

        protected override Task<(IEnumerable<string> items, int totalCount)> FetchMoreAsync(
            int page, int pageSize, string query, CancellationToken ct = default)
            => Task.FromResult<(IEnumerable<string>, int)>(([], 0));

        protected override Task ExecuteDeleteAsync(IEnumerable<string> items) => Task.CompletedTask;
        protected override string BuildDeleteConfirmMessage(IList<string> items) => string.Empty;
        protected override Task NavigateToAddAsync() => Task.CompletedTask;
        protected override Task NavigateToEditAsync(string item) => Task.CompletedTask;
        protected override void RaiseEntityEmptyStateProperties() { }
    }

    // ── Single-threaded pump simulating the UI SynchronizationContext ─────
    // Posted continuations execute on a dedicated thread that has this
    // context installed, so `await Task.Yield()` continuations actually run
    // (a non-pumping fake would hang the test).

    private sealed class PumpSynchronizationContext : SynchronizationContext, IDisposable
    {
        private readonly BlockingCollection<(SendOrPostCallback callback, object state)> _queue = new();
        private readonly Thread _pumpThread;

        public PumpSynchronizationContext()
        {
            _pumpThread = new Thread(Pump) { IsBackground = true, Name = "FakeUiThread" };
            _pumpThread.Start();
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            try { _queue.Add((d, state)); }
            catch (InvalidOperationException)
            {
                // Pump torn down (test already failed its timeout guard) — run the
                // continuation on the thread pool so SUT finally blocks still execute
                // (a swallowed post would strand the SUT's finally blocks and hang
                // every later test).
                ThreadPool.QueueUserWorkItem(_ => d(state));
            }
        }

        private void Pump()
        {
            SetSynchronizationContext(this);
            foreach (var (callback, state) in _queue.GetConsumingEnumerable())
                callback(state);
        }

        public void Dispose() => _queue.CompleteAdding();
    }
}
