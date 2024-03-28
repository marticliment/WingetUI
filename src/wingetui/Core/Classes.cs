using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace UniGetUI.Core
{
    public class SortableObservableCollection<T> : ObservableCollection<T>
    {
        public Func<T, object> SortingSelector { get; set; }
        public bool Descending { get; set; }
        public bool BlockSorting { get; set; } = false;
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!BlockSorting)
            {
                base.OnCollectionChanged(e);
                if (SortingSelector == null
                    || e.Action == NotifyCollectionChangedAction.Remove
                    || e.Action == NotifyCollectionChangedAction.Reset)
                    return;
                Sort();
            }

        }
        public void Sort()
        {
            BlockSorting = true;

            List<T> sorted = Descending ? this.OrderByDescending(SortingSelector).ToList() : this.OrderBy(SortingSelector).ToList();
            foreach (T item in sorted)
            {
                Move(IndexOf(item), sorted.IndexOf(item));
            }
            BlockSorting = false;
            base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    public abstract class SingletonBase<T> where T : SingletonBase<T>
    {
        private static readonly Lazy<T> Lazy =
            new(() => (Activator.CreateInstance(typeof(T), true) as T)!);

        public static T Instance => Lazy.Value;
    }
}
