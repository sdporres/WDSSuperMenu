using System.Configuration;

namespace WDSSuperMenu.Properties
{
    internal sealed partial class Settings : ApplicationSettingsBase
    {
        private static Settings? defaultInstance = (Settings)Synchronized(new Settings());

        public static Settings Default
        {
            get { return defaultInstance!; }
        }

        [UserScopedSetting()]
        [DefaultSettingValue("true")]
        public bool AutoCheckUpdates
        {
            get { return (bool)this["AutoCheckUpdates"]; }
            set { this["AutoCheckUpdates"] = value; }
        }

        [UserScopedSetting()]
        [DefaultSettingValue("")]
        public string SkippedVersion
        {
            get { return (string)this["SkippedVersion"]; }
            set { this["SkippedVersion"] = value; }
        }

        [UserScopedSetting()]
        [DefaultSettingValue("2000-01-01")]
        public DateTime LastUpdateCheck
        {
            get { return (DateTime)this["LastUpdateCheck"]; }
            set { this["LastUpdateCheck"] = value; }
        }
    }
}