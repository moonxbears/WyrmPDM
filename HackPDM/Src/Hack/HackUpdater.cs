using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using HackPDM.Forms.Helper;
using HackPDM.Odoo;

namespace HackPDM.Hack;

internal class HackUpdater
{
	const long REPO_ID = 28426033L;
	const string BRANCH_NAME = "justinOdooIntegration";
	const string PUBLISH_URL = "\\\\freedom\\Engineering\\hackpdm\\setup.exe";

	private static string _odooClientVersion;

	private static Version? CurrentVersion()
	{
		return Assembly.GetExecutingAssembly().GetName().Version;
	}
	private static bool IsCorrectOdooVersion(Version? version)
	{
		if (version is null)
		{
			if (MessageBox.Show($"Unable to get App version. Would you like to download the latest version?", "Versions",
								MessageBox.MessageBoxType.YesNoCancel) == MessageBox.DialogResult.Yes)
			{
				UpdaterProcess();
			}
			return false;
		}
        _odooClientVersion = OdooDefaults.HpSettings.Where( s => s.Name == OdooDefaults.ODOO_VERSION_KEY_NAME ).First().CharValue;
			
		if (_odooClientVersion == $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}")
			return true;

		if ( MessageBox.Show( $"Latest version: {version.Major}.{version.Minor}.{version.Build}.{version.Revision} doesn't match odoo version: {_odooClientVersion}\n" +
		                      $"Would you like to download the latest version?",
			    "Versions",
			    MessageBox.MessageBoxType.YesNoCancel ) == MessageBox.DialogResult.Yes )
		{
			UpdaterProcess( );
		}

		return false;
	}
	public static bool EnsureUpdated()
	{
		var info = CurrentVersion();
		return IsCorrectOdooVersion(info);
	}
	public static void UpdaterProcess( )
	{
		try
		{
			MessageBox.Show($"Opening {PUBLISH_URL}");
			Process proc = Process.Start( PUBLISH_URL );
		}
		catch
		{
			Debug.WriteLine( "Failed to open download link.." );
		}
	}
}