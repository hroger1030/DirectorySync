using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DirectoryCopy
{
    public partial class frmMain : Form
    {
        protected delegate void SetTextCallback(string text, bool flush_log);
        protected delegate void UpdateProgressBar(int amount);

        #region Fields

            private static readonly int MAX_PATH = 260;

            protected StringBuilder _MessageLog;
            protected bool _TestMode;
            protected volatile bool _HaltProcessing;
            protected Form _MainForm;
            protected Stopwatch _Timer;

        #endregion

        #region Methods

            protected bool ValidatePaths()
            {
                if (!Directory.Exists(this.txtSourceDir.Text))
                {
                    LogMesage("Error: Source directory does not exist.");
                    return false;
                }

                if (!Directory.Exists(this.txtDestinationDir.Text))
                {
                    LogMesage("Error: Destination directory does not exist.");
                    return false;
                }

                if (this.txtSourceDir.Text == this.txtDestinationDir.Text)
                {
                    LogMesage("Error: Source directory and destination directory must be different.");
                    return false;
                }

                return true;
            } 

            protected void RemoveFileAttributes(string directory, bool recursive)
            {
                DirectoryInfo directory_info = new DirectoryInfo(directory);

                if (directory_info.Attributes != FileAttributes.Normal)
                    directory_info.Attributes = FileAttributes.Normal;

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

            protected int CountFiles(string directory)
            {
                int count = Directory.GetFiles(directory).Length;

                foreach (var sub_directory in Directory.GetDirectories(directory))
                    count += CountFiles(sub_directory);
                
                return count;
            }

            protected async void SyncDirectory(string source_directory, string destination_directory)
            {       
                if (ValidatePaths())
                {
                    _Timer.Restart();

                    LogMesage("Beginning Synchronization.", true);

                    await Task.Run(() =>
                    {
                        SyncDirectory(source_directory, destination_directory, source_directory, destination_directory);
                    });

                    if (_HaltProcessing)
                        LogMesage("Synchronization halted.", true);
                    else
                        LogMesage("Synchronization complete.", true);

                    _Timer.Stop();
                }
            }

            protected void SyncDirectory(string source_root, string destination_root, string source_directory, string destination_directory)
            {
                if (_HaltProcessing == true)
                    return;       

                int file_count = 0; 

                LogMesage(string.Format("Starting sync of directory {0}.", source_directory));

                if (!Directory.Exists(destination_directory))
                {
                    LogMesage("Creating " + destination_directory);

                    if (!_TestMode)
                        Directory.CreateDirectory(destination_directory);
                }
             
                // remove non-existing files. Look at each file in the destination directory and if a 
                // matching file does not exist in the source directory remove it.
                if (Directory.Exists(destination_directory))
                {
                    foreach (string file in Directory.GetFiles(destination_directory))
                    {
                        string source_filename = source_directory + "\\" + Path.GetFileName(file);

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
                if (Directory.Exists(source_directory))
                {
                    foreach (string item in Directory.GetFiles(source_directory))
                    {
                        string destination_filename = destination_directory + "\\"+ Path.GetFileName(item);

                        if (File.Exists(destination_filename))
                        {
                            if (File.GetLastWriteTime(item) != File.GetLastWriteTime(destination_filename))
                            {
                                if (destination_filename.Length > 260)
                                    LogMesage(string.Format("Path {0} is {1} characters long. This exceeds the {2} character limit.", destination_filename, destination_filename.Length, MAX_PATH));

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
                if (Directory.Exists(destination_directory))
                {
                    foreach (string directory in Directory.GetDirectories(destination_directory))    
                    {
                        string source = directory.Replace(destination_root, source_root);

                        if (!Directory.Exists(source))
                        {
                            LogMesage("Deleting " + directory);

                            if (!_TestMode)
                            {
                                DirectoryInfo di = new DirectoryInfo(directory);
                                RemoveFileAttributes(directory, true);
                                di.Delete(true);
                            }
                        }
                    } 
                }

                string[] directory_list = Directory.GetDirectories(source_directory);

                //// call function recursively on subdirectories
                //foreach (var item in directory_list)    
                //{
                //    string destination = destination_root + item.Replace(source_root, string.Empty);
                //    SyncDirectory(source_root, destination_root ,item, destination);
                //}

                Parallel.ForEach(directory_list, new ParallelOptions { MaxDegreeOfParallelism = 4 }, current_directory =>
                {
                    string destination = destination_root + current_directory.Replace(source_root, string.Empty);
                    SyncDirectory(source_root, destination_root ,current_directory, destination);
                });

                LogMesage(string.Format("Sync of directory {0} complete.", source_directory), true);
                UpdateProgress(file_count);
            }

            protected void UpdateProgress(int amount)
            {
                if (this.pbFiles.InvokeRequired)
                {
				    UpdateProgressBar delegate_call = new UpdateProgressBar(UpdateProgress);
				    this.Invoke(delegate_call, new object[] { amount });
                }
                else
                {
                    this.pbFiles.Step = amount;
                    this.pbFiles.PerformStep();

                    Console.WriteLine("Amount: " + amount.ToString());
                }
            }

            protected void LogMesage(string message)
            {
                LogMesage(message, false);
            }

            protected void LogMesage(string message, bool flush_log)
            {
                if (_MessageLog == null)
                    _MessageLog = new StringBuilder();
             
                // only update UI every several seconds

                if (this.txtLog.InvokeRequired)
                {
				    SetTextCallback delegate_call = new SetTextCallback(LogMesage);
				    this.Invoke(delegate_call, new object[] { message, flush_log });
                }
                else
                {
                    _MessageLog.AppendLine(string.Format("<{0}> {1}", DateTime.Now.ToLongTimeString(), message));

                    if (!_Timer.IsRunning)
                    {
                        this.txtLog.Text = _MessageLog.ToString();
                    }
                    else if (_Timer.ElapsedMilliseconds > 5000 || flush_log)
                    { 
                        this.txtLog.Text = _MessageLog.ToString();

                        _Timer.Restart();
                        _MessageLog.Clear();
                    }
                }
            }

            protected void SetTestMode()
            {
                _TestMode = this.chkTestMode.Checked;

                if (_TestMode)
                    LogMesage("Test mode is ON: No updates will be made.");
                else
                    LogMesage("Test mode is OFF: Updates will be made normally.");
            }

        #endregion

        #region Events

            public frmMain()
            {
                InitializeComponent();

                this.Text               = "Jolly Roger's Directory Sync";
                this.btnSync.Enabled    = false;
                this.btnCancel.Enabled  = false;
                this.lblAbout.Text      = "Written by Roger Hill, 2011";
                
                //_MessageWindow          = this.txtLog;
                _MainForm               = this;
                _Timer                  = new Stopwatch();

                LogMesage("Jolly Roger's Directory Sync, Version " + Application.ProductVersion);
                LogMesage("Please select a source and destination directory. Any content in the destination directory will be updated to match that of the source directory.");

                SetTestMode();
            }

            private async void btnSync_Click(object sender, EventArgs e)
            {
                _HaltProcessing = false;

                try
                {
                    this.pbFiles.Maximum = CountFiles(this.txtSourceDir.Text);
                    this.pbFiles.Value = 0;

                    await Task.Run(() =>
                    {
                        SyncDirectory(this.txtSourceDir.Text, this.txtDestinationDir.Text);
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
                FolderBrowserDialog folder_picker       = new FolderBrowserDialog();
                folder_picker.RootFolder                = Environment.SpecialFolder.MyComputer;
                folder_picker.Description               = "Please select the source folder.";

                if (this.txtSourceDir.Text != string.Empty)
                    folder_picker.SelectedPath = this.txtSourceDir.Text;

                DialogResult result = folder_picker.ShowDialog();

                if (result == DialogResult.OK)
                      this.txtSourceDir.Text = folder_picker.SelectedPath;
            }

            private void btnDestination_Click(object sender, EventArgs e)
            {
                FolderBrowserDialog folder_picker       = new FolderBrowserDialog();
                folder_picker.RootFolder                = Environment.SpecialFolder.MyComputer;
                folder_picker.Description               = "Please select the destination folder.";

                DialogResult result = folder_picker.ShowDialog();

                if (result == DialogResult.OK)
                    this.txtDestinationDir.Text = folder_picker.SelectedPath;
            }

            private void btnClearLog_Click(object sender, EventArgs e)
            {
                _MessageLog = null;
                LogMesage("Log Cleared.");
                this.pbFiles.Value = 0;
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
                bool ready_to_sync      = (this.txtDestinationDir.Text.Length > 0) && (this.txtSourceDir.Text.Length > 0);

                this.btnSync.Enabled    = ready_to_sync;
                this.btnCancel.Enabled  = ready_to_sync;
            }

            private void txtDestinationDir_TextChanged(object sender, EventArgs e)
            {
                bool ready_to_sync      = (this.txtDestinationDir.Text.Length > 0) && (this.txtSourceDir.Text.Length > 0);

                this.btnSync.Enabled    = ready_to_sync;
                this.btnCancel.Enabled  = ready_to_sync;
            }

            private void chkTestMode_CheckedChanged(object sender, EventArgs e)
            {
                SetTestMode();
            }        

        #endregion
    }
}
