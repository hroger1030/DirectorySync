using System.Configuration;

namespace DirectorySync.Properties
{
    internal sealed class Settings : ApplicationSettingsBase
    {
        private static readonly Settings _Default = (Settings)Synchronized(new Settings());

        public static Settings Default => _Default;

        [UserScopedSetting]
        [DefaultSettingValue("System")]
        public string Theme
        {
            get => (string)this[nameof(Theme)];
            set => this[nameof(Theme)] = value;
        }
    }
}
