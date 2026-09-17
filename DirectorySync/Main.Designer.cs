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
            btnSync = new Button();
            txtSourceDir = new TextBox();
            label1 = new Label();
            label2 = new Label();
            txtDestinationDir = new TextBox();
            btnSelectSource = new Button();
            btnDestination = new Button();
            btnClearLog = new Button();
            txtLog = new RichTextBox();
            lblAbout = new Label();
            chkTestMode = new CheckBox();
            btnCancel = new Button();
            pbFiles = new ProgressBar();
            SuspendLayout();
            // 
            // btnSync
            // 
            btnSync.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSync.Location = new Point(326, 257);
            btnSync.Margin = new Padding(4);
            btnSync.Name = "btnSync";
            btnSync.Size = new Size(153, 26);
            btnSync.TabIndex = 0;
            btnSync.Text = "Syncronize Directories";
            btnSync.UseVisualStyleBackColor = true;
            btnSync.Click += btnSync_Click;
            // 
            // txtSourceDir
            // 
            txtSourceDir.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtSourceDir.Location = new Point(150, 14);
            txtSourceDir.Margin = new Padding(4);
            txtSourceDir.Name = "txtSourceDir";
            txtSourceDir.Size = new Size(381, 23);
            txtSourceDir.TabIndex = 1;
            txtSourceDir.TextChanged += txtSourceDir_TextChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(36, 17);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(97, 15);
            label1.TabIndex = 2;
            label1.Text = "Source Directory:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(14, 56);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(121, 15);
            label2.TabIndex = 3;
            label2.Text = "Destination Directory:";
            // 
            // txtDestinationDir
            // 
            txtDestinationDir.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtDestinationDir.Location = new Point(147, 53);
            txtDestinationDir.Margin = new Padding(4);
            txtDestinationDir.Name = "txtDestinationDir";
            txtDestinationDir.Size = new Size(384, 23);
            txtDestinationDir.TabIndex = 4;
            txtDestinationDir.TextChanged += txtDestinationDir_TextChanged;
            // 
            // btnSelectSource
            // 
            btnSelectSource.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSelectSource.Location = new Point(538, 11);
            btnSelectSource.Margin = new Padding(4);
            btnSelectSource.Name = "btnSelectSource";
            btnSelectSource.Size = new Size(130, 26);
            btnSelectSource.TabIndex = 6;
            btnSelectSource.Text = "Select Source";
            btnSelectSource.UseVisualStyleBackColor = true;
            btnSelectSource.Click += btnSelectSource_Click;
            // 
            // btnDestination
            // 
            btnDestination.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnDestination.Location = new Point(538, 51);
            btnDestination.Margin = new Padding(4);
            btnDestination.Name = "btnDestination";
            btnDestination.Size = new Size(130, 26);
            btnDestination.TabIndex = 7;
            btnDestination.Text = "Select Destination";
            btnDestination.UseVisualStyleBackColor = true;
            btnDestination.Click += btnDestination_Click;
            // 
            // btnClearLog
            // 
            btnClearLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClearLog.Location = new Point(580, 257);
            btnClearLog.Margin = new Padding(4);
            btnClearLog.Name = "btnClearLog";
            btnClearLog.Size = new Size(88, 26);
            btnClearLog.TabIndex = 9;
            btnClearLog.Text = "Clear Log";
            btnClearLog.UseVisualStyleBackColor = true;
            btnClearLog.Click += btnClearLog_Click;
            // 
            // txtLog
            // 
            txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtLog.Location = new Point(14, 92);
            txtLog.Margin = new Padding(4);
            txtLog.Name = "txtLog";
            txtLog.Size = new Size(653, 118);
            txtLog.TabIndex = 10;
            txtLog.Text = "";
            // 
            // lblAbout
            // 
            lblAbout.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblAbout.Location = new Point(14, 263);
            lblAbout.Margin = new Padding(4, 0, 4, 0);
            lblAbout.Name = "lblAbout";
            lblAbout.Size = new Size(217, 26);
            lblAbout.TabIndex = 11;
            lblAbout.Text = "Author Notice";
            // 
            // chkTestMode
            // 
            chkTestMode.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            chkTestMode.AutoSize = true;
            chkTestMode.Location = new Point(239, 262);
            chkTestMode.Margin = new Padding(4);
            chkTestMode.Name = "chkTestMode";
            chkTestMode.Size = new Size(80, 19);
            chkTestMode.TabIndex = 12;
            chkTestMode.Text = "Test Mode";
            chkTestMode.UseVisualStyleBackColor = true;
            chkTestMode.CheckedChanged += chkTestMode_CheckedChanged;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.Location = new Point(486, 257);
            btnCancel.Margin = new Padding(4);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(88, 26);
            btnCancel.TabIndex = 13;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // pbFiles
            // 
            pbFiles.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pbFiles.Location = new Point(14, 219);
            pbFiles.Margin = new Padding(4);
            pbFiles.Name = "pbFiles";
            pbFiles.Size = new Size(653, 32);
            pbFiles.Step = 1;
            pbFiles.TabIndex = 14;
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(681, 298);
            Controls.Add(pbFiles);
            Controls.Add(btnCancel);
            Controls.Add(chkTestMode);
            Controls.Add(lblAbout);
            Controls.Add(txtLog);
            Controls.Add(btnClearLog);
            Controls.Add(btnDestination);
            Controls.Add(btnSelectSource);
            Controls.Add(txtDestinationDir);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(txtSourceDir);
            Controls.Add(btnSync);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4);
            MinimumSize = new Size(697, 337);
            Name = "frmMain";
            Text = "Jolly Roger's Directory Sync";
            ResumeLayout(false);
            PerformLayout();

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

