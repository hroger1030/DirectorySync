namespace DirectorySync
{
    partial class frmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.btnSync = new System.Windows.Forms.Button();
            this.txtSourceDir = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtDestinationDir = new System.Windows.Forms.TextBox();
            this.btnSelectSource = new System.Windows.Forms.Button();
            this.btnDestination = new System.Windows.Forms.Button();
            this.btnClearLog = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.RichTextBox();
            this.lblAbout = new System.Windows.Forms.Label();
            this.chkTestMode = new System.Windows.Forms.CheckBox();
            this.btnCancel = new System.Windows.Forms.Button();
            this.pbFiles = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // btnSync
            // 
            this.btnSync.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSync.Location = new System.Drawing.Point(383, 366);
            this.btnSync.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnSync.Name = "btnSync";
            this.btnSync.Size = new System.Drawing.Size(175, 35);
            this.btnSync.TabIndex = 0;
            this.btnSync.Text = "Syncronize Directories";
            this.btnSync.UseVisualStyleBackColor = true;
            this.btnSync.Click += new System.EventHandler(this.btnSync_Click);
            // 
            // txtSourceDir
            // 
            this.txtSourceDir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSourceDir.Location = new System.Drawing.Point(171, 18);
            this.txtSourceDir.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtSourceDir.Name = "txtSourceDir";
            this.txtSourceDir.Size = new System.Drawing.Size(445, 27);
            this.txtSourceDir.TabIndex = 1;
            this.txtSourceDir.TextChanged += new System.EventHandler(this.txtSourceDir_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(41, 23);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(122, 20);
            this.label1.TabIndex = 2;
            this.label1.Text = "Source Directory:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 75);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(153, 20);
            this.label2.TabIndex = 3;
            this.label2.Text = "Destination Directory:";
            // 
            // txtDestinationDir
            // 
            this.txtDestinationDir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDestinationDir.Location = new System.Drawing.Point(168, 71);
            this.txtDestinationDir.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtDestinationDir.Name = "txtDestinationDir";
            this.txtDestinationDir.Size = new System.Drawing.Size(445, 27);
            this.txtDestinationDir.TabIndex = 4;
            this.txtDestinationDir.TextChanged += new System.EventHandler(this.txtDestinationDir_TextChanged);
            // 
            // btnSelectSource
            // 
            this.btnSelectSource.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSelectSource.Location = new System.Drawing.Point(625, 15);
            this.btnSelectSource.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnSelectSource.Name = "btnSelectSource";
            this.btnSelectSource.Size = new System.Drawing.Size(148, 35);
            this.btnSelectSource.TabIndex = 6;
            this.btnSelectSource.Text = "Select Source";
            this.btnSelectSource.UseVisualStyleBackColor = true;
            this.btnSelectSource.Click += new System.EventHandler(this.btnSelectSource_Click);
            // 
            // btnDestination
            // 
            this.btnDestination.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDestination.Location = new System.Drawing.Point(625, 68);
            this.btnDestination.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnDestination.Name = "btnDestination";
            this.btnDestination.Size = new System.Drawing.Size(148, 35);
            this.btnDestination.TabIndex = 7;
            this.btnDestination.Text = "Select Destination";
            this.btnDestination.UseVisualStyleBackColor = true;
            this.btnDestination.Click += new System.EventHandler(this.btnDestination_Click);
            // 
            // btnClearLog
            // 
            this.btnClearLog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClearLog.Location = new System.Drawing.Point(673, 366);
            this.btnClearLog.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(100, 35);
            this.btnClearLog.TabIndex = 9;
            this.btnClearLog.Text = "Clear Log";
            this.btnClearLog.UseVisualStyleBackColor = true;
            this.btnClearLog.Click += new System.EventHandler(this.btnClearLog_Click);
            // 
            // txtLog
            // 
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.Location = new System.Drawing.Point(16, 123);
            this.txtLog.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtLog.Name = "txtLog";
            this.txtLog.Size = new System.Drawing.Size(756, 179);
            this.txtLog.TabIndex = 10;
            this.txtLog.Text = "";
            // 
            // lblAbout
            // 
            this.lblAbout.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblAbout.Location = new System.Drawing.Point(16, 374);
            this.lblAbout.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblAbout.Name = "lblAbout";
            this.lblAbout.Size = new System.Drawing.Size(248, 35);
            this.lblAbout.TabIndex = 11;
            this.lblAbout.Text = "Author Notice";
            // 
            // chkTestMode
            // 
            this.chkTestMode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.chkTestMode.AutoSize = true;
            this.chkTestMode.Location = new System.Drawing.Point(274, 374);
            this.chkTestMode.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.chkTestMode.Name = "chkTestMode";
            this.chkTestMode.Size = new System.Drawing.Size(101, 24);
            this.chkTestMode.TabIndex = 12;
            this.chkTestMode.Text = "Test Mode";
            this.chkTestMode.UseVisualStyleBackColor = true;
            this.chkTestMode.CheckedChanged += new System.EventHandler(this.chkTestMode_CheckedChanged);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Location = new System.Drawing.Point(566, 366);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 35);
            this.btnCancel.TabIndex = 13;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // pbFiles
            // 
            this.pbFiles.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pbFiles.Location = new System.Drawing.Point(16, 314);
            this.pbFiles.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.pbFiles.Name = "pbFiles";
            this.pbFiles.Size = new System.Drawing.Size(757, 43);
            this.pbFiles.Step = 1;
            this.pbFiles.TabIndex = 14;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(789, 420);
            this.Controls.Add(this.pbFiles);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.chkTestMode);
            this.Controls.Add(this.lblAbout);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.btnClearLog);
            this.Controls.Add(this.btnDestination);
            this.Controls.Add(this.btnSelectSource);
            this.Controls.Add(this.txtDestinationDir);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtSourceDir);
            this.Controls.Add(this.btnSync);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MinimumSize = new System.Drawing.Size(794, 437);
            this.Name = "frmMain";
            this.Text = "Jolly Roger\'s Directory Sync";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSync;
        private System.Windows.Forms.TextBox txtSourceDir;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtDestinationDir;
        private System.Windows.Forms.Button btnSelectSource;
        private System.Windows.Forms.Button btnDestination;
        private System.Windows.Forms.Button btnClearLog;
        private System.Windows.Forms.RichTextBox txtLog;
        private System.Windows.Forms.Label lblAbout;
        private System.Windows.Forms.CheckBox chkTestMode;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ProgressBar pbFiles;
    }
}

