// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Text;

namespace BondTech.HotKeyManagement.WPF._4
{
    public static class HotKeyShared
    {
        /// <summary>Checks if a string is a valid object name.
        /// </summary>
        /// <param name="text">The string to check</param>
        /// <returns>true if the name is valid.</returns>
        public static bool IsValidHotkeyName(string text)
        {
            //If the name starts with a number, contains space or is null, return false.
            if (string.IsNullOrEmpty(text)) return false;

            if (text.Contains(" ") || char.IsDigit((char)text.ToCharArray().GetValue(0)))
                return false;

            return true;
        }
        /// <summary>Parses a shortcut string like 'Control + Alt + Shift + V' and returns the key and modifiers.
        /// </summary>
        /// <param name="text">The shortcut string to parse.</param>
        /// <returns>The Modifier in the lower bound and the key in the upper bound.</returns>
        public static object[] ParseShortcut(string text)
        {
            bool HasAlt = false; bool HasControl = false; bool HasShift = false; bool HasWin = false;

            ModifierKeys Modifier = ModifierKeys.None;		//Variable to contain modifier.
            Key key = 0;           //The key to register.
            int current = 0;

            string[] result;
            string[] separators = new string[] { " + " };
            result = text.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            //Iterate through the keys and find the modifier.
            foreach (string entry in result)
            {
                //Find the Control Key.
                if (entry.Trim() == Key.Control.ToString())
                {
                    HasControl = true;
                }
                //Find the Alt key.
                if (entry.Trim() == Key.Alt.ToString())
                {
                    HasAlt = true;
                }
                //Find the Shift key.
                if (entry.Trim() == Key.Shift.ToString())
                {
                    HasShift = true;
                }
                //Find the Window key.
                if (entry.Trim() == Key.LWin.ToString() && current != result.Length - 1)
                {
                    HasWin = true;
                }

                current++;
            }

            if (HasControl) { Modifier |= ModifierKeys.Control; }
            if (HasAlt) { Modifier |= ModifierKeys.Alt; }
            if (HasShift) { Modifier |= ModifierKeys.Shift; }
            if (HasWin) { Modifier |= ModifierKeys.Windows; }

            KeysConverter keyconverter = new KeysConverter();
            key = (Key)keyconverter.ConvertFrom(result.GetValue(result.Length - 1));

            return new object[] { Modifier, key };
        }
        /// <summary>Parses a shortcut string like 'Control + Alt + Shift + V' and returns the key and modifiers.
        /// </summary>
        /// <param name="text">The shortcut string to parse.</param>
        /// <param name="separator">The delimiter for the shortcut.</param>
        /// <returns>The Modifier in the lower bound and the key in the upper bound.</returns>
        public static object[] ParseShortcut(string text, string separator)
        {
            bool HasAlt = false; bool HasControl = false; bool HasShift = false; bool HasWin = false;

            ModifierKeys Modifier = ModifierKeys.None;		//Variable to contain modifier.
            Key key = 0;           //The key to register.
            int current = 0;

            string[] result;
            string[] separators = new string[] { separator };
            result = text.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            //Iterate through the keys and find the modifier.
            foreach (string entry in result)
            {
                //Find the Control Key.
                if (entry.Trim() == Key.Control.ToString())
                {
                    HasControl = true;
                }
                //Find the Alt key.
                if (entry.Trim() == Key.Alt.ToString())
                {
                    HasAlt = true;
                }
                //Find the Shift key.
                if (entry.Trim() == Key.Shift.ToString())
                {
                    HasShift = true;
                }
                //Find the Window key.
                if (entry.Trim() == Key.LWin.ToString() && current != result.Length - 1)
                {
                    HasWin = true;
                }
                current++;
            }

            if (HasControl) { Modifier |= ModifierKeys.Control; }
            if (HasAlt) { Modifier |= ModifierKeys.Alt; }
            if (HasShift) { Modifier |= ModifierKeys.Shift; }
            if (HasWin) { Modifier |= ModifierKeys.Windows; }

            KeysConverter keyconverter = new KeysConverter();
            key = (Key)keyconverter.ConvertFrom(result.GetValue(result.Length - 1));

            return new object[] { Modifier, key };
        }
        /// <summary>Combines the modifier and key to a shortcut.
        /// Changes Control;Shift;Alt;T to Control + Shift + Alt + T
        /// </summary>
        /// <param name="mod">The modifier.</param>
        /// <param name="key">The key.</param>
        /// <returns>A string representation of the modifier and key.</returns>
        public static string CombineShortcut(ModifierKeys mod, Key key)
        {
            string hotkey = "";
            foreach (ModifierKeys a in new HotKeyShared.ParseModifier((int)mod))
            {
                hotkey += a.ToString() + " + ";
            }

            if (hotkey.Contains(ModifierKeys.None.ToString())) hotkey = "";
            hotkey += key.ToString();
            return hotkey;
        }
        /// <summary>Combines the modifier and key to a shortcut.
        /// Changes Control;Shift;Alt;T to Control + Shift + Alt + T
        /// </summary>
        /// <param name="mod">The modifier.</param>
        /// <param name="key">The key.</param>
        /// <returns>A string representation of the modifier and key.</returns>
        public static string CombineShortcut(ModifierKeys mod, System.Windows.Input.Key key)
        {
            string hotkey = "";
            foreach (ModifierKeys a in new HotKeyShared.ParseModifier((int)mod))
            {
                hotkey += a.ToString() + " + ";
            }

            if (hotkey.Contains(ModifierKeys.None.ToString())) hotkey = "";
            hotkey += key.ToString();
            return hotkey;
        }
        /// <summary>Combines the modifier and key to a shortcut.
        /// Changes Control;Shift;Alt; to Control + Shift + Alt
        /// </summary>
        /// <param name="mod">The modifier.</param>
        /// <returns>A string representation of the modifier</returns>
        public static string CombineShortcut(ModifierKeys mod)
        {
            string hotkey = "";
            foreach (ModifierKeys a in new HotKeyShared.ParseModifier((int)mod))
            {
                hotkey += a.ToString() + " + ";
            }

            if (hotkey.Contains(ModifierKeys.None.ToString())) hotkey = "";
            if (hotkey.Trim().EndsWith("+")) hotkey = hotkey.Trim().Substring(0, hotkey.Length - 1);

            return hotkey;
        }
        /// <summary>Allows the conversion of an integer to its modifier representation.
        /// </summary>
        public struct ParseModifier : IEnumerable
        {
            private List<ModifierKeys> Enumeration;
            public bool HasAlt;
            public bool HasControl;
            public bool HasShift;
            public bool HasWin;

            /// <summary>Initializes this class.
            /// </summary>
            /// <param name="Modifier">The integer representation of the modifier to parse.</param>
            public ParseModifier(int Modifier)
            {
                Enumeration = new List<ModifierKeys>();
                HasAlt = false;
                HasWin = false;
                HasShift = false;
                HasControl = false;
                switch (Modifier)
                {
                    case 0:
                        Enumeration.Add(ModifierKeys.None);
                        break;
                    case 1:
                        HasAlt = true;
                        Enumeration.Add(ModifierKeys.Alt);
                        break;
                    case 2:
                        HasControl = true;
                        Enumeration.Add(ModifierKeys.Control);
                        break;
                    case 3:
                        HasAlt = true;
                        HasControl = true;
                        Enumeration.Add(ModifierKeys.Control);
                        Enumeration.Add(ModifierKeys.Alt);
                        break;
                    case 4:
                        HasShift = true;
                        Enumeration.Add(ModifierKeys.Shift);
                        break;
                    case 5:
                        HasShift = true;
                        HasAlt = true;
                        Enumeration.Add(ModifierKeys.Shift);
                        Enumeration.Add(ModifierKeys.Alt);
                        break;
                    case 6:
                        HasShift = true;
                        HasControl = true;
                        Enumeration.Add(ModifierKeys.Shift);
                        Enumeration.Add(ModifierKeys.Control);
                        break;
                    case 7:
                        HasControl = true;
                        HasShift = true;
                        HasAlt = true;
                        Enumeration.Add(ModifierKeys.Shift);
                        Enumeration.Add(ModifierKeys.Control);
                        Enumeration.Add(ModifierKeys.Alt);
                        break;
                    case 8:
                        HasWin = true;
                        Enumeration.Add(ModifierKeys.Windows);
                        break;
                    case 9:
                        HasAlt = true;
                        HasWin = true;
                        Enumeration.Add(ModifierKeys.Alt);
                        Enumeration.Add(ModifierKeys.Windows);
                        break;
                    case 10:
                        HasControl = true;
                        HasWin = true;
                        Enumeration.Add(ModifierKeys.Control);
                        Enumeration.Add(ModifierKeys.Windows);
                        break;
                    case 11:
                        HasControl = true;
                        HasAlt = true;
                        HasWin = true;
                        Enumeration.Add(ModifierKeys.Control);
                        Enumeration.Add(ModifierKeys.Alt);
                        Enumeration.Add(ModifierKeys.Windows);
                        break;
                    case 12:
                        HasShift = true;
                        HasWin = true;
                        Enumeration.Add(ModifierKeys.Shift);
                        Enumeration.Add(ModifierKeys.Windows);
                        break;
                    case 13:
                        HasShift = true;
                        HasAlt = true;
                        HasWin = true;
                        Enumeration.Add(ModifierKeys.Shift);
                        Enumeration.Add(ModifierKeys.Alt);
                        Enumeration.Add(ModifierKeys.Windows);
                        break;
                    case 14:
                        HasShift = true;
                        HasControl = true;
                        HasWin = true;
                        Enumeration.Add(ModifierKeys.Shift);
                        Enumeration.Add(ModifierKeys.Control);
                        Enumeration.Add(ModifierKeys.Windows);
                        break;
                    case 15:
                        HasShift = true;
                        HasControl = true;
                        HasAlt = true;
                        HasWin = true;
                        Enumeration.Add(ModifierKeys.Shift);
                        Enumeration.Add(ModifierKeys.Control);
                        Enumeration.Add(ModifierKeys.Alt);
                        Enumeration.Add(ModifierKeys.Windows);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("The argument is parsed is more than the expected range", "Modifier");
                }
            }
            /// <summary>Initializes this class.
            /// </summary>
            /// <param name="mod">the modifier to parse.</param>
            public ParseModifier(ModifierKeys mod) : this((int)mod) { }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return Enumeration.GetEnumerator();
            }
        }
    }

    /// <summary>Provides a System.ComponentModel.TypeConverter to convert System.Windows.Forms.Keys
    ///     objects to and from other representations.
    /// </summary>
    public class KeysConverter : TypeConverter, IComparer
    {
        private List<string> displayOrder;
        private const Key FirstAscii = Key.A;
        private const Key FirstDigit = Key.D0;
        private const Key FirstNumpadDigit = Key.NumPad0;
        private IDictionary keyNames;
        private const Key LastAscii = Key.Z;
        private const Key LastDigit = Key.D9;
        private const Key LastNumpadDigit = Key.NumPad9;
        private TypeConverter.StandardValuesCollection values;

        private void AddKey(string key, Key value)
        {
            this.keyNames[key] = value;
            this.displayOrder.Add(key);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, System.Type sourceType)
        {
            if ((sourceType != typeof(string)) && (sourceType != typeof(Enum[])))
            {
                return base.CanConvertFrom(context, sourceType);
            }
            return true;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, System.Type destinationType)
        {
            return ((destinationType == typeof(Enum[])) || base.CanConvertTo(context, destinationType));
        }

        public int Compare(object a, object b)
        {
            return string.Compare(base.ConvertToString(a), base.ConvertToString(b), false, CultureInfo.InvariantCulture);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                string str = ((string)value).Trim();
                if (str.Length == 0)
                {
                    return null;
                }
                string[] strArray = str.Split(new char[] { '+' });
                for (int i = 0; i < strArray.Length; i++)
                {
                    strArray[i] = strArray[i].Trim();
                }
                Key none = Key.None;
                bool flag = false;
                for (int j = 0; j < strArray.Length; j++)
                {
                    object obj2 = this.KeyNames[strArray[j]];
                    if (obj2 == null)
                    {
                        obj2 = Enum.Parse(typeof(Key), strArray[j]);
                    }
                    if (obj2 == null)
                    {
                        throw new FormatException("Invalid Key Name");
                    }
                    Key keys2 = (Key)obj2;
                    if ((keys2 & Key.KeyCode) != Key.None)
                    {
                        if (flag)
                        {
                            throw new FormatException("Invalid Key Combination");
                        }
                        flag = true;
                    }
                    none |= keys2;
                }
                return none;
            }
            if (!(value is Enum[]))
            {
                return base.ConvertFrom(context, culture, value);
            }
            long num3 = 0L;
            foreach (Enum enum2 in (Enum[])value)
            {
                num3 |= Convert.ToInt64(enum2, CultureInfo.InvariantCulture);
            }
            return Enum.ToObject(typeof(Key), num3);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, System.Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException("destinationType");
            }
            if ((value is Key) || (value is int))
            {
                bool flag = destinationType == typeof(string);
                bool flag2 = false;
                if (!flag)
                {
                    flag2 = destinationType == typeof(Enum[]);
                }
                if (flag || flag2)
                {
                    Key keys = (Key)value;
                    bool flag3 = false;
                    ArrayList list = new ArrayList();
                    Key keys2 = keys & ~Key.KeyCode;
                    for (int i = 0; i < this.DisplayOrder.Count; i++)
                    {
                        string str = this.DisplayOrder[i];
                        Key keys3 = (Key)this.keyNames[str];
                        if ((keys3 & keys2) != Key.None)
                        {
                            if (flag)
                            {
                                if (flag3)
                                {
                                    list.Add("+");
                                }
                                list.Add(str);
                            }
                            else
                            {
                                list.Add(keys3);
                            }
                            flag3 = true;
                        }
                    }
                    Key keys4 = keys & Key.KeyCode;
                    bool flag4 = false;
                    if (flag3 && flag)
                    {
                        list.Add("+");
                    }
                    for (int j = 0; j < this.DisplayOrder.Count; j++)
                    {
                        string str2 = this.DisplayOrder[j];
                        Key keys5 = (Key)this.keyNames[str2];
                        if (keys5.Equals(keys4))
                        {
                            if (flag)
                            {
                                list.Add(str2);
                            }
                            else
                            {
                                list.Add(keys5);
                            }
                            flag3 = true;
                            flag4 = true;
                            break;
                        }
                    }
                    if (!flag4 && Enum.IsDefined(typeof(Key), (int)keys4))
                    {
                        if (flag)
                        {
                            list.Add(keys4.ToString());
                        }
                        else
                        {
                            list.Add(keys4);
                        }
                    }
                    if (!flag)
                    {
                        return (Enum[])list.ToArray(typeof(Enum));
                    }
                    StringBuilder builder = new StringBuilder(0x20);
                    foreach (string str3 in list)
                    {
                        builder.Append(str3);
                    }
                    return builder.ToString();
                }
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if (this.values == null)
            {
                ArrayList list = new ArrayList();
                foreach (object obj2 in this.KeyNames.Values)
                {
                    list.Add(obj2);
                }
                list.Sort(this);
                this.values = new TypeConverter.StandardValuesCollection(list.ToArray());
            }
            return this.values;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return false;
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        private void Initialize()
        {
            this.keyNames = new Hashtable(0x22);
            this.displayOrder = new List<string>(0x22);
            this.AddKey(Key.Enter.ToString().ToUpper(), Key.Enter);
            this.AddKey("F12", Key.F12);
            this.AddKey("F11", Key.F11);
            this.AddKey("F10", Key.F10);
            this.AddKey(Key.End.ToString().ToUpper(), Key.End);
            this.AddKey(Key.Control.ToString().ToUpper(), Key.Control);
            this.AddKey("F8", Key.F8);
            this.AddKey("F9", Key.F9);
            this.AddKey(Key.Alt.ToString().ToUpper(), Key.Alt);
            this.AddKey("F4", Key.F4);
            this.AddKey("F5", Key.F5);
            this.AddKey("F6", Key.F6);
            this.AddKey("F7", Key.F7);
            this.AddKey("F1", Key.F1);
            this.AddKey("F2", Key.F2);
            this.AddKey("F3", Key.F3);
            this.AddKey(Key.PageDown.ToString().ToUpper(), Key.PageDown);
            this.AddKey(Key.Insert.ToString().ToUpper(), Key.Insert);
            this.AddKey(Key.Home.ToString().ToUpper(), Key.Home);
            this.AddKey(Key.Delete.ToString().ToUpper(), Key.Delete);
            this.AddKey(Key.Shift.ToString().ToUpper(), Key.Shift);
            this.AddKey(Key.PageUp.ToString().ToUpper(), Key.PageUp);
            this.AddKey(Key.Back.ToString().ToUpper(), Key.Back);
            this.AddKey("0", Key.D0);
            this.AddKey("1", Key.D1);
            this.AddKey("2", Key.D2);
            this.AddKey("3", Key.D3);
            this.AddKey("4", Key.D4);
            this.AddKey("5", Key.D5);
            this.AddKey("6", Key.D6);
            this.AddKey("7", Key.D7);
            this.AddKey("8", Key.D8);
            this.AddKey("9", Key.D9);
        }

        private List<string> DisplayOrder
        {
            get
            {
                if (this.displayOrder == null)
                {
                    this.Initialize();
                }
                return this.displayOrder;
            }
        }

        private IDictionary KeyNames
        {
            get
            {
                if (this.keyNames == null)
                {
                    this.Initialize();
                }
                return this.keyNames;
            }
        }
    }

    internal static class HelperMethods
    {
        /// <summary>
        /// This delegate matches the type of parameter "lpfn" for the NativeMethods method "SetWindowsHookEx".
        /// For more information: http://msdn.microsoft.com/en-us/library/ms644986(VS.85).aspx
        /// </summary>
        /// <param name="nCode">
        /// Specifies whether the hook procedure must process the message. 
        /// If nCode is HC_ACTION, the hook procedure must process the message. 
        /// If nCode is less than zero, the hook procedure must pass the message to the 
        /// CallNextHookEx function without further processing and must return the 
        /// value returned by CallNextHookEx.
        /// </param>
        /// <param name="wParam">
        /// Specifies whether the message was sent by the current thread. 
        /// If the message was sent by the current thread, it is nonzero; otherwise, it is zero.
        /// </param>
        /// <param name="lParam">Pointer to a CWPSTRUCT structure that contains details about the message.
        /// </param>
        /// <returns>
        /// If nCode is less than zero, the hook procedure must return the value returned by CallNextHookEx. 
        /// If nCode is greater than or equal to zero, it is highly recommended that you call CallNextHookEx 
        /// and return the value it returns; otherwise, other applications that have installed WH_CALLWNDPROC 
        /// hooks will not receive hook notifications and may behave incorrectly as a result. If the hook 
        /// procedure does not call CallNextHookEx, the return value should be zero.
        /// </returns>
        internal delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>Registers a shortcut on a global level.
        /// </summary>
        /// <param name="hwnd">
        /// Handle to the window that will receive WM_HOTKEY messages generated by the hot key.
        /// If this parameter is NULL, WM_HOTKEY messages are posted to the message queue of the calling thread and must be processed in the message loop. 
        /// </param>
        /// <param name="id">Specifies the identifier of the hot key.
        /// If the hWnd parameter is NULL, then the hot key is associated with the current thread rather than with a particular window.
        /// If a hot key already exists with the same hWnd and id parameters
        /// </param>
        /// <param name="modifiers">
        /// Specifies keys that must be pressed in combination with the key specified by the Key parameter in order to generate the WM_HOTKEY message.
        /// The fsModifiers parameter can be a combination of the following values. 
        ///MOD_ALT
        ///Either ALT key must be held down.
        ///MOD_CONTROL
        ///Either CTRL key must be held down.
        ///MOD_SHIFT
        ///Either SHIFT key must be held down.
        ///MOD_WIN
        ///Either WINDOWS key was held down. These keys are labelled with the Windows logo.
        ///Keyboard shortcuts that involve the WINDOWS key are reserved for use by the operating system.
        ///</param>
        /// <param name="key">Specifies the virtual-key code of the hot key.
        ///</param>
        /// <returns>If the function succeeds, the return value is nonzero. 
        ///If the function fails, the return value is zero. To get extended error information, call GetLastError. 
        ///</returns>
        [DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int RegisterHotKey(IntPtr hwnd, int id, int modifiers, int key);

        /// <summary>
        /// </summary>
        /// <param name="hwnd">Handle to the window associated with the hot key to be freed.
        /// This parameter should be NULL if the hot key is not associated with a window. 
        ///</param>
        /// <param name="id">Specifies the identifier of the hot key to be freed. 
        ///</param>
        /// <returns>If the function succeeds, the return value is nonzero.
        ///If the function fails, the return value is zero. To get extended error information, call GetLastError. 
        ///</returns>
        [DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int UnregisterHotKey(IntPtr hwnd, int id);

        /// <summary>
        /// The SetWindowsHookEx function installs an application-defined hook procedure into a hook chain. 
        /// You would install a hook procedure to monitor the system for certain types of events. These events 
        /// are associated either with a specific thread or with all threads in the same desktop as the calling thread. 
        /// </summary>
        /// <param name="idHook">
        /// [in] Specifies the type of hook procedure to be installed. This parameter can be one of the following values.
        /// </param>
        /// <param name="lpfn">
        /// [in] Pointer to the hook procedure. If the dwThreadId parameter is zero or specifies the identifier of a 
        /// thread created by a different process, the lpfn parameter must point to a hook procedure in a dynamic-link 
        /// library (DLL). Otherwise, lpfn can point to a hook procedure in the code associated with the current process.
        /// </param>
        /// <param name="hMod">
        /// [in] Handle to the DLL containing the hook procedure pointed to by the lpfn parameter. 
        /// The hMod parameter must be set to NULL if the dwThreadId parameter specifies a thread created by 
        /// the current process and if the hook procedure is within the code associated with the current process. 
        /// </param>
        /// <param name="dwThreadId">
        /// [in] Specifies the identifier of the thread with which the hook procedure is to be associated. 
        /// If this parameter is zero, the hook procedure is associated with all existing threads running in the 
        /// same desktop as the calling thread. 
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is the handle to the hook procedure.
        /// If the function fails, the return value is NULL. To get extended error information, call GetLastError.
        /// </returns>
        /// <remarks>
        /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/windowing/hooks/hookreference/hookfunctions/setwindowshookex.asp
        /// </remarks>
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);

        /// <summary>Retrieves a module handle for the specified module.
        /// The module must have been loaded by the calling process.
        /// </summary>
        /// <param name="lpModuleName">
        /// The name of the loaded module (either a .dll or .exe file). 
        /// If the file name extension is omitted, the default library extension .dll is appended.
        /// The file name string can include a trailing point character (.) to indicate that the module name has no extension. The string does not have to specify a path. When specifying a path, be sure to use backslashes (\), not forward slashes (/). The name is compared (case independently) to the names of modules currently mapped into the address space of the calling process. 
        ///If this parameter is NULL, GetModuleHandle returns a handle to the file used to create the calling process (.exe file). 
        ///The GetModuleHandle function does not retrieve handles for modules that were loaded using the LOAD_LIBRARY_AS_DATAFILE flag.
        ///</param>
        ///<returns>
        ///If the function succeeds, the return value is a handle to the specified module.
        ///If the function fails, the return value is NULL.
        /// </returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetModuleHandle(string lpModuleName);

        /// <summary>
        /// The UnhookWindowsHookEx function removes a hook procedure installed in a hook chain by the SetWindowsHookEx function. 
        /// </summary>
        /// <param name="idHook">
        /// [in] Handle to the hook to be removed. This parameter is a hook handle obtained by a previous call to SetWindowsHookEx. 
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero.
        /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
        /// </returns>
        /// <remarks>
        /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/windowing/hooks/hookreference/hookfunctions/setwindowshookex.asp
        /// </remarks>
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern int UnhookWindowsHookEx(IntPtr idHook);

        /// <summary>
        /// The CallNextHookEx function passes the hook information to the next hook procedure in the current hook chain. 
        /// A hook procedure can call this function either before or after processing the hook information. 
        /// </summary>
        /// <param name="idHook">Ignored.</param>
        /// <param name="nCode">
        /// [in] Specifies the hook code passed to the current hook procedure. 
        /// The next hook procedure uses this code to determine how to process the hook information.
        /// </param>
        /// <param name="wParam">
        /// [in] Specifies the wParam value passed to the current hook procedure. 
        /// The meaning of this parameter depends on the type of hook associated with the current hook chain. 
        /// </param>
        /// <param name="lParam">
        /// [in] Specifies the lParam value passed to the current hook procedure. 
        /// The meaning of this parameter depends on the type of hook associated with the current hook chain. 
        /// </param>
        /// <returns>
        /// This value is returned by the next hook procedure in the chain. 
        /// The current hook procedure must also return this value. The meaning of the return value depends on the hook type. 
        /// For more information, see the descriptions of the individual hook procedures.
        /// </returns>
        /// <remarks>
        /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/windowing/hooks/hookreference/hookfunctions/setwindowshookex.asp
        /// </remarks>
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>
        ///The MapVirtualKey function translates (maps) a virtual-key code into a scan code or character value, or translates a scan code into a virtual-key code. 
        ///</summary>
        ///<param name="uCode">Specifies the virtual-key code or scan code for a key.
        ///How this value is interpreted depends on the value of the uMapType parameter. 
        ///</param>
        ///<param name="uMapType">Specifies the translation to perform.</param>
        ///<returns>The return value is either a scan code, a virtual-key code, or a character value, depending on the value of uCode and uMapType.
        ///If there is no translation, the return value is zero. 
        ///</returns>
        [DllImport("user32.dll")]
        internal static extern uint MapVirtualKey(uint uCode, uint uMapType);

        ///<summary>
        ///The keybd_event function synthesizes a keystroke. 
        ///The system can use such a synthesized keystroke to generate a WM_KEYUP or WM_KEYDOWN message.
        ///</summary>
        ///<param name="key">Specifies a virtual-key code. The code must be a value in the range 1 to 254.</param>
        ///<param name="scan">Specifies a hardware scan code for the key.
        ///</param>
        ///<param name="flags">
        ///Specifies various aspects of function operation. This parameter can be one or more of the following values. 
        ///KEYEVENTF_EXTENDEDKEY
        ///If specified, the scan code was preceded by a prefix byte having the value 0xE0 (224).
        ///KEYEVENTF_KEYUP
        ///If specified, the key is being released. If not specified, the key is being depressed.
        ///</param>
        ///<param name="extraInfo">Specifies an additional value associated with the key stroke. 
        ///</param>
        [DllImport("user32.dll")]
        internal static extern void keybd_event(byte key, byte scan, int flags, int extraInfo);

        internal static IntPtr SetWindowsHook(int hookType, HookProc callback)
        {
            IntPtr hookId;
            using (var currentProcess = Process.GetCurrentProcess())
            using (var currentModule = currentProcess.MainModule)
            {
                var handle = HelperMethods.GetModuleHandle(currentModule.ModuleName);
                hookId = HelperMethods.SetWindowsHookEx(hookType, callback, handle, 0);
            }
            return hookId;
        }
    }
}
