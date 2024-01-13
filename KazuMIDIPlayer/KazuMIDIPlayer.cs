using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KazuMIDIPlayer
{
    public partial class KazuMIDIPlayer : Form
    {
        private int currentTick = 0;
        private int noteCount = 0;
        private int polyphony = 0;
        private int maxPolyphony = 0;
        private bool useKDMAPI = false;
        private bool KDMAPIInitialized = false;

        private static readonly object polyphony_lockObj = new();
        private static readonly object pianoKeyboardArray_lockObj = new();
        private static readonly object pianoKeyboardChannelArray_lockObj = new();

        private readonly System.Timers.Timer graphicUpdateTimer = new();
        private readonly Color[] channelColorMap =
        [
            Color.FromArgb(51, 102, 255),
            Color.FromArgb(255, 126, 51),
            Color.FromArgb(51, 255, 102),
            Color.FromArgb(255, 51, 129),
            Color.FromArgb(51, 255, 255),
            Color.FromArgb(228, 51, 255),
            Color.FromArgb(153, 255, 51),
            Color.FromArgb(75, 51, 255),
            Color.FromArgb(255, 204, 51),
            Color.FromArgb(51, 180, 255),
            Color.FromArgb(255, 51, 51),
            Color.FromArgb(51, 255, 177),
            Color.FromArgb(255, 51, 204),
            Color.FromArgb(78, 255, 51),
            Color.FromArgb(153, 51, 255),
            Color.FromArgb(231, 255, 51)
        ];

        private readonly int[] pianoKeyboardArray = new int[128];
        private readonly bool[][] pianoKeyboardChannelArray = new bool[16][].Select(_ => new bool[128]).ToArray();

        private readonly static int pianoKeyboard_whiteKeyWidth = 400 * 2 / 75;
        private readonly static int pianoKeyboard_whiteKeyHeight = 200 * 2 / (9 - 1);
        private readonly static int pianoKeyboard_blackKeyWidth = (int)(pianoKeyboard_whiteKeyWidth * 0.65);
        private readonly static int pianoKeyboard_blackKeyHeight = (int)(pianoKeyboard_whiteKeyHeight * 0.65);
        private static int pianoKeyboard_keyXPos = 0;

        private readonly static int pianoKeyboardChannel_whiteKeyWidth = 150 * 2 / 75;
        private readonly static int pianoKeyboardChannel_whiteKeyHeight = 50 * 2 / (9 - 1);
        private readonly static int pianoKeyboardChannel_blackKeyWidth = (int)(pianoKeyboardChannel_whiteKeyWidth * 0.65);
        private readonly static int pianoKeyboardChannel_blackKeyHeight = (int)(pianoKeyboardChannel_whiteKeyHeight * 0.65);
        private static int pianoKeyboardChannel_keyXPos = 0;

        private readonly MIDIPlayer midiPlayer = new();

        private readonly WindowManager windowManager = new();

        private void StopAllNotes()
        {
            for (int channel = 0; channel <= 16; channel++)
            {
                for (int note = 0; note < 128; note++)
                {
                    byte statusByte = (byte)(0x80 | channel);
                    byte noteNumber = (byte)note;
                    byte velocity = 0;

                    SendMIDIEvent(statusByte, noteNumber, velocity);
                }
            }
        }

        private void SendMIDIEvent(int status, int data1, int data2)
        {
            if (useKDMAPI) _ = KDMAPI.SendDirectData((uint)(status | data1 << 8 | (data2 << 16)));
        }

        public KazuMIDIPlayer()
        {
            InitializeComponent();
        }

        private void KazuMIDIPlayer_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.windowMaximized)
            {
                WindowState = FormWindowState.Maximized;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                && Properties.Settings.Default.useKDMAPI)
            {
                useKDMAPI = true;
                if (KDMAPIInitialized) KDMAPI.TerminateKDMAPIStream();
                KDMAPI.InitializeKDMAPIStream();
                KDMAPIInitialized = true;
            }

            GraphicPanel.GetType().InvokeMember(
                "DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null,
                GraphicPanel,
                new object[] { true }
            );

            Properties.Settings.Default.SettingChanging += Default_SettingChanging;

            midiPlayer.OnLoadingEvent += MidiPlayer_OnLoadingEvent;
            midiPlayer.OnLoadedEvent += MidiPlayer_OnLoadedEvent;
            midiPlayer.OnLoadErrorEvent += MidiPlayer_OnLoadErrorEvent;
            midiPlayer.OnMIDIEvent += MidiPlayer_OnMIDIEvent;
            midiPlayer.OnTickEvent += MidiPlayer_OnTickEvent;
            midiPlayer.OnPlaybackStateChangeEvent += MidiPlayer_OnPlaybackStateChangeEvent;

            graphicUpdateTimer.Interval = 1000 / 60;
            graphicUpdateTimer.Elapsed += GraphicUpdateTimer_Elapsed;
            graphicUpdateTimer.Start();

            if (!Properties.Settings.Default.firstLaunch)
            {
                Point windowPos = Properties.Settings.Default.windowPos;
                Size windowSize = Properties.Settings.Default.windowSize;
                Location = new Point(windowPos.X, windowPos.Y);
                Size = new Size(windowSize.Width, windowSize.Height);
            }
            StatusStrip_PlaybackStatusLabel.Text = "PlaybackStatus: Stopped";
            PlayPauseCheckBox.Enabled = false;

            // statusWindow
            Window statusWindow = windowManager.NewWindow("Status", 20, 20, 160, 65);

            statusWindow.DrawGraphicsEvent += (g, x, y, w, h) =>
            {
                int margin = 5;
                int column = 0;
                int spacing = 15;

                Font font = new("MS Gothic", 10);
                SolidBrush fontBrush = new(Color.FromArgb(255, 255, 255));
                StringFormat sf = new();

                Pen polyphonyProgressBarBorderPen = new(Color.FromArgb(51, 51, 51));
                SolidBrush polyphonyProgressBarBackgroundBrush = new(Color.FromArgb(90, 90, 90));
                SolidBrush polyphonyProgressBarBrush = new(Color.FromArgb(169, 206, 236));

                string tickString = $"Tick: {currentTick} / {midiPlayer.midiFile.Division}";
                string playedNotesString = $"Played Notes: {noteCount}";
                string polyphonyString = $"Polyphony: {polyphony} / {maxPolyphony}";

                SizeF? tickStringWidth = g?.MeasureString(tickString, font, 1000, sf);
                SizeF? playedNotesStringWidth = g?.MeasureString(playedNotesString, font, 1000, sf);
                SizeF? polyphonyStringWidth = g?.MeasureString(polyphonyString, font, 1000, sf);

                int statusTextMaxSize = (int)Math.Max(tickStringWidth?.Width ?? 0,
                    Math.Max(playedNotesStringWidth?.Width ?? 0,
                    polyphonyStringWidth?.Width ?? 0)) + 5; // padding

                windowManager.ResizeWindow(statusWindow.Id, statusTextMaxSize, h);

                // Tick
                g?.DrawString(tickString, font, fontBrush, x + 2, y + margin + column * spacing);
                column++;

                // Played Notes
                g?.DrawString(playedNotesString, font, fontBrush, x + 2, y + margin + column * spacing);
                column++;

                // Polyphony
                g?.DrawString(polyphonyString, font, fontBrush, x + 2, y + margin + column * spacing);
                column++;

                // Polyphony Progress bar
                // Border
                g?.DrawRectangle(polyphonyProgressBarBorderPen, x + 2, y + margin + column * spacing, statusWindow.Rectangle.Width - 5, 10);
                // Progress bar
                if (maxPolyphony != 0)
                {
                    float progressRatio = (float)polyphony / maxPolyphony;
                    g?.FillRectangle(polyphonyProgressBarBackgroundBrush, x + 3, y + margin + column * spacing + 1, statusWindow.Rectangle.Width - 6, 9);
                    g?.FillRectangle(polyphonyProgressBarBrush, x + 3, y + margin + column * spacing + 1, progressRatio * statusWindow.Rectangle.Width - 6, 9);
                }
                else
                {
                    g?.FillRectangle(polyphonyProgressBarBackgroundBrush, x + 3, y + margin + column * spacing + 1, statusWindow.Rectangle.Width - 6, 9);
                }
                column++;

                font.Dispose();
                sf.Dispose();
                fontBrush.Dispose();
                polyphonyProgressBarBackgroundBrush.Dispose();
                polyphonyProgressBarBrush.Dispose();
            };

            // keyboardWindow
            Window keyboardWindow = windowManager.NewWindow("Piano Keyboard", 20, 130, pianoKeyboard_whiteKeyWidth * 75 + 1, pianoKeyboard_whiteKeyHeight + 1); // + 1 for the outlines

            keyboardWindow.DrawGraphicsEvent += (g, x, y, w, h) =>
            {
                Pen pianoKeyboardBorderPen = new(Color.FromArgb(160, 160, 160));
                SolidBrush brush = new(Color.FromArgb(255, 128, 0));
                SolidBrush brush2 = new(Color.FromArgb(255, 0, 0));
                // Draw keyboard
                pianoKeyboard_keyXPos = x;

                // White key
                for (int i = 0; i < 128; i++)
                {
                    bool isBlackKey =
                      i % 12 == 1 ||
                      i % 12 == 3 ||
                      i % 12 == 6 ||
                      i % 12 == 8 ||
                      i % 12 == 10;

                    if (!isBlackKey)
                    {
                        lock (pianoKeyboardArray_lockObj)
                        {
                            SolidBrush keyBrush = new(((pianoKeyboardArray[i] & 1) != 0) ? channelColorMap[pianoKeyboardArray[i] >> 1] : Color.FromArgb(255, 255, 255));

                            if (pianoKeyboardArray[i] != 0)
                            {
                                g?.FillRectangle(keyBrush, pianoKeyboard_keyXPos, y + 1, pianoKeyboard_whiteKeyWidth, pianoKeyboard_whiteKeyHeight - 1);
                            }
                            else
                            {
                                g?.FillRectangle(keyBrush, pianoKeyboard_keyXPos, y + 1, pianoKeyboard_whiteKeyWidth, pianoKeyboard_whiteKeyHeight - 1);
                            }
                            g?.DrawRectangle(pianoKeyboardBorderPen, pianoKeyboard_keyXPos, y + 1, pianoKeyboard_whiteKeyWidth, pianoKeyboard_whiteKeyHeight - 1);
                            pianoKeyboard_keyXPos += pianoKeyboard_whiteKeyWidth;
                            keyBrush.Dispose();
                        }
                    }
                }

                pianoKeyboard_keyXPos = x;

                // Black key
                for (int i = 0; i < 128; i++)
                {
                    bool isBlackKey =
                      i % 12 == 1 ||
                      i % 12 == 3 ||
                      i % 12 == 6 ||
                      i % 12 == 8 ||
                      i % 12 == 10;

                    if (isBlackKey)
                    {
                        lock (pianoKeyboardArray_lockObj)
                        {
                            SolidBrush keyBrush = new(((pianoKeyboardArray[i] & 1) != 0) ? channelColorMap[pianoKeyboardArray[i] >> 1] : Color.FromArgb(0, 0, 0));
                            int shift = pianoKeyboard_blackKeyWidth / 2;
                            if (i % 12 == 1 || i % 12 == 6) shift += pianoKeyboard_blackKeyWidth / 8;
                            if (i % 12 == 3 || i % 12 == 10) shift -= pianoKeyboard_blackKeyWidth / 8;

                            g?.FillRectangle(keyBrush, pianoKeyboard_keyXPos - shift, y + 1, pianoKeyboard_blackKeyWidth, pianoKeyboard_blackKeyHeight);
                            g?.DrawRectangle(pianoKeyboardBorderPen, pianoKeyboard_keyXPos - shift, y + 1, pianoKeyboard_blackKeyWidth, pianoKeyboard_blackKeyHeight);
                            keyBrush.Dispose();
                        }
                    }
                    else
                    {
                        pianoKeyboard_keyXPos += pianoKeyboard_whiteKeyWidth;
                    }
                }

                brush.Dispose();
                brush2.Dispose();
            };

            // keyboardChanenlWindow
            Window keyboardChanenlWindow = windowManager.NewWindow("Piano Keyboard Channel", 120, 20, pianoKeyboardChannel_whiteKeyWidth * 75 + 1, 16 * pianoKeyboardChannel_whiteKeyHeight + 1); // + 1 for the outlines
            keyboardChanenlWindow.DrawGraphicsEvent += (g, x, y, w, h) =>
            {
                Pen pianoKeyboardBorderPen = new(Color.FromArgb(160, 160, 160));
                //Color pianoKeyboardColor = Color.FromArgb(65, 105, 225);

                for (int row = 0; row < 16; row++)
                {
                    // Draw keyboard for each row
                    pianoKeyboardChannel_keyXPos = x;

                    // White key
                    for (int i = 0; i < 128; i++)
                    {
                        bool isBlackKey =
                          i % 12 == 1 ||
                          i % 12 == 3 ||
                          i % 12 == 6 ||
                          i % 12 == 8 ||
                          i % 12 == 10;

                        if (!isBlackKey)
                        {
                            lock (pianoKeyboardChannelArray_lockObj)
                            {
                                SolidBrush keyBrush = new(pianoKeyboardChannelArray[row][i] ? channelColorMap[row] : Color.FromArgb(255, 255, 255));

                                g?.FillRectangle(keyBrush, pianoKeyboardChannel_keyXPos, y + row * pianoKeyboardChannel_whiteKeyHeight + 1, pianoKeyboardChannel_whiteKeyWidth, pianoKeyboardChannel_whiteKeyHeight - 1);
                                g?.DrawRectangle(pianoKeyboardBorderPen, pianoKeyboardChannel_keyXPos, y + row * pianoKeyboardChannel_whiteKeyHeight + 1, pianoKeyboardChannel_whiteKeyWidth, pianoKeyboardChannel_whiteKeyHeight - 1);
                                pianoKeyboardChannel_keyXPos += pianoKeyboardChannel_whiteKeyWidth;
                                keyBrush.Dispose();
                            }
                        }
                    }

                    pianoKeyboardChannel_keyXPos = x;

                    // Black key
                    for (int i = 0; i < 128; i++)
                    {
                        bool isBlackKey =
                          i % 12 == 1 ||
                          i % 12 == 3 ||
                          i % 12 == 6 ||
                          i % 12 == 8 ||
                          i % 12 == 10;

                        if (isBlackKey)
                        {
                            lock (pianoKeyboardChannelArray_lockObj)
                            {
                                SolidBrush keyBrush = new(pianoKeyboardChannelArray[row][i] ? channelColorMap[row] : Color.FromArgb(0, 0, 0));
                                int shift = pianoKeyboardChannel_blackKeyWidth / 2;
                                if (i % 12 == 1 || i % 12 == 6) shift += pianoKeyboardChannel_blackKeyWidth / 8;
                                if (i % 12 == 3 || i % 12 == 10) shift -= pianoKeyboardChannel_blackKeyWidth / 8;

                                if (pianoKeyboardChannelArray[row][i] == true)
                                {
                                    g?.FillRectangle(keyBrush, pianoKeyboardChannel_keyXPos - shift, y + 1 + row * pianoKeyboardChannel_whiteKeyHeight + 1, pianoKeyboardChannel_blackKeyWidth, pianoKeyboardChannel_blackKeyHeight - 1);
                                }
                                else
                                {
                                    g?.FillRectangle(keyBrush, pianoKeyboardChannel_keyXPos - shift, y + 1 + row * pianoKeyboardChannel_whiteKeyHeight + 1, pianoKeyboardChannel_blackKeyWidth, pianoKeyboardChannel_blackKeyHeight - 1);
                                }
                                g?.DrawRectangle(pianoKeyboardBorderPen, pianoKeyboardChannel_keyXPos - shift, y + 1 * row * pianoKeyboardChannel_whiteKeyHeight + 1, pianoKeyboardChannel_blackKeyWidth, pianoKeyboardChannel_blackKeyHeight - 1);
                                keyBrush.Dispose();
                            }
                        }
                        else
                        {
                            pianoKeyboardChannel_keyXPos += pianoKeyboardChannel_whiteKeyWidth;
                        }
                    }
                }
            };
        }

        private void GraphicPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            windowManager.MouseDown(e.Location, GraphicPanel.ClientRectangle);
        }

        private void GraphicPanel_MouseMove(object sender, MouseEventArgs e)
        {
            windowManager.MouseMove(e.Location);
        }

        private void GraphicPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            windowManager.MouseUp();
        }

        private void GraphicPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            Color clearColor = Color.FromArgb(51, 51, 51);

            // Clear screen
            g.Clear(clearColor);

            // Draw window
            windowManager.DrawWindow(g);
        }

        private void GraphicUpdateTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            GraphicPanel.Invalidate();
        }

        private void MidiPlayer_OnLoadingEvent(object? sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void MidiPlayer_OnLoadedEvent(object? sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void MidiPlayer_OnLoadErrorEvent(object? sender, LoadErrorEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void MidiPlayer_OnMIDIEvent(object? sender, MIDIEventArgs e)
        {
            if ((e.Status & 0xF0) == 0x90)
            {
                int channel = e.Status & 0x0F;
                int note = e.Data1;
                int velocity = e.Data2 ?? 0;

                if (velocity != 0)
                {
                    lock (polyphony_lockObj)
                    {
                        polyphony += 1;
                        if (polyphony > maxPolyphony) maxPolyphony = polyphony;
                    }
                    noteCount += 1;
                    pianoKeyboardArray[note] = channel << 1 | 1;
                    pianoKeyboardChannelArray[channel][note] = true;
                }
                else
                {
                    lock (polyphony_lockObj)
                    {
                        if (polyphony > 0) polyphony -= 1;
                    }
                    pianoKeyboardArray[note] = 0;
                    pianoKeyboardChannelArray[channel][note] = false;
                }
            }
            if ((e.Status & 0xF0) == 0x80)
            {
                int channel = e.Status & 0x0F;
                int note = e.Data1;

                pianoKeyboardArray[note] = 0;
                pianoKeyboardChannelArray[channel][note] = false;
                lock (polyphony_lockObj)
                {
                    if (polyphony > 0) polyphony -= 1;
                }
            }
            SendMIDIEvent(e.Status, e.Data1, e.Data2 ?? 0);
        }

        private void MidiPlayer_OnTickEvent(object? sender, TickEventArgs e)
        {
            currentTick = e.CurrentTick;
        }

        private void MidiPlayer_OnPlaybackStateChangeEvent(object? sender, PlaybackStateEventArgs e)
        {
            switch (e.State)
            {
                case PlaybackStateEventArgs.StateEnum.Playing:
                    PlayPauseCheckBox.Invoke(new System.Windows.Forms.MethodInvoker(delegate
                    {
                        PlayPauseCheckBox.Enabled = true;
                    }));
                    StatusStrip_PlaybackStatusLabel.Text = "PlaybackStatus: Playing";
                    PlayPauseCheckBox.Invoke(new System.Windows.Forms.MethodInvoker(delegate
                    {
                        PlayPauseCheckBox.Checked = true;
                    }));
                    break;
                case PlaybackStateEventArgs.StateEnum.Paused:
                    PlayPauseCheckBox.Invoke(new System.Windows.Forms.MethodInvoker(delegate
                    {
                        PlayPauseCheckBox.Enabled = true;
                    }));
                    StopAllNotes();
                    //if (useKDMAPI) KDMAPI.ResetKDMAPIStream(); //no
                    StatusStrip_PlaybackStatusLabel.Text = "PlaybackStatus: Paused";
                    PlayPauseCheckBox.Invoke(new System.Windows.Forms.MethodInvoker(delegate
                    {
                        PlayPauseCheckBox.Checked = false;
                    }));
                    break;
                case PlaybackStateEventArgs.StateEnum.Stopped:
                    noteCount = 0;
                    polyphony = 0;
                    maxPolyphony = 0;
                    PlayPauseCheckBox.Invoke(new System.Windows.Forms.MethodInvoker(delegate
                    {
                        PlayPauseCheckBox.Enabled = false;
                    }));
                    StopAllNotes();
                    if (useKDMAPI) KDMAPI.ResetKDMAPIStream();
                    StatusStrip_PlaybackStatusLabel.Text = "PlaybackStatus: Stopped";
                    PlayPauseCheckBox.Invoke(new System.Windows.Forms.MethodInvoker(delegate
                    {
                        PlayPauseCheckBox.Checked = false;
                    }));
                    for (int i = 0; i < 128; i++)
                    {
                        pianoKeyboardArray[i] = 0;
                    }
                    for (int i = 0; i < 16; i++)
                    {
                        for (int j = 0; j < 128; j++)
                        {
                            pianoKeyboardChannelArray[i][j] = false;
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        private void Default_SettingChanging(object sender, System.Configuration.SettingChangingEventArgs e)
        {
            switch (e.SettingName)
            {
                case "useKDMAPI":
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) e.Cancel = true;

                    if ((bool)e.NewValue)
                    {
                        if (KDMAPI.InitializeKDMAPIStream()) useKDMAPI = true;
                    }
                    else
                    {
                        if (KDMAPI.TerminateKDMAPIStream()) useKDMAPI = false;
                    }
                    break;
                default:
                    break;
            }
        }

        private void MenuStrip_FileMenu_LoadMIDI_Click(object sender, EventArgs e)
        {
            using OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "MIDI (*.midi *.mid)|*.midi;*.mid";
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                midiPlayer.Load(filePath);
            }
        }

        private void MenuStrip_FileMenu_Exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void MenuStrip_Options_Click(object sender, EventArgs e)
        {
            OptionWindow optionWindow = new();
            optionWindow.ShowDialog();
        }

        private void KazuMIDIPlayer_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (e.Data.GetData(DataFormats.FileDrop, false) is string[] files)
                {
                    string? fileExtension = Path.GetExtension(files[0]);
                    if (fileExtension != null && fileExtension.Equals(".mid", StringComparison.CurrentCultureIgnoreCase))
                    {
                        e.Effect = DragDropEffects.All;
                    }
                    else
                    {
                        e.Effect = DragDropEffects.None;
                    }
                }
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void KazuMIDIPlayer_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data == null) return;
            if (e.Data.GetData(DataFormats.FileDrop, false) is string[] files && files.Length > 0)
            {
                if (files[0] != null)
                {
                    string targetFile = files[0];
                    if (targetFile != null && Path.GetExtension(targetFile).Equals(".mid", StringComparison.CurrentCultureIgnoreCase))
                    {
                        midiPlayer.Load(files[0]);
                    }
                }
            }
        }

        private void KazuMIDIPlayer_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            graphicUpdateTimer.Stop();
            Properties.Settings.Default.firstLaunch = false;
            if (WindowState == FormWindowState.Normal)
            {
                Properties.Settings.Default.windowPos = Location;
                Properties.Settings.Default.windowSize = Size;
                Properties.Settings.Default.windowMaximized = false;
            }
            else if (WindowState == FormWindowState.Maximized)
            {
                Properties.Settings.Default.windowPos = RestoreBounds.Location;
                Properties.Settings.Default.windowSize = RestoreBounds.Size;
                Properties.Settings.Default.windowMaximized = true;
            }
            else
            {
                Properties.Settings.Default.windowPos = RestoreBounds.Location;
                Properties.Settings.Default.windowSize = RestoreBounds.Size;
                Properties.Settings.Default.windowMaximized = false;
            }
            Properties.Settings.Default.Save();
            e.Cancel = false;
        }

        private void PlayPauseCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (PlayPauseCheckBox.Checked)
            {
                midiPlayer.UnPause();
            }
            else
            {
                midiPlayer.Pause();
            }
        }
    }
}
