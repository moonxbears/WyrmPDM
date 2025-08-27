using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using HackPDM.HackClient;
using HackPDM.Src.Forms.Hack;
using HackPDM.Src.Forms.Helper;
using HackPDM.Src.Forms.Odoo;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;

using static HackPDM.Src.Forms.Helper.MessageBox;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HackPDM.Src.Forms.Settings
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ProfileManager : Page
    {
        public ProfileManager()
        {
            InitializeComponent();
        }
        public void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (App.Window != null)
            {
                App.Window.Title = "Profile Manager - HackPDM";
            }
            odooSettingsBtn.Click += (s, e) =>
            {
                App.RootFrame?.Navigate(typeof(OdooSettings));
            };
            HackSettingsBtn.Click += (s, e) =>
            {
                App.RootFrame?.Navigate(typeof(HackSettings));
            };
            OdooLoginBtn.Click += (s, e) =>
            {
                if (AbleToLogin())
                {
                    try
                    {
                        if (HackUpdater.EnsureUpdated() && OdooDefaults.MyNode is not null)
                        {
                            var HFM = new HackFileManager();
                        }
                        else
                        {
                            MessageBox.Show("local version is different than server version");
                        }
                    }
                    catch
                    {
                        MessageBox.Show("local version is different than server version");
                    }
                }
            };
        }
        private bool AbleToLogin()
        {
            try
            {
                List<string> errors = [];
                int status = OdooRpcCs.OdooClient.CorrectUserID();
                switch (status)
                {
                    case 1: return true;
                    case 0:
                        {
                            errors.Add("invalid odoo credentials");
                            break;
                        }
                    default:
                        {
                            errors.Add("odoo server isn't running");
                            break;
                        }
                }

                if (!OdooRpcCs.OdooClient.CorrectOdooAddress())
                {
                    errors.Add("invalid odoo address or unreachable host");
                }
                else if (!OdooRpcCs.OdooClient.CorrectOdooPort())
                {
                    errors.Add("invalid odoo port or server is down");
                }
                else
                {
                    errors.Add("invalid odoo credentials");
                }
                if (errors.Count > 0)
                {
                    foreach (string message in errors)
                    {
                        var listItem = HackFileManager.EmptyListItem(ProfileManStatusList);

                        listItem.SubItems["Status"].Text = "ERROR";
                        listItem.SubItems["Message"].Text = message;

                        ProfileManStatusList.Items.Add(listItem);
                    }
                    return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
