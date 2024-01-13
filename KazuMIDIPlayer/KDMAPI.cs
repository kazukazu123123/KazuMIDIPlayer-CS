using System.Runtime.InteropServices;

namespace KazuMIDIPlayer
{
    internal partial class KDMAPI
    {
        private const string OmniMIDILibrary = "OmniMIDI\\OmniMIDI";

        [LibraryImport(OmniMIDILibrary)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool InitializeKDMAPIStream();

        [LibraryImport(OmniMIDILibrary)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool TerminateKDMAPIStream();

        [LibraryImport(OmniMIDILibrary)]
        public static partial void ResetKDMAPIStream();

        [LibraryImport(OmniMIDILibrary)]
        public static partial uint SendDirectData(uint data);
    }
}
