using System.Collections.Generic;
using System.Drawing;
using System.Text;

using HackPDM.Properties;

using Microsoft.UI.Xaml.Media;

using Windows.Storage;
using Windows.UI;

using Color = Windows.UI.Color;


namespace HackPDM.Src
{
    public static class StorageBox
    {
    #region Application Settings
        public const string APP_NAME = "HackPDM";
        public const string APP_VERSION = "1.0.0";
        public const string APP_DEVELOPER = "Justin";
        public static string PWAPathAbsolute
        {
            get => Settings.Get<string>("PWAPathAbsolute");
            set => Settings.Set("PWAPathAbsolute", value);
        }
    #endregion
    #region Profile Manager
        public const int PROFILE_MANAGER_WIDTH = 600;
        public const int PROFILE_MANAGER_HEIGHT = 200;
    #endregion
    #region Message Box
        public const int MESSAGE_BOX_WIDTH = 400;
        public const int MESSAGE_BOX_HEIGHT = 200;
        public const string MESSAGE_BOX_TITLE = "Info";
        public const string MESSAGE_BOX_OK = "OK";
        public const string MESSAGE_BOX_CANCEL = "Cancel";
        public const string MESSAGE_BOX_YES = "Yes";
        public const string MESSAGE_BOX_NO = "No";
        public const string MESSAGE_BOX_CONTENT = "";
    #endregion
    #region HackFileManager
        public const int HACK_FILE_MANAGER_WIDTH = 1280;
        public const int HACK_FILE_MANAGER_HEIGHT = 720;
        public const string HACK_FILE_MANAGER_TITLE = "Hack File Manager - HackPDM";
        public const string EMPTY_PLACEHOLDER = "-";
		#endregion
	#region OdooDefaults
        public const string DEFAULT_ODOO_CREDENTIALS = "HackPDM-OdooUser";
		#endregion
	#region Color Settings
        public static readonly Color WHITE              = Color.FromArgb(255, 255, 255, 255);
        public static readonly Color BLACK              = Color.FromArgb(255, 0, 0, 0);
        public static readonly Color LIGHT_GRAY         = Color.FromArgb(255, 211, 211, 211);
        public static readonly Color GRAY               = Color.FromArgb(255, 128, 128, 128);
        public static readonly Color DARK_GRAY          = Color.FromArgb(255, 64, 64, 64);

		public static readonly SolidColorBrush BRUSH_WHITE      = new(WHITE);
		public static readonly SolidColorBrush BRUSH_LIGHT_GRAY = new(LIGHT_GRAY);
		public static readonly SolidColorBrush BRUSH_GRAY       = new(GRAY);
		public static readonly SolidColorBrush BRUSH_DARK_GRAY  = new(DARK_GRAY);
		public static readonly SolidColorBrush BRUSH_BLACK      = new(BLACK);
	#endregion
	#region
	    public const string ASSETSPREFIX    = "ms-appx:///";
	    public const string LOCALPREFIX     = "ms-appdata:///local";
        public const string ASSETSFOLDER    = "Assets";
		public const string IMAGEFOLDER     = "Images";
        public const string EXTENSIONFOLDER = "ExtensionIcons";
        public const string FOLDERICONS     = "FolderIcons";
        public const string STATUSFOLDER    = "StatusIcons";
	#endregion
	}



}
