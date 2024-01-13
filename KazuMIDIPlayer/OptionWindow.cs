using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KazuMIDIPlayer
{
    public partial class OptionWindow : Form
    {
        public OptionWindow()
        {
            InitializeComponent();
        }

        private void OptionWindow_Shown(object sender, EventArgs e)
        {
            Properties.Settings.Default.Reload();
            checkBox_useKDMAPI.Checked = Properties.Settings.Default.useKDMAPI;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.useKDMAPI = checkBox_useKDMAPI.Checked;
            Properties.Settings.Default.Save();
            Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
