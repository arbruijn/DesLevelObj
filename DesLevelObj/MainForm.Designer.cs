namespace DesLevelObj
{
    partial class MainForm
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
            this.lblLevelFile = new System.Windows.Forms.Label();
            this.txtLevelFile = new System.Windows.Forms.TextBox();
            this.btnLevelFile = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.btnConvert = new System.Windows.Forms.Button();
            this.lblLevel = new System.Windows.Forms.Label();
            this.cmbLevel = new System.Windows.Forms.ComboBox();
            this.lblGameDir = new System.Windows.Forms.Label();
            this.txtGameDir = new System.Windows.Forms.TextBox();
            this.btnGameDir = new System.Windows.Forms.Button();
            this.chkTexPng = new System.Windows.Forms.CheckBox();
            this.btnOutDir = new System.Windows.Forms.Button();
            this.txtOutDir = new System.Windows.Forms.TextBox();
            this.lblOutDir = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblLevelFile
            // 
            this.lblLevelFile.AutoSize = true;
            this.lblLevelFile.Location = new System.Drawing.Point(9, 36);
            this.lblLevelFile.Name = "lblLevelFile";
            this.lblLevelFile.Size = new System.Drawing.Size(52, 13);
            this.lblLevelFile.TabIndex = 3;
            this.lblLevelFile.Text = "Level File";
            // 
            // txtLevelFile
            // 
            this.txtLevelFile.Location = new System.Drawing.Point(100, 32);
            this.txtLevelFile.Name = "txtLevelFile";
            this.txtLevelFile.Size = new System.Drawing.Size(377, 20);
            this.txtLevelFile.TabIndex = 4;
            this.txtLevelFile.TextChanged += new System.EventHandler(this.txtLevelFile_TextChanged);
            // 
            // btnLevelFile
            // 
            this.btnLevelFile.Location = new System.Drawing.Point(483, 31);
            this.btnLevelFile.Name = "btnLevelFile";
            this.btnLevelFile.Size = new System.Drawing.Size(24, 23);
            this.btnLevelFile.TabIndex = 5;
            this.btnLevelFile.Text = "...";
            this.btnLevelFile.UseVisualStyleBackColor = true;
            this.btnLevelFile.Click += new System.EventHandler(this.btnLevelFile_Click);
            // 
            // txtLog
            // 
            this.txtLog.Location = new System.Drawing.Point(12, 165);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.Size = new System.Drawing.Size(499, 144);
            this.txtLog.TabIndex = 13;
            // 
            // btnConvert
            // 
            this.btnConvert.Location = new System.Drawing.Point(12, 112);
            this.btnConvert.Name = "btnConvert";
            this.btnConvert.Size = new System.Drawing.Size(75, 23);
            this.btnConvert.TabIndex = 11;
            this.btnConvert.Text = "Convert";
            this.btnConvert.UseVisualStyleBackColor = true;
            this.btnConvert.Click += new System.EventHandler(this.btnConvert_Click);
            // 
            // lblLevel
            // 
            this.lblLevel.AutoSize = true;
            this.lblLevel.Location = new System.Drawing.Point(9, 62);
            this.lblLevel.Name = "lblLevel";
            this.lblLevel.Size = new System.Drawing.Size(33, 13);
            this.lblLevel.TabIndex = 6;
            this.lblLevel.Text = "Level";
            // 
            // cmbLevel
            // 
            this.cmbLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLevel.FormattingEnabled = true;
            this.cmbLevel.Location = new System.Drawing.Point(100, 59);
            this.cmbLevel.Name = "cmbLevel";
            this.cmbLevel.Size = new System.Drawing.Size(377, 21);
            this.cmbLevel.TabIndex = 7;
            this.cmbLevel.SelectedIndexChanged += new System.EventHandler(this.cmbLevel_SelectedIndexChanged);
            // 
            // lblGameDir
            // 
            this.lblGameDir.AutoSize = true;
            this.lblGameDir.Location = new System.Drawing.Point(9, 9);
            this.lblGameDir.Name = "lblGameDir";
            this.lblGameDir.Size = new System.Drawing.Size(67, 13);
            this.lblGameDir.TabIndex = 0;
            this.lblGameDir.Text = "Game Folder";
            // 
            // txtGameDir
            // 
            this.txtGameDir.Location = new System.Drawing.Point(100, 6);
            this.txtGameDir.Name = "txtGameDir";
            this.txtGameDir.Size = new System.Drawing.Size(377, 20);
            this.txtGameDir.TabIndex = 1;
            this.txtGameDir.TextChanged += new System.EventHandler(this.txtGameDir_TextChanged);
            // 
            // btnGameDir
            // 
            this.btnGameDir.Location = new System.Drawing.Point(483, 4);
            this.btnGameDir.Name = "btnGameDir";
            this.btnGameDir.Size = new System.Drawing.Size(24, 23);
            this.btnGameDir.TabIndex = 2;
            this.btnGameDir.Text = "...";
            this.btnGameDir.UseVisualStyleBackColor = true;
            this.btnGameDir.Click += new System.EventHandler(this.btnGameDir_Click);
            // 
            // chkTexPng
            // 
            this.chkTexPng.AutoSize = true;
            this.chkTexPng.Location = new System.Drawing.Point(13, 142);
            this.chkTexPng.Name = "chkTexPng";
            this.chkTexPng.Size = new System.Drawing.Size(147, 17);
            this.chkTexPng.TabIndex = 12;
            this.chkTexPng.Text = "Write textures as png files";
            this.chkTexPng.UseVisualStyleBackColor = true;
            this.chkTexPng.CheckedChanged += new System.EventHandler(this.chkTexPng_CheckedChanged);
            // 
            // btnOutDir
            // 
            this.btnOutDir.Location = new System.Drawing.Point(483, 85);
            this.btnOutDir.Name = "btnOutDir";
            this.btnOutDir.Size = new System.Drawing.Size(24, 23);
            this.btnOutDir.TabIndex = 10;
            this.btnOutDir.Text = "...";
            this.btnOutDir.UseVisualStyleBackColor = true;
            this.btnOutDir.Click += new System.EventHandler(this.btnOutDir_Click);
            // 
            // txtOutDir
            // 
            this.txtOutDir.Location = new System.Drawing.Point(100, 86);
            this.txtOutDir.Name = "txtOutDir";
            this.txtOutDir.Size = new System.Drawing.Size(377, 20);
            this.txtOutDir.TabIndex = 9;
            this.txtOutDir.TextChanged += new System.EventHandler(this.txtOutDir_TextChanged);
            // 
            // lblOutDir
            // 
            this.lblOutDir.AutoSize = true;
            this.lblOutDir.Location = new System.Drawing.Point(9, 90);
            this.lblOutDir.Name = "lblOutDir";
            this.lblOutDir.Size = new System.Drawing.Size(71, 13);
            this.lblOutDir.TabIndex = 8;
            this.lblOutDir.Text = "Output Folder";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(523, 321);
            this.Controls.Add(this.btnOutDir);
            this.Controls.Add(this.txtOutDir);
            this.Controls.Add(this.lblOutDir);
            this.Controls.Add(this.chkTexPng);
            this.Controls.Add(this.btnGameDir);
            this.Controls.Add(this.txtGameDir);
            this.Controls.Add(this.lblGameDir);
            this.Controls.Add(this.cmbLevel);
            this.Controls.Add(this.lblLevel);
            this.Controls.Add(this.btnConvert);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.btnLevelFile);
            this.Controls.Add(this.txtLevelFile);
            this.Controls.Add(this.lblLevelFile);
            this.Name = "MainForm";
            this.Text = "DesLevelObj v0.8.5";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblLevelFile;
        private System.Windows.Forms.TextBox txtLevelFile;
        private System.Windows.Forms.Button btnLevelFile;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Button btnConvert;
        private System.Windows.Forms.Label lblLevel;
        private System.Windows.Forms.ComboBox cmbLevel;
        private System.Windows.Forms.Label lblGameDir;
        private System.Windows.Forms.TextBox txtGameDir;
        private System.Windows.Forms.Button btnGameDir;
        private System.Windows.Forms.CheckBox chkTexPng;
        private System.Windows.Forms.Button btnOutDir;
        private System.Windows.Forms.TextBox txtOutDir;
        private System.Windows.Forms.Label lblOutDir;
    }
}