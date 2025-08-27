using System.Collections.Generic;
using System.Text;

using HackPDM.Properties;

using Windows.Storage;


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
    #endregion
    }

    public interface ISettingsProvider
    {
        T Get<T>(string key, T defaultValue = default);
        void Set<T>(string key, T value);
    }

}
