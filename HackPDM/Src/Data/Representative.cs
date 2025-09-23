using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HackPDM.Odoo.OdooModels.Models;
using HackPDM.Properties;
using HackPDM.Extensions.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

using static HackPDM.Odoo.OdooModels.Models.HpVersionProperty;
using HackPDM.Src.ClientUtils.Types;

namespace HackPDM.Data;

public class EntryRow : ItemData, IRowData
{
	public BitmapImage? Icon        { get; set; } = Assets.GetImage("file-icon_32") as BitmapImage;
	public int?          Id         { get; set; }
	public string?      Type        { get; set; }
	public long?         Size       { get; set; }
	public FileStatus   Status      { get; set; } = FileStatus.Lo;
	public HpUser?      Checkout    { get; set; }
	public HpCategory?  Category    { get; set; }
	public DateTime?    LocalDate   { get; set; }
	public DateTime?    RemoteDate  { get; set; }
	public string?      FullName    { get; set; }

	public bool? IsLocal
	{
		get
		{
			return Id is null or 0;
		}
		set => field = value;
	}
	public bool			IsOnlyLocal => (IsLocal ?? true) && !IsRemote;
	public bool			IsRemote	{ get; set; }
}
public class HistoryRow : ItemData, IRowData
{
	public int          Version     { get; set; }
	public HpUser?      ModUser     { get; set; }
	public DateTime?    ModDate     { get; set; }
	public long?         Size        { get; set; }
	public DateTime?    RelDate     { get; set; }
	public HistoryRow() {}
}
public class ParentRow : ItemData, IRowData
{
	public int          Version     { get; set; }
	public string?      BasePath    { get; set; }
	public ParentRow() {}
}
public class ChildrenRow : ItemData, IRowData
{
	public int          Version     { get; set; }
	public string?      BasePath    { get; set; }
	public ChildrenRow() {}
}
public class PropertiesRow : ItemData, IRowData
{
	public int          Version     { get; set; }
	public int?         Property    { get; set; }
	public string?      Configuration{  get; set; }
	public PropertyType?Type        { get; set; }
	public object?      ValueData       { get; set; }
	public PropertiesRow() {}
}
public class VersionRow : ItemData, IRowData
{
	public int Id { get; set; }
	public long? FileSize { get; set; }
	public int? DirectoryId { get; set; }
	public int? NodeId { get; set; }
	public int? EntryId { get; set; }
	public int? AttachmentId { get; set; }
	public DateTime? ModifyDate { get; set; }
	public string? Checksum { get; set; }
	public string? OdooCompletePath { get; set; }
}
public class SearchRow : ItemData, IRowData
{
	public int Id { get; set; }
	public string? Directory { get; set; }
}
public class SearchPropRow : ItemData, IRowData
{
	public string? Comparer { get; set; }
	public string? Value { get; set; }
}
public class FileTypeRow : ItemData, IRowData
{
	public string? Extension { get; set; }
	public string? Category { get; set; }
	public string? RegEx { get; set; }
	public string? Description { get; set; }
}
public class FileTypeEntryFilterRow : ItemData, IRowData
{
	public int Id { get; set; }
	public string? Proto { get; set; }
	public string? RegEx { get; set; }
	public string? Description { get; set; }
}
public class FileTypeLocRow : ItemData, IRowData
{
	public string? Extension { get; set; }
	public string? Status { get; set; }
	public string? Example { get; set; }
}
public class FileTypeLocDatRow : ItemData, IRowData
{
	public string? Extension { get; set; }
	public string? RegEx { get; set; }
	public string? Category { get; set; }
	public string? Description { get; set; }
	public object? Icon { get; set; } // Type is Unknown, so use object
	public object? RemoveIcon { get; set; } // Type is Unknown, so use object
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
	public int? DirectoryId { get; set; }
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

public class BasicStatusMessage
{
	public StatusMessage Status { get; set; } = StatusMessage.OTHER;
	public string? Message { get; set; }
}
public partial class ItemData
{
	public virtual ListViewItem? Item { get; set; }
	public virtual string Name { get; set; } = "";
	public virtual string? Text
	{
		get => field ??= Name;
		set;
	}

	public virtual bool IsSelected
	{
		get => Item?.IsSelected ?? false;	
		set => Item?.IsSelected = value;
	}
}