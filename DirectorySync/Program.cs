/*
The MIT License (MIT)

Copyright (c) 2017 Roger Hill

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files
(the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge,
publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System.Diagnostics;

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

            // Defense-in-depth: prevents an exception from a forgotten/unawaited Task from
            // ever reaching the process as a last-resort crash. The actual file-operation
            // error handling lives in frmMain, right at each risky call — this is only a
            // safety net for anything unexpected that slips past it.
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;

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
            MessageBox.Show($"Error caught: {ex}", "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void TaskSchedulerOnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            Trace.WriteLine($"Unobserved task exception: {e.Exception}");
        }
    }
}