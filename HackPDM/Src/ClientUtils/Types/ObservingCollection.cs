using Microsoft.UI.Xaml.Controls;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Subjects;

namespace HackPDM.Src.ClientUtils.Types
{
	public class ObservingCollection<T> : ObservableCollection<T>
	{
        public event EventHandler<ItemChangedEventArgs<T>> ItemChanged;

        private readonly Subject<ItemChangedEventArgs<T>> _subject = new();
        public IObservable<ItemChangedEventArgs<T>> ItemChanges => _subject;

        private readonly List<IItemChangeListener<T>> _listeners = [];

        public ObservingCollection() : base() 
        {
             
        }


        public void AddListener(IItemChangeListener<T> listener)
            => _listeners.Add(listener); 
        public void RemoveListener(IItemChangeListener<T> listener)
            => _listeners.Remove(listener);
        
        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            RaiseItemChanged(item, ChangeType.Added);
        }
        protected override void RemoveItem(int index)
        {
            var item = this[index];
            base.RemoveItem(index);
            RaiseItemChanged(item, ChangeType.Removed);
        }
        protected override void SetItem(int index, T item)
        {
            base.SetItem(index, item);
            RaiseItemChanged(item, ChangeType.Updated);
        }
		protected override void MoveItem(int oldIndex, int newIndex)
		{
			base.MoveItem(oldIndex, newIndex);
            RaiseItemChanged(this[newIndex], ChangeType.Updated);
        }
        
        private void RaiseItemChanged(T item, ChangeType change)
        {
            var args = new ItemChangedEventArgs<T>(item, change);
            ItemChanged?.Invoke(this, args);
            _subject.OnNext(args);
            foreach (var listener in _listeners)
            {
                switch (change)
                {
                    case ChangeType.Added:
                        listener.OnItemAdded(this, args);
                        break;
                    case ChangeType.Removed:
                        listener.OnItemRemoved(this, args);
                        break;
                    case ChangeType.Updated:
                        listener.OnItemUpdated(this, args);
                        break;
                    case ChangeType.Selected:
                        listener.OnItemSelected(this, args);
                        break;
                    case ChangeType.Clicked:
                        listener.OnItemClicked(this, args);
                        break;
                    case ChangeType.Rendering:
                        listener.OnItemRendering(this, args);
                        break;
                    case ChangeType.Focused:
                        listener.OnItemFocused(this, args);
                        break;
                    case ChangeType.Hovered:
                        listener.OnItemHovered(this, args);
                        break;
                }
            }
        }
    }
    public class ListViewCollection<T> : ObservingCollection<T>
    {
        public ListView ListView { get; }
        public ListViewCollection(ListView listView)
        {
            ListView = listView;
            ListView.ItemsSource = this;
        }
    }
    public class CommonItemEventArgs<T> : EventArgs
    {
        public T Item { get; internal set; }
        public DateTime Timestamp { get; internal set; }
        public CommonItemEventArgs(T item)
        {
            Item = item;
            Timestamp = DateTime.UtcNow;
        }
    }
    public class GenericItemEventArg<T, U> : CommonItemEventArgs<T>
    {
        public U Data { get; }
        public GenericItemEventArg(T item, U data) : base(item)
        {
            Data = data;
        }
    }
    public class ItemChangedEventArgs<T>(T item, ChangeType changeType) : GenericItemEventArg<T, ChangeType>(item, changeType) {}
    public class ListItemSelectionEventArgs<T>(T item, bool isSelected) : GenericItemEventArg<T, bool>(item, isSelected) {}
    public enum ChangeType
    {
        Added,
        Removed,
        Updated,
        Selected,
        Clicked,
        Rendering,
        Focused,
        Hovered,
    }
}
