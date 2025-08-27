using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

using HackPDM.ClientUtils;

using Theme = HackPDM.ClientUtils.Theme;
using Form = System.Windows.Forms.Form;

namespace HackPDM.Extensions.Control
{
    public static class ExtensionForm
    {
        private static ConditionalWeakTable<Form, HolderValues> _data = new();
        
        public static bool SetFormTheme(this System.Windows.Forms.Control control, Theme theme, bool isRoot = true)
        {
            if (control == null) throw new ArgumentNullException(nameof(control));
            if (theme == null) throw new ArgumentNullException(nameof(theme));
            try
            {
                Debug.WriteLine($"c name: {control.Name}");
                control.BackColor = isRoot ? theme.BackgroundColor ?? Color.White : theme.SecondaryBackgroundColor ?? Color.LightGray;
                control.ForeColor = theme.ForegroundColor ?? Color.Black;
                control.Font = new Font(theme.FontFamily, theme.FontSize);

                foreach (System.Windows.Forms.Control item in control.Controls)
                {
                    ExtensionForm.SetFormTheme(item, theme, false);
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.Fail($"Failed to set theme on form: {ex.Message}");
                return false;
            }
        }
        public static TreeNode FindTreeNode(this TreeView view, string path)
        {
            TreeNodeCollection nodes = null;
            TreeNode node = null;
            string[] paths = path.Split('\\');
            try
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    if (i == 0)
                        nodes = view.Nodes;
                    else
                        nodes = node.Nodes;

                    bool wasFound = false;
                    foreach (TreeNode n in nodes)
                    {
                        if (n.Text == paths[i])
                        {
                            wasFound = true;
                            node = n;
                            break;
                        }
                    }
                    if (!wasFound)
                        return null;
                }
                return node;
            }
            catch
            {
                return null;
            }
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
