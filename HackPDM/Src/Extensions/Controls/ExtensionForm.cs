using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI;
using HackPDM.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
//using Microsoft.UI.Xaml.Controls;
using Theme = HackPDM.Src.ClientUtils.Types.Theme;
using Control = Microsoft.UI.Xaml.Controls.Control;
using HackPDM.Src.ClientUtils.Types;
//using System.Windows.Controls;

namespace HackPDM.Extensions.Controls;

public static class ExtensionForm
{
	private static ConditionalWeakTable<DependencyObject, HolderValues> _data = new();
	private static GradientStopCollection _gradientStops;
	private static GradientStop _gradientStop;
	private static GradientStop _gradientStop2;
	private static LinearGradientBrush _brush;

	static ExtensionForm()
	{
		_gradientStop = new();
		_gradientStop2 = new();
		_gradientStops = new();

		_gradientStop.Color = Color.FromArgb(255, 139, 224, 249);
		_gradientStop2.Color = Color.FromArgb(255, 209, 242, 252);

		_gradientStops.Add(_gradientStop);
		_gradientStops.Add(_gradientStop2);
            
		_brush = new()
		{
			GradientStops = _gradientStops,
			StartPoint = new Windows.Foundation.Point(0, 0),
			EndPoint = new Windows.Foundation.Point(1, 1)
		};
	}

	extension(ListViewItem item)
	{
		public ItemData LinkedItem
		{
			get
			{
				if (_data.TryGetValue(item, out var holder)) return holder.ItemData;
				holder = new()
				{
					ItemData = new(),
				};
				_data.Add(item, holder);

				return holder.ItemData;
			}

			set
			{
				if (!_data.TryGetValue(item, out var holder))
				{
					holder = new();
					_data.Add(item, holder);
				}
				holder.ItemData = value;
				item.Content = holder.ItemData;
			}
		}
	}
	extension (TreeViewNode node)
	{
		public TreeData LinkedData
		{
			get 
			{
				if(!_data.TryGetValue(node, out var holder))
				{
					holder = new HolderValues();
					_data.Add(node, holder);
				}
				TreeData? dat = node.Content as TreeData;
				if (holder.TreeNodeData is not null && dat is not null && holder.TreeNodeData != dat)
				{
					holder.TreeNodeData.Name ??= dat.Name;
					holder.TreeNodeData.Node = node;
					List<ITreeItem?> children = [.. holder.TreeNodeData.Children?.Union(dat.Children ?? []) ?? []];
					holder.TreeNodeData.Children = children?.Count is not null and > 0 ? children?.Distinct()?.ToList() : null;
					holder.TreeNodeData.Parent ??= dat.Parent;
					holder.TreeNodeData.FullPath ??= dat.FullPath;
					holder.TreeNodeData.Tag ??= dat.Tag;
					holder.TreeNodeData.Icon ??= dat.Icon;
				}

				holder.TreeNodeData ??= dat ?? new("");
				node.Content = holder.TreeNodeData;

				return holder.TreeNodeData;
			} 
			set
			{
				if (!_data.TryGetValue(node, out var holder))
				{
					holder = new HolderValues();
					_data.Add(node, holder);
				}
				holder.TreeNodeData = value;
				node.Content = value;
			}
		}
	}

	extension<T>(T page) where T : Page
	{
		public TWin? GetWindow<TWin>() where TWin : Window
		{
			return InstanceManager.GetAWindow<T, TWin>(page);
		}
		public Window? Window
        {
			get => InstanceManager.GetAWindow<T, Window>(page);
		}
	}
	public static T? Content<T>(this TreeViewNode node) where T : class
	{   
		ArgumentNullException.ThrowIfNull(node);
		return node.Content as T;
	}

	public static IEnumerable<Control> GetAllControls(this Control control)
	{
		ArgumentNullException.ThrowIfNull(control);
		var controls = new List<Control> { control };
		if (control.XamlRoot?.Content is Panel panel)
		{
			foreach (var child in panel.Children)
			{
				if (child is Control childControl)
				{
					controls.AddRange(GetAllControls(childControl));
				}
			}
		}
		return controls;
	}
	public static void SetControlTheme(this Control control, Theme theme)
	{
		ArgumentNullException.ThrowIfNull(control);
		ArgumentNullException.ThrowIfNull(theme);
		try
		{
			Debug.WriteLine($"c name: {control.Name}");
			control.Background = new SolidColorBrush(theme.SecondaryBackgroundColor ?? StorageBox.Gray);
			control.Foreground = theme.ForegroundColor is null ? StorageBox.BrushBlack : new SolidColorBrush(theme.ForegroundColor ?? StorageBox.Black);
			control.FontFamily = new FontFamily(theme.FontFamily ?? "Segoe UI");
			control.FontSize = theme.FontSize;
		}
		catch (Exception ex)
		{
			Debug.Fail($"Failed to set theme on control: {ex.Message}");
		}
	}
	public static void SetFrameworkElementTheme(this Panel panel, Theme theme)
	{
		ArgumentNullException.ThrowIfNull(panel);
		ArgumentNullException.ThrowIfNull(theme);
		try
		{
			Debug.WriteLine($"fe name: {panel.Name}");
			panel.Background = new SolidColorBrush(theme.BackgroundColor ?? StorageBox.LightGray);
			foreach (var child in panel.Children)
			{
				if (child is Panel subPanel) subPanel.SetFrameworkElementTheme(theme);
				else if (child is Control rootControl) rootControl.SetControlTheme(theme);
			}
		}
		catch (Exception ex)
		{
			Debug.Fail($"Failed to set theme on framework element: {ex.Message}");
		}
	}
	public static bool SetFormTheme(this Control control, Theme theme, bool isRoot = true)
	{
		if (control == null) throw new ArgumentNullException(nameof(control));
		if (theme == null) throw new ArgumentNullException(nameof(theme));
		try
		{
			Debug.WriteLine($"c name: {control.Name}");

			control.Background = isRoot ? new SolidColorBrush(theme.BackgroundColor ?? StorageBox.LightGray) : new SolidColorBrush(theme.SecondaryBackgroundColor ?? StorageBox.Gray);
			control.Foreground = theme.ForegroundColor is null ? StorageBox.BrushBlack : new SolidColorBrush(theme.ForegroundColor ?? StorageBox.Black);
			control.FontFamily = new FontFamily(theme.FontFamily ?? "Segoe UI");
			control.FontSize = theme.FontSize;
				
			UIElement rootContent = control.XamlRoot.Content;
			if (rootContent is Panel panel) panel.SetFrameworkElementTheme(theme);
			else if (rootContent is Control rootControl) rootControl.SetControlTheme(theme);
   
			return true;
		}
		catch (Exception ex)
		{
			Debug.Fail($"Failed to set theme on form: {ex.Message}");
			return false;
		}
	}
	public static TreeData? RecurseNode(this TreeData node, ReadOnlySpan<string> paths)
	{
		foreach (TreeData tNode in node.Children ?? [])
		{
			if (tNode.Name == paths[0])
			{
				return paths.Length == 1 ? node : node.RecurseNode(paths[1..]);
			}
		}
		return null;
	}
	public static TreeData? FindTreeNode(this TreeView view, string path)
	{
		ArgumentNullException.ThrowIfNull(view, nameof(view));
		Span<string> pathSpan = path.Split("\\").AsSpan();
		List<TreeViewNode>? children = view.RootNodes as List<TreeViewNode>;

		if (children is null) return null;
		foreach (TreeViewNode node in children)
		{
			TreeData treeData = node.LinkedData;
			if (treeData.Name == pathSpan[0])
			{
				return pathSpan.Length == 1 ? treeData : treeData.RecurseNode(pathSpan[1..]);
			}
		}
		return null;        
	}
	public static string GetTreeNodePath(this TreeData node)
	{
		ArgumentNullException.ThrowIfNull(node);
		return node.FullPath;
	}
	public static T? ItemsSource<T>(this ItemsControl control) where T : class
	{
		ArgumentNullException.ThrowIfNull(control);
		return control.ItemsSource as T;
	}
	public static void Sort<T>(this ItemsControl items, Comparison<T> comparison) 
	{
		var casted = items.ItemsSource as List<T>;
		casted?.Sort(comparison.Invoke);
		if (casted is not null) items.ItemsSource = casted;
	}
	//extension(Form form)
	//{
	//    public bool IsSingleton
	//    {
	//        get => _data.TryGetValue(form, out var holder) && holder.IsSingleton;
	//        set => _data.GetOrCreateValue(form).IsSingleton = value;
	//    }
	//    public Form SingletonInstance
	//    {
	//        get => _data.TryGetValue(form, out var holder) ? holder.SingletonInstance : null;
	//        set => _data.GetOrCreateValue(form).SingletonInstance = value;
	//    }
	//}
	public static void EnsureVisible(this TreeData node, TreeView tree)
	{
		ArgumentNullException.ThrowIfNull(node);
		ArgumentNullException.ThrowIfNull(tree);
		TreeData? current = node;
		TreeViewItem treeItem = new();
		while (current != null)
		{
			current.IsExpanded = true;
			current = current.Parent;
		}
		tree.SelectedNode = node.Node;
		TreeViewItem? item = tree.ContainerFromNode(node.Node) as TreeViewItem;

		item?.StartBringIntoView();
	}

	private class HolderValues
	{
		public bool IsSingleton { get; set; }=false;
		public TreeData? TreeNodeData { get; set;  } = null;
		public ItemData? ItemData { get; set;  } = null;
	}
}