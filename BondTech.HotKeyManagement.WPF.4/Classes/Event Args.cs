// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



using System;
using System.Windows;
using System.Windows.Input;
using System.Globalization;

namespace BondTech.HotKeyManagement.WPF._4
{
    public class GlobalHotKeyEventArgs : EventArgs
    {
        public GlobalHotKey HotKey { get; private set; }

        public GlobalHotKeyEventArgs(GlobalHotKey hotKey)
        {
            HotKey = hotKey;
        }
    }

    public class LocalHotKeyEventArgs : EventArgs // RoutedEventArgs
    {
        public LocalHotKey HotKey { get; private set; }
        public LocalHotKeyEventArgs(LocalHotKey hotKey)
        {
            HotKey = hotKey;
        }
    }

    public class PreChordHotKeyEventArgs : EventArgs
    {
        private LocalHotKey HotKey;
        private bool handled;
        /// <summary>The base key of the chord that raised this event.
        /// </summary>
        public Key BaseKey { get { return HotKey.Key; } }
        /// <summary>The base modifier of the chord that raised this event.
        /// </summary>
        public ModifierKeys BaseModifier { get { return HotKey.Modifier; } }
        /// <summary>Gets or sets if the chord event should be handled.
        /// </summary>
        public bool HandleChord { get { return handled; } set { handled = value; } }
        /// <summary>Displays information about
        /// </summary>
        public override string ToString()
        {
            return Info();
        }
        /// <summary>Displays the Modifier and key in extended format.
        /// </summary>
        /// <returns>The key and modifier in string.</returns>
        public string Info()
        {
            string info = "";
            foreach (ModifierKeys mod in new HotKeyShared.ParseModifier((int)BaseModifier))
            {
                info += mod + " + ";
            }

            info += BaseKey.ToString();
            return info;
        }

        public PreChordHotKeyEventArgs(LocalHotKey hotkey) { HotKey = hotkey; }
    }

    public class ChordHotKeyEventArgs : EventArgs
    {
        /// <summary>The HotKey that raised this event.
        /// </summary>
        public ChordHotKey HotKey { get; private set; }
        public ChordHotKeyEventArgs(ChordHotKey hotkey) { HotKey = hotkey; }
    }

    public class HotKeyEventArgs : EventArgs
    {
        public Key Key { get; private set; }
        public ModifierKeys Modifier { get; private set; }
        public RaiseLocalEvent KeyPressEvent { get; private set; }

        public HotKeyEventArgs(Key key, ModifierKeys modifier, RaiseLocalEvent KeyPressevent)
        {
            Key = key;
            Modifier = modifier;
            KeyPressEvent = KeyPressevent;
        }
    }

    public class KeyboardHookEventArgs : EventArgs
    {
        public KeyboardHookEventArgs(KeyboardHookStruct lparam)
        {
            LParam = lparam;
        }

        private KeyboardHookStruct lParam;
        private bool handled;
        private KeyboardHookStruct LParam
        {
            get { return lParam; }
            set
            {
                lParam = value;
                var nonVirtual = HelperMethods.MapVirtualKey((uint)VirtualKeyCode, 2);
                Char = Convert.ToChar(nonVirtual);
            }
        }

        /// <summary>The ASCII code of the key pressed.
        /// </summary>
        public int VirtualKeyCode { get { return LParam.VirtualKeyCode; } }
        /// <summary>The Key pressed.
        /// </summary>
        public Key Key { get { return (Key)VirtualKeyCode; } }

        public char Char { get; private set; }

        public string KeyString
        {
            get
            {
                if (Char == '\0')
                {
                    return Key == Key.Return ? "[Enter]" : string.Format("[{0}]", Key);
                }
                if (Char == '\r')
                {
                    Char = '\0';
                    return "[Enter]";
                }
                if (Char == '\b')
                {
                    Char = '\0';
                    return "[Backspace]";
                }
                return Char.ToString(CultureInfo.InvariantCulture);
            }
        }
        /// <summary>Specifies if this key should be processed  by other windows.
        /// </summary>
        public bool Handled
        {
            get { return handled; }
            set
            {
                //Because a key cannot be handled when it is already up, we'll ignore this.
                if (KeyboardEventName != KeyboardEventNames.KeyUp)
                    handled = value;
            }
        }
        /// <summary>The event that raised this 'event' Whether KeyUp or KeyDown.
        /// </summary>
        public KeyboardEventNames KeyboardEventName { get; internal set; }

        public enum modifiers
        {
            /// <summary>Specifies that no modifier key is pressed.
            /// </summary>
            None,
            /// <summary>Specifies that only the Shift key is pressed.
            /// </summary>
            Shift,
            /// <summary>Specifies that only the Control key is pressed.
            /// </summary>
            Control,
            /// <summary>Specifies that only the Alt key is pressed.
            /// </summary>
            Alt,
            /// <summary>Specifies that the Shift and Control key are pressed.
            /// </summary>
            ShiftControl,
            /// <summary>Specifies that the Shift and Alt key are pressed.
            /// </summary>
            ShiftAlt,
            /// <summary>Specifies that the Control and Alt key are pressed.
            /// </summary>
            ControlAlt,
            /// <summary>Specifies that the Shift, Control and Alt key are pressed.
            /// </summary>
            ShiftControlAlt
        }
        /// <summary>Gets the modifier that is pressed when this event was raised.
        /// </summary>
        public modifiers Modifier
        {
            get
            {
                Microsoft.VisualBasic.Devices.Keyboard KeyBoard = new Microsoft.VisualBasic.Devices.Keyboard();
                if (KeyBoard.AltKeyDown && KeyBoard.CtrlKeyDown && KeyBoard.ShiftKeyDown)
                    return modifiers.ShiftControlAlt;
                if (KeyBoard.AltKeyDown && KeyBoard.CtrlKeyDown && !KeyBoard.ShiftKeyDown)
                    return modifiers.ControlAlt;
                if (KeyBoard.AltKeyDown && !KeyBoard.CtrlKeyDown && KeyBoard.ShiftKeyDown)
                    return modifiers.ShiftAlt;
                if (!KeyBoard.AltKeyDown && KeyBoard.CtrlKeyDown && KeyBoard.ShiftKeyDown)
                    return modifiers.ShiftControl;
                if (!KeyBoard.AltKeyDown && !KeyBoard.CtrlKeyDown && KeyBoard.ShiftKeyDown)
                    return modifiers.Shift;
                if (KeyBoard.AltKeyDown && !KeyBoard.CtrlKeyDown && !KeyBoard.ShiftKeyDown)
                    return modifiers.Alt;
                if (!KeyBoard.AltKeyDown && KeyBoard.CtrlKeyDown && !KeyBoard.ShiftKeyDown)
                    return modifiers.Control;
                return modifiers.None;
            }
        }
    }

    public class HotKeyIsSetEventArgs : RoutedEventArgs
    {
        public Key UserKey { get; private set; }
        public ModifierKeys UserModifier { get; private set; }
        public bool Cancel { get; set; }
        public string Shortcut { get { return HotKeyShared.CombineShortcut(UserModifier, UserKey); } }
        public HotKeyIsSetEventArgs(RoutedEvent routedevent, Key key, ModifierKeys modifier)
            : base(routedevent)
        {
            UserKey = key;
            UserModifier = modifier;
        }
    }
}
