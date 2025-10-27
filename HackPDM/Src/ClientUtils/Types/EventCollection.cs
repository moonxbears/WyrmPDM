using Microsoft.UI.Xaml.Controls;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Subjects;

namespace HackPDM.Src.ClientUtils.Types
{
    public class EventCollection<T> : ObservableCollection<T>
    {
		public void ManualNotifyCollectionChanged(ReasonForCall reason)
		{
			NotifyCollectionChangedEventArgs arg = new(NotifyCollectionChangedAction.Replace, this, reason);
			OnCollectionChanged(arg);
		}

		public enum ReasonForCall
		{
			EndOfUpdate,
			BeginningOfUpdate,
			Other,
		}
	}
    public class ListViewCollection<T> : EventCollection<T>
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
    public class ItemChangedEventArgs<T>(T item, ChangeType changeType) : GenericItemEventArg<T, ChangeType>(item, changeType) { }
    public class ListItemSelectionEventArgs<T>(T item, bool isSelected) : GenericItemEventArg<T, bool>(item, isSelected) { }
    public class Ev<T> : INotifyPropertyChanged, INotifyPropertyChanging, INotifyNull<T>
    {
        public event PropertyChangingEventHandler? PropertyChanging;
        public event PropertyChangedEventHandler? PropertyChanged;
		public event PropertyNullHandler<T>? BecomingNull;
		public event PropertyNullHandler<T>? BecameNotNull;

		public T Value
        {
            get
            {
                return field;
            }
            set
            {
                if (!EqualityComparer<T>.Default.Equals(field, value))
                {
                    if (value is null && field is not null)
                        BecomingNull?.Invoke(this, new(false, recursiveDepth: 0));

					PropertyChanging?.Invoke(this, new(nameof(Value)));
                    field = value;
					
                    if (field is null)
						BecameNotNull?.Invoke(this, new(true, recursiveDepth: 0));

					PropertyChanged?.Invoke(this, new(nameof(Value)));
                }
            }
        }
        public Ev(T value) => Value = value;
        public static implicit operator T(Ev<T> prop) => prop.Value;
        public static implicit operator Ev<T>(T value) => new(value);
        public override string ToString() => Value?.ToString() ?? "null";
    }
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
    public class PropertyNullEventArgs<T> : EventArgs
    {
        public virtual bool IsNull { get; }
        public virtual int RecursiveFixCount { get; set; }
		public virtual NullFixType NullFix { get; set; } = NullFixType.Pass;
		public NullChangeType ChangeType { get; set; } = NullChangeType.AssignNull;
		public PropertyNullEventArgs(bool isNull = false, NullChangeType nullChange = NullChangeType.AssignNull, NullFixType nullFix = NullFixType.Pass, int recursiveDepth = 0, Func<T>? func = null)
        {
            IsNull = isNull;
            ChangeType = nullChange;
			NullFix = nullFix;
            RecursiveFixCount = recursiveDepth;
            if (isNull && nullFix == NullFixType.Error && func is not null)
            {
                RecursiveFixCount++;
                ChangeType = NullChangeType.ModifyWithFunction;
			}
		}
    }
    public delegate void PropertyNullHandler<T>(object? sender, PropertyNullEventArgs<T> e);
    public interface INotifyNull<T>
    {
        public event PropertyNullHandler<T>? BecomingNull;
        public event PropertyNullHandler<T>? BecameNotNull;
    }
    public enum NullChangeType
    {
        NoAssign,
        ModifyWithFunction,
		AssignNull
    }
    public enum NullFixType
    {
        Pass,
        Error,
        Success,
	}
}