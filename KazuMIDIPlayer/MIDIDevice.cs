using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KazuMIDIPlayer
{
    internal class MIDIDevice
    {
        private bool useKDMAPI;
        private string currentSelectedMIDIDevice = "";

        public string GetMIDIDevice()
        {
            return currentSelectedMIDIDevice;
        }

        public void SetMIDIDevice(string midiDevice)
        {
            if (midiDevice == null || currentSelectedMIDIDevice == midiDevice)
                return;

            // Disable previously selected device
            switch (currentSelectedMIDIDevice)
            {
                case "[NONE]":
                    break;
                case "KDMAPI":
                    if (useKDMAPI)
                    {
                        KDMAPI.TerminateKDMAPIStream();
                        useKDMAPI = false;
                    }
                    break;
                default:
                    break;
            }

            currentSelectedMIDIDevice = midiDevice;
            switch (currentSelectedMIDIDevice)
            {
                case "[NONE]":
                    break;
                case "KDMAPI":
                    if (!useKDMAPI)
                    {
                        KDMAPI.InitializeKDMAPIStream();
                        useKDMAPI = true;
                    }
                    break;
                default:
                    break;
            }
        }
        public void SendMIDIEvent(int status, int data1, int data2)
        {
            switch (currentSelectedMIDIDevice)
            {
                case "[NONE]":
                    break;
                case "KDMAPI":
                    _ = KDMAPI.SendDirectData((uint)(status | data1 << 8 | (data2 << 16)));
                    break;
                default:
                    break;
            }
        }
    }
}
