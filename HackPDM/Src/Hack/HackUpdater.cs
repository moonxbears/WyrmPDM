using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using HackPDM.Forms.Helper;
using HackPDM.Odoo;

using static HackPDM.Forms.Helper.MessageBox;

namespace HackPDM.Hack;

internal class HackUpdater
{
	const long REPO_ID = 28426033L;
	const string BRANCH_NAME = "justinOdooIntegration";
	const string PUBLISH_URL = "\\\\freedom\\Engineering\\hackpdm\\setup.exe";

	private static string _odooClientVersion;

	private static Version CurrentVersion()
	{
		return Assembly.GetExecutingAssembly().GetName().Version;
	}
	private static bool IsCorrectOdooVersion(Version version)
	{
		_odooClientVersion = OdooDefaults.HpSettings.Where( s => s.Name == OdooDefaults.ODOO_VERSION_KEY_NAME ).First().CharValue;
			
		if (_odooClientVersion == $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}")
			return true;

		if ( MessageBox.Show( $"Latest version: {version.Major}.{version.Minor}.{version.Build}.{version.Revision} doesn't match odoo version: {_odooClientVersion}\n" +
		                      $"Would you like to download the latest version?",
			    "Versions",
			    MessageBoxType.YesNoCancel ) == DialogResult.Yes )
		{
			UpdaterProcess( );
		}

		return false;
	}
	public static bool EnsureUpdated()
	{
		var info = CurrentVersion();
		//var ghBranch = await GetBranchRepo(repoID, branchName);
		//var taskSync = GetReleasesAsync(repoID);
		//taskSync.Wait();
		//var ghReleases = taskSync.Result;

		//if ( ghReleases.Count == 0 )
		//{
		//	MessageBox.Show( "No releases found on GitHub" );
		//	return;
		//}

		//if (!IsLatestVersion(ghReleases[0], info))
		//{
		//	if (MessageBox.Show($"Latest version: {ghReleases[0].TagName}, doesn't match your version: {info}\n" +
		//	 $"Would you like to download the latest version?",
		//	 "Versions",
		//	 MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
		//	{
		//		UpdaterProcess(ghReleases[0]);
		//	}
		//	throw new Exception("Update to latest version");
		//}

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