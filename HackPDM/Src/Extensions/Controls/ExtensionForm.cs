using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using HackPDM.ClientUtils;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

using Theme = HackPDM.ClientUtils.Theme;
using Control = Microsoft.UI.Xaml.Controls.Control;
using HackPDM.Src.Extensions.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;

namespace HackPDM.Src.Extensions.Controls
{
    public static class ExtensionForm
    {
        private static ConditionalWeakTable<Control, HolderValues> _data = new();
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
        public static TreeViewNode RecurseNode(this TreeViewNode node, ReadOnlySpan<string> paths)
        {
            
        }
        public static TreeViewNode FindTreeNode(this TreeView view, string path)
        {
            view.Item
            ArgumentNullException.ThrowIfNull(view, nameof(view));
            Span<string> pathSpan = path.Split("\\").AsSpan();
            foreach(var node in view.RootNodes)
            {
                if (node.)
            }
			//TreeNodeCollection nodes = null;
   //         TreeNode node = null;
   //         string[] paths = path.Split('\\');
   //         try
   //         {
   //             for (int i = 0; i < paths.Length; i++)
   //             {
   //                 if (i == 0)
   //                     nodes = view.Nodes;
   //                 else
   //                     nodes = node.Nodes;

   //                 bool wasFound = false;
   //                 foreach (TreeViewNode n in nodes)
   //                 {
   //                     if (n.Text == paths[i])
   //                     {
   //                         wasFound = true;
   //                         node = n;
   //                         break;
   //                     }
   //                 }
   //                 if (!wasFound)
   //                     return null;
   //             }
   //             return node;
   //         }
   //         catch
   //         {
   //             return null;
   //         }
        }
        
        
        extension(Form form)
        {
            public bool IsSingleton
            {
                get => _data.TryGetValue(form, out var holder) && holder.IsSingleton;
                set => _data.GetOrCreateValue(form).IsSingleton = value;
            }
            public Form SingletonInstance
            {
                get => _data.TryGetValue(form, out var holder) ? holder.SingletonInstance : null;
                set => _data.GetOrCreateValue(form).SingletonInstance = value;
            }
        }
        private class HolderValues
        {
            public bool IsSingleton { get; set; }=false;
            public Form SingletonInstance { get;set; }
        }
    }
}
