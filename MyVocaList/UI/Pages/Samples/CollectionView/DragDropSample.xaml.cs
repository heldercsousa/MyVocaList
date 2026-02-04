using DevExpress.Maui.CollectionView;

namespace MyVocaList.UI.Pages.Samples.CollectionView;

public partial class DragDropSample : ContentPage
{
    private DragDropSampleViewModel ViewModel => (DragDropSampleViewModel)BindingContext;

    public DragDropSample()
    {
        InitializeComponent();
    }

    private void OnDragItem(object? sender, DragItemEventArgs e)
    {
        // Visual feedback during drag
        if (e.DragItem is DragDropQueueItem draggedItem)
        {
            Console.WriteLine($"Dragging: {draggedItem.SongTitle}");
        }
    }

    private void OnDropItem(object? sender, DropItemEventArgs e)
    {
        if (e.DragItem is DragDropQueueItem draggedItem && e.DropItem is DragDropQueueItem dropTarget)
        {
            Console.WriteLine($"Dropping: {draggedItem.SongTitle} at {dropTarget.SongTitle}");
        }
    }

    private void OnCompleteItemDragDrop(object? sender, CompleteItemDragDropEventArgs e)
    {
        // Update item positions after drag completes
        ViewModel.UpdatePositions();

        if (e.Item is DragDropQueueItem movedItem)
        {
            Console.WriteLine($"Moved: {movedItem.SongTitle}");
        }
    }
}