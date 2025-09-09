using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using HackPDM.ClientUtils;
using HackPDM.Properties;
using HackPDM.Src;
using HackPDM.Src.Extensions.Controls;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace HackPDM.Data
{
    public class EntryRow : IListItem<EntryRow>, IRowData
    {
        public int?          ID         { get; set; }
        public string?      Name        { get; set; }
        public string?      Type        { get; set; }
        public long?         Size       { get; set; }
        public FileStatus   Status      { get; set; } = FileStatus.LO;
        public HpUser?      Checkout    { get; set; }
        public HpCategory?  Category    { get; set; }
        public DateTime?    LocalDate   { get; set; }
        public DateTime?    RemoteDate  { get; set; }
        public string?      FullName    { get; set; }

		public EntryRow?    Value       { get => this; }
		public bool         IsSelected  { get; set; }
	}
    public class HistoryRow : IListItem<HistoryRow>, IRowData
    {
        public int          Version     { get; set; }
        public HpUser?      ModUser     { get; set; }
        public DateTime?    ModDate     { get; set; }
        public long         Size        { get; set; }
        public DateTime?    RelDate     { get; set; }
		public HistoryRow? Value { get; }
		public bool IsSelected { get; set; }
	}
    public class ParentRow : IListItem<ParentRow>, IRowData
    {
        public int          Version     { get; set; }
        public string?      Name        { get; set; }
        public string?      BasePath    { get; set; }
		public ParentRow? Value { get; }
		public bool IsSelected { get; set; }
	}
    public class ChildrenRow : IListItem<ChildrenRow>, IRowData
    {
        public int          Version     { get; set; }
        public string?      Name        { get; set; }
        public string?      BasePath    { get; set; }
		public ChildrenRow? Value { get; }
		public bool IsSelected { get; set; }
	}
    public class PropertiesRow : IListItem<PropertiesRow>, IRowData
    {
        public int          Version     { get; set; }
        public string?      Configuration {  get; set; }
        public string?      Name        { get; set; }
        public string?      Property    { get; set; }
        public string?      Type        { get; set; }
		public PropertiesRow? Value { get; }
		public bool IsSelected { get; set; }
	}
    public class VersionRow : IListItem<VersionRow>, IRowData
    {
        public int ID { get; set; }
        public string? Name { get; set; }
        public long FileSize { get; set; }
        public int DirectoryID { get; set; }
        public int NodeID { get; set; }
        public int EntryID { get; set; }
        public int AttachmentID { get; set; }
        public DateTime? ModifyDate { get; set; }
        public string? Checksum { get; set; }
        public string? OdooCompletePath { get; set; }
		public VersionRow? Value { get; }
		public bool IsSelected { get; set; }
	}
    public class SearchRow : IListItem<SearchRow>, IRowData
    {
        public int ID { get; set; }
        public string? Name { get; set; }
        public string? Directory { get; set; }
		public SearchRow? Value { get; }
		public bool IsSelected { get; set; }
	}
    public class SearchPropRow : IListItem<SearchPropRow>, IRowData
    {
        public string? Name { get; set; }
        public string? Comparer { get; set; }
        public string? Value { get; set; }
		public bool IsSelected { get; set; }
		SearchPropRow? IListItem<SearchPropRow>.Value { get; }
	}
    public class FileTypeRow : IListItem<FileTypeRow>, IRowData
    {
        public string? Extension { get; set; }
        public string? Category { get; set; }
        public string? RegEx { get; set; }
        public string? Description { get; set; }
		public FileTypeRow? Value { get; }
		public bool IsSelected { get; set; }
	}
    public class FileTypeEntryFilterRow : IListItem<FileTypeEntryFilterRow>, IRowData
    {
        public int ID { get; set; }
        public string? Proto { get; set; }
        public string? RegEx { get; set; }
        public string? Description { get; set; }
		public FileTypeEntryFilterRow? Value { get; }
		public bool IsSelected { get; set; }
	}
    public class FileTypeLocRow : IListItem<FileTypeLocRow>, IRowData
    {
        public string? Extension { get; set; }
        public string? Status { get; set; }
        public string? Example { get; set; }
		public FileTypeLocRow? Value { get; }
		public bool IsSelected { get; set; }
	}
    public class FileTypeLocDatRow : IListItem<FileTypeLocDatRow>, IRowData
    {
        public string? Extension { get; set; }
        public string? RegEx { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public object? Icon { get; set; } // Type is Unknown, so use object
        public object? RemoveIcon { get; set; } // Type is Unknown, so use object
		public FileTypeLocDatRow? Value { get; }
		public bool IsSelected { get; set; }
	}
	public partial class TreeData : ITreeItem, IEnumerable<TreeData>
	{
		public string? Name 
        {
            get
            {
                if (string.IsNullOrEmpty(field))
                {
                    field = StorageBox.EMPTY_PLACEHOLDER;
                }
                return field;
			} 
            set; 
        }
		public string? FullPath 
        {
            get
            {
                if (Parent is null)
                {
                    field = Name;
                    return field;
                }
                field = $"{Parent.FullPath}\\{Name}";
				return field;
			} 
            set; 
        }
		public TreeData? Parent 
        { 
            get
            {
                field ??= Node?.Parent.LinkedData;
                return field;
			}
            set; 
        }
		public object? Tag { get; set; }
		public int? DirectoryID { get; set; }
        public BitmapImage? Icon { get; set; } = Assets.GetImage("simple-folder-icon_32") as BitmapImage;
		public TreeViewNode? Node { get; internal set; }
		public List<ITreeItem>? Children
        {
            get
            {
                field ??= Node?.Children.Select(n => n.LinkedData as ITreeItem).ToList();
                return field;
            }
            set
            {
                field = value;
            }
        }
        public bool IsLinked => Node is not null;
        public bool HasChildren => Node?.HasChildren ?? false;
        public bool IsExpanded
        {
            get => Node?.IsExpanded ?? false;
            set => Node?.IsExpanded = value;
		}
		public TreeData(string? name, TreeData? parent = null)
		{
			Name = name;
			Parent = parent;
			Parent?.AddChild(this);
			FullPath = Parent is null ? Name : $"{parent?.FullPath}\\{Name}";
            Children = [];
			Tag = null;
		}
		public void AddChild(TreeData child)
		{
			child.Parent = this;
			Children.Add(child);
		}
        public void AddChildren(IEnumerable<TreeData> children)
        {
            foreach (var child in children)
            {
                AddChild(child);
            }
        }
        public void RemoveChild(TreeData child)
        {
            if (Children.Contains(child))
            {
                child.Parent = null;
                Children.Remove(child);
            }
        }
        public void RemoveChildren(IEnumerable<TreeData>? children)
        {
            if (children is null) return;
			foreach (var child in children)
            {
                RemoveChild(child);
            }
		}
        public void Clear() => RemoveChildren(Children as IEnumerable<TreeData>);

		public IEnumerator<TreeData> GetEnumerator() => Children.Cast<TreeData>().GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		public override string ToString()
		{
			return Name ?? "";
		}
	}
}
