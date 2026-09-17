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
        protected delegate void SetTextCallback(string text, bool flushLog);
        protected delegate void UpdateProgressBar(int amount);

        private static readonly int MAX_PATH = 260;
        protected StringBuilder _MessageLog;
        protected bool _TestMode;
        protected volatile bool _HaltProcessing;
        protected Form _MainForm;
        protected Stopwatch _Timer;
        protected Stopwatch _SyncTimer;
        private long _FilesCopied;
        private long _BytesCopied;
        private long _FilesRemoved;
        private long _FilesSkipped;

        protected bool ValidatePaths()
        {
            if (!Directory.Exists(txtSourceDir.Text))
            {
                LogMesage("Error: Source directory does not exist.");
                return false;
            }

            if (!Directory.Exists(txtDestinationDir.Text))
            {
                LogMesage("Error: Destination directory does not exist.");
                return false;
            }

            string sourceFull = Path.GetFullPath(txtSourceDir.Text).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string destinationFull = Path.GetFullPath(txtDestinationDir.Text).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (string.Equals(sourceFull, destinationFull, StringComparison.OrdinalIgnoreCase))
            {
                LogMesage("Error: Source directory and destination directory must be different.");
                return false;
            }

            if (IsSubdirectoryOf(destinationFull, sourceFull) || IsSubdirectoryOf(sourceFull, destinationFull))
            {
                LogMesage("Error: Source directory and destination directory cannot be nested inside one another.");
                return false;
            }

            return true;
        }

        private static bool IsSubdirectoryOf(string candidateChild, string parent)
        {
            string parentWithSeparator = parent + Path.DirectorySeparatorChar;
            return candidateChild.StartsWith(parentWithSeparator, StringComparison.OrdinalIgnoreCase);
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

        protected static int CountFiles(string startingDirectory)
        {
            if (string.IsNullOrWhiteSpace(startingDirectory))
                throw new ArgumentException("Directory path cannot be null or empty");

            if (!Directory.Exists(startingDirectory))
                throw new ArgumentException($"Directory {startingDirectory} does not exist");

            int count = 0;
            var workQueue = new Queue<string>();

            workQueue.Enqueue(startingDirectory);

            while (workQueue.Count > 0)
            {
                string currentFiretory = workQueue.Dequeue();

                var buffer = Directory.GetDirectories(currentFiretory);

                foreach (var subdirectory in buffer)
                    workQueue.Enqueue(subdirectory);

                count += Directory.GetFiles(currentFiretory).Length;
            }

            return count;
        }

        protected async Task SyncDirectory(string sourceDirectory, string destinationDirectory)
        {
            if (ValidatePaths())
            {
                _Timer.Restart();

                LogMesage("Calculating avaialble space...", true);

                var driveRoot = Path.GetPathRoot(destinationDirectory);

                long sourceSize = GetTotalDirectorySize(sourceDirectory);
                long destinationSize = GetTotalDirectorySize(destinationDirectory);
                long freeSpace = GetTotalFreeSpace(driveRoot!);

                if (sourceSize > (destinationSize + freeSpace))
                {
                    LogMesage($"Cannot copy files, not enought space avaialble on '{driveRoot}'", true);
                    return;
                }

                LogMesage("Sufficent space avaialble, beginning Synchronization.", true);

                await Task.Run(() =>
                {
                    SyncDirectory(sourceDirectory, destinationDirectory, sourceDirectory, destinationDirectory);
                });

                if (_HaltProcessing)
                    LogMesage("Synchronization halted.", true);
                else
                    LogMesage("Synchronization complete.", true);

                _Timer.Stop();
            }
        }

        protected void SyncDirectory(string sourceRoot, string destinationRoot, string sourceDirectory, string destinationDirectory)
        {
            if (_HaltProcessing == true)
                return;

            int fileCount = 0;

            LogMesage($"Starting sync of directory {sourceDirectory}.");

            if (!Directory.Exists(destinationDirectory))
            {
                LogMesage("Creating " + destinationDirectory);

                if (!_TestMode)
                    Directory.CreateDirectory(destinationDirectory);
            }

            // remove non-existing files. Look at each file in the destination directory and if a 
            // matching file does not exist in the source directory remove it.
            if (Directory.Exists(destinationDirectory))
            {
                foreach (string file in Directory.GetFiles(destinationDirectory))
                {
                    string sourceFilename = sourceDirectory + "\\" + Path.GetFileName(file);

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
            }

            // copy files.  
            // If the file exists and doesn't match the last write time, overwrite                
            if (Directory.Exists(sourceDirectory))
            {
                foreach (string item in Directory.GetFiles(sourceDirectory))
                {
                    string destinationFilename = destinationDirectory + "\\" + Path.GetFileName(item);

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
                            Interlocked.Add(ref _BytesCopied, new FileInfo(item).Length);
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
                        Interlocked.Add(ref _BytesCopied, new FileInfo(item).Length);
                        LogMesage("Copying " + destinationFilename);
                    }

                    fileCount++;
                }
            }

            // remove any directories that exist in the destination but not in the source
            // need to check for existance beacuse in test mode, new directories will not be created.
            if (Directory.Exists(destinationDirectory))
            {
                foreach (string directory in Directory.GetDirectories(destinationDirectory))
                {
                    string source = Path.Combine(sourceRoot, Path.GetRelativePath(destinationRoot, directory));

                    if (!Directory.Exists(source))
                    {
                        LogMesage("Deleting " + directory);

                        if (!_TestMode)
                        {
                            var di = new DirectoryInfo(directory);
                            RemoveFileAttributes(directory, true);
                            di.Delete(true);
                        }
                    }
                }
            }

            string[] directoryList = Directory.GetDirectories(sourceDirectory);

            //// call function recursively on subdirectories
            //foreach (var item in directoryList)
            //{
            //    string destination = destinationRoot + item.Replace(sourceRoot, string.Empty);
            //    SyncDirectory(sourceRoot, destinationRoot ,item, destination);
            //}

            Parallel.ForEach(directoryList, new ParallelOptions { MaxDegreeOfParallelism = 4 }, currentDirectory =>
            {
                string destination = Path.Combine(destinationRoot, Path.GetRelativePath(sourceRoot, currentDirectory));
                SyncDirectory(sourceRoot, destinationRoot, currentDirectory, destination);
            });

            LogMesage($"Sync of directory {sourceDirectory} complete.", true);
            UpdateProgress(fileCount);
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

        protected long GetTotalDirectorySize(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Directory path cannot be null or empty");

            if (!Directory.Exists(directoryPath))
                throw new ArgumentException($"Directory '{directoryPath}' does not exist");

            var directoryInfo = new DirectoryInfo(directoryPath);
            return GetDirectorySize(directoryInfo, true);
        }

        protected long GetDirectorySize(DirectoryInfo directoryInfo, bool recursive = true)
        {
            if (directoryInfo == null)
                throw new ArgumentNullException(nameof(directoryInfo));

            long totalSize = 0;

            foreach (var fileInfo in directoryInfo.GetFiles())
                Interlocked.Add(ref totalSize, fileInfo.Length);

            if (recursive)
                Parallel.ForEach(directoryInfo.GetDirectories(), (subDirectory) => Interlocked.Add(ref totalSize, GetDirectorySize(subDirectory, recursive)));

            return totalSize;
        }

        protected void UpdateProgress(int amount)
        {
            if (pbFiles.InvokeRequired)
            {
                var delegateCall = new UpdateProgressBar(UpdateProgress);
                Invoke(delegateCall, new object[] { amount });
            }
            else
            {
                pbFiles.Step = amount;
                pbFiles.PerformStep();

                UpdateStatistics();

                Console.WriteLine("Amount: " + amount.ToString());
            }
        }

        protected void LogMesage(string message)
        {
            LogMesage(message, false);
        }

        protected void LogMesage(string message, bool flushLog)
        {
            // only update UI every several seconds

            if (txtLog.InvokeRequired)
            {
                var delegateCall = new SetTextCallback(LogMesage);
                Invoke(delegateCall, new object[] { message, flushLog });
            }
            else
            {
                _MessageLog.AppendLine($"<{DateTime.Now.ToLongTimeString()}> {message}");

                if (!_Timer.IsRunning)
                {
                    txtLog.Text = _MessageLog.ToString();
                }
                else if (_Timer.ElapsedMilliseconds > 5000 || flushLog)
                {
                    txtLog.Text = _MessageLog.ToString();

                    _Timer.Restart();
                    _MessageLog.Clear();
                }
            }
        }

        protected void SetTestMode()
        {
            _TestMode = chkTestMode.Checked;

            if (_TestMode)
                LogMesage("Test mode is ON: No updates will be made.");
            else
                LogMesage("Test mode is OFF: Updates will be made normally.");
        }


        public frmMain()
        {
            InitializeComponent();

            Text = "Jolly Roger's Directory Sync";
            btnSync.Enabled = false;
            btnCancel.Enabled = false;
            lblAbout.Text = "Written by Roger Hill, 2011";

            _MainForm = this;
            _Timer = new Stopwatch();
            _SyncTimer = new Stopwatch();
            _MessageLog = new StringBuilder();

            LogMesage("Jolly Roger's Directory Sync, Version " + Application.ProductVersion);
            LogMesage("Please select a source and destination directory. Any content in the destination directory will be updated to match that of the source directory.");

            SetTestMode();
        }

        private async void btnSync_Click(object sender, EventArgs e)
        {
            _HaltProcessing = false;
            ResetStatistics();
            _SyncTimer.Restart();

            try
            {
                pbFiles.Maximum = CountFiles(txtSourceDir.Text);
                pbFiles.Value = 0;

                await Task.Run(async () =>
                {
                    await SyncDirectory(txtSourceDir.Text, txtDestinationDir.Text);
                });
            }
            catch (Exception ex)
            {
                LogMesage("Error processing : " + ex.Message);
                LogMesage("Halting synchronization.");
            }
            finally
            {
                _SyncTimer.Stop();
                UpdateStatistics();
            }
        }

        protected void ResetStatistics()
        {
            Interlocked.Exchange(ref _FilesCopied, 0);
            Interlocked.Exchange(ref _BytesCopied, 0);
            Interlocked.Exchange(ref _FilesRemoved, 0);
            Interlocked.Exchange(ref _FilesSkipped, 0);
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
            _MessageLog.Clear();
            LogMesage("Log Cleared.");
            pbFiles.Value = 0;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (!_HaltProcessing)
            {
                _HaltProcessing = true;
                LogMesage("Attempting to halt synchronization...");
            }
        }

        private void txtSourceDir_TextChanged(object sender, EventArgs e)
        {
            bool readyToSync = (txtDestinationDir.Text.Length > 0) && (txtSourceDir.Text.Length > 0);

            this.btnSync.Enabled = readyToSync;
            this.btnCancel.Enabled = readyToSync;
        }

        private void txtDestinationDir_TextChanged(object sender, EventArgs e)
        {
            bool readyToSync = (txtDestinationDir.Text.Length > 0) && (txtSourceDir.Text.Length > 0);

            this.btnSync.Enabled = readyToSync;
            this.btnCancel.Enabled = readyToSync;
        }

        private void chkTestMode_CheckedChanged(object sender, EventArgs e)
        {
            SetTestMode();
        }
    }
}
