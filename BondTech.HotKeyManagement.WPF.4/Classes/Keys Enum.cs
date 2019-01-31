// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



/* The Keys in this enum are exactly the ones in WinForm, since WPF WndProc still sends the same keys
 * as WinForm and I've not found a suitable way of converting them to their WPF equivalent. I've tried 
 * using the KeyInterop class but it just makes it cumbersome. With the view of providing the same
 * functionalities as WinForm in this library. I've resulted to this. Feel free to explore the KeyInterop
 * class in the System.Windows.Input namespace */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

using wpfKey = System.Windows.Input;

namespace BondTech.HotKeyManagement.WPF._4
{
    #region The Keys enum
    /// <summary>Specifies key codes and modifiers.
    /// </summary>
    [Flags]
    [ComVisible(true)]
    [TypeConverter(typeof(KeysConverter))]
    public enum Key
    {
        /// <summary>The bitmask to extract modifiers from a key value.
        /// </summary>
        Modifiers = -65536,
        /// <summary>No key pressed.
        /// </summary>
        None = 0,
        /// <summary>The left mouse button.
        /// </summary>
        LButton = 1,
        /// <summary>The right mouse button.
        /// </summary>
        RButton = 2,
        /// <summary>The CANCEL key.
        /// </summary>
        Cancel = 3,
        /// <summary>The middle mouse button (three-button mouse).
        /// </summary>
        MButton = 4,
        /// <summary>The first x mouse button (five-button mouse).
        /// </summary>
        XButton1 = 5,
        /// <summary>The second x mouse button (five-button mouse).
        /// </summary>
        XButton2 = 6,
        /// <summary>The BACKSPACE key.
        /// </summary>     
        Back = 8,
        /// <summary>The TAB key.
        /// </summary>
        Tab = 9,
        /// <summary>The LINEFEED key.
        /// </summary>
        LineFeed = 10,
        /// <summary>The CLEAR key.
        /// </summary>
        Clear = 12,
        /// <summary>The ENTER key.
        /// </summary>
        Enter = 13,
        /// <summary>The RETURN key.
        /// </summary>
        Return = 13,
        /// <summary>The SHIFT key.
        /// </summary>
        ShiftKey = 16,
        /// <summary>The CTRL key.
        /// </summary>
        ControlKey = 17,
        /// <summary>The ALT key.
        /// </summary>
        Menu = 18,
        /// <summary>The PAUSE key.
        /// </summary>
        Pause = 19,
        /// <summary>The CAPS LOCK key.
        /// </summary>
        CapsLock = 20,
        /// <summary>The CAPS LOCK key.
        /// </summary>
        Capital = 20,
        /// <summary>The IME Kana mode key.
        /// </summary>
        KanaMode = 21,
        /// <summary>The IME Hanguel mode key. (maintained for compatibility; use HangulMode)
        /// </summary>
        HanguelMode = 21,
        /// <summary>The IME Hangul mode key.
        /// </summary>
        HangulMode = 21,
        /// <summary>The IME Junja mode key.
        /// </summary>
        JunjaMode = 23,
        /// <summary>The IME final mode key.
        /// </summary>
        FinalMode = 24,
        /// <summary>The IME Kanji mode key.
        /// </summary>
        KanjiMode = 25,
        /// <summary>The IME Hanja mode key.
        /// </summary>
        HanjaMode = 25,
        /// <summary>The ESC key.
        /// </summary>
        Escape = 27,
        /// <summary>The IME convert key.
        /// </summary>
        IMEConvert = 28,
        /// <summary>The IME nonconvert key.
        /// </summary>
        IMENonconvert = 29,
        /// <summary>The IME accept key. Obsolete, use System.Windows.Forms.Keys.IMEAccept instead.
        /// </summary>
        IMEAceept = 30,
        /// <summary>The IME accept key, replaces System.Windows.Forms.Keys.IMEAceept.
        /// </summary>
        IMEAccept = 30,
        /// <summary>The IME mode change key.
        /// </summary>
        IMEModeChange = 31,
        /// <summary>The SPACEBAR key.
        /// </summary>
        Space = 32,
        /// <summary>The PAGE UP key.
        /// </summary>
        Prior = 33,
        /// <summary>The PAGE UP key.
        /// </summary>
        PageUp = 33,
        /// <summary>The PAGE DOWN key.
        /// </summary>
        Next = 34,
        /// <summary>The PAGE DOWN key.
        /// </summary>
        PageDown = 34,
        /// <summary>The END key.
        /// </summary>
        End = 35,
        /// <summary>The HOME key.
        /// </summary>
        Home = 36,
        /// <summary>The LEFT ARROW key.
        /// </summary>
        Left = 37,
        /// <summary>The UP ARROW key.
        /// </summary>
        Up = 38,
        /// <summary>The RIGHT ARROW key.
        /// </summary>
        Right = 39,
        /// <summary>The DOWN ARROW key.
        /// </summary>
        Down = 40,
        /// <summary>The SELECT key.
        /// </summary>
        Select = 41,
        /// <summary>The PRINT key.
        /// </summary>
        Print = 42,
        /// <summary>The EXECUTE key.
        /// </summary>
        Execute = 43,
        /// <summary>The PRINT SCREEN key.
        /// </summary>
        PrintScreen = 44,
        /// <summary>The PRINT SCREEN key.
        /// </summary>
        Snapshot = 44,
        /// <summary>The INS key.
        /// </summary>
        Insert = 45,
        /// <summary>The DEL key.
        /// </summary>
        Delete = 46,
        /// <summary>The HELP key.
        /// </summary>
        Help = 47,
        /// <summary>The 0 key.
        /// </summary>
        D0 = 48,
        /// <summary>The 1 key.
        /// </summary>
        D1 = 49,
        /// <summary>The 2 key.
        /// </summary>
        D2 = 50,
        /// <summary>The 3 key.
        /// </summary>
        D3 = 51,
        /// <summary>The 4 key.
        /// </summary>
        D4 = 52,
        /// <summary>The 5 key.
        /// </summary>
        D5 = 53,
        /// <summary>The 6 key.
        /// </summary>
        D6 = 54,
        /// <summary>The 7 key.
        /// </summary>
        D7 = 55,
        /// <summary>The 8 key.
        /// </summary>
        D8 = 56,
        /// <summary>The 9 key.
        /// </summary>
        D9 = 57,
        /// <summary>The A key.
        /// </summary>
        A = 65,
        /// <summary>The B key.
        /// </summary>
        B = 66,
        /// <summary>The C key.
        /// </summary>
        C = 67,
        /// <summary>The D key.
        /// </summary>
        D = 68,
        /// <summary>The E key.
        /// </summary>
        E = 69,
        /// <summary>The F key.
        /// </summary>
        F = 70,
        /// <summary>The G key.
        /// </summary>
        G = 71,
        /// <summary>The H key.
        /// </summary>
        H = 72,
        /// <summary>The I key.
        /// </summary>
        I = 73,
        /// <summary>The J key.
        /// 
        /// </summary>
        J = 74,
        /// <summary>The K key.
        /// </summary>
        K = 75,
        /// <summary>The L key.
        /// </summary>
        L = 76,
        /// <summary>The M key.
        /// </summary>
        M = 77,
        /// <summary>The N key.
        /// </summary>
        N = 78,
        /// <summary>The O key.
        /// </summary>
        O = 79,
        /// <summary>The P key.
        /// </summary>
        P = 80,
        /// <summary>The Q key.
        /// </summary>
        Q = 81,
        /// <summary>The R key.
        /// </summary>
        R = 82,
        /// <summary>The S key.
        /// </summary>
        S = 83,
        /// <summary>The T key.
        /// </summary>
        T = 84,
        /// <summary>The U key.
        /// </summary>
        U = 85,
        /// <summary>The V key.
        /// </summary>
        V = 86,
        /// <summary>The W key.
        /// </summary>
        W = 87,
        /// <summary>The X key.
        /// </summary>
        X = 88,
        /// <summary>The Y key.
        /// </summary>
        Y = 89,
        /// <summary>The Z key.
        /// </summary>
        Z = 90,
        /// <summary>The left Windows logo key (Microsoft Natural Keyboard).
        /// </summary>
        LWin = 91,
        /// <summary>The right Windows logo key (Microsoft Natural Keyboard).
        /// </summary>
        RWin = 92,
        /// <summary>The application key (Microsoft Natural Keyboard).
        /// </summary>
        Apps = 93,
        /// <summary>The computer sleep key.
        /// </summary>
        Sleep = 95,
        /// <summary>The 0 key on the numeric keypad.
        /// </summary>
        NumPad0 = 96,
        /// <summary>The 1 key on the numeric keypad.
        /// </summary>
        NumPad1 = 97,
        /// <summary>The 2 key on the numeric keypad.
        /// </summary>
        NumPad2 = 98,
        /// <summary>The 3 key on the numeric keypad.
        /// </summary>
        NumPad3 = 99,
        /// <summary>The 4 key on the numeric keypad.
        /// </summary>
        NumPad4 = 100,
        /// <summary>The 5 key on the numeric keypad.
        /// </summary>
        NumPad5 = 101,
        /// <summary>The 6 key on the numeric keypad.
        /// </summary>
        NumPad6 = 102,
        /// <summary>The 7 key on the numeric keypad.
        /// </summary>
        NumPad7 = 103,
        /// <summary>The 8 key on the numeric keypad.
        /// </summary>
        NumPad8 = 104,
        /// <summary>The 9 key on the numeric keypad.
        /// </summary>
        NumPad9 = 105,
        /// <summary>The multiply key.
        /// </summary>
        Multiply = 106,
        /// <summary>The add key.
        /// </summary>
        Add = 107,
        /// <summary>The separator key.
        /// </summary>
        Separator = 108,
        /// <summary>The subtract key.
        /// </summary>
        Subtract = 109,
        /// <summary>The decimal key.
        /// </summary>
        Decimal = 110,
        /// <summary>The divide key.
        /// </summary>
        Divide = 111,
        /// <summary>The F1 key.
        /// </summary>
        F1 = 112,
        /// <summary>The F2 key.
        /// </summary>
        F2 = 113,
        /// <summary>The F3 key.
        /// </summary>
        F3 = 114,
        /// <summary>The F4 key.
        /// </summary>
        F4 = 115,
        /// <summary>The F5 key.
        /// </summary>
        F5 = 116,
        /// <summary>The F6 key.
        /// </summary>
        F6 = 117,
        /// <summary>The F7 key.
        /// </summary>
        F7 = 118,
        /// <summary>The F8 key.
        /// </summary>
        F8 = 119,
        /// <summary>The F9 key.
        /// </summary>
        F9 = 120,
        /// <summary>The F10 key.
        /// </summary>
        F10 = 121,
        /// <summary>The F11 key.
        /// </summary>
        F11 = 122,
        /// <summary>The F12 key.
        /// </summary>
        F12 = 123,
        /// <summary>The F13 key.
        /// </summary>
        F13 = 124,
        /// <summary>The F14 key.
        /// </summary>
        F14 = 125,
        /// <summary>The F15 key.
        /// </summary>
        F15 = 126,
        /// <summary>The F16 key.
        /// </summary>
        F16 = 127,
        /// <summary>The F17 key.
        /// </summary>
        F17 = 128,
        /// <summary>The F18 key.
        /// </summary>
        F18 = 129,
        /// <summary>The F19 key.
        /// </summary>
        F19 = 130,
        /// <summary>The F20 key.
        /// </summary>
        F20 = 131,
        /// <summary>The F21 key.
        /// </summary>
        F21 = 132,
        /// <summary>The F22 key.
        /// </summary>
        F22 = 133,
        /// <summary>The F23 key.
        /// </summary>
        F23 = 134,
        /// <summary>The F24 key.
        /// </summary>
        F24 = 135,
        /// <summary>The NUM LOCK key.
        /// </summary>
        NumLock = 144,
        /// <summary>The SCROLL LOCK key.
        /// </summary>
        Scroll = 145,
        /// <summary>The left SHIFT key.
        /// </summary>
        LShiftKey = 160,
        /// <summary>The right SHIFT key.
        /// </summary>
        RShiftKey = 161,
        /// <summary>The left CTRL key.
        /// </summary>
        LControlKey = 162,
        /// <summary>The right CTRL key.
        /// </summary>
        RControlKey = 163,
        /// <summary>The left ALT key.
        /// </summary>
        LMenu = 164,
        /// <summary>The right ALT key.
        /// </summary>
        RMenu = 165,
        /// <summary>The browser back key (Windows 2000 or later).
        /// </summary>
        BrowserBack = 166,
        /// <summary>The browser forward key (Windows 2000 or later).
        /// </summary>
        BrowserForward = 167,
        /// <summary>The browser refresh key (Windows 2000 or later).
        /// </summary>
        BrowserRefresh = 168,
        /// <summary>The browser stop key (Windows 2000 or later).
        /// </summary>
        BrowserStop = 169,
        /// <summary>The browser search key (Windows 2000 or later).
        /// </summary>
        BrowserSearch = 170,
        /// <summary>The browser favourites key (Windows 2000 or later).
        /// </summary>
        BrowserFavorites = 171,
        /// <summary>The browser home key (Windows 2000 or later).
        /// </summary>
        BrowserHome = 172,
        /// <summary>The volume mute key (Windows 2000 or later).
        /// </summary>
        VolumeMute = 173,
        /// <summary>The volume down key (Windows 2000 or later).
        /// </summary>
        VolumeDown = 174,
        /// <summary>The volume up key (Windows 2000 or later).
        /// </summary>
        VolumeUp = 175,
        /// <summary>The media next track key (Windows 2000 or later).
        /// </summary>
        MediaNextTrack = 176,
        /// <summary>The media previous track key (Windows 2000 or later).
        /// </summary>
        MediaPreviousTrack = 177,
        /// <summary>The media Stop key (Windows 2000 or later).
        /// </summary>
        MediaStop = 178,
        /// <summary>The media play pause key (Windows 2000 or later).
        /// </summary>
        MediaPlayPause = 179,
        /// <summary>The launch mail key (Windows 2000 or later).
        /// </summary>
        LaunchMail = 180,
        /// <summary>The select media key (Windows 2000 or later).
        /// </summary>
        SelectMedia = 181,
        /// <summary>The start application one key (Windows 2000 or later).
        /// </summary>
        LaunchApplication1 = 182,
        /// <summary>The start application two key (Windows 2000 or later).
        /// </summary>
        LaunchApplication2 = 183,
        /// <summary>The OEM 1 key.
        /// </summary>
        Oem1 = 186,
        /// <summary>The OEM Semicolon key on a US standard keyboard (Windows 2000 or later).
        /// </summary>
        OemSemicolon = 186,
        /// <summary>The OEM plus key on any country/region keyboard (Windows 2000 or later).
        /// </summary>
        Oemplus = 187,
        /// <summary>The OEM comma key on any country/region keyboard (Windows 2000 or later).
        /// </summary>
        Oemcomma = 188,
        /// <summary>The OEM minus key on any country/region keyboard (Windows 2000 or later).
        /// </summary>
        OemMinus = 189,
        /// <summary>The OEM period key on any country/region keyboard (Windows 2000 or later).
        /// </summary>
        OemPeriod = 190,
        /// <summary>The OEM question mark key on a US standard keyboard (Windows 2000 or later).
        /// </summary>
        OemQuestion = 191,
        /// <summary>The OEM 2 key.
        /// </summary>
        Oem2 = 191,
        /// <summary>The OEM tilde key on a US standard keyboard (Windows 2000 or later).
        /// </summary>
        Oemtilde = 192,
        /// <summary>The OEM 3 key.
        /// </summary>
        Oem3 = 192,
        /// <summary>The OEM 4 key.
        /// </summary>
        Oem4 = 219,
        /// <summary>The OEM open bracket key on a US standard keyboard (Windows 2000 or later).
        /// </summary>
        OemOpenBrackets = 219,
        /// <summary>The OEM pipe key on a US standard keyboard (Windows 2000 or later).
        /// </summary>
        OemPipe = 220,
        /// <summary>The OEM 5 key.
        /// </summary>
        Oem5 = 220,
        /// <summary>The OEM 6 key.
        /// </summary>
        Oem6 = 221,
        /// <summary>The OEM close bracket key on a US standard keyboard (Windows 2000 or later).
        /// </summary>
        OemCloseBrackets = 221,
        /// <summary>The OEM 7 key.
        /// </summary>
        Oem7 = 222,
        /// <summary>The OEM singled/double quote key on a US standard keyboard (Windows 2000 or later).
        /// </summary>
        OemQuotes = 222,
        /// <summary>The OEM 8 key.
        /// </summary>
        Oem8 = 223,
        /// <summary>The OEM 102 key.
        /// </summary>
        Oem102 = 226,
        /// <summary>The OEM angle bracket or backslash key on the RT 102 key keyboard (Windows 2000 or later).
        /// </summary>
        OemBackslash = 226,
        /// <summary>The PROCESS KEY key.
        /// </summary>
        ProcessKey = 229,
        /// <summary>Used to pass Unicode characters as if they were keystrokes.
        ///     The Packet key value is the low word of a 32-bit virtual-key value used for non-keyboard
        ///     input methods.
        /// </summary>
        Packet = 231,
        /// <summary>The ATTN key.
        /// </summary>
        Attn = 246,
        /// <summary>The CRSEL key.
        /// </summary>
        Crsel = 247,
        /// <summary>The EXSEL key.
        /// </summary>
        Exsel = 248,
        /// <summary>The ERASE EOF key.
        /// </summary>
        EraseEof = 249,
        /// <summary>The PLAY key.
        /// </summary>
        Play = 250,
        /// <summary>The ZOOM key.
        /// </summary>
        Zoom = 251,
        /// <summary>A constant reserved for future use.
        /// </summary>
        NoName = 252,
        /// <summary>The PA1 key.
        /// </summary>
        Pa1 = 253,
        /// <summary>The CLEAR key.
        /// </summary>
        OemClear = 254,
        /// <summary>The bitmask to extract a key code from a key value.
        /// </summary>
        KeyCode = 65535,
        /// <summary>The SHIFT modifier key.
        /// </summary>
        Shift = 65536,
        /// <summary>The CTRL modifier key.
        /// </summary>
        Control = 131072,
        /// <summary>The ALT modifier key.
        /// </summary>
        Alt = 262144,
    }
    #endregion

    public class Keys
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetKeyboardState(byte[] lpKeyState);

        public static wpfKey.Key ConvertToWpfKey(Key key)
        {
            switch (key)
            {
                case Key.LShiftKey:
                    return wpfKey.Key.LeftShift;
                case Key.RShiftKey:
                    return wpfKey.Key.RightShift;
                case Key.Shift:
                    return wpfKey.Key.LeftShift;

                case Key.LControlKey:
                    return wpfKey.Key.LeftCtrl;
                case Key.RControlKey:
                    return wpfKey.Key.RightCtrl;
                case Key.ControlKey:

                    return wpfKey.Key.LeftCtrl;
                case Key.Alt:
                    return wpfKey.Key.LeftAlt;

                case Key.Menu:
                    return wpfKey.Key.System;
                case Key.RMenu:
                    return wpfKey.Key.System;
            }

            switch (key)
            {
                case Key.A:
                    return wpfKey.Key.A;
                case Key.Add:
                    return wpfKey.Key.Add;
                case Key.Alt:
                    return wpfKey.Key.LeftAlt;
                case Key.Apps:
                    return wpfKey.Key.Apps;
                case Key.Attn:
                    return wpfKey.Key.Attn;
                case Key.B:
                    return wpfKey.Key.B;
                case Key.Back:
                    return wpfKey.Key.Back;
                case Key.BrowserBack:
                    return wpfKey.Key.BrowserBack;
                case Key.BrowserFavorites:
                    return wpfKey.Key.BrowserFavorites;
                case Key.BrowserForward:
                    return wpfKey.Key.BrowserForward;
                case Key.BrowserHome:
                    return wpfKey.Key.BrowserHome;
                case Key.BrowserRefresh:
                    return wpfKey.Key.BrowserRefresh;
                case Key.BrowserSearch:
                    return wpfKey.Key.BrowserSearch;
                case Key.BrowserStop:
                    return wpfKey.Key.BrowserStop;
                case Key.C:
                    return wpfKey.Key.C;
                case Key.Cancel:
                    return wpfKey.Key.Cancel;
                //case Key.Capital:
                //    return wpfKey.Key.Capital;
                case Key.CapsLock:
                    return wpfKey.Key.CapsLock;
                case Key.Clear:
                    return wpfKey.Key.Clear;
                //case Key.Control:
                //  return wpfKey.Key.LeftCtrl;
                case Key.ControlKey:
                    return wpfKey.Key.LeftCtrl;
                case Key.Crsel:
                    return wpfKey.Key.CrSel;
                case Key.D:
                    return wpfKey.Key.D;
                case Key.D0:
                    return wpfKey.Key.D0;
                case Key.D1:
                    return wpfKey.Key.D1;
                case Key.D2:
                    return wpfKey.Key.D2;
                case Key.D3:
                    return wpfKey.Key.D3;
                case Key.D4:
                    return wpfKey.Key.D4;
                case Key.D5:
                    return wpfKey.Key.D5;
                case Key.D6:
                    return wpfKey.Key.D6;
                case Key.D7:
                    return wpfKey.Key.D7;
                case Key.D8:
                    return wpfKey.Key.D8;
                case Key.D9:
                    return wpfKey.Key.D9;
                case Key.Decimal:
                    return wpfKey.Key.Decimal;
                case Key.Delete:
                    return wpfKey.Key.Delete;
                case Key.Divide:
                    return wpfKey.Key.Divide;
                case Key.Down:
                    return wpfKey.Key.Down;
                case Key.E:
                    return wpfKey.Key.E;
                case Key.End:
                    return wpfKey.Key.End;
                case Key.Enter:
                    return wpfKey.Key.Enter;
                case Key.EraseEof:
                    return wpfKey.Key.EraseEof;
                case Key.Escape:
                    return wpfKey.Key.Escape;
                case Key.Execute:
                    return wpfKey.Key.Execute;
                case Key.Exsel:
                    return wpfKey.Key.ExSel;
                case Key.F:
                    return wpfKey.Key.F;
                case Key.F1:
                    return wpfKey.Key.F1;
                case Key.F10:
                    return wpfKey.Key.F10;
                case Key.F11:
                    return wpfKey.Key.F11;
                case Key.F12:
                    return wpfKey.Key.F12;
                case Key.F13:
                    return wpfKey.Key.F13;
                case Key.F14:
                    return wpfKey.Key.F14;
                case Key.F15:
                    return wpfKey.Key.F15;
                case Key.F16:
                    return wpfKey.Key.F16;
                case Key.F17:
                    return wpfKey.Key.F17;
                case Key.F18:
                    return wpfKey.Key.F18;
                case Key.F19:
                    return wpfKey.Key.F19;
                case Key.F2:
                    return wpfKey.Key.F2;
                case Key.F20:
                    return wpfKey.Key.F20;
                case Key.F21:
                    return wpfKey.Key.F21;
                case Key.F22:
                    return wpfKey.Key.F22;
                case Key.F23:
                    return wpfKey.Key.F23;
                case Key.F24:
                    return wpfKey.Key.F24;
                case Key.F3:
                    return wpfKey.Key.F3;
                case Key.F4:
                    return wpfKey.Key.F4;
                case Key.F5:
                    return wpfKey.Key.F5;
                case Key.F6:
                    return wpfKey.Key.F6;
                case Key.F7:
                    return wpfKey.Key.F7;
                case Key.F8:
                    return wpfKey.Key.F8;
                case Key.F9:
                    return wpfKey.Key.F9;
                case Key.FinalMode:
                    return wpfKey.Key.FinalMode;
                case Key.G:
                    return wpfKey.Key.G;
                case Key.H:
                    return wpfKey.Key.H;
                case Key.HanguelMode:
                    return wpfKey.Key.HangulMode;
                case Key.HanjaMode:
                    return wpfKey.Key.HanjaMode;
                case Key.Help:
                    return wpfKey.Key.Help;
                case Key.Home:
                    return wpfKey.Key.Home;
                case Key.I:
                    return wpfKey.Key.I;
                case Key.IMEAccept:
                    return wpfKey.Key.ImeAccept;
                case Key.IMEConvert:
                    return wpfKey.Key.ImeConvert;
                case Key.IMEModeChange:
                    return wpfKey.Key.ImeModeChange;
                case Key.IMENonconvert:
                    return wpfKey.Key.ImeNonConvert;
                case Key.Insert:
                    return wpfKey.Key.Insert;
                case Key.J:
                    return wpfKey.Key.J;
                case Key.JunjaMode:
                    return wpfKey.Key.JunjaMode;
                case Key.K:
                    return wpfKey.Key.K;
                //case Key.KanjiMode:
                //    return wpfKey.Key.KanjiMode;
                case Key.L:
                    return wpfKey.Key.L;
                case Key.LaunchApplication1:
                    return wpfKey.Key.LaunchApplication1;
                case Key.LaunchMail:
                    return wpfKey.Key.LaunchMail;
                case Key.LControlKey:
                    return wpfKey.Key.LeftCtrl;
                case Key.Left:
                    return wpfKey.Key.Left;
                case Key.LineFeed:
                    return wpfKey.Key.LineFeed;
                case Key.LShiftKey:
                    return wpfKey.Key.LeftShift;
                case Key.LWin:
                    return wpfKey.Key.LWin;
                case Key.M:
                    return wpfKey.Key.M;
                case Key.MediaNextTrack:
                    return wpfKey.Key.MediaNextTrack;
                case Key.MediaPlayPause:
                    return wpfKey.Key.MediaPlayPause;
                case Key.MediaPreviousTrack:
                    return wpfKey.Key.MediaPreviousTrack;
                case Key.MediaStop:
                    return wpfKey.Key.MediaStop;
                case Key.Multiply:
                    return wpfKey.Key.Multiply;
                case Key.N:
                    return wpfKey.Key.N;
                //case Key.Next:
                //return wpfKey.Key.Next;
                case Key.NoName:
                    return wpfKey.Key.NoName;
                case Key.None:
                    return wpfKey.Key.None;
                case Key.NumLock:
                    return wpfKey.Key.NumLock;
                case Key.NumPad0:
                    return wpfKey.Key.NumPad0;
                case Key.NumPad1:
                    return wpfKey.Key.NumPad1;
                case Key.NumPad2:
                    return wpfKey.Key.NumPad2;
                case Key.NumPad3:
                    return wpfKey.Key.NumPad3;
                case Key.NumPad4:
                    return wpfKey.Key.NumPad4;
                case Key.NumPad5:
                    return wpfKey.Key.NumPad5;
                case Key.NumPad6:
                    return wpfKey.Key.NumPad6;
                case Key.NumPad7:
                    return wpfKey.Key.NumPad7;
                case Key.NumPad8:
                    return wpfKey.Key.NumPad8;
                case Key.NumPad9:
                    return wpfKey.Key.NumPad9;
                case Key.O:
                    return wpfKey.Key.O;
                case Key.Oem1:
                    return wpfKey.Key.Oem1;
                case Key.Oem102:
                    return wpfKey.Key.Oem102;
                case Key.Oem2:
                    return wpfKey.Key.Oem2;
                case Key.Oem3:
                    return wpfKey.Key.Oem3;
                case Key.Oem4:
                    return wpfKey.Key.Oem4;
                case Key.Oem5:
                    return wpfKey.Key.Oem5;
                case Key.Oem6:
                    return wpfKey.Key.Oem6;
                case Key.Oem7:
                    return wpfKey.Key.Oem7;
                case Key.Oem8:
                    return wpfKey.Key.Oem8;
                // case Key.OemBackslash:
                //    return wpfKey.Key.OemBackslash;
                case Key.OemClear:
                    return wpfKey.Key.OemClear;
                // case Key.OemCloseBrackets:
                //    return wpfKey.Key.OemCloseBrackets;
                case Key.Oemcomma:
                    return wpfKey.Key.OemComma;
                case Key.OemMinus:
                    return wpfKey.Key.OemMinus;
                //case Key.OemOpenBrackets:
                //    return wpfKey.Key.OemOpenBrackets;
                case Key.OemPeriod:
                    return wpfKey.Key.OemPeriod;
                //case Key.OemPipe:
                //    return wpfKey.Key.OemPipe;
                case Key.Oemplus:
                    return wpfKey.Key.OemPlus;
                //case Key.OemQuestion:
                //    return wpfKey.Key.OemQuestion;
                // case Key.OemQuotes:
                //    return wpfKey.Key.OemQuotes;
                // case Key.OemSemicolon:
                //    return wpfKey.Key.OemSemicolon;
                //case Key.Oemtilde:
                //    return wpfKey.Key.OemTilde;
                case Key.P:
                    return wpfKey.Key.P;
                case Key.Pa1:
                    return wpfKey.Key.Pa1;
                case Key.PageDown:
                    return wpfKey.Key.PageDown;
                case Key.PageUp:
                    return wpfKey.Key.PageUp;
                case Key.Pause:
                    return wpfKey.Key.Pause;
                case Key.Play:
                    return wpfKey.Key.Play;
                case Key.Print:
                    return wpfKey.Key.Print;
                case Key.PrintScreen:
                    return wpfKey.Key.PrintScreen;
                //case Key.Prior:
                //    return wpfKey.Key.Prior;
                case Key.Q:
                    return wpfKey.Key.Q;
                case Key.R:
                    return wpfKey.Key.R;
                case Key.RControlKey:
                    return wpfKey.Key.RightCtrl;
                //case Key.Return:
                //   return wpfKey.Key.Return;
                case Key.Right:
                    return wpfKey.Key.Right;
                case Key.RShiftKey:
                    return wpfKey.Key.RightShift;
                case Key.RWin:
                    return wpfKey.Key.RWin;
                case Key.S:
                    return wpfKey.Key.S;
                case Key.Scroll:
                    return wpfKey.Key.Scroll;
                case Key.Select:
                    return wpfKey.Key.Select;
                case Key.SelectMedia:
                    return wpfKey.Key.SelectMedia;
                case Key.Separator:
                    return wpfKey.Key.Separator;
                case Key.Shift:
                    return wpfKey.Key.LeftShift;
                case Key.ShiftKey:
                    return wpfKey.Key.LeftShift;
                case Key.Sleep:
                    return wpfKey.Key.Sleep;
                //case Key.Snapshot:
                //   return wpfKey.Key.Snapshot;
                case Key.Space:
                    return wpfKey.Key.Space;
                case Key.Subtract:
                    return wpfKey.Key.Subtract;
                case Key.T:
                    return wpfKey.Key.T;
                case Key.Tab:
                    return wpfKey.Key.Tab;
                case Key.U:
                    return wpfKey.Key.U;
                case Key.Up:
                    return wpfKey.Key.Up;
                case Key.V:
                    return wpfKey.Key.V;
                case Key.VolumeDown:
                    return wpfKey.Key.VolumeDown;
                case Key.VolumeMute:
                    return wpfKey.Key.VolumeMute;
                case Key.VolumeUp:
                    return wpfKey.Key.VolumeUp;
                case Key.W:
                    return wpfKey.Key.W;
                case Key.X:
                    return wpfKey.Key.X;
                case Key.Y:
                    return wpfKey.Key.Y;
                case Key.Z:
                    return wpfKey.Key.Z;
                case Key.Zoom:
                    return wpfKey.Key.Zoom;

            }

            return wpfKey.Key.NoName;
        }

        public static Key ConvertFromWpfKey(wpfKey.Key key)
        {
            switch (key)
            {
                case wpfKey.Key.LeftShift:
                    return Key.LShiftKey;
                case wpfKey.Key.RightShift:
                    return Key.RShiftKey;
                //case wpfKey.Key.LeftShift:
                //   return Key.LShiftKey;

                case wpfKey.Key.LeftCtrl:
                    return Key.LControlKey;
                case wpfKey.Key.RightCtrl:
                    return Key.RControlKey;

                case wpfKey.Key.LeftAlt:
                    return Key.Alt;
                case wpfKey.Key.RightAlt:
                    return Key.Alt;

                case wpfKey.Key.System:
                    return Key.Menu;

            }

            switch (key)
            {
                case wpfKey.Key.A:
                    return Key.A;
                case wpfKey.Key.Add:
                    return Key.Add;
                case wpfKey.Key.Apps:
                    return Key.Apps;
                case wpfKey.Key.Attn:
                    return Key.Attn;
                case wpfKey.Key.B:
                    return Key.B;
                case wpfKey.Key.Back:
                    return Key.Back;
                case wpfKey.Key.BrowserBack:
                    return Key.BrowserBack;
                case wpfKey.Key.BrowserFavorites:
                    return Key.BrowserFavorites;
                case wpfKey.Key.BrowserForward:
                    return Key.BrowserForward;
                case wpfKey.Key.BrowserHome:
                    return Key.BrowserHome;
                case wpfKey.Key.BrowserRefresh:
                    return Key.BrowserRefresh;
                case wpfKey.Key.BrowserSearch:
                    return Key.BrowserSearch;
                case wpfKey.Key.BrowserStop:
                    return Key.BrowserStop;

                case wpfKey.Key.C:
                    return Key.C;
                case wpfKey.Key.Cancel:
                    return Key.Cancel;
                //case wpfKey.Key.Capital:
                //   return Key.Capital;
                case wpfKey.Key.CapsLock:
                    return Key.CapsLock;
                case wpfKey.Key.Clear:
                    return Key.Clear;
                case wpfKey.Key.CrSel:
                    return Key.Crsel;
                case wpfKey.Key.D:
                    return Key.D;
                case wpfKey.Key.D0:
                    return Key.D0;
                case wpfKey.Key.D1:
                    return Key.D1;
                case wpfKey.Key.D2:
                    return Key.D2;
                case wpfKey.Key.D3:
                    return Key.D3;
                case wpfKey.Key.D4:
                    return Key.D4;
                case wpfKey.Key.D5:
                    return Key.D5;
                case wpfKey.Key.D6:
                    return Key.D6;
                case wpfKey.Key.D7:
                    return Key.D7;
                case wpfKey.Key.D8:
                    return Key.D8;
                case wpfKey.Key.D9:
                    return Key.D9;
                case wpfKey.Key.DbeAlphanumeric:
                    return Key.Modifiers;
                case wpfKey.Key.Decimal:
                    return Key.Decimal;
                case wpfKey.Key.Delete:
                    return Key.Delete;
                case wpfKey.Key.Divide:
                    return Key.Divide;
                case wpfKey.Key.Down:
                    return Key.Down;
                case wpfKey.Key.E:
                    return Key.E;
                case wpfKey.Key.End:
                    return Key.End;
                case wpfKey.Key.Enter:
                    return Key.Enter;
                case wpfKey.Key.EraseEof:
                    return Key.EraseEof;
                case wpfKey.Key.Escape:
                    return Key.Escape;
                case wpfKey.Key.Execute:
                    return Key.Execute;
                case wpfKey.Key.ExSel:
                    return Key.Exsel;
                case wpfKey.Key.F:
                    return Key.F;
                case wpfKey.Key.F1:
                    return Key.F1;
                case wpfKey.Key.F10:
                    return Key.F10;
                case wpfKey.Key.F11:
                    return Key.F11;
                case wpfKey.Key.F12:
                    return Key.F12;
                case wpfKey.Key.F13:
                    return Key.F13;
                case wpfKey.Key.F14:
                    return Key.F14;
                case wpfKey.Key.F15:
                    return Key.F15;
                case wpfKey.Key.F16:
                    return Key.F16;
                case wpfKey.Key.F17:
                    return Key.F17;
                case wpfKey.Key.F18:
                    return Key.F18;
                case wpfKey.Key.F19:
                    return Key.F19;
                case wpfKey.Key.F2:
                    return Key.F2;
                case wpfKey.Key.F20:
                    return Key.F20;
                case wpfKey.Key.F21:
                    return Key.F21;
                case wpfKey.Key.F22:
                    return Key.F22;
                case wpfKey.Key.F23:
                    return Key.F23;
                case wpfKey.Key.F24:
                    return Key.F24;
                case wpfKey.Key.F3:
                    return Key.F3;
                case wpfKey.Key.F4:
                    return Key.F4;
                case wpfKey.Key.F5:
                    return Key.F5;
                case wpfKey.Key.F6:
                    return Key.F6;
                case wpfKey.Key.F7:
                    return Key.F7;
                case wpfKey.Key.F8:
                    return Key.F8;
                case wpfKey.Key.F9:
                    return Key.F9;
                case wpfKey.Key.FinalMode:
                    return Key.FinalMode;
                case wpfKey.Key.G:
                    return Key.G;
                case wpfKey.Key.H:
                    return Key.H;
                case wpfKey.Key.HangulMode:
                    return Key.HangulMode;
                case wpfKey.Key.HanjaMode:
                    return Key.HanjaMode;
                case wpfKey.Key.Help:
                    return Key.Help;
                case wpfKey.Key.Home:
                    return Key.Home;
                case wpfKey.Key.I:
                    return Key.I;
                case wpfKey.Key.ImeAccept:
                    return Key.IMEAccept;
                case wpfKey.Key.ImeConvert:
                    return Key.IMEConvert;
                case wpfKey.Key.ImeModeChange:
                    return Key.IMEModeChange;
                case wpfKey.Key.ImeNonConvert:
                    return Key.IMENonconvert;
                case wpfKey.Key.Insert:
                    return Key.Insert;
                case wpfKey.Key.J:
                    return Key.J;
                case wpfKey.Key.JunjaMode:
                    return Key.JunjaMode;
                case wpfKey.Key.K:
                    return Key.K;
                //case wpfKey.Key.KanaMode:
                //    return Key.KanaMode;
                //case wpfKey.Key.KanjiMode:
                //    return Key.KanjiMode;
                case wpfKey.Key.L:
                    return Key.L;
                case wpfKey.Key.LaunchApplication1:
                    return Key.LaunchApplication1;
                case wpfKey.Key.LaunchApplication2:
                    return Key.LaunchApplication2;
                case wpfKey.Key.LaunchMail:
                    return Key.LaunchMail;
                case wpfKey.Key.Left:
                    return Key.Left;
                case wpfKey.Key.LeftAlt:
                    return Key.Alt;
                case wpfKey.Key.LeftCtrl:
                    return Key.RControlKey;
                case wpfKey.Key.LeftShift:
                    return Key.LShiftKey;
                case wpfKey.Key.LineFeed:
                    return Key.LineFeed;
                case wpfKey.Key.LWin:
                    return Key.LWin;
                case wpfKey.Key.M:
                    return Key.M;
                case wpfKey.Key.MediaNextTrack:
                    return Key.MediaNextTrack;
                case wpfKey.Key.MediaPlayPause:
                    return Key.MediaPlayPause;
                case wpfKey.Key.MediaPreviousTrack:
                    return Key.MediaPreviousTrack;
                case wpfKey.Key.MediaStop:
                    return Key.MediaStop;
                case wpfKey.Key.Multiply:
                    return Key.Multiply;
                case wpfKey.Key.N:
                    return Key.N;
                //case wpfKey.Key.Next:
                //    return Key.Next;
                case wpfKey.Key.NoName:
                    return Key.NoName;
                case wpfKey.Key.None:
                    return Key.None;
                case wpfKey.Key.NumLock:
                    return Key.NumLock;
                case wpfKey.Key.NumPad0:
                    return Key.NumPad0;
                case wpfKey.Key.NumPad1:
                    return Key.NumPad1;
                case wpfKey.Key.NumPad2:
                    return Key.NumPad2;
                case wpfKey.Key.NumPad3:
                    return Key.NumPad3;
                case wpfKey.Key.NumPad4:
                    return Key.NumPad4;
                case wpfKey.Key.NumPad5:
                    return Key.NumPad5;
                case wpfKey.Key.NumPad6:
                    return Key.NumPad6;
                case wpfKey.Key.NumPad7:
                    return Key.NumPad7;
                case wpfKey.Key.NumPad8:
                    return Key.NumPad8;
                case wpfKey.Key.NumPad9:
                    return Key.NumPad9;
                case wpfKey.Key.O:
                    return Key.O;
                case wpfKey.Key.Oem1:
                    return Key.Oem1;
                case wpfKey.Key.Oem102:
                    return Key.Oem102;
                case wpfKey.Key.Oem2:
                    return Key.Oem2;
                case wpfKey.Key.Oem3:
                    return Key.Oem3;
                case wpfKey.Key.Oem4:
                    return Key.Oem4;
                case wpfKey.Key.Oem5:
                    return Key.Oem5;
                case wpfKey.Key.Oem6:
                    return Key.Oem6;
                case wpfKey.Key.Oem7:
                    return Key.Oem7;
                case wpfKey.Key.Oem8:
                    return Key.Oem8;
                //case wpfKey.Key.OemBackslash:
                //    return Key.OemBackslash;
                case wpfKey.Key.OemClear:
                    return Key.OemClear;
                //case wpfKey.Key.OemCloseBrackets:
                //   return Key.OemCloseBrackets;
                case wpfKey.Key.OemComma:
                    return Key.Oemcomma;
                case wpfKey.Key.OemMinus:
                    return Key.OemMinus;
                //case wpfKey.Key.OemOpenBrackets:
                //    return Key.OemOpenBrackets;
                case wpfKey.Key.OemPeriod:
                    return Key.OemPeriod;
                //case wpfKey.Key.OemPipe:
                //    return Key.OemPipe;
                case wpfKey.Key.OemPlus:
                    return Key.Oemplus;
                //case wpfKey.Key.OemQuestion:
                //    return Key.OemQuestion;
                //case wpfKey.Key.OemQuotes:
                //    return Key.OemQuotes;
                // case wpfKey.Key.OemSemicolon:
                //    return Key.OemSemicolon;
                //case wpfKey.Key.OemTilde:
                //     return Key.Oemtilde;
                case wpfKey.Key.P:
                    return Key.P;
                case wpfKey.Key.Pa1:
                    return Key.Pa1;
                case wpfKey.Key.PageDown:
                    return Key.PageDown;
                case wpfKey.Key.PageUp:
                    return Key.PageUp;
                case wpfKey.Key.Pause:
                    return Key.Pause;
                //case wpfKey.Key.Pause:
                //   return Key.Play;
                case wpfKey.Key.Play:
                    return Key.Play;
                case wpfKey.Key.Print:
                    return Key.Print;
                case wpfKey.Key.PrintScreen:
                    return Key.PrintScreen;
                //case wpfKey.Key.Prior:
                //    return Key.Prior;
                case wpfKey.Key.Q:
                    return Key.Q;
                case wpfKey.Key.R:
                    return Key.R;
                //case wpfKey.Key.Return:
                //    return Key.Return;
                case wpfKey.Key.Right:
                    return Key.Right;
                case wpfKey.Key.RightAlt:
                    return Key.Alt;
                case wpfKey.Key.RightCtrl:
                    return Key.RControlKey;
                case wpfKey.Key.RightShift:
                    return Key.RShiftKey;
                case wpfKey.Key.RWin:
                    return Key.RWin;
                case wpfKey.Key.S:
                    return Key.S;
                case wpfKey.Key.Scroll:
                    return Key.Scroll;
                case wpfKey.Key.Select:
                    return Key.Select;
                case wpfKey.Key.SelectMedia:
                    return Key.SelectMedia;
                case wpfKey.Key.Separator:
                    return Key.Separator;
                //case wpfKey.Key.LeftShift:
                //    return Key.Shift;
                case wpfKey.Key.Sleep:
                    return Key.Sleep;
                //case wpfKey.Key.Snapshot:
                //   return Key.Snapshot;
                case wpfKey.Key.Space:
                    return Key.Space;
                case wpfKey.Key.Subtract:
                    return Key.Subtract;
                case wpfKey.Key.T:
                    return Key.T;
                case wpfKey.Key.Tab:
                    return Key.Tab;
                case wpfKey.Key.U:
                    return Key.U;
                case wpfKey.Key.Up:
                    return Key.Up;
                case wpfKey.Key.V:
                    return Key.V;
                case wpfKey.Key.VolumeDown:
                    return Key.VolumeDown;
                case wpfKey.Key.VolumeMute:
                    return Key.VolumeMute;
                case wpfKey.Key.VolumeUp:
                    return Key.VolumeUp;
                case wpfKey.Key.W:
                    return Key.W;
                case wpfKey.Key.X:
                    return Key.X;
                case wpfKey.Key.Y:
                    return Key.Y;
                case wpfKey.Key.Z:
                    return Key.Z;
                case wpfKey.Key.Zoom:
                    return Key.Zoom;
            }

            return Key.Modifiers;
        }

        public static Key[] GetPressdKeys()
        {
            byte[] keysArray = new byte[256];
            GetKeyboardState(keysArray);

            List<Key> keys = new List<Key>();

            for (int i = 0; i < keysArray.Length; i++)
            {

                if ((keysArray[i] & 0x80) != 0)
                {
                    keys.Add(GetLogicalKey((byte)i));
                    var k = GetLogicalKey((byte)i);

                    int l = 0;
                    l++;

                    var wp = ConvertToWpfKey(k);
                    var wn = ConvertFromWpfKey(wp);

                    int t = 0;
                    t++;
                }
            }

            return keys.ToArray();
        }

        private static byte GetVirtualKeyCode(Key key)
        {
            int value = (int)key;
            return (byte)(value & 0xFF);
        }

        private static Key GetLogicalKey(byte key)
        {
            return (Key)((int)key);
        }

        public static Key[] ClearRepeatedKeys(Key[] keys)
        {
            List<Key> _keys = new List<Key>(keys);

            if (_keys.Contains(Key.ControlKey) && (_keys.Contains(Key.LControlKey) || _keys.Contains(Key.RControlKey)))
                _keys.Remove(Key.ControlKey);

            if (_keys.Contains(Key.Menu) && (_keys.Contains(Key.LMenu) || _keys.Contains(Key.RMenu)))
                _keys.Remove(Key.Menu);

            if (_keys.Contains(Key.ShiftKey) && (_keys.Contains(Key.RShiftKey) || _keys.Contains(Key.LShiftKey)))
                _keys.Remove(Key.ShiftKey);

            return _keys.ToArray();
        }

        public static Key[] ClearMousKeys(Key[] keys)
        {
            List<Key> _keys = new List<Key>(keys);

            _keys.RemoveAll(x => x == Key.LButton);
            _keys.RemoveAll(x => x == Key.RButton);

            _keys.RemoveAll(x => x == Key.MButton);
            _keys.RemoveAll(x => x == Key.XButton1);
            _keys.RemoveAll(x => x == Key.XButton2);

            return _keys.ToArray();
        }
    }
}
