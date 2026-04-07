using System.Collections.Specialized;

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
            bool added = false;
            foreach (var item in items)
            {
                Items.Add(item);
                added = true;
            }
            if (added)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
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