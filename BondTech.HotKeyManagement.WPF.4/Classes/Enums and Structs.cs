// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



using System;
using System.Runtime.InteropServices;

namespace BondTech.HotKeyManagement.WPF._4
{
    /// <summary>Specifies when the event for a LocalHotKey is raised.
    /// </summary>
    public enum RaiseLocalEvent
    {
        /// <summary>Specifies that the event for a LocalHotKey should be raised when the key is down
        /// </summary>
        OnKeyDown = 0x100, //Also 256. Same as WM_KEYDOWN.
        /// <summary>Specifies that the event for a LocalHotKey should be raised when the key is released.
        /// </summary>
        OnKeyUp = 0x101 //Also 257, Same as WM_KEYUP.
    }

    internal enum KeyboardMessages : int
    {
        /// <summary>A key is down.
        /// </summary>
        WmKeydown = 0x0100,
        /// <summary>A key is released.
        /// </summary>
        WmKeyup = 0x0101,
        /// <summary> Same as KeyDown but captures keys pressed after Alt.
        /// </summary>
        WmSyskeydown = 0x0104,
        /// <summary>Same as KeyUp but captures keys pressed after Alt.
        /// </summary>
        WmSyskeyup = 0x0105,
        /// <summary> When a hotkey is pressed.
        /// </summary>
        WmHotKey = 786
    }

    internal enum KeyboardHookEnum : int
    {
        KeyboardHook = 0xD,
        Keyboard_ExtendedKey = 0x1,
        Keyboard_KeyUp = 0x2
    }

    /// <summary>
    /// The KBDLLHOOKSTRUCT structure contains information about a low-level keyboard input event. 
    /// </summary>
    /// <remarks>
    /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/windowing/hooks/hookreference/hookstructures/cwpstruct.asp
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct KeyboardHookStruct
    {
        /// <summary>
        /// Specifies a virtual-key code. The code must be a value in the range 1 to 254. 
        /// </summary>
        public int VirtualKeyCode;
        /// <summary>
        /// Specifies a hardware scan code for the key. 
        /// </summary>
        public int ScanCode;
        /// <summary>
        /// Specifies the extended-key flag, event-injected flag, context code, and transition-state flag.
        /// </summary>
        public int Flags;
        /// <summary>
        /// Specifies the Time stamp for this message.
        /// </summary>
        public int Time;
        /// <summary>
        /// Specifies extra information associated with the message. 
        /// </summary>
        public int ExtraInfo;
    }

    public enum KeyboardEventNames
    {
        KeyDown,
        KeyUp
    }
}
