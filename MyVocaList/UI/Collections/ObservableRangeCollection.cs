using System.Collections.Specialized;
using System.ComponentModel;

namespace MyVocaList.UI.Collections
{
    /// <summary>
    /// Minimal ObservableRangeCollection to perform batch updates with a single Reset notification.
    /// Keeps API small but effective for reducing layout churn.
    /// </summary>
    public class ObservableRangeCollection<T> : ObservableCollection<T>
    {
        public ObservableRangeCollection() : base() { }
        public ObservableRangeCollection(IEnumerable<T> collection) : base(collection ?? Array.Empty<T>()) { }

        public void AddRange(IEnumerable<T> items)
        {
            if (items == null) return;
            CheckReentrancy();

            var startingIndex = Items.Count;
            var addedItems = new List<T>();
            foreach (var item in items)
            {
                Items.Add(item);
                addedItems.Add(item);
            }

            if (addedItems.Count == 0) return;

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

            // BUG-048: raise a single Add-action notification (not Reset) so DXCollectionView appends
            // the new items instead of fully re-rendering and resetting scroll position on page load.
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, addedItems, startingIndex));
        }

        public void ReplaceRange(IEnumerable<T> items)
        {
            CheckReentrancy();
            Items.Clear();
            if (items != null)
            {
                foreach (var item in items)
                    Items.Add(item);
            }
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void ClearRange()
        {
            if (Items.Count == 0) return;
            CheckReentrancy();
            Items.Clear();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);
        }
    }
}