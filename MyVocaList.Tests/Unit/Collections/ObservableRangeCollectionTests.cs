using System.Collections.Specialized;
using MyVocaList.UI.Collections;

namespace MyVocaList.Tests.Unit.Collections;

/// <summary>
/// Regression tests for BUG-048: <see cref="ObservableRangeCollection{T}.AddRange"/> must raise
/// a single <see cref="NotifyCollectionChangedAction.Add"/> event (not Reset) so DXCollectionView
/// appends new items instead of fully re-rendering and resetting scroll position on "load more".
/// </summary>
public class ObservableRangeCollectionTests
{
    [Fact]
    // [AC] BUG-048: AddRange on a populated collection raises an Add action, not Reset.
    public void AddRange_OnPopulatedCollection_RaisesAddAction_NotReset()
    {
        var sut = new ObservableRangeCollection<int>(new[] { 1, 2, 3 });
        NotifyCollectionChangedEventArgs? capturedArgs = null;
        sut.CollectionChanged += (_, e) => capturedArgs = e;

        sut.AddRange(new[] { 4, 5 });

        Assert.NotNull(capturedArgs);
        Assert.Equal(NotifyCollectionChangedAction.Add, capturedArgs!.Action);
    }

    [Fact]
    // [AC] BUG-048: the Add event carries the correct starting index (Count before insertion).
    public void AddRange_OnPopulatedCollection_UsesCorrectStartingIndex()
    {
        var sut = new ObservableRangeCollection<int>(new[] { 1, 2, 3 });
        NotifyCollectionChangedEventArgs? capturedArgs = null;
        sut.CollectionChanged += (_, e) => capturedArgs = e;

        sut.AddRange(new[] { 4, 5 });

        Assert.Equal(3, capturedArgs!.NewStartingIndex);
    }

    [Fact]
    // [AC] BUG-048: the Add event carries all newly added items (single event for the whole batch).
    public void AddRange_OnPopulatedCollection_NewItemsMatchAddedBatch()
    {
        var sut = new ObservableRangeCollection<int>(new[] { 1, 2, 3 });
        NotifyCollectionChangedEventArgs? capturedArgs = null;
        sut.CollectionChanged += (_, e) => capturedArgs = e;

        sut.AddRange(new[] { 4, 5 });

        Assert.NotNull(capturedArgs!.NewItems);
        Assert.Equal(new[] { 4, 5 }, capturedArgs.NewItems!.Cast<int>());
    }

    [Fact]
    // [AC] BUG-048: only a single CollectionChanged event fires for the whole batch.
    public void AddRange_RaisesExactlyOneCollectionChangedEvent()
    {
        var sut = new ObservableRangeCollection<int>(new[] { 1, 2, 3 });
        var eventCount = 0;
        sut.CollectionChanged += (_, _) => eventCount++;

        sut.AddRange(new[] { 4, 5 });

        Assert.Equal(1, eventCount);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, sut);
    }

    [Fact]
    // Existing behavior preserved: null items is a safe no-op.
    public void AddRange_NullItems_NoOp()
    {
        var sut = new ObservableRangeCollection<int>(new[] { 1, 2, 3 });
        var eventFired = false;
        sut.CollectionChanged += (_, _) => eventFired = true;

        sut.AddRange(null!);

        Assert.False(eventFired);
        Assert.Equal(new[] { 1, 2, 3 }, sut);
    }

    [Fact]
    // Existing behavior preserved: empty items is a safe no-op (no event, since nothing changed).
    public void AddRange_EmptyItems_NoOp()
    {
        var sut = new ObservableRangeCollection<int>(new[] { 1, 2, 3 });
        var eventFired = false;
        sut.CollectionChanged += (_, _) => eventFired = true;

        sut.AddRange(Array.Empty<int>());

        Assert.False(eventFired);
        Assert.Equal(new[] { 1, 2, 3 }, sut);
    }
}
