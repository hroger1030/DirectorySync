namespace DirectorySync
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += ApplicationThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            ApplicationConfiguration.Initialize();
            Application.Run(new frmMain());
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            MessageBox.Show($"Fatal error caught: {ex}", "Fatal Domain Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
        {
            var ex = (Exception)e.Exception;
            MessageBox.Show($"Fatal error caught: {ex}", "Fatal THread Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}