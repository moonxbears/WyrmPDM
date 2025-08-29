using System.Runtime.Versioning;

using Windows.Storage;


namespace HackPDM.Src.Helper.Compatibility
{
    [SupportedOSPlatform("windows10.0.17763.0")]
    public class ModernSettingsProvider : ISettingsProvider
    {
        private static ApplicationDataContainer Settings => ApplicationData.Current.LocalSettings;

        public T? Get<T>(string key, T defaultValue = default)
        {
            return Settings.Values.TryGetValue(key, out var value) && value is T typed
                ? typed
                : defaultValue;
        }

        public void Set<T>(string key, T? value)
        {
            Settings.Values[key] = value;
        }
    }

}
