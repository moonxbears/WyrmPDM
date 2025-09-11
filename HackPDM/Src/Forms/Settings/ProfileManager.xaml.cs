using System.Collections.Generic;
using HackPDM.Forms.Helper;
using HackPDM.Forms.Odoo;
using HackPDM.Hack;
using HackPDM.Odoo;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using HackFileManager = HackPDM.Forms.Hack.HackFileManager;
using HackSettings = HackPDM.Src.Forms.Hack.HackSettings;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HackPDM.Forms.Settings;

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
        if (HackApp.Window != null)
        {
            HackApp.Window.Title = "Profile Manager - HackPDM";
        }
        odooSettingsBtn.Click += (s, e) =>
        {
            HackApp.RootFrame?.Navigate(typeof(OdooSettings));
        };
        HackSettingsBtn.Click += (s, e) =>
        {
            HackApp.RootFrame?.Navigate(typeof(HackSettings));
        };
        OdooLoginBtn.Click += (s, e) =>
        {
            if (AbleToLogin())
            {
                try
                {
                    if (HackUpdater.EnsureUpdated() && OdooDefaults.MyNode is not null)
                    {
                        var hfm = new HackFileManager();
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
            int status = OdooClient.CorrectUserId();
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

            if (!OdooClient.CorrectOdooAddress())
            {
                errors.Add("invalid odoo address or unreachable host");
            }
            else if (!OdooClient.CorrectOdooPort())
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