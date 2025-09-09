using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using HackPDM.ClientUtils;
//using Microsoft.UI.Xaml.Controls;
using Windows.UI;

using Theme = HackPDM.ClientUtils.Theme;
using Control = Microsoft.UI.Xaml.Controls.Control;
using HackPDM.Src.Extensions.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using HackPDM.Data;
//using System.Windows.Controls;
using System.Linq;
using Microsoft.UI.Xaml.Controls;

namespace HackPDM.Src.Extensions.Controls
{
    public static class ExtensionForm
    {
        private static ConditionalWeakTable<DependencyObject, HolderValues> _data = new();
        private static GradientStopCollection gradientStops;
		private static GradientStop gradientStop;
		private static GradientStop gradientStop2;
		private static LinearGradientBrush brush;

		static ExtensionForm()
        {
            gradientStop = new();
            gradientStop2 = new();
            gradientStops = new();

            gradientStop.Color = Color.FromArgb(255, 139, 224, 249);
            gradientStop2.Color = Color.FromArgb(255, 209, 242, 252);

			gradientStops.Add(gradientStop);
            gradientStops.Add(gradientStop2);
            
			brush = new()
			{
                GradientStops = gradientStops,
				StartPoint = new Windows.Foundation.Point(0, 0),
				EndPoint = new Windows.Foundation.Point(1, 1)
			};
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
                control.Background = new SolidColorBrush(theme.SecondaryBackgroundColor ?? StorageBox.GRAY);
                control.Foreground = theme.ForegroundColor is null ? StorageBox.BRUSH_BLACK : new SolidColorBrush(theme.ForegroundColor ?? StorageBox.BLACK);
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
                panel.Background = new SolidColorBrush(theme.BackgroundColor ?? StorageBox.LIGHT_GRAY);
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

                control.Background = isRoot ? new SolidColorBrush(theme.BackgroundColor ?? StorageBox.LIGHT_GRAY) : new SolidColorBrush(theme.SecondaryBackgroundColor ?? StorageBox.GRAY);
                control.Foreground = theme.ForegroundColor is null ? StorageBox.BRUSH_BLACK : new SolidColorBrush(theme.ForegroundColor ?? StorageBox.BLACK);
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
		}
    }
}
