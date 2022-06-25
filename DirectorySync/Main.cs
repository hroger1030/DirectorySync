using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DirectorySync
{
    public partial class frmMain : Form
    {
        protected delegate void SetTextCallback(string text, bool flush_log);
        protected delegate void UpdateProgressBar(int amount);

        private static readonly int MAX_PATH = 260;
        protected StringBuilder _MessageLog;
        protected bool _TestMode;
        protected volatile bool _HaltProcessing;
        protected Form _MainForm;
        protected Stopwatch _Timer;

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

            if (txtSourceDir.Text == txtDestinationDir.Text)
            {
                LogMesage("Error: Source directory and destination directory must be different.");
                return false;
            }

            return true;
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
            var work_queue = new Queue<string>();

            work_queue.Enqueue(startingDirectory);

            while (work_queue.Count > 0)
            {
                string current_firetory = work_queue.Dequeue();

                var buffer = Directory.GetDirectories(current_firetory);

                foreach (var subdirectory in buffer)
                    work_queue.Enqueue(subdirectory);

                count += Directory.GetFiles(current_firetory).Length;
            }

            return count;
        }

        protected async void SyncDirectory(string sourceDirectory, string destinationDirectory)
        {
            if (ValidatePaths())
            {
                _Timer.Restart();

                LogMesage("Calculating avaialble space...", true);

                var drive_root = Path.GetPathRoot(destinationDirectory);

                long source_size = GetTotalDirectorySize(sourceDirectory);
                long destination_size = GetTotalDirectorySize(destinationDirectory);
                long free_space = GetTotalFreeSpace(drive_root!);

                if (source_size > (destination_size + free_space))
                {
                    LogMesage($"Cannot copy files, not enought space avaialble on '{drive_root}'", true);
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

            int file_count = 0;

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
                    string source_filename = sourceDirectory + "\\" + Path.GetFileName(file);

                    if (!File.Exists(source_filename))
                    {
                        if (!_TestMode)
                        {
                            var file_attributes = File.GetAttributes(file);

                            if (file_attributes != FileAttributes.Normal)
                                File.SetAttributes(file, FileAttributes.Normal);

                            File.Delete(file);
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
                    string destination_filename = destinationDirectory + "\\" + Path.GetFileName(item);

                    if (File.Exists(destination_filename))
                    {
                        if (File.GetLastWriteTime(item) != File.GetLastWriteTime(destination_filename))
                        {
                            if (destination_filename.Length > 260)
                                LogMesage($"Path {destination_filename}' is {destination_filename.Length} characters long. This exceeds the {MAX_PATH} character limit.");

                            if (!_TestMode)
                            {
                                // Make sure we are clear for move.

                                var file_attributes = File.GetAttributes(destination_filename);

                                if (file_attributes != FileAttributes.Normal)
                                    File.SetAttributes(destination_filename, FileAttributes.Normal);

                                File.Copy(item, destination_filename, true);
                            }

                            LogMesage("Updating out of date file " + destination_filename);
                        }
                    }
                    else
                    {
                        if (!_TestMode)
                            File.Copy(item, destination_filename);

                        LogMesage("Copying " + destination_filename);
                    }

                    file_count++;
                }
            }

            // remove any directories that exist in the destination but not in the source
            // need to check for existance beacuse in test mode, new directories will not be created.
            if (Directory.Exists(destinationDirectory))
            {
                foreach (string directory in Directory.GetDirectories(destinationDirectory))
                {
                    string source = directory.Replace(destinationRoot, sourceRoot);

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

            string[] directory_list = Directory.GetDirectories(sourceDirectory);

            //// call function recursively on subdirectories
            //foreach (var item in directory_list)    
            //{
            //    string destination = destination_root + item.Replace(source_root, string.Empty);
            //    SyncDirectory(source_root, destination_root ,item, destination);
            //}

            Parallel.ForEach(directory_list, new ParallelOptions { MaxDegreeOfParallelism = 4 }, current_directory =>
            {
                string destination = destinationRoot + current_directory.Replace(sourceRoot, string.Empty);
                SyncDirectory(sourceRoot, destinationRoot, current_directory, destination);
            });

            LogMesage($"Sync of directory {sourceDirectory} complete.", true);
            UpdateProgress(file_count);
        }

        protected static long GetTotalFreeSpace(string driveName)
        {
            if (string.IsNullOrWhiteSpace(driveName))
                throw new ArgumentNullException(nameof(driveName));

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.Name == driveName)
                {
                    return drive.AvailableFreeSpace;
                }
            }

            throw new Exception($"Found no drives named '{driveName}'");
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

            long total_size = 0;

            foreach (var fileInfo in directoryInfo.GetFiles())
                Interlocked.Add(ref total_size, fileInfo.Length);

            if (recursive)
                Parallel.ForEach(directoryInfo.GetDirectories(), (subDirectory) => Interlocked.Add(ref total_size, GetDirectorySize(subDirectory, recursive)));

            return total_size;
        }

        protected void UpdateProgress(int amount)
        {
            if (pbFiles.InvokeRequired)
            {
                var delegate_call = new UpdateProgressBar(UpdateProgress);
                Invoke(delegate_call, new object[] { amount });
            }
            else
            {
                pbFiles.Step = amount;
                pbFiles.PerformStep();

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
                var delegate_call = new SetTextCallback(LogMesage);
                Invoke(delegate_call, new object[] { message, flushLog });
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
            _MessageLog = new StringBuilder();

            LogMesage("Jolly Roger's Directory Sync, Version " + Application.ProductVersion);
            LogMesage("Please select a source and destination directory. Any content in the destination directory will be updated to match that of the source directory.");

            SetTestMode();
        }

        private async void btnSync_Click(object sender, EventArgs e)
        {
            _HaltProcessing = false;

            try
            {
                pbFiles.Maximum = CountFiles(txtSourceDir.Text);
                pbFiles.Value = 0;

                await Task.Run(() =>
                {
                    SyncDirectory(txtSourceDir.Text, txtDestinationDir.Text);
                });
            }
            catch (Exception ex)
            {
                LogMesage("Error processing : " + ex.Message);
                LogMesage("Halting synchronization.");
            }
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
            var folder_picker = new FolderBrowserDialog
            {
                RootFolder = Environment.SpecialFolder.MyComputer,
                Description = "Please select the destination folder."
            };

            DialogResult result = folder_picker.ShowDialog();

            if (result == DialogResult.OK)
                this.txtDestinationDir.Text = folder_picker.SelectedPath;
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
            bool ready_to_sync = (txtDestinationDir.Text.Length > 0) && (txtSourceDir.Text.Length > 0);

            this.btnSync.Enabled = ready_to_sync;
            this.btnCancel.Enabled = ready_to_sync;
        }

        private void txtDestinationDir_TextChanged(object sender, EventArgs e)
        {
            bool ready_to_sync = (txtDestinationDir.Text.Length > 0) && (txtSourceDir.Text.Length > 0);

            this.btnSync.Enabled = ready_to_sync;
            this.btnCancel.Enabled = ready_to_sync;
        }

        private void chkTestMode_CheckedChanged(object sender, EventArgs e)
        {
            SetTestMode();
        }
    }
}
