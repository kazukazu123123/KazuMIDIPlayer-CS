using System.Diagnostics;

namespace KazuMIDIPlayer
{
    internal class LoadedEventArgs(int trackLength) : EventArgs
    {
        public int TrackLength { get; } = trackLength;
    }

    internal class LoadingEventArgs(int trackLoaded, int trackLength) : EventArgs
    {
        public int TrackLoaded { get; } = trackLoaded;
        public int TrackLength { get; } = trackLength;
    }

    internal class LoadErrorEventArgs(string errorMessage) : EventArgs
    {
        public string ErrorMessage { get; } = errorMessage;
    }

    internal class PlaybackStateEventArgs(PlaybackStateEventArgs.StateEnum state) : EventArgs
    {
        public enum StateEnum
        {
            Stopped,
            Paused,
            Playing,
        }

        public StateEnum State { get; } = state;
    }

    internal class MIDIEventArgs(int status, int data1, int? data2 = null) : EventArgs
    {
        public int Status { get; } = status;
        public int Data1 { get; } = data1;
        public int? Data2 { get; } = data2;
    }

    internal class TickEventArgs(int tick) : EventArgs
    {
        public int CurrentTick { get; } = tick;
    }

    public struct MIDIFile
    {
        public int TrackCount { get; set; }
        public int Division { get; set; }
        public uint CurrentTick { get; set; }
        public byte[] Data { get; set; }
        public int[] TrackOffsets { get; set; }
        public Track[] Tracks { get; set; }
        public bool Running { get; set; }
        public bool Paused { get; set; }
        public long LastTime { get; set; }
    }

    public class Track
    {
        public int RunningStatus { get; set; }
        public uint Tick { get; set; }
        public bool Ended { get; set; }
    }

    internal class MIDIPlayer
    {
        private CancellationTokenSource? cancellationTokenSource;
        public event EventHandler<LoadingEventArgs>? OnLoadingEvent;
        public event EventHandler<LoadedEventArgs>? OnLoadedEvent;
        public event EventHandler<LoadErrorEventArgs>? OnLoadErrorEvent;
        public event EventHandler<PlaybackStateEventArgs>? OnPlaybackStateChangeEvent;
        public event EventHandler<TickEventArgs>? OnTickEvent;
        public event EventHandler<MIDIEventArgs>? OnMIDIEvent;
        public event EventHandler? OnPlaybackFinisheEvent;
        public MIDIFile midiFile;

        private static byte[] ReadBytes(BinaryReader reader, int count)
        {
            byte[] bytes = reader.ReadBytes(count);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        private static async Task SleepNanos(long nanos)
        {
            var stopwatch = Stopwatch.StartNew();

            while (true)
            {
                var elapsedNanos = stopwatch.ElapsedTicks * 1000000000 / Stopwatch.Frequency;

                if (elapsedNanos >= nanos)
                {
                    break;
                }
            }

            await Task.Yield();
        }

        private static long GetNs()
        {
            return Stopwatch.GetTimestamp();
        }

        public async void Load(string filePath)
        {
            Stop();

            midiFile.TrackCount = 0;
            midiFile.Division = 0;
            midiFile.CurrentTick = 0;
            midiFile.Data = [];
            midiFile.TrackOffsets = [];
            midiFile.Tracks = [];

            GC.Collect();
            GC.WaitForPendingFinalizers();

            using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);
            using BinaryReader reader = new(fs);
            ulong fileSize = (ulong)fs.Length;

            byte[] buf32 = new byte[4];
            byte[] buf16 = new byte[2];

            // read the header
            uint header = BitConverter.ToUInt32(ReadBytes(reader, 4), 0);
            if (header != 0x4D546864) // "MThd"
            {
                OnLoadErrorEvent?.Invoke(this, new LoadErrorEventArgs($"\"{filePath}\" is not a midi file..."));
                return;
            }

            // read the header length
            int headerLength = BitConverter.ToInt32(ReadBytes(reader, 4), 0);
            if (headerLength != 6)
            {
                OnLoadErrorEvent?.Invoke(this, new LoadErrorEventArgs($"\"{filePath}\" has an invalid header length."));
                return;
            }

            // read format
            int midiFormat = BitConverter.ToInt16(ReadBytes(reader, 2), 0);
            if (midiFormat > 1)
            {
                OnLoadErrorEvent?.Invoke(this, new LoadErrorEventArgs($"\"{filePath}\" has an invalid midi format \"{midiFormat}\"..."));
                return;
            }

            // read track count
            int trackCount = BitConverter.ToInt16(ReadBytes(reader, 2), 0);

            // read file division (ppq)
            int division = BitConverter.ToInt16(ReadBytes(reader, 2), 0);
            if (division >= 0x8000)
            {
                OnLoadErrorEvent?.Invoke(this, new LoadErrorEventArgs("SMTPE timing is not supported."));
                return;
            }

            Console.WriteLine($"Format: {midiFormat}");
            Console.WriteLine($"Tracks: {trackCount}");
            Console.WriteLine($"Division: {division}");

            ulong dataSize = fileSize - ((ulong)trackCount * 8 + 14);

            Console.WriteLine($"Allocating {dataSize} bytes for midi data...");

            midiFile.TrackCount = trackCount;
            midiFile.Division = division;
            midiFile.CurrentTick = 0;

            midiFile.Data = new byte[dataSize];
            midiFile.TrackOffsets = new int[trackCount];
            midiFile.Tracks = new Track[trackCount];

            midiFile.Running = true;

            int offset = 0;

            for (int i = 0; i < trackCount; i++)
            {
                // read track id
                uint trackId = BitConverter.ToUInt32(ReadBytes(reader, 4), 0);

                // read track size
                int trackLength = BitConverter.ToInt32(ReadBytes(reader, 4), 0);

                midiFile.Tracks[i] = new Track
                {
                    RunningStatus = 0,
                    Tick = 0,
                    Ended = false,
                };

                midiFile.TrackOffsets[i] = offset;

                int bytesRead = reader.Read(midiFile.Data, offset, trackLength);

                offset += bytesRead;

                OnLoadingEvent?.Invoke(this, new LoadingEventArgs(i, trackLength));

                Console.WriteLine($"Track {i + 1} size: {trackLength} bytes");
                Console.WriteLine($"Total read: {offset}");
            }

            OnLoadedEvent?.Invoke(this, new LoadedEventArgs(trackCount));

            cancellationTokenSource = new CancellationTokenSource();
            await Task.Run(() => Play(cancellationTokenSource.Token));
        }

        public void UnLoad()
        {
            Stop();

            midiFile.TrackCount = 0;
            midiFile.Division = 0;
            midiFile.CurrentTick = 0;
            Array.Clear(midiFile.Data, 0, midiFile.Data.Length);
            Array.Clear(midiFile.TrackOffsets, 0, midiFile.TrackOffsets.Length);
            Array.Clear(midiFile.Tracks, 0, midiFile.Tracks.Length);

            midiFile.Data = [];
            midiFile.TrackOffsets = [];
            midiFile.Tracks = [];

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public void Pause()
        {
            if (!midiFile.Running) return;
            midiFile.Paused = true;
            OnPlaybackStateChangeEvent?.Invoke(this, new PlaybackStateEventArgs(
                PlaybackStateEventArgs.StateEnum.Paused
            ));
        }

        public void UnPause()
        {
            if (!midiFile.Running) return;
            midiFile.LastTime = GetNs();
            midiFile.Paused = false;
            OnPlaybackStateChangeEvent?.Invoke(this, new PlaybackStateEventArgs(
                PlaybackStateEventArgs.StateEnum.Playing
            ));
        }

        public void TogglePause()
        {
            if (!midiFile.Running) return;
            if (midiFile.Paused)
            {
                UnPause();
            }
            else
            {
                Pause();
            }
        }

        public void Stop()
        {
            if (midiFile.Running)
            {
                cancellationTokenSource?.Cancel();
                midiFile.Running = false;
                OnPlaybackStateChangeEvent?.Invoke(this, new PlaybackStateEventArgs(
                    PlaybackStateEventArgs.StateEnum.Stopped
                ));
            }
        }

        private async void Play(CancellationToken cancellationToken)
        {
            OnPlaybackStateChangeEvent?.Invoke(this, new(
                PlaybackStateEventArgs.StateEnum.Playing
            ));

            long sleepold = 0;
            long sleepdelta = 0;

            long ticker;

            long tempo = 500000;
            long tickMult = Math.Max((tempo * 10) / midiFile.Division, 1);

            for (int c = 0; c < midiFile.TrackCount; c++)
            {
                uint nval = 0;
                byte byte1;

                do
                {
                    byte1 = midiFile.Data[midiFile.TrackOffsets[c]++];
                    nval = (uint)((nval << 7) + (byte1 & 0x7F));
                } while (byte1 >= 0x80);

                midiFile.Tracks[c].Tick += nval;
            }

            ticker = GetNs();

            midiFile.LastTime = ticker;

            int activeTracks = midiFile.TrackCount;

            while (midiFile.Running && !cancellationToken.IsCancellationRequested)
            {
                uint minTick = uint.MaxValue;

                for (int i = 0; i < midiFile.TrackCount; i++)
                {
                    while (midiFile.Tracks[i].Tick <= midiFile.CurrentTick && !midiFile.Paused && !midiFile.Tracks[i].Ended)
                    {
                        byte byteValue = midiFile.Data[midiFile.TrackOffsets[i]];

                        if ((byteValue & 0x80) != 0)
                        {
                            midiFile.Tracks[i].RunningStatus = byteValue;
                            midiFile.TrackOffsets[i]++;
                        }

                        if (midiFile.Tracks[i].RunningStatus < 0xc0 || (0xe0 <= midiFile.Tracks[i].RunningStatus && midiFile.Tracks[i].RunningStatus < 0xf0))
                        {   
                            OnMIDIEvent?.Invoke(this, new MIDIEventArgs(
                                midiFile.Tracks[i].RunningStatus,
                                midiFile.Data[midiFile.TrackOffsets[i]],
                                midiFile.Data[midiFile.TrackOffsets[i] + 1]
                            ));
                            midiFile.TrackOffsets[i] += 2;
                        }
                        else if (midiFile.Tracks[i].RunningStatus < 0xe0)
                        {
                            OnMIDIEvent?.Invoke(this, new MIDIEventArgs(
                                midiFile.Tracks[i].RunningStatus,
                                midiFile.Data[midiFile.TrackOffsets[i]++]
                            ));
                        }
                        else if ((midiFile.Tracks[i].RunningStatus & 0xf0) != 0)
                        {
                            byte temp2 = 0;
                            if (midiFile.Tracks[i].RunningStatus != 0xf0)
                                temp2 = midiFile.Data[midiFile.TrackOffsets[i]++];

                            long lmsglen = 0;
                            byte byte1;

                            do
                            {
                                byte1 = midiFile.Data[midiFile.TrackOffsets[i]++];
                                lmsglen = (lmsglen << 7) + (byte1 & 0x7F);
                            } while (byte1 >= 0x80);

                            if ((midiFile.Tracks[i].RunningStatus & 0xFF) == 0xF0)
                            {
                                //add longmsg later
                            }
                            else
                            {
                                if (temp2 == 0x51)
                                {
                                    tempo =
                                        (midiFile.Data[midiFile.TrackOffsets[i]] << 16) |
                                        (midiFile.Data[midiFile.TrackOffsets[i] + 1] << 8) |
                                        midiFile.Data[midiFile.TrackOffsets[i] + 2];
                                    tickMult = Math.Max((tempo * 10) / midiFile.Division, 1);
                                }
                                else if (temp2 == 0x2F)
                                {
                                    midiFile.Tracks[i].Ended = true;
                                    activeTracks--;
                                }
                            }
                            midiFile.TrackOffsets[i] += (int)lmsglen;
                        }

                        long nval2 = 0;
                        byte byte2;

                        do
                        {
                            byte2 = (byte)(midiFile.TrackOffsets[i] < midiFile.Data.Length ? midiFile.Data[midiFile.TrackOffsets[i]] : 0);

                            if (midiFile.TrackOffsets[i] < midiFile.Data.Length)
                            {
                                nval2 = (nval2 << 7) + (byte2 & 0x7F);
                                midiFile.TrackOffsets[i]++;
                            }
                        } while (byte2 >= 0x80 && midiFile.TrackOffsets[i] < midiFile.Data.Length);


                        midiFile.Tracks[i].Tick += (uint)nval2;
                    }

                    if (!midiFile.Tracks[i].Ended && midiFile.Tracks[i].Tick < minTick)
                        minTick = midiFile.Tracks[i].Tick;
                }

                if (activeTracks == 0)
                {
                    Stop();

                    OnPlaybackFinisheEvent?.Invoke(this, EventArgs.Empty);

                    OnPlaybackStateChangeEvent?.Invoke(this, new PlaybackStateEventArgs(
                        PlaybackStateEventArgs.StateEnum.Stopped
                    ));

                    UnLoad();
                    break;
                }

                uint deltaTick = minTick - midiFile.CurrentTick;

                midiFile.CurrentTick += deltaTick;

                OnTickEvent?.Invoke(this, new TickEventArgs((int)midiFile.CurrentTick));

                if (!midiFile.Paused)
                {
                    ticker = GetNs();

                    long temp = ticker - midiFile.LastTime;
                    midiFile.LastTime = ticker;

                    temp -= sleepold;

                    sleepold = (long)Math.Round((double)(deltaTick * tickMult));
                    sleepdelta += temp;
                    if (sleepdelta > 0) temp = sleepold - sleepdelta;
                    else temp = sleepold;

                    long del = temp / 10;
                    if (temp > 0)
                    {
                        await SleepNanos(del * 1000);
                    }
                }
            }
        }
    }
}