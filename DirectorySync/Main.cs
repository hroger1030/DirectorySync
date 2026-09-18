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
using System.Text;

namespace DirectorySync
{
    public partial class frmMain : Form
    {
        private static readonly int MAX_PATH = 260;
        private const int PROGRESS_BAR_SCALE = 10000;
        private static readonly ParallelOptions _ParallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 4 };
        private readonly object _LogLock = new object();
        private readonly object _ProgressLock = new object();
        protected StringBuilder _MessageLog;
        protected bool _TestMode;
        protected volatile bool _HaltProcessing;
        protected Form _MainForm;
        protected Stopwatch _Timer;
        protected Stopwatch _SyncTimer;
        protected Stopwatch _ProgressTimer;
        private long _FilesCopied;
        private long _BytesCopied;
        private long _BytesProcessed;
        private long _FilesRemoved;
        private long _FilesSkipped;
        private long _Errors;
        private long _TotalSourceBytes;

        protected bool ValidatePaths()
        {
            if (!Directory.Exists(txtSourceDir.Text))
            {
                LogImportant("Error: Source directory does not exist.", ThemeManager.ErrorColor);
                return false;
            }

            // The destination itself doesn't need to exist yet — it's created lazily, right
            // before the first file is written into it (see SyncDirectory), and never in Test
            // Mode. We only need SOME existing ancestor, so free space can be checked against
            // the real volume rather than trusting an unverified, possibly-bogus path string.
            try
            {
                FindNearestExistingAncestor(txtDestinationDir.Text);
            }
            catch (DirectoryNotFoundException)
            {
                LogImportant("Error: Destination path is not reachable (no existing parent directory found).", ThemeManager.ErrorColor);
                return false;
            }

            string sourceFull = Path.GetFullPath(txtSourceDir.Text).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string destinationFull = Path.GetFullPath(txtDestinationDir.Text).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (string.Equals(sourceFull, destinationFull, StringComparison.OrdinalIgnoreCase))
            {
                LogImportant("Error: Source directory and destination directory must be different.", ThemeManager.ErrorColor);
                return false;
            }

            if (IsSubdirectoryOf(destinationFull, sourceFull) || IsSubdirectoryOf(sourceFull, destinationFull))
            {
                LogImportant("Error: Source directory and destination directory cannot be nested inside one another.", ThemeManager.ErrorColor);
                return false;
            }

            return true;
        }

        private static bool IsSubdirectoryOf(string candidateChild, string parent)
        {
            string parentWithSeparator = parent + Path.DirectorySeparatorChar;
            return candidateChild.StartsWith(parentWithSeparator, StringComparison.OrdinalIgnoreCase);
        }

        // Walks up from path until it finds a directory that actually exists, without ever
        // creating anything. Used so a not-yet-created destination can still be checked for
        // free space against its real volume, instead of requiring the exact path to exist.
        private static string FindNearestExistingAncestor(string path)
        {
            string? current = Path.GetFullPath(path);

            while (!string.IsNullOrEmpty(current) && !Directory.Exists(current))
                current = Path.GetDirectoryName(current);

            if (string.IsNullOrEmpty(current))
                throw new DirectoryNotFoundException($"No existing parent directory could be found for '{path}'.");

            return current;
        }

        protected void RemoveFileAttributes(string directory, bool recursive)
        {
            var directoryInfo = new DirectoryInfo(directory);

            if (directoryInfo.Attributes != FileAttributes.Normal)
                directoryInfo.Attributes = FileAttributes.Normal;

            string[] files = Directory.GetFiles(directory);

            foreach (string file in files)
                File.SetAttributes(file, FileAttributes.Normal);

            if (recursive)
            {
                string[] directories = Directory.GetDirectories(directory);

                foreach (string subdirectory in directories)
                    RemoveFileAttributes(subdirectory, recursive);
            }
        }

        protected static (int FileCount, long TotalBytes) GetDirectoryStats(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Directory path cannot be null or empty");

            if (!Directory.Exists(directoryPath))
                throw new ArgumentException($"Directory '{directoryPath}' does not exist");

            int fileCount = 0;
            long totalBytes = 0;

            void Walk(DirectoryInfo directoryInfo)
            {
                FileInfo[] files;
                DirectoryInfo[] subdirectories;

                try
                {
                    files = directoryInfo.GetFiles();
                    subdirectories = directoryInfo.GetDirectories();
                }
                catch (Exception)
                {
                    // Can't enumerate this directory (e.g. permission denied) — skip it for
                    // this size/count estimate. The real sync will hit and report the same
                    // problem, per-item, when it actually gets there.
                    return;
                }

                foreach (var fileInfo in files)
                {
                    Interlocked.Increment(ref fileCount);
                    Interlocked.Add(ref totalBytes, fileInfo.Length);
                }

                Parallel.ForEach(subdirectories, _ParallelOptions, Walk);
            }

            Walk(new DirectoryInfo(directoryPath));

            return (fileCount, totalBytes);
        }

        // Enumeration can fail just like any other file-system access (permission denied,
        // path too long, device unavailable, ...). These let a single unreadable directory
        // get logged and skipped instead of aborting everything above it in the tree.
        protected string[] TryGetFiles(string directory)
        {
            try
            {
                return Directory.GetFiles(directory);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _Errors);
                LogImportant($"Error listing files in '{directory}': {ex.Message}", ThemeManager.ErrorColor);
                return Array.Empty<string>();
            }
        }

        protected string[] TryGetDirectories(string directory)
        {
            try
            {
                return Directory.GetDirectories(directory);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _Errors);
                LogImportant($"Error listing subdirectories in '{directory}': {ex.Message}", ThemeManager.ErrorColor);
                return Array.Empty<string>();
            }
        }

        protected async Task SyncDirectory(string sourceDirectory, string destinationDirectory)
        {
            if (ValidatePaths())
            {
                _Timer.Restart();

                LogMesage("Calculating avaialble space...", true);

                // The destination may not exist yet (it's created lazily, on write) — check free
                // space against the nearest existing ancestor's volume instead of requiring the
                // exact path to be there, and treat a not-yet-created destination as empty.
                string existingDestinationAncestor = FindNearestExistingAncestor(destinationDirectory);
                var driveRoot = Path.GetPathRoot(existingDestinationAncestor);

                var sourceStats = GetDirectoryStats(sourceDirectory);
                var destinationStats = Directory.Exists(destinationDirectory)
                    ? GetDirectoryStats(destinationDirectory)
                    : (FileCount: 0, TotalBytes: 0L);
                long freeSpace = GetTotalFreeSpace(driveRoot!);

                if (sourceStats.TotalBytes > (destinationStats.TotalBytes + freeSpace))
                {
                    LogImportant($"Cannot copy files, not enought space avaialble on '{driveRoot}'", ThemeManager.ErrorColor);
                    return;
                }

                ResetProgress(sourceStats.TotalBytes);

                LogMesage("Sufficent space avaialble, beginning Synchronization.", true);

                await Task.Run(() =>
                {
                    SyncDirectory(sourceDirectory, destinationDirectory, sourceDirectory, destinationDirectory);
                });

                if (_HaltProcessing)
                {
                    LogMesage("Synchronization halted.", true);
                }
                else
                {
                    LogImportant("Synchronization complete.", ThemeManager.SuccessColor);
                    SetProgressValue(PROGRESS_BAR_SCALE);
                }

                _Timer.Stop();
            }
        }

        protected void SyncDirectory(string sourceRoot, string destinationRoot, string sourceDirectory, string destinationDirectory)
        {
            if (_HaltProcessing == true)
                return;

            LogMesage($"Starting sync of directory {sourceDirectory}.");

            if (!Directory.Exists(destinationDirectory))
            {
                LogMesage("Creating " + destinationDirectory);

                if (!_TestMode)
                {
                    try
                    {
                        Directory.CreateDirectory(destinationDirectory);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref _Errors);
                        LogImportant($"Error creating directory '{destinationDirectory}': {ex.Message}", ThemeManager.ErrorColor);
                        return;
                    }
                }
            }

            // remove non-existing files. Look at each file in the destination directory and if a
            // matching file does not exist in the source directory remove it.
            if (Directory.Exists(destinationDirectory))
            {
                Parallel.ForEach(TryGetFiles(destinationDirectory), _ParallelOptions, (file, state) =>
                {
                    if (_HaltProcessing)
                    {
                        state.Stop();
                        return;
                    }

                    try
                    {
                        string sourceFilename = Path.Combine(sourceDirectory, Path.GetFileName(file));

                        if (!File.Exists(sourceFilename))
                        {
                            if (!_TestMode)
                            {
                                var fileAttributes = File.GetAttributes(file);

                                if (fileAttributes != FileAttributes.Normal)
                                    File.SetAttributes(file, FileAttributes.Normal);

                                File.Delete(file);
                                Interlocked.Increment(ref _FilesRemoved);
                            }

                            LogMesage("Deleting " + file);
                        }
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref _Errors);
                        LogImportant($"Error deleting '{file}': {ex.Message}", ThemeManager.ErrorColor);
                    }
                });
            }

            // copy files.
            // If the file exists and doesn't match the last write time, overwrite
            if (Directory.Exists(sourceDirectory))
            {
                Parallel.ForEach(TryGetFiles(sourceDirectory), _ParallelOptions, (item, state) =>
                {
                    if (_HaltProcessing)
                    {
                        state.Stop();
                        return;
                    }

                    string destinationFilename = Path.Combine(destinationDirectory, Path.GetFileName(item));
                    long itemLength = 0;

                    try
                    {
                        itemLength = new FileInfo(item).Length;

                        if (destinationFilename.Length > MAX_PATH)
                            LogMesage($"Path '{destinationFilename}' is {destinationFilename.Length} characters long. This exceeds the {MAX_PATH} character limit.");

                        if (File.Exists(destinationFilename))
                        {
                            if (File.GetLastWriteTime(item) != File.GetLastWriteTime(destinationFilename))
                            {
                                if (!_TestMode)
                                {
                                    // Make sure we are clear for move.

                                    var fileAttributes = File.GetAttributes(destinationFilename);

                                    if (fileAttributes != FileAttributes.Normal)
                                        File.SetAttributes(destinationFilename, FileAttributes.Normal);

                                    File.Copy(item, destinationFilename, true);
                                }

                                Interlocked.Increment(ref _FilesCopied);
                                Interlocked.Add(ref _BytesCopied, itemLength);
                                LogMesage("Updating out of date file " + destinationFilename);
                            }
                            else
                            {
                                Interlocked.Increment(ref _FilesSkipped);
                            }
                        }
                        else
                        {
                            if (!_TestMode)
                                File.Copy(item, destinationFilename);

                            Interlocked.Increment(ref _FilesCopied);
                            Interlocked.Add(ref _BytesCopied, itemLength);
                            LogMesage("Copying " + destinationFilename);
                        }
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref _Errors);
                        LogImportant($"Error copying '{item}' to '{destinationFilename}': {ex.Message}", ThemeManager.ErrorColor);
                    }

                    // Track progress by bytes of source examined (copied, skipped, or failed), not
                    // file count — a byte-weighted ratio reflects actual completion far better than
                    // "N of M files" when file sizes vary wildly, and a failed file still needs to
                    // count so the bar doesn't stall waiting for bytes that will never land.
                    AddProgress(itemLength);
                });
            }

            // remove any directories that exist in the destination but not in the source
            // need to check for existance beacuse in test mode, new directories will not be created.
            if (Directory.Exists(destinationDirectory))
            {
                foreach (string directory in TryGetDirectories(destinationDirectory))
                {
                    if (_HaltProcessing)
                        break;

                    string source = Path.Combine(sourceRoot, Path.GetRelativePath(destinationRoot, directory));

                    if (!Directory.Exists(source))
                    {
                        LogMesage("Deleting " + directory);

                        if (!_TestMode)
                        {
                            try
                            {
                                var di = new DirectoryInfo(directory);
                                RemoveFileAttributes(directory, true);
                                di.Delete(true);
                            }
                            catch (Exception ex)
                            {
                                Interlocked.Increment(ref _Errors);
                                LogImportant($"Error deleting directory '{directory}': {ex.Message}", ThemeManager.ErrorColor);
                            }
                        }
                    }
                }
            }

            var directoryList = TryGetDirectories(sourceDirectory);

            Parallel.ForEach(directoryList, _ParallelOptions, (currentDirectory, state) =>
            {
                if (_HaltProcessing)
                {
                    state.Stop();
                    return;
                }

                var destination = Path.Combine(destinationRoot, Path.GetRelativePath(sourceRoot, currentDirectory));
                SyncDirectory(sourceRoot, destinationRoot, currentDirectory, destination);
            });

            LogMesage($"Sync of directory {sourceDirectory} complete.", true);
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

        protected static long GetTotalFreeSpace(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            // Use GetDiskFreeSpaceEx (rather than DriveInfo) so this works for both local
            // drive letters and UNC network share paths.
            if (!GetDiskFreeSpaceEx(path, out ulong freeBytesAvailable, out _, out _))
                throw new IOException($"Unable to determine free space for '{path}'. Win32 error {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}.");

            return (long)freeBytesAvailable;
        }

        protected void ResetProgress(long totalSourceBytes)
        {
            _TotalSourceBytes = totalSourceBytes;
            Interlocked.Exchange(ref _BytesProcessed, 0);
            _ProgressTimer.Reset();

            SetProgressValue(0);
        }

        protected void AddProgress(long bytes)
        {
            Interlocked.Add(ref _BytesProcessed, bytes);

            // Decide whether to flush, and compute the value to show, entirely under the lock
            // (cheap, safe from any thread). Only the actual UI write below gets marshaled,
            // and only when a flush is actually due — most calls return here for free.
            int? progressValue = null;

            lock (_ProgressLock)
            {
                if (!_ProgressTimer.IsRunning || _ProgressTimer.ElapsedMilliseconds > 200)
                {
                    long processed = Interlocked.Read(ref _BytesProcessed);
                    double ratio = _TotalSourceBytes > 0 ? Math.Min(1.0, processed / (double)_TotalSourceBytes) : 1.0;
                    progressValue = (int)(ratio * PROGRESS_BAR_SCALE);

                    _ProgressTimer.Restart();
                }
            }

            if (progressValue != null)
                SetProgressValue(progressValue.Value);
        }

        protected void SetProgressValue(int value)
        {
            if (pbFiles.InvokeRequired)
            {
                Invoke(new Action<int>(SetProgressValue), value);
                return;
            }

            pbFiles.Value = value;
            lblProgressPercent.Text = (value / 100.0).ToString("0.0") + "%";
            UpdateStatistics();
        }

        protected void LogMesage(string message)
        {
            LogMesage(message, false);
        }

        protected void LogMesage(string message, bool flushLog)
        {
            // Mutate the shared buffer directly (guarded by a lock, since multiple sync
            // worker threads can call this concurrently) instead of forcing every call
            // through a blocking UI-thread hop. Only marshal to the UI thread when a
            // batch is actually ready to flush, which is the rare case, not the common one.
            string? batch = null;

            lock (_LogLock)
            {
                _MessageLog.AppendLine($"<{DateTime.Now.ToLongTimeString()}> {message}");

                if (!_Timer.IsRunning || _Timer.ElapsedMilliseconds > 5000 || flushLog)
                {
                    batch = _MessageLog.ToString();
                    _MessageLog.Clear();
                    _Timer.Restart();
                }
            }

            if (batch == null)
                return;

            if (txtLog.InvokeRequired)
                Invoke(new Action<string>(FlushLogBatch), batch);
            else
                FlushLogBatch(batch);
        }

        protected void FlushLogBatch(string batch)
        {
            txtLog.AppendText(batch);
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        // For messages that must appear immediately (bypassing the normal batching) and
        // stand out visually, e.g. a cancel notice. Flushes whatever's already pending first
        // so ordering in the log stays correct, then appends this line in the given color.
        protected void LogImportant(string message, Color color)
        {
            string coloredLine = $"<{DateTime.Now.ToLongTimeString()}> {message}" + Environment.NewLine;
            string? pendingBatch;

            lock (_LogLock)
            {
                pendingBatch = _MessageLog.Length > 0 ? _MessageLog.ToString() : null;
                _MessageLog.Clear();
                _Timer.Restart();
            }

            if (txtLog.InvokeRequired)
                Invoke(new Action<string?, string, Color>(FlushImportant), pendingBatch, coloredLine, color);
            else
                FlushImportant(pendingBatch, coloredLine, color);
        }

        protected void FlushImportant(string? pendingBatch, string coloredLine, Color color)
        {
            if (!string.IsNullOrEmpty(pendingBatch))
                txtLog.AppendText(pendingBatch);

            int start = txtLog.TextLength;
            txtLog.AppendText(coloredLine);
            txtLog.Select(start, coloredLine.Length);
            txtLog.SelectionColor = color;

            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionLength = 0;
            txtLog.SelectionColor = txtLog.ForeColor;

            txtLog.ScrollToCaret();
        }

        protected void ClearLog()
        {
            lock (_LogLock)
            {
                _MessageLog.Clear();
            }

            txtLog.Clear();
        }

        protected void SetTestMode()
        {
            _TestMode = chkTestMode.Checked;

            if (_TestMode)
                LogMesage("Test mode is ON: No updates will be made.");
            else
                LogMesage("Test mode is OFF: Updates will be made normally.");
        }


        public frmMain(string? initialSourceDir = null)
        {
            InitializeComponent();

            Text = "Jolly Roger's Directory Sync";
            btnSync.Enabled = false;
            btnCancel.Enabled = false;

            _MainForm = this;
            _Timer = new Stopwatch();
            _SyncTimer = new Stopwatch();
            _ProgressTimer = new Stopwatch();
            _MessageLog = new StringBuilder();

            pbFiles.Maximum = PROGRESS_BAR_SCALE;

            ThemeManager.Apply(ThemeManager.LoadSaved());
            UpdateThemeMenuChecks();

            LogImportant("Jolly Roger's Directory Sync, Version " + Application.ProductVersion, ThemeManager.SuccessColor);
            LogImportant("Please select a source and destination directory. Any content in the destination directory will be updated to match that of the source directory.", ThemeManager.SuccessColor);

            SetTestMode();

            if (!string.IsNullOrEmpty(initialSourceDir) && Directory.Exists(initialSourceDir))
                txtSourceDir.Text = initialSourceDir;
        }

        private void ApplyTheme(AppTheme theme)
        {
            ThemeManager.Apply(theme);

            // Native dark-mode theming (titlebar, control chrome) is applied on handle creation,
            // so an already-open form needs its handle recreated to pick up a change made at runtime.
            RecreateHandle();

            UpdateThemeMenuChecks();
        }

        private void UpdateThemeMenuChecks()
        {
            mnuThemeLight.Checked = ThemeManager.Current == AppTheme.Light;
            mnuThemeDark.Checked = ThemeManager.Current == AppTheme.Dark;
            mnuThemeSystem.Checked = ThemeManager.Current == AppTheme.System;
        }

        private void mnuExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void mnuAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Jolly Roger's Directory Sync, Version " + Application.ProductVersion + Environment.NewLine + "Written by Roger Hill, 2011",
                "About",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void mnuThemeLight_Click(object sender, EventArgs e)
        {
            ApplyTheme(AppTheme.Light);
        }

        private void mnuThemeDark_Click(object sender, EventArgs e)
        {
            ApplyTheme(AppTheme.Dark);
        }

        private void mnuThemeSystem_Click(object sender, EventArgs e)
        {
            ApplyTheme(AppTheme.System);
        }

        private async void btnSync_Click(object sender, EventArgs e)
        {
            _HaltProcessing = false;
            ClearLog();
            ResetStatistics();
            _SyncTimer.Restart();
            btnCancel.Enabled = true;

            try
            {
                await Task.Run(async () =>
                {
                    await SyncDirectory(txtSourceDir.Text, txtDestinationDir.Text);
                });
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _Errors);
                LogImportant("Error processing : " + ex.Message, ThemeManager.ErrorColor);
                LogMesage("Halting synchronization.");
            }
            finally
            {
                _SyncTimer.Stop();
                UpdateStatistics();
                btnCancel.Enabled = false;
            }
        }

        protected void ResetStatistics()
        {
            Interlocked.Exchange(ref _FilesCopied, 0);
            Interlocked.Exchange(ref _BytesCopied, 0);
            Interlocked.Exchange(ref _FilesRemoved, 0);
            Interlocked.Exchange(ref _FilesSkipped, 0);
            Interlocked.Exchange(ref _Errors, 0);
            UpdateStatistics();
        }

        protected void UpdateStatistics()
        {
            if (lblFilesCopiedValue.InvokeRequired)
            {
                Invoke(new Action(UpdateStatistics));
                return;
            }

            double megabytesCopied = Interlocked.Read(ref _BytesCopied) / (1024d * 1024d);
            double elapsedSeconds = Math.Max(_SyncTimer.Elapsed.TotalSeconds, 0.001d);
            double megabytesPerSecond = megabytesCopied / elapsedSeconds;

            lblFilesCopiedValue.Text = Interlocked.Read(ref _FilesCopied).ToString("N0");
            lblMegabytesCopiedValue.Text = megabytesCopied.ToString("N2");
            lblMegabytesPerSecondValue.Text = megabytesPerSecond.ToString("N2");
            lblFilesRemovedValue.Text = Interlocked.Read(ref _FilesRemoved).ToString("N0");
            lblFilesSkippedValue.Text = Interlocked.Read(ref _FilesSkipped).ToString("N0");
            lblErrorsValue.Text = Interlocked.Read(ref _Errors).ToString("N0");
            lblTotalTimeValue.Text = _SyncTimer.Elapsed.ToString(@"hh\:mm\:ss");
        }

        private void btnSelectSource_Click(object sender, EventArgs e)
        {
            var folderPicker = new FolderBrowserDialog
            {
                RootFolder = Environment.SpecialFolder.MyComputer,
                Description = "Please select the source folder."
            };

            if (txtSourceDir.Text != string.Empty)
                folderPicker.SelectedPath = txtSourceDir.Text;

            DialogResult result = folderPicker.ShowDialog();

            if (result == DialogResult.OK)
                txtSourceDir.Text = folderPicker.SelectedPath;
        }

        private void btnDestination_Click(object sender, EventArgs e)
        {
            var folderPicker = new FolderBrowserDialog
            {
                RootFolder = Environment.SpecialFolder.MyComputer,
                Description = "Please select the destination folder."
            };

            DialogResult result = folderPicker.ShowDialog();

            if (result == DialogResult.OK)
                this.txtDestinationDir.Text = folderPicker.SelectedPath;
        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            ClearLog();
            LogMesage("Log Cleared.");
            pbFiles.Value = 0;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (!_HaltProcessing)
            {
                _HaltProcessing = true;
                btnCancel.Enabled = false;
                LogImportant("Cancel requested... completing current operations and terminating.", ThemeManager.ErrorColor);
            }
        }

        private void txtSourceDir_TextChanged(object sender, EventArgs e)
        {
            bool readyToSync = (txtDestinationDir.Text.Length > 0) && (txtSourceDir.Text.Length > 0);

            this.btnSync.Enabled = readyToSync;
        }

        private void txtDestinationDir_TextChanged(object sender, EventArgs e)
        {
            bool readyToSync = (txtDestinationDir.Text.Length > 0) && (txtSourceDir.Text.Length > 0);

            this.btnSync.Enabled = readyToSync;
        }

        private void chkTestMode_CheckedChanged(object sender, EventArgs e)
        {
            SetTestMode();
        }
    }
}
