namespace Slackord
{
    partial class Slackord
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Slackord));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.FileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.OpenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ExitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.EnterBotTokenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ConnectionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ConnectBotToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.DisconnectBotToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.failOnCharacterLimitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.HelpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CheckForUpdatesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.AboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.DonateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.ToolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.ToolStripButton2 = new System.Windows.Forms.ToolStripButton();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.menuStrip1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileToolStripMenuItem,
            this.settingsToolStripMenuItem,
            this.HelpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(4, 74);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(7, 2, 0, 2);
            this.menuStrip1.Size = new System.Drawing.Size(925, 24);
            this.menuStrip1.Stretch = false;
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // FileToolStripMenuItem
            // 
            this.FileToolStripMenuItem.BackColor = System.Drawing.Color.Silver;
            this.FileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OpenToolStripMenuItem,
            this.ExitToolStripMenuItem});
            this.FileToolStripMenuItem.ForeColor = System.Drawing.Color.Black;
            this.FileToolStripMenuItem.Name = "FileToolStripMenuItem";
            this.FileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.FileToolStripMenuItem.Text = "File";
            // 
            // OpenToolStripMenuItem
            // 
            this.OpenToolStripMenuItem.Name = "OpenToolStripMenuItem";
            this.OpenToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
            this.OpenToolStripMenuItem.Text = "Import JSON";
            this.OpenToolStripMenuItem.Click += new System.EventHandler(this.OpenToolStripMenuItem_Click);
            // 
            // ExitToolStripMenuItem
            // 
            this.ExitToolStripMenuItem.Name = "ExitToolStripMenuItem";
            this.ExitToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
            this.ExitToolStripMenuItem.Text = "Exit";
            this.ExitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItem_Click);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.BackColor = System.Drawing.Color.Silver;
            this.settingsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.EnterBotTokenToolStripMenuItem,
            this.ConnectionToolStripMenuItem,
            this.failOnCharacterLimitToolStripMenuItem});
            this.settingsToolStripMenuItem.ForeColor = System.Drawing.Color.Black;
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.settingsToolStripMenuItem.Text = "Settings";
            // 
            // EnterBotTokenToolStripMenuItem
            // 
            this.EnterBotTokenToolStripMenuItem.Name = "EnterBotTokenToolStripMenuItem";
            this.EnterBotTokenToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.EnterBotTokenToolStripMenuItem.Text = "Enter Bot Token";
            this.EnterBotTokenToolStripMenuItem.Click += new System.EventHandler(this.EnterBotTokenToolStripMenuItem_Click);
            // 
            // ConnectionToolStripMenuItem
            // 
            this.ConnectionToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ConnectBotToolStripMenuItem,
            this.DisconnectBotToolStripMenuItem});
            this.ConnectionToolStripMenuItem.Name = "ConnectionToolStripMenuItem";
            this.ConnectionToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.ConnectionToolStripMenuItem.Text = "Bot Connection";
            // 
            // ConnectBotToolStripMenuItem
            // 
            this.ConnectBotToolStripMenuItem.Name = "ConnectBotToolStripMenuItem";
            this.ConnectBotToolStripMenuItem.Size = new System.Drawing.Size(133, 22);
            this.ConnectBotToolStripMenuItem.Text = "Connect";
            this.ConnectBotToolStripMenuItem.Click += new System.EventHandler(this.ConnectBotToolStripMenuItem_Click);
            // 
            // DisconnectBotToolStripMenuItem
            // 
            this.DisconnectBotToolStripMenuItem.Name = "DisconnectBotToolStripMenuItem";
            this.DisconnectBotToolStripMenuItem.Size = new System.Drawing.Size(133, 22);
            this.DisconnectBotToolStripMenuItem.Text = "Disconnect";
            this.DisconnectBotToolStripMenuItem.Click += new System.EventHandler(this.DisconnectBotToolStripMenuItem_Click);
            // 
            // failOnCharacterLimitToolStripMenuItem
            // 
            this.failOnCharacterLimitToolStripMenuItem.Checked = true;
            this.failOnCharacterLimitToolStripMenuItem.CheckOnClick = true;
            this.failOnCharacterLimitToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.failOnCharacterLimitToolStripMenuItem.Name = "failOnCharacterLimitToolStripMenuItem";
            this.failOnCharacterLimitToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.failOnCharacterLimitToolStripMenuItem.Text = "Ignore Char Limit Failures";
            this.failOnCharacterLimitToolStripMenuItem.ToolTipText = "Check if you want to skip parsing and sending messages that would exceed the Disc" +
    "ord character limit.";
            this.failOnCharacterLimitToolStripMenuItem.Click += new System.EventHandler(this.FailOnCharacterLimitToolStripMenuItem_Click);
            // 
            // HelpToolStripMenuItem
            // 
            this.HelpToolStripMenuItem.BackColor = System.Drawing.Color.Silver;
            this.HelpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CheckForUpdatesToolStripMenuItem,
            this.AboutToolStripMenuItem,
            this.DonateToolStripMenuItem});
            this.HelpToolStripMenuItem.ForeColor = System.Drawing.Color.Black;
            this.HelpToolStripMenuItem.Name = "HelpToolStripMenuItem";
            this.HelpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.HelpToolStripMenuItem.Text = "Help";
            // 
            // CheckForUpdatesToolStripMenuItem
            // 
            this.CheckForUpdatesToolStripMenuItem.Name = "CheckForUpdatesToolStripMenuItem";
            this.CheckForUpdatesToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.CheckForUpdatesToolStripMenuItem.Text = "Check For Updates";
            this.CheckForUpdatesToolStripMenuItem.Click += new System.EventHandler(this.CheckForUpdatesToolStripMenuItem_Click_1);
            // 
            // AboutToolStripMenuItem
            // 
            this.AboutToolStripMenuItem.Name = "AboutToolStripMenuItem";
            this.AboutToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.AboutToolStripMenuItem.Text = "About";
            this.AboutToolStripMenuItem.Click += new System.EventHandler(this.AboutToolStripMenuItem_Click);
            // 
            // DonateToolStripMenuItem
            // 
            this.DonateToolStripMenuItem.Name = "DonateToolStripMenuItem";
            this.DonateToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.DonateToolStripMenuItem.Text = "Donate";
            this.DonateToolStripMenuItem.Click += new System.EventHandler(this.DonateToolStripMenuItem_Click_1);
            // 
            // richTextBox1
            // 
            this.richTextBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBox1.BackColor = System.Drawing.Color.LightGray;
            this.richTextBox1.Location = new System.Drawing.Point(0, 105);
            this.richTextBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.ReadOnly = true;
            this.richTextBox1.Size = new System.Drawing.Size(933, 300);
            this.richTextBox1.TabIndex = 1;
            this.richTextBox1.Text = "";
            this.richTextBox1.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.Link_Clicked);
            this.richTextBox1.TextChanged += new System.EventHandler(this.RichTextBox1_TextChanged);
            // 
            // ToolStripButton1
            // 
            this.ToolStripButton1.BackColor = System.Drawing.Color.Silver;
            this.ToolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.ToolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("ToolStripButton1.Image")));
            this.ToolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ToolStripButton1.Name = "ToolStripButton1";
            this.ToolStripButton1.Overflow = System.Windows.Forms.ToolStripItemOverflow.Never;
            this.ToolStripButton1.Padding = new System.Windows.Forms.Padding(0, 5, 20, 0);
            this.ToolStripButton1.Size = new System.Drawing.Size(40, 25);
            this.ToolStripButton1.Text = "toolStripButton1";
            this.ToolStripButton1.ToolTipText = "copy selected/all text";
            this.ToolStripButton1.Click += new System.EventHandler(this.ToolStripButton1_Click);
            // 
            // ToolStripButton2
            // 
            this.ToolStripButton2.BackColor = System.Drawing.Color.Silver;
            this.ToolStripButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.ToolStripButton2.Image = ((System.Drawing.Image)(resources.GetObject("ToolStripButton2.Image")));
            this.ToolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.ToolStripButton2.Name = "ToolStripButton2";
            this.ToolStripButton2.Overflow = System.Windows.Forms.ToolStripItemOverflow.Never;
            this.ToolStripButton2.Padding = new System.Windows.Forms.Padding(20, 5, 0, 0);
            this.ToolStripButton2.Size = new System.Drawing.Size(40, 25);
            this.ToolStripButton2.Text = "toolStripButton2";
            this.ToolStripButton2.ToolTipText = "clear the entire log window";
            this.ToolStripButton2.Click += new System.EventHandler(this.ToolStripButton2_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.AllowMerge = false;
            this.toolStrip1.AutoSize = false;
            this.toolStrip1.BackColor = System.Drawing.Color.White;
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.toolStrip1.GripMargin = new System.Windows.Forms.Padding(0);
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripButton1,
            this.ToolStripButton2});
            this.toolStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            this.toolStrip1.Location = new System.Drawing.Point(4, 408);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(925, 28);
            this.toolStrip1.TabIndex = 4;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // Slackord
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(933, 439);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "Slackord";
            this.Padding = new System.Windows.Forms.Padding(4, 74, 4, 3);
            this.Text = "Slackord 2";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem FileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem HelpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem AboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem EnterBotTokenToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem OpenToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ExitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ConnectionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ConnectBotToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem DisconnectBotToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem CheckForUpdatesToolStripMenuItem;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.ToolStripButton ToolStripButton1;
        private System.Windows.Forms.ToolStripButton ToolStripButton2;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripMenuItem DonateToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem failOnCharacterLimitToolStripMenuItem;
    }
}

