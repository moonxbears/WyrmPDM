using System;
using System.Configuration;

using HackPDM.Src;


namespace HackPDM.Properties
{
    internal partial class Settings : ApplicationSettingsBase
    {
        public static ISettingsProvider Provider { get; } = GetProvider();

        private static ISettingsProvider GetProvider()
        {
            var os = Environment.OSVersion.Version;
#pragma warning disable CA1416 // Validate platform compatibility
            return os.Major >= 10
                ? new ModernSettingsProvider()
                : new LegacySettingsProvider();
#pragma warning restore CA1416 // Validate platform compatibility
        }

        public static T? Get<T>(string key, T? defaultValue = default) => Provider.Get(key, defaultValue);
        public static void Set<T>(string key, T value) => Provider.Set(key, value);
    }

}
