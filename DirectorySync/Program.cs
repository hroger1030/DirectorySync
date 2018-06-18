using System;
using System.Threading;
using System.Windows.Forms;

namespace DirectoryCopy
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += ApplicationThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

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
