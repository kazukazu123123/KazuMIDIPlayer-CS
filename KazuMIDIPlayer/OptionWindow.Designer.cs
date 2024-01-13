namespace KazuMIDIPlayer
{
    partial class OptionWindow
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
            tabControl = new TabControl();
            tabPage_Main = new TabPage();
            checkBox_useKDMAPI = new CheckBox();
            SaveButton = new Button();
            CancelButton = new Button();
            tabControl.SuspendLayout();
            tabPage_Main.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl
            // 
            tabControl.Controls.Add(tabPage_Main);
            tabControl.Location = new Point(12, 12);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(440, 228);
            tabControl.TabIndex = 0;
            // 
            // tabPage_Main
            // 
            tabPage_Main.Controls.Add(checkBox_useKDMAPI);
            tabPage_Main.Location = new Point(4, 24);
            tabPage_Main.Name = "tabPage_Main";
            tabPage_Main.Padding = new Padding(3);
            tabPage_Main.Size = new Size(432, 200);
            tabPage_Main.TabIndex = 0;
            tabPage_Main.Text = "Main";
            tabPage_Main.UseVisualStyleBackColor = true;
            // 
            // checkBox_useKDMAPI
            // 
            checkBox_useKDMAPI.AutoSize = true;
            checkBox_useKDMAPI.Location = new Point(6, 6);
            checkBox_useKDMAPI.Name = "checkBox_useKDMAPI";
            checkBox_useKDMAPI.Size = new Size(92, 19);
            checkBox_useKDMAPI.TabIndex = 0;
            checkBox_useKDMAPI.Text = "Use KDMAPI";
            checkBox_useKDMAPI.UseVisualStyleBackColor = true;
            // 
            // SaveButton
            // 
            SaveButton.Location = new Point(296, 246);
            SaveButton.Name = "SaveButton";
            SaveButton.Size = new Size(75, 23);
            SaveButton.TabIndex = 1;
            SaveButton.Text = "Save";
            SaveButton.UseVisualStyleBackColor = true;
            SaveButton.Click += SaveButton_Click;
            // 
            // CancelButton
            // 
            CancelButton.Location = new Point(377, 246);
            CancelButton.Name = "CancelButton";
            CancelButton.Size = new Size(75, 23);
            CancelButton.TabIndex = 2;
            CancelButton.Text = "Cancel";
            CancelButton.UseVisualStyleBackColor = true;
            CancelButton.Click += CancelButton_Click;
            // 
            // OptionWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(464, 281);
            Controls.Add(CancelButton);
            Controls.Add(SaveButton);
            Controls.Add(tabControl);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "OptionWindow";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Option";
            Shown += OptionWindow_Shown;
            tabControl.ResumeLayout(false);
            tabPage_Main.ResumeLayout(false);
            tabPage_Main.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TabControl tabControl;
        private TabPage tabPage_Main;
        private Button SaveButton;
        private new Button CancelButton;
        private CheckBox checkBox_useKDMAPI;
    }
}