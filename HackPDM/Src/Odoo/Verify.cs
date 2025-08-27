﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HackPDM.Src.Forms.Helper;

using OdooRpcCs;

namespace HackPDM.Verifier
{
	public static class Verify
	{
		public static bool CorrectOdooAddress() => OdooRpcCs.OdooClient.CorrectOdooAddress();
		public static bool CorrectOdooPort() => OdooRpcCs.OdooClient.CorrectOdooPort();

		public static bool VerifySettings()
		{
			List<string> errors = [];

			bool odooLogin = AbleToLogin(ref errors);
			bool hackSettings = CorrectHackSettings(ref errors);

			if (errors.Count > 0)
			{
				MessageBox.Show($"There were problems with:\n\t{string.Join("\n\t", errors)}");
				return false;
			}
			return true;
		}
		public static bool AbleToLogin(ref List<string> errors)
        {
            if (OdooDefaults.OdooID != 0) return true;
			
			if (!CorrectOdooAddress())
			{
				errors.Add("ODOO: invalid odoo address or unreachable host");
			} 
			else if (!CorrectOdooPort())
			{
				errors.Add("ODOO: invalid odoo port or server is down");
			}
			else
			{
				errors.Add("ODOO: invalid odoo credentials and/or database name");
			}
			
			return errors.Count < 1;
		}
		public static bool CorrectHackSettings(ref List<string> errors)
		{
			if ( !Directory.Exists( Properties.Settings.Default["PWAPathAbsolute"] )) errors.Add( "HACK: invalid pwa directory path" );
			if ( !Directory.Exists( Properties.Settings.Default["TemporaryPath"] )) errors.Add( "HACK: invalid temporary directory path" );

			return errors.Count < 1;
		}
	}
}
