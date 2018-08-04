namespace GitUI.CommandsDialogs.BrowseDialog
{
    partial class FormUpdates
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormUpdates));
            this.closeButton = new System.Windows.Forms.Button();
            this.UpdateLabel = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.linkChangeLog = new System.Windows.Forms.LinkLabel();
            this.btnInstallNow = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // closeButton
            // 
            this.closeButton.Location = new System.Drawing.Point(365, 66);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(75, 23);
            this.closeButton.TabIndex = 0;
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.CloseButtonClick);
            // 
            // UpdateLabel
            // 
            this.UpdateLabel.AutoSize = true;
            this.UpdateLabel.Location = new System.Drawing.Point(13, 13);
            this.UpdateLabel.Name = "UpdateLabel";
            this.UpdateLabel.Size = new System.Drawing.Size(113, 13);
            this.UpdateLabel.TabIndex = 1;
            this.UpdateLabel.Text = "Searching for updates";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(16, 37);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(424, 23);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar1.TabIndex = 3;
            // 
            // linkChangeLog
            // 
            this.linkChangeLog.AutoSize = true;
            this.linkChangeLog.Location = new System.Drawing.Point(14, 71);
            this.linkChangeLog.Name = "linkChangeLog";
            this.linkChangeLog.Size = new System.Drawing.Size(90, 13);
            this.linkChangeLog.TabIndex = 2;
            this.linkChangeLog.TabStop = true;
            this.linkChangeLog.Text = "Show ChangeLog";
            this.linkChangeLog.Visible = false;
            this.linkChangeLog.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkChangeLog_LinkClicked);
            // 
            // btnDownloadNow
            // 
            this.btnInstallNow.Enabled = false;
            this.btnInstallNow.Location = new System.Drawing.Point(247, 66);
            this.btnInstallNow.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnInstallNow.Name = "btnInstallNow";
            this.btnInstallNow.Size = new System.Drawing.Size(112, 23);
            this.btnInstallNow.TabIndex = 4;
            this.btnInstallNow.Text = "Install Now";
            this.btnInstallNow.UseVisualStyleBackColor = true;
            this.btnInstallNow.Click += new System.EventHandler(this.btnInstallNow_Click);
            // 
            // FormUpdates
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(452, 100);
            this.Controls.Add(this.btnInstallNow);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.linkChangeLog);
            this.Controls.Add(this.UpdateLabel);
            this.Controls.Add(this.closeButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "FormUpdates";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Check for update";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.Label UpdateLabel;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.LinkLabel linkChangeLog;
        private System.Windows.Forms.Button btnInstallNow;
    }
}