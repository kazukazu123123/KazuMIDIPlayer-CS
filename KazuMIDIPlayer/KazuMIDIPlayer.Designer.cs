namespace KazuMIDIPlayer
{
    partial class KazuMIDIPlayer
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            StatusStrip = new StatusStrip();
            StatusStrip_PlaybackStatusLabel = new ToolStripStatusLabel();
            MenuStrip = new MenuStrip();
            MenuStrip_FileMenu = new ToolStripMenuItem();
            MenuStrip_FileMenu_LoadMIDI = new ToolStripMenuItem();
            MenuStrip_FileMenu_Exit = new ToolStripMenuItem();
            MenuStrip_Options = new ToolStripMenuItem();
            ControlPanel = new Panel();
            PlayPauseCheckBox = new CheckBox();
            GraphicPanel = new Panel();
            StatusStrip.SuspendLayout();
            MenuStrip.SuspendLayout();
            ControlPanel.SuspendLayout();
            SuspendLayout();
            // 
            // StatusStrip
            // 
            StatusStrip.Items.AddRange(new ToolStripItem[] { StatusStrip_PlaybackStatusLabel });
            StatusStrip.Location = new Point(0, 379);
            StatusStrip.Name = "StatusStrip";
            StatusStrip.Size = new Size(624, 22);
            StatusStrip.SizingGrip = false;
            StatusStrip.TabIndex = 1;
            StatusStrip.Text = "statusStrip1";
            // 
            // StatusStrip_PlaybackStatusLabel
            // 
            StatusStrip_PlaybackStatusLabel.Name = "StatusStrip_PlaybackStatusLabel";
            StatusStrip_PlaybackStatusLabel.Size = new Size(174, 17);
            StatusStrip_PlaybackStatusLabel.Text = "statusStrip_playbackStatusLabel";
            // 
            // MenuStrip
            // 
            MenuStrip.Items.AddRange(new ToolStripItem[] { MenuStrip_FileMenu, MenuStrip_Options });
            MenuStrip.Location = new Point(0, 0);
            MenuStrip.Name = "MenuStrip";
            MenuStrip.Size = new Size(624, 24);
            MenuStrip.TabIndex = 2;
            MenuStrip.Text = "menuStrip1";
            // 
            // MenuStrip_FileMenu
            // 
            MenuStrip_FileMenu.DropDownItems.AddRange(new ToolStripItem[] { MenuStrip_FileMenu_LoadMIDI, MenuStrip_FileMenu_Exit });
            MenuStrip_FileMenu.Name = "MenuStrip_FileMenu";
            MenuStrip_FileMenu.Size = new Size(37, 20);
            MenuStrip_FileMenu.Text = "File";
            // 
            // MenuStrip_FileMenu_LoadMIDI
            // 
            MenuStrip_FileMenu_LoadMIDI.Name = "MenuStrip_FileMenu_LoadMIDI";
            MenuStrip_FileMenu_LoadMIDI.Size = new Size(128, 22);
            MenuStrip_FileMenu_LoadMIDI.Text = "Load MIDI";
            MenuStrip_FileMenu_LoadMIDI.Click += MenuStrip_FileMenu_LoadMIDI_Click;
            // 
            // MenuStrip_FileMenu_Exit
            // 
            MenuStrip_FileMenu_Exit.Name = "MenuStrip_FileMenu_Exit";
            MenuStrip_FileMenu_Exit.Size = new Size(128, 22);
            MenuStrip_FileMenu_Exit.Text = "Exit";
            MenuStrip_FileMenu_Exit.Click += MenuStrip_FileMenu_Exit_Click;
            // 
            // MenuStrip_Options
            // 
            MenuStrip_Options.Name = "MenuStrip_Options";
            MenuStrip_Options.Size = new Size(61, 20);
            MenuStrip_Options.Text = "Options";
            MenuStrip_Options.Click += MenuStrip_Options_Click;
            // 
            // ControlPanel
            // 
            ControlPanel.Controls.Add(PlayPauseCheckBox);
            ControlPanel.Dock = DockStyle.Bottom;
            ControlPanel.Location = new Point(0, 348);
            ControlPanel.Name = "ControlPanel";
            ControlPanel.Size = new Size(624, 31);
            ControlPanel.TabIndex = 4;
            // 
            // PlayPauseCheckBox
            // 
            PlayPauseCheckBox.Appearance = Appearance.Button;
            PlayPauseCheckBox.AutoSize = true;
            PlayPauseCheckBox.Location = new Point(3, 3);
            PlayPauseCheckBox.Name = "PlayPauseCheckBox";
            PlayPauseCheckBox.Size = new Size(81, 25);
            PlayPauseCheckBox.TabIndex = 5;
            PlayPauseCheckBox.Text = "Play / Pause";
            PlayPauseCheckBox.UseVisualStyleBackColor = true;
            PlayPauseCheckBox.CheckedChanged += PlayPauseCheckBox_CheckedChanged;
            // 
            // GraphicPanel
            // 
            GraphicPanel.BackColor = Color.Black;
            GraphicPanel.BorderStyle = BorderStyle.FixedSingle;
            GraphicPanel.Dock = DockStyle.Fill;
            GraphicPanel.Location = new Point(0, 24);
            GraphicPanel.Name = "GraphicPanel";
            GraphicPanel.Size = new Size(624, 324);
            GraphicPanel.TabIndex = 10;
            GraphicPanel.Paint += GraphicPanel_Paint;
            GraphicPanel.MouseDown += GraphicPanel_MouseDown;
            GraphicPanel.MouseMove += GraphicPanel_MouseMove;
            GraphicPanel.MouseUp += GraphicPanel_MouseUp;
            // 
            // KazuMIDIPlayer
            // 
            AllowDrop = true;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(624, 401);
            Controls.Add(GraphicPanel);
            Controls.Add(ControlPanel);
            Controls.Add(StatusStrip);
            Controls.Add(MenuStrip);
            MainMenuStrip = MenuStrip;
            MinimumSize = new Size(640, 360);
            Name = "KazuMIDIPlayer";
            Text = "KazuMIDIPlayer";
            FormClosing += KazuMIDIPlayer_FormClosing;
            Load += KazuMIDIPlayer_Load;
            DragDrop += KazuMIDIPlayer_DragDrop;
            DragEnter += KazuMIDIPlayer_DragEnter;
            StatusStrip.ResumeLayout(false);
            StatusStrip.PerformLayout();
            MenuStrip.ResumeLayout(false);
            MenuStrip.PerformLayout();
            ControlPanel.ResumeLayout(false);
            ControlPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private StatusStrip StatusStrip;
        private ToolStripStatusLabel StatusStrip_PlaybackStatusLabel;
        private MenuStrip MenuStrip;
        private ToolStripMenuItem MenuStrip_FileMenu;
        private ToolStripMenuItem MenuStrip_FileMenu_LoadMIDI;
        private ToolStripMenuItem MenuStrip_Options;
        private Panel ControlPanel;
        private CheckBox PlayPauseCheckBox;
        private ToolStripMenuItem MenuStrip_FileMenu_Exit;
        private Panel GraphicPanel;
    }
}
