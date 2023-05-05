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
            menuStrip1 = new MenuStrip();
            FileToolStripMenuItem = new ToolStripMenuItem();
            importJSONFolderToolStripMenuItem = new ToolStripMenuItem();
            ExitToolStripMenuItem = new ToolStripMenuItem();
            settingsToolStripMenuItem = new ToolStripMenuItem();
            EnterBotTokenToolStripMenuItem = new ToolStripMenuItem();
            ConnectionToolStripMenuItem = new ToolStripMenuItem();
            ConnectBotToolStripMenuItem = new ToolStripMenuItem();
            DisconnectBotToolStripMenuItem = new ToolStripMenuItem();
            HelpToolStripMenuItem = new ToolStripMenuItem();
            CheckForUpdatesToolStripMenuItem = new ToolStripMenuItem();
            AboutToolStripMenuItem = new ToolStripMenuItem();
            DonateToolStripMenuItem = new ToolStripMenuItem();
            richTextBox1 = new RichTextBox();
            ToolStripButton1 = new ToolStripButton();
            ToolStripButton2 = new ToolStripButton();
            toolStrip1 = new ToolStrip();
            progressBar1 = new ProgressBar();
            menuStrip1.SuspendLayout();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { FileToolStripMenuItem, settingsToolStripMenuItem, HelpToolStripMenuItem });
            menuStrip1.Location = new Point(4, 74);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Padding = new Padding(7, 2, 0, 2);
            menuStrip1.Size = new Size(925, 24);
            menuStrip1.Stretch = false;
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // FileToolStripMenuItem
            // 
            FileToolStripMenuItem.BackColor = Color.Silver;
            FileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { importJSONFolderToolStripMenuItem, ExitToolStripMenuItem });
            FileToolStripMenuItem.ForeColor = Color.Black;
            FileToolStripMenuItem.Name = "FileToolStripMenuItem";
            FileToolStripMenuItem.Size = new Size(37, 20);
            FileToolStripMenuItem.Text = "File";
            // 
            // importJSONFolderToolStripMenuItem
            // 
            importJSONFolderToolStripMenuItem.Name = "importJSONFolderToolStripMenuItem";
            importJSONFolderToolStripMenuItem.Size = new Size(177, 22);
            importJSONFolderToolStripMenuItem.Text = "Import JSON Folder";
            importJSONFolderToolStripMenuItem.Click += ImportJSONFolderToolStripMenuItem_Click;
            // 
            // ExitToolStripMenuItem
            // 
            ExitToolStripMenuItem.Name = "ExitToolStripMenuItem";
            ExitToolStripMenuItem.Size = new Size(177, 22);
            ExitToolStripMenuItem.Text = "Exit";
            ExitToolStripMenuItem.Click += ExitToolStripMenuItem_Click;
            // 
            // settingsToolStripMenuItem
            // 
            settingsToolStripMenuItem.BackColor = Color.Silver;
            settingsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { EnterBotTokenToolStripMenuItem, ConnectionToolStripMenuItem });
            settingsToolStripMenuItem.ForeColor = Color.Black;
            settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            settingsToolStripMenuItem.Size = new Size(61, 20);
            settingsToolStripMenuItem.Text = "Settings";
            // 
            // EnterBotTokenToolStripMenuItem
            // 
            EnterBotTokenToolStripMenuItem.Name = "EnterBotTokenToolStripMenuItem";
            EnterBotTokenToolStripMenuItem.Size = new Size(157, 22);
            EnterBotTokenToolStripMenuItem.Text = "Enter Bot Token";
            EnterBotTokenToolStripMenuItem.Click += EnterBotTokenToolStripMenuItem_Click;
            // 
            // ConnectionToolStripMenuItem
            // 
            ConnectionToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { ConnectBotToolStripMenuItem, DisconnectBotToolStripMenuItem });
            ConnectionToolStripMenuItem.Name = "ConnectionToolStripMenuItem";
            ConnectionToolStripMenuItem.Size = new Size(157, 22);
            ConnectionToolStripMenuItem.Text = "Bot Connection";
            // 
            // ConnectBotToolStripMenuItem
            // 
            ConnectBotToolStripMenuItem.Name = "ConnectBotToolStripMenuItem";
            ConnectBotToolStripMenuItem.Size = new Size(133, 22);
            ConnectBotToolStripMenuItem.Text = "Connect";
            ConnectBotToolStripMenuItem.Click += ConnectBotToolStripMenuItem_Click;
            // 
            // DisconnectBotToolStripMenuItem
            // 
            DisconnectBotToolStripMenuItem.Name = "DisconnectBotToolStripMenuItem";
            DisconnectBotToolStripMenuItem.Size = new Size(133, 22);
            DisconnectBotToolStripMenuItem.Text = "Disconnect";
            DisconnectBotToolStripMenuItem.Click += DisconnectBotToolStripMenuItem_Click;
            // 
            // HelpToolStripMenuItem
            // 
            HelpToolStripMenuItem.BackColor = Color.Silver;
            HelpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { CheckForUpdatesToolStripMenuItem, AboutToolStripMenuItem, DonateToolStripMenuItem });
            HelpToolStripMenuItem.ForeColor = Color.Black;
            HelpToolStripMenuItem.Name = "HelpToolStripMenuItem";
            HelpToolStripMenuItem.Size = new Size(44, 20);
            HelpToolStripMenuItem.Text = "Help";
            // 
            // CheckForUpdatesToolStripMenuItem
            // 
            CheckForUpdatesToolStripMenuItem.Name = "CheckForUpdatesToolStripMenuItem";
            CheckForUpdatesToolStripMenuItem.Size = new Size(173, 22);
            CheckForUpdatesToolStripMenuItem.Text = "Check For Updates";
            CheckForUpdatesToolStripMenuItem.Click += CheckForUpdatesToolStripMenuItem_Click_1;
            // 
            // AboutToolStripMenuItem
            // 
            AboutToolStripMenuItem.Name = "AboutToolStripMenuItem";
            AboutToolStripMenuItem.Size = new Size(173, 22);
            AboutToolStripMenuItem.Text = "About";
            AboutToolStripMenuItem.Click += AboutToolStripMenuItem_Click;
            // 
            // DonateToolStripMenuItem
            // 
            DonateToolStripMenuItem.Name = "DonateToolStripMenuItem";
            DonateToolStripMenuItem.Size = new Size(173, 22);
            DonateToolStripMenuItem.Text = "Donate";
            DonateToolStripMenuItem.Click += DonateToolStripMenuItem_Click_1;
            // 
            // richTextBox1
            // 
            richTextBox1.BackColor = Color.LightGray;
            richTextBox1.Dock = DockStyle.Top;
            richTextBox1.Location = new Point(4, 98);
            richTextBox1.Margin = new Padding(4, 3, 4, 3);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.ReadOnly = true;
            richTextBox1.Size = new Size(925, 283);
            richTextBox1.TabIndex = 1;
            richTextBox1.Text = "";
            richTextBox1.LinkClicked += Link_Clicked;
            // 
            // ToolStripButton1
            // 
            ToolStripButton1.BackColor = Color.Silver;
            ToolStripButton1.DisplayStyle = ToolStripItemDisplayStyle.Image;
            ToolStripButton1.Image = (Image)resources.GetObject("ToolStripButton1.Image");
            ToolStripButton1.ImageTransparentColor = Color.Magenta;
            ToolStripButton1.Name = "ToolStripButton1";
            ToolStripButton1.Overflow = ToolStripItemOverflow.Never;
            ToolStripButton1.Padding = new Padding(0, 5, 20, 0);
            ToolStripButton1.Size = new Size(40, 25);
            ToolStripButton1.Text = "toolStripButton1";
            ToolStripButton1.ToolTipText = "copy selected/all text";
            ToolStripButton1.Click += ToolStripButton1_Click;
            // 
            // ToolStripButton2
            // 
            ToolStripButton2.BackColor = Color.Silver;
            ToolStripButton2.DisplayStyle = ToolStripItemDisplayStyle.Image;
            ToolStripButton2.Image = (Image)resources.GetObject("ToolStripButton2.Image");
            ToolStripButton2.ImageTransparentColor = Color.Magenta;
            ToolStripButton2.Name = "ToolStripButton2";
            ToolStripButton2.Overflow = ToolStripItemOverflow.Never;
            ToolStripButton2.Padding = new Padding(20, 5, 0, 0);
            ToolStripButton2.Size = new Size(40, 25);
            ToolStripButton2.Text = "toolStripButton2";
            ToolStripButton2.ToolTipText = "clear the entire log window";
            ToolStripButton2.Click += ToolStripButton2_Click;
            // 
            // toolStrip1
            // 
            toolStrip1.AllowMerge = false;
            toolStrip1.AutoSize = false;
            toolStrip1.BackColor = Color.White;
            toolStrip1.Dock = DockStyle.Bottom;
            toolStrip1.GripMargin = new Padding(0);
            toolStrip1.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip1.Items.AddRange(new ToolStripItem[] { ToolStripButton1, ToolStripButton2 });
            toolStrip1.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
            toolStrip1.Location = new Point(4, 408);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(925, 28);
            toolStrip1.TabIndex = 4;
            toolStrip1.Text = "toolStrip1";
            // 
            // progressBar1
            // 
            progressBar1.Dock = DockStyle.Bottom;
            progressBar1.Location = new Point(4, 381);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(925, 27);
            progressBar1.Step = 1;
            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.TabIndex = 5;
            // 
            // Slackord
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(933, 439);
            Controls.Add(progressBar1);
            Controls.Add(richTextBox1);
            Controls.Add(toolStrip1);
            Controls.Add(menuStrip1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            Margin = new Padding(4, 3, 4, 3);
            Name = "Slackord";
            Padding = new Padding(4, 74, 4, 3);
            Text = "Slackord 2";
            Load += Form1_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem FileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem HelpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem AboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem EnterBotTokenToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ExitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ConnectionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ConnectBotToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem DisconnectBotToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem CheckForUpdatesToolStripMenuItem;
        public System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.ToolStripButton ToolStripButton1;
        private System.Windows.Forms.ToolStripButton ToolStripButton2;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripMenuItem DonateToolStripMenuItem;
        private ToolStripMenuItem importJSONFolderToolStripMenuItem;
        private ProgressBar progressBar1;
    }
}

