namespace DirectorySync
{
    public enum AppTheme
    {
        Light,
        Dark,
        System
    }

    public static class ThemeManager
    {
        public static AppTheme Current { get; private set; } = AppTheme.System;

        // Colors the built-in dark mode doesn't touch (log lines set an explicit Color today).
        public static Color SuccessColor => Current == AppTheme.Dark ? Color.MediumSeaGreen : Color.DarkGreen;
        public static Color ErrorColor => Current == AppTheme.Dark ? Color.Tomato : Color.Red;

        public static void Apply(AppTheme theme)
        {
            Current = theme;

            Application.SetColorMode(theme switch
            {
                AppTheme.Light => SystemColorMode.Classic,
                AppTheme.Dark => SystemColorMode.Dark,
                _ => SystemColorMode.System,
            });

            Properties.Settings.Default.Theme = theme.ToString();
            Properties.Settings.Default.Save();
        }

        public static AppTheme LoadSaved()
        {
            return Enum.TryParse(Properties.Settings.Default.Theme, out AppTheme saved) ? saved : AppTheme.System;
        }
    }
}
