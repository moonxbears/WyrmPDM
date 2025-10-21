using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using HackPDM.Odoo.OdooModels.Models;
using HackPDM.Properties;
using HackPDM.Extensions.Controls;
using HackPDM.Forms.Hack;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

using static HackPDM.Odoo.OdooModels.Models.HpVersionProperty;
using HackPDM.Src.ClientUtils.Types;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace HackPDM.Data;

#region VIEWS
public partial class EntryRow : ItemData, IRowData
{
	// (MVVM) VIEW
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
	public int?			LatestId	{ get; set; }

	public partial bool? IsLocal { get; set; }
	public partial bool	 IsOnlyLocal { get; }
	public bool			IsRemote	{ get; set; }

	public ObservableCollection<HistoryRow>? History { get; set; }
	public ObservableCollection<VersionRow>? Versions { get; set; }
	public ObservableCollection<PropertiesRow>? Properties { get; set; }
	public ObservableCollection<ParentRow>? Parents { get; set; }
	public ObservableCollection<ChildrenRow>? Children { get; set; }
}
public class HistoryRow : ItemData, IRowData
{
	// (MVVM) VIEW
	public int          Version     { get; set; }
	public HpUser?      ModUser     { get; set; }
	public DateTime?    ModDate     { get; set; }
	public long?         Size        { get; set; }
	public DateTime?    RelDate     { get; set; }
	public HistoryRow() {}
}
public class ParentRow : ItemData, IRowData
{
	// (MVVM) VIEW
	public int          Version     { get; set; }
	public string?      BasePath    { get; set; }
	public ParentRow() {}
}
public class ChildrenRow : ItemData, IRowData
{
	// (MVVM) VIEW
	public int          Version     { get; set; }
	public string?      BasePath    { get; set; }
	public ChildrenRow() {}
}
public class PropertiesRow : ItemData, IRowData
{
	// (MVVM) VIEW
	public int          Version     { get; set; }
	public int?         Property    { get; set; }
	public string?      Configuration{  get; set; }
	public PropertyType?Type        { get; set; }
	public object?      ValueData       { get; set; }
	public PropertiesRow() {}
}
public class VersionRow : ItemData, IRowData
{
	// (MVVM) VIEW
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
	// (MVVM) VIEW
	public int Id { get; set; }
	public string? Directory { get; set; }
}
public class SearchPropRow : ItemData, IRowData
{
	// (MVVM) VIEW
	public string? Comparer { get; set; }
	public string? Value { get; set; }
}
public class FileTypeRow : ItemData, IRowData
{
	// (MVVM) VIEW
	public string? Extension { get; set; }
	public string? Category { get; set; }
	public string? RegEx { get; set; }
	public string? Description { get; set; }
}
public class FileTypeEntryFilterRow : ItemData, IRowData
{
	// (MVVM) VIEW
	public int Id { get; set; }
	public string? Proto { get; set; }
	public string? RegEx { get; set; }
	public string? Description { get; set; }
}
public class FileTypeLocRow : ItemData, IRowData
{
	// (MVVM) VIEW
	public string? Extension { get; set; }
	public string? Status { get; set; }
	public string? Example { get; set; }
}
public class FileTypeLocDatRow : ItemData, IRowData
{
	// (MVVM) VIEW
	public string? Extension { get; set; }
	public string? RegEx { get; set; }
	public string? Category { get; set; }
	public string? Description { get; set; }
	public object? Icon { get; set; } // Type is Unknown, so use object
	public object? RemoveIcon { get; set; } // Type is Unknown, so use object
}
public partial class TreeData : IEnumerable<TreeData>
{
	// (MVVM) VIEW
	public partial string? Name { get; set; }
	public partial string? FullPath { get; }
	public partial TreeData? Parent { get; }
	public object? Tag { get; set; }
	public int? DirectoryId { get; set; }
	public BitmapImage? Icon { get; set; } = Assets.GetImage("simple-folder-icon_32") as BitmapImage;
	public TreeView? ParentTree { get; internal set; }
	public TreeViewNode? Node { get; internal set; }
	public partial TreeViewItem? VisualContainer { get; }
	public partial IEnumerable<TreeData>? Children { get; }
	public partial bool IsLinked { get; }
	public partial bool HasChildren { get; }
	public partial int Depth { get; }
	public partial bool IsExpanded { get; set; }
}
#endregion

#region VIEWMODELS
public partial class TreeData : IEnumerable<TreeData>
{
	// (MVVM) ViewModel
	public partial string? Name 
	{
		get
		{
			return field ??= Depth is < 0 ? null : StorageBox.EMPTY_PLACEHOLDER;
		} 
		set; 
	}
	public partial string? FullPath => Parent is null ? Depth < 0 ? null : Name : $"{Parent?.FullPath}\\{Name}";
	public partial TreeData? Parent => Node?.Depth <= 0 ? null : Node?.Parent?.LinkedData;
	public partial TreeViewItem? VisualContainer => ParentTree?.ContainerFromNode(Node) as TreeViewItem;
	public partial IEnumerable<TreeData>? Children => Node?.Children.Select(n => n.LinkedData);
	public partial bool IsLinked => Node is not null;
	public partial bool HasChildren => Node?.HasChildren ?? false;
	public partial int Depth => Node?.Depth ?? -1;
	public partial bool IsExpanded
	{
		get => Node?.IsExpanded ?? false;
		set => Node?.IsExpanded = value;
	}
	public TreeData(string? name)
	{
		Name = name;
		Tag = null;
	}
	public IEnumerator<TreeData> GetEnumerator() => Children?.GetEnumerator() ?? Enumerable.Empty<TreeData>().GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
	public override string ToString()
	{
		return Name ?? "";
	}

	public void SortTree()
	{
		var root = Node;
		if (root is null || root.Children.Count == 0) return;
		
		// sort children by TreeData.Name
		ObservableCollection<TreeViewNode> sortedChildren =
		[
			.. root.Children
				.OrderBy(n => (n.LinkedData?.Name ?? string.Empty), StringComparer.OrdinalIgnoreCase)
		];
		root.Children.Clear();
		foreach (var child in sortedChildren)
		{
			root.Children.Add(child);
			child.LinkedData.SortTree();
		}
	}
}
public partial class EntryRow : ItemData, IRowData
{
	// (MVVM) ViewModel
	public partial bool? IsLocal
	{
		get
		{
			return Id is null or 0;
		}
		set => field = value;
	}
	public partial bool	IsOnlyLocal => (IsLocal ?? true) && !IsRemote;
	
}
#endregion

public class BasicStatusMessage : IRowData
{
	// // (MVVM) VIEW
	public StatusMessage Status { get; set; } = StatusMessage.OTHER;
	public string? Message { get; set; }
}
public partial class ItemData
{
	public virtual ListViewItem? Item { get; set; }
	public virtual string? Name { get; set; } = "";
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
public class Wrap<T>(T value) where T : struct
{
	T Value = value;
	public static implicit operator T(Wrap<T> wrap) => wrap.Value;
	public static implicit operator Wrap<T>(T value) => new Wrap<T>(value);
}

