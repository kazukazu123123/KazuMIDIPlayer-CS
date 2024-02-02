using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
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

        private void RefreshMIDIDeviceList()
        {
            comboBox_MIDIOutSelect.Items.Clear();

            comboBox_MIDIOutSelect.Items.Add("[NONE]");
            comboBox_MIDIOutSelect.Items.Add("KDMAPI");

            comboBox_MIDIOutSelect.SelectedItem = Properties.Settings.Default.midiDevice;
        }

        private void OptionWindow_Shown(object sender, EventArgs e)
        {
            Properties.Settings.Default.Reload();

            RefreshMIDIDeviceList();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (comboBox_MIDIOutSelect.SelectedItem == null) return;
            string selectedDevice = (string)comboBox_MIDIOutSelect.SelectedItem;
            Properties.Settings.Default.midiDevice = selectedDevice;
            Properties.Settings.Default.Save();
            Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void RefreshMIDIDeviceButton_Click(object sender, EventArgs e)
        {
            RefreshMIDIDeviceList();
        }
    }
}
