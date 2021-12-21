// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace BondTech.HotKeyManagement.WPF._4
{
    #region **HotKeyManager.
    /// <summary>
    /// The HotKeyHost needed for working with hotKeys.
    /// </summary>
    public sealed class HotKeyManager : IDisposable //, IEnumerable, IEnumerable<GlobalHotKey>, IEnumerable<LocalHotKey>, IEnumerable<ChordHotKey>
    {
        #region **Properties
        public enum CheckKey
        {
            /// <summary>Specifies that the HotKey should be checked against Local and Global HotKeys.
            /// </summary>
            Both = 0,
            /// <summary>Specifies that the HotKey should be checked against GlobalHotKeys only.
            /// </summary>
            GlobalHotKey = 1,
            /// <summary>Specifies that the HotKey should be checked against LocalHotKeys only.
            /// </summary>
            LocalHotKey = 2
        }

        private HwndSourceHook hook;
        private HwndSource hwndSource;
        private static readonly SerialCounter idGen = new SerialCounter(-1); //Will keep track of all the registered GlobalHotKeys
        private IntPtr hookId;
        private HelperMethods.HookProc callback;
        private bool hooked;
        static bool InChordMode; //Will determine if a chord has started.

        private List<GlobalHotKey> GlobalHotKeyContainer = new List<GlobalHotKey>(); //Will hold our GlobalHotKeys
        private List<LocalHotKey> LocalHotKeyContainer = new List<LocalHotKey>(); //Will hold our LocalHotKeys.
        private List<ChordHotKey> ChordHotKeyContainer = new List<ChordHotKey>(); //Will hold our ChordHotKeys.

        //Keep the previous key and modifier that started a chord.
        Key PreChordKey;
        ModifierKeys PreChordModifier;

        /// <summary>Determines if exceptions should be raised when an error occurs.
        /// </summary>
        public bool SuppressException { get; set; } //Determines if you want exceptions to be thrown.
        /// <summary>Determines if the manager is active.
        /// </summary>
        public bool Enabled { get; set; } //Refuse to listen to any windows message.
        /// <summary>Specifies if the keyboard has been hooked.
        /// </summary>
        public bool KeyboardHooked { get { return hooked; } }
        /// <summary>Returns the total number of registered GlobalHotkeys.
        /// </summary>
        public int GlobalHotKeyCount { get; private set; }
        /// <summary>Returns the total number of registered LocalHotkeys.
        /// </summary>
        public int LocalHotKeyCount { get; private set; }
        /// <summary>Returns the total number of registered ChordHotKeys.
        /// </summary>
        public int ChordHotKeyCount { get; private set; }
        /// <summary>Returns the total number of registered HotKey with the HotKeyManager.
        /// </summary>
        public int HotKeyCount { get { return LocalHotKeyCount + GlobalHotKeyCount + ChordHotKeyCount; } }
        #endregion

        #region **Event Handlers.
        /// <summary>Will be raised if a registered GlobalHotKey is pressed
        /// </summary>
        public event GlobalHotKeyEventHandler GlobalHotKeyPressed = delegate { };
        /// <summary>Will be raised if an local Hotkey is pressed.
        /// </summary>
        public event LocalHotKeyEventHandler LocalHotKeyPressed = delegate { };
        /// <summary>Will be raised if a Key is help down on the keyboard.
        /// The keyboard has to be hooked for this event to be raised.
        /// </summary>
        public event KeyboardHookEventHandler KeyBoardKeyDown = delegate { };
        /// <summary>Will be raised if a key is released on the keyboard.
        /// The keyboard has to be hooked for this event to be raised.
        /// </summary>
        public event KeyboardHookEventHandler KeyBoardKeyUp = delegate { };
        /// <summary>Will be raised if a key is pressed on the keyboard.
        /// The keyboard has to be hooked for this event to be raised.
        /// </summary>
        public event KeyboardHookEventHandler KeyBoardKeyEvent = delegate { };
        /// <summary>Will be raised if a key is pressed in the current application.
        /// </summary>
        public event HotKeyEventHandler KeyPressEvent = delegate { };
        /// <summary>Will be raised if a Chord has started.
        /// </summary>
        public event PreChordHotkeyEventHandler ChordStarted = delegate { };
        /// <summary>Will be raised if a chord is pressed.
        /// </summary>
        public event ChordHotKeyEventHandler ChordPressed = delegate { };
        #endregion

        #region **Enumerations.
        /// <summary>Use for enumerating through all GlobalHotKeys.
        /// </summary>
        public IEnumerable EnumerateGlobalHotKeys { get { return GlobalHotKeyContainer; } }
        /// <summary>Use for enumerating through all LocalHotKeys.
        /// </summary>
        public IEnumerable EnumerateLocalHotKeys { get { return LocalHotKeyContainer; } }
        /// <summary>Use for enumerating through all ChordHotKeys.
        /// </summary>
        public IEnumerable EnumerateChordHotKeys { get { return ChordHotKeyContainer; } }

        //IEnumerator<GlobalHotKey> IEnumerable<GlobalHotKey>.GetEnumerator()
        //{
        //    return GlobalHotKeyContainer.GetEnumerator();
        //}

        //IEnumerator<LocalHotKey> IEnumerable<LocalHotKey>.GetEnumerator()
        //{
        //    return LocalHotKeyContainer.GetEnumerator();
        //}

        //IEnumerator<ChordHotKey> IEnumerable<ChordHotKey>.GetEnumerator()
        //{
        //    return ChordHotKeyContainer.GetEnumerator();
        //}

        //IEnumerator IEnumerable.GetEnumerator()
        //{
        //    yield break;
        //    //return (IEnumerator)((IEnumerable<GlobalHotKey>)this).GetEnumerator();
        //}
        #endregion

        #region **Handle GlobalHotKey Property Changing.
        void GlobalHotKeyPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var kvPair = sender as GlobalHotKey;

            if (kvPair != null)
            {
                if (e.PropertyName == "Enabled")
                {
                    if (kvPair.Enabled)
                        RegisterGlobalHotKey(kvPair.Id, kvPair);
                    else
                        UnregisterGlobalHotKey(kvPair.Id);
                }
                else if (e.PropertyName == "Key" || e.PropertyName == "Modifier")
                {
                    if (kvPair.Enabled)
                    {
                        UnregisterGlobalHotKey(kvPair.Id);
                        RegisterGlobalHotKey(kvPair.Id, kvPair);
                    }
                }
            }
        }
        #endregion

        #region **Constructor.
        /// <summary>Creates a new HotKeyManager object
        /// </summary>
        /// <param name="form">The form to associate hotkeys with. Must not be null.</param>
        public HotKeyManager(Window window) : this(window, false) { }
        /// <summary>
        /// Creates a new HotKeyManager Object.
        /// </summary>
        /// <param name="hwndSource">The handle of the window. Must not be null.</param>
        public HotKeyManager(Window window, bool suppressExceptions)
        {
            if (window == null)
                throw new ArgumentNullException("window");

            this.hook = new HwndSourceHook(WndProc); //Hook to to Windows messages.

            this.hwndSource = (HwndSource)HwndSource.FromVisual(window); // new WindowInteropHelper(window).Handle // If the InPtr is needed.
            this.hwndSource.AddHook(hook);
            this.SuppressException = suppressExceptions;
            this.Enabled = true;

            //AutoDispose
            window.Closing += (s, e) => { this.Dispose(); };
        }
        #endregion

        #region **Keyboard Hook.
        private void OnKeyboardKeyDown(KeyboardHookEventArgs e)
        {
            if (KeyBoardKeyDown != null)
                KeyBoardKeyDown(this, e);
            OnKeyboardKeyEvent(e);
        }

        private void OnKeyboardKeyUp(KeyboardHookEventArgs e)
        {
            if (KeyBoardKeyUp != null)
                KeyBoardKeyUp(this, e);
            OnKeyboardKeyEvent(e);
        }

        private void OnKeyboardKeyEvent(KeyboardHookEventArgs e)
        {
            if (KeyBoardKeyEvent != null)
                KeyBoardKeyEvent(this, e);
        }

        /// <summary>Allows the application to listen to all keyboard messages.
        /// </summary>
        public void KeyBoardHook()
        {
            callback = KeyboardHookCallback;
            hookId = HelperMethods.SetWindowsHook((int)KeyboardHookEnum.KeyboardHook, callback);
            hooked = true;
        }
        /// <summary>Stops the application from listening to all keyboard messages.
        /// </summary>
        public void KeyBoardUnHook()
        {
            try
            {
                if (!hooked) return;
                HelperMethods.UnhookWindowsHookEx(hookId);
                callback = null;
                hooked = false;
            }
            catch (MarshalDirectiveException)
            {
                //if (!SuppressException) throw (e);
            }
        }
        /// <summary>
        /// This is the call-back method that is called whenever a keyboard event is triggered.
        /// We use it to call our individual custom events.
        /// </summary>
        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (!Enabled) return HelperMethods.CallNextHookEx(hookId, nCode, wParam, lParam);

            if (nCode >= 0)
            {
                var lParamStruct = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));
                var e = new KeyboardHookEventArgs(lParamStruct);
                switch ((KeyboardMessages)wParam) //-V3002
                {
                    case KeyboardMessages.WmSyskeydown:
                    case KeyboardMessages.WmKeydown:
                        e.KeyboardEventName = KeyboardEventNames.KeyDown;
                        OnKeyboardKeyDown(e);
                        break;

                    case KeyboardMessages.WmSyskeyup:
                    case KeyboardMessages.WmKeyup:
                        e.KeyboardEventName = KeyboardEventNames.KeyUp;
                        OnKeyboardKeyUp(e);
                        break;
                }

                if (e.Handled) { return (IntPtr)(-1); }
            }
            return HelperMethods.CallNextHookEx(hookId, nCode, wParam, lParam);
        }
        #endregion

        #region **Simulation.
        /// <summary>Simulates pressing a key.
        /// </summary>
        /// <param name="key">The key to press.</param>
        public void SimulateKeyDown(Key key)
        {
            HelperMethods.keybd_event(ParseKey(key), 0, 0, 0);
        }
        /// <sum.mary>Simulates releasing a key
        /// </summary>
        /// <param name="key">The key to release.</param>
        public void SimulateKeyUp(Key key)
        {
            HelperMethods.keybd_event(ParseKey(key), 0, (int)KeyboardHookEnum.Keyboard_KeyUp, 0);
        }
        /// <summary>Simulates pressing a key. The key is pressed, then released.
        /// </summary>
        /// <param name="key">The key to press.</param>
        public void SimulateKeyPress(Key key)
        {
            SimulateKeyDown(key);
            SimulateKeyUp(key);
        }

        static byte ParseKey(Key key)
        {
            // Alt, Shift, and Control need to be changed for API function to work with them
            switch (key)
            {
                case Key.Alt:
                    return (byte)18;
                case Key.Control:
                    return (byte)17;
                case Key.Shift:
                    return (byte)16;
                default:
                    return (byte)key;
            }
        }
        #endregion

        #region **Listen to Windows messages.
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (!Enabled) { return IntPtr.Zero; }

            //For LocalHotKeys, determine if modifiers Alt, Shift and Control is pressed.
            Microsoft.VisualBasic.Devices.Keyboard UserKeyBoard = new Microsoft.VisualBasic.Devices.Keyboard();
            bool AltPressed = UserKeyBoard.AltKeyDown;
            bool ControlPressed = UserKeyBoard.CtrlKeyDown;
            bool ShiftPressed = UserKeyBoard.ShiftKeyDown;

            ModifierKeys LocalModifier = ModifierKeys.None;
            if (AltPressed) { LocalModifier = ModifierKeys.Alt; }
            if (ControlPressed) { LocalModifier |= ModifierKeys.Control; }
            if (ShiftPressed) { LocalModifier |= ModifierKeys.Shift; }

            switch ((KeyboardMessages)msg)
            {
                case (KeyboardMessages.WmSyskeydown):
                case (KeyboardMessages.WmKeydown):
                    Key keydownCode = (Key)(int)wParam;

                    if (KeyPressEvent != null)
                        KeyPressEvent(this, new HotKeyEventArgs(keydownCode, LocalModifier, RaiseLocalEvent.OnKeyDown));

                    //Check if a chord has started.
                    if (InChordMode)
                    {
                        //Check if the Key down is a modifier, we'll have to wait for a real key.
                        switch (keydownCode)
                        {
                            case Key.Control:
                            case Key.ControlKey:
                            case Key.LControlKey:
                            case Key.RControlKey:
                            case Key.Shift:
                            case Key.ShiftKey:
                            case Key.LShiftKey:
                            case Key.RShiftKey:
                            case Key.Alt:
                            case Key.Menu:
                            case Key.LMenu:
                            case Key.RMenu:
                            case Key.LWin:
                                return IntPtr.Zero;
                        }

                        ChordHotKey ChordMain = ChordHotKeyContainer.Where(
                            item => (item.BaseKey == PreChordKey && item.BaseModifier == PreChordModifier
                                && item.ChordKey == keydownCode && item.ChordModifier == LocalModifier))
                                .FirstOrDefault();

                        //    ChordHotKey ChordMain = ChordHotKeyContainer.Find
                        //    (
                        //    delegate(ChordHotKey cm)
                        //    {
                        //        return ((cm.BaseKey == PreChordKey) && (cm.BaseModifier == PreChordModifier) && (cm.ChordKey == keydownCode) && (cm.ChordModifier == LocalModifier));
                        //    }
                        //);

                        if (ChordMain != null)
                        {
                            ChordMain.RaiseOnHotKeyPressed();

                            if (ChordPressed != null && ChordMain.Enabled == true)
                                ChordPressed(this, new ChordHotKeyEventArgs(ChordMain));

                            InChordMode = false;
                            new Microsoft.VisualBasic.Devices.Computer().Audio.PlaySystemSound(System.Media.SystemSounds.Exclamation);
                            return IntPtr.Zero;
                        }

                        InChordMode = false;
                        return IntPtr.Zero;
                    }

                    //Check for a LocalHotKey.
                    LocalHotKey KeyDownHotkey = (from items in LocalHotKeyContainer
                                                 where items.Key == keydownCode && items.Modifier == LocalModifier
                                                 where items.WhenToRaise == RaiseLocalEvent.OnKeyDown
                                                 select items).FirstOrDefault();

                    //LocalHotKey KeyDownHotkey = LocalHotKeyContainer.Find
                    //    (
                    //    delegate(LocalHotKey d)
                    //    {
                    //        return ((d.Key == keydownCode) && (d.Modifier == LocalModifier));
                    //    }
                    //);

                    if (KeyDownHotkey != null)
                    {
                        KeyDownHotkey.RaiseOnHotKeyPressed();
                        if (LocalHotKeyPressed != null && KeyDownHotkey.Enabled == true)
                            LocalHotKeyPressed(this, new LocalHotKeyEventArgs(KeyDownHotkey));

                        return IntPtr.Zero;
                    }

                    //Check for ChordHotKeys.
                    ChordHotKey ChordBase = System.Linq.Enumerable.Where(
                        ChordHotKeyContainer, item => item.BaseKey == keydownCode && item.BaseModifier == LocalModifier)
                        .FirstOrDefault();

                    //ChordHotKey ChordBase = ChordHotKeyContainer.Find
                    //    (
                    //    delegate(ChordHotKey c)
                    //    {
                    //        return ((c.BaseKey == keydownCode) && (c.BaseModifier == LocalModifier));
                    //    }
                    //);

                    if (ChordBase != null)
                    {
                        PreChordKey = ChordBase.BaseKey;
                        PreChordModifier = ChordBase.BaseModifier;

                        var e = new PreChordHotKeyEventArgs(new LocalHotKey(ChordBase.Name, ChordBase.BaseModifier, ChordBase.BaseKey));
                        if (ChordStarted != null)
                            ChordStarted(this, e);


                        InChordMode = !e.HandleChord;
                        return IntPtr.Zero;
                    }

                    InChordMode = false;
                    return IntPtr.Zero;

                case (KeyboardMessages.WmSyskeyup):
                case (KeyboardMessages.WmKeyup):
                    Key keyupCode = (Key)(int)wParam;

                    if (KeyPressEvent != null)
                        KeyPressEvent(this, new HotKeyEventArgs(keyupCode, LocalModifier, RaiseLocalEvent.OnKeyDown));

                    LocalHotKey KeyUpHotkey = (from items in LocalHotKeyContainer
                                               where items.Key == keyupCode && items.Modifier == LocalModifier
                                               where items.WhenToRaise == RaiseLocalEvent.OnKeyUp
                                               select items).FirstOrDefault();

                    //LocalHotKey KeyUpHotkey = LocalHotKeyContainer.Find
                    //    (
                    //    delegate(LocalHotKey u)
                    //    {
                    //        return ((u.Key == keyupCode) && (u.Modifier == LocalModifier));
                    //    }
                    //);

                    if (KeyUpHotkey != null)
                    {
                        KeyUpHotkey.RaiseOnHotKeyPressed();
                        if (LocalHotKeyPressed != null && KeyUpHotkey.Enabled == true)
                            LocalHotKeyPressed(this, new LocalHotKeyEventArgs(KeyUpHotkey));

                        return IntPtr.Zero;
                    }

                    return IntPtr.Zero;

                case KeyboardMessages.WmHotKey:

                    GlobalHotKey Pressed = GlobalHotKeyContainer.Where(item => item.Id == (int)wParam).FirstOrDefault();

                    //GlobalHotKey Pressed = GlobalHotKeyContainer.Find
                    //    (
                    //    delegate(GlobalHotKey g)
                    //    {
                    //        return (g.Id == (int)wParam);
                    //    }
                    //);

                    if (Pressed != null)
                    {
                        Pressed.RaiseOnHotKeyPressed();
                        if (GlobalHotKeyPressed != null)
                            GlobalHotKeyPressed(this, new GlobalHotKeyEventArgs(Pressed));
                    }

                    break;
            }

            return IntPtr.Zero;
        }
        #endregion

        #region **Events, Methods and Helpers
        private void RegisterGlobalHotKey(int id, GlobalHotKey hotKey)
        {
            if ((int)hwndSource.Handle != 0)
            {
                if (hotKey.Key == Key.LWin && (hotKey.Modifier & ModifierKeys.Windows) == ModifierKeys.None)
                    HelperMethods.RegisterHotKey(hwndSource.Handle, id, (int)(hotKey.Modifier | ModifierKeys.Windows), (int)hotKey.Key);
                else
                    HelperMethods.RegisterHotKey(hwndSource.Handle, id, (int)hotKey.Modifier, (int)(hotKey.Key));

                int error = Marshal.GetLastWin32Error();
                if (error != 0)
                {
                    if (!this.SuppressException)
                    {
                        Exception e = new Win32Exception(error);

                        if (error == 1409)
                            throw new HotKeyAlreadyRegisteredException(e.Message, hotKey, e);
                        else if (error != 2)
                            throw e;
                    }
                }
            }
            else
                if (!this.SuppressException)
            {
                throw new InvalidOperationException("Handle is invalid");
            }
        }

        private void UnregisterGlobalHotKey(int id)
        {
            if ((int)hwndSource.Handle != 0)
            {
                HelperMethods.UnregisterHotKey(hwndSource.Handle, id);
                int error = Marshal.GetLastWin32Error();
                if (error != 0 && error != 2)
                    if (!this.SuppressException)
                    {
                        if (id >= GlobalHotKeyContainer.Count)
                            id = GlobalHotKeyContainer.Count - 1;

                        GlobalHotKey ghCont = null;
                        if (id >= 0)
                            ghCont = GlobalHotKeyContainer[id];

                        if (ghCont == null)
                            ghCont = new GlobalHotKey("List empty", ModifierKeys.None, 0);

                        throw new HotKeyUnregistrationFailedException("The hotkey could not be unregistered", ghCont, new Win32Exception(error));
                    }
            }
        }

        private class SerialCounter
        {
            public SerialCounter(int start)
            {
                Current = start;
            }

            public int Current { get; private set; }

            public int Next()
            {
                return ++Current;
            }
        }
        /// <summary>Registers a GlobalHotKey if enabled.
        /// </summary>
        /// <param name="hotKey">The hotKey which will be added. Must not be null and can be registered only once.</param>
        /// <exception cref="HotKeyAlreadyRegisteredException">Thrown is a GlobalHotkey with the same name, and or key and modifier has already been added.</exception>
        /// <exception cref="System.ArgumentNullException">thrown if a the HotKey to be added is null, or the key is not specified.</exception>
        public bool AddGlobalHotKey(GlobalHotKey hotKey)
        {
            if (hotKey == null)
            {
                if (!this.SuppressException)
                    throw new ArgumentNullException("value");

                return false;
            }
            if (hotKey.Key == 0)
            {
                if (!this.SuppressException)
                    throw new ArgumentNullException("value.Key");

                return false;
            }
            if (GlobalHotKeyContainer.Contains(hotKey))
            {
                if (!this.SuppressException)
                    throw new HotKeyAlreadyRegisteredException("HotKey already registered!", hotKey);

                return false;
            }

            int id = idGen.Next();
            if (hotKey.Enabled)
                RegisterGlobalHotKey(id, hotKey);
            hotKey.Id = id;
            hotKey.PropertyChanged += GlobalHotKeyPropertyChanged;
            GlobalHotKeyContainer.Add(hotKey);
            ++GlobalHotKeyCount;
            return true;
        }
        /// <summary>Registers a LocalHotKey.
        /// </summary>
        /// <param name="hotKey">The hotKey which will be added. Must not be null and can be registered only once.</param>
        /// <exception cref="HotKeyAlreadyRegisteredException">thrown if a LocalHotkey with the same name and or key and modifier has already been added.</exception>
        public bool AddLocalHotKey(LocalHotKey hotKey)
        {
            if (hotKey == null)
            {
                if (!this.SuppressException)
                    throw new ArgumentNullException("value");

                return false;
            }
            if (hotKey.Key == 0)
            {
                if (!this.SuppressException)
                    throw new ArgumentNullException("value.Key");

                return false;
            }

            //Check if a chord already has its BaseKey and BaseModifier.
            bool ChordExits = ChordHotKeyContainer.Exists
            (
                delegate (ChordHotKey f)
                {
                    return (f.BaseKey == hotKey.Key && f.BaseModifier == hotKey.Modifier);
                }
            );

            if (LocalHotKeyContainer.Contains(hotKey) || ChordExits)
            {
                if (!this.SuppressException)
                    throw new HotKeyAlreadyRegisteredException("HotKey already registered!", hotKey);

                return false;
            }

            LocalHotKeyContainer.Add(hotKey);
            ++LocalHotKeyCount;
            return true;
        }
        /// <summary>Registers a ChordHotKey.
        /// </summary>
        /// <param name="hotKey">The hotKey which will be added. Must not be null and can be registered only once.</param>
        /// <returns>True if registered successfully, false otherwise.</returns>
        /// <exception cref="HotKeyAlreadyRegisteredException">thrown if a LocalHotkey with the same name and or key and modifier has already been added.</exception>
        public bool AddChordHotKey(ChordHotKey hotKey)
        {
            if (hotKey == null)
            {
                if (!this.SuppressException)
                    throw new ArgumentNullException("value");

                return false;
            }
            if (hotKey.BaseKey == 0 || hotKey.ChordKey == 0)
            {
                if (!this.SuppressException)
                    throw new ArgumentNullException("value.Key");

                return false;
            }

            //Check if a LocalHotKey already has its Key and Modifier.
            bool LocalExits = LocalHotKeyContainer.Exists
            (
                delegate (LocalHotKey f)
                {
                    return (f.Key == hotKey.BaseKey && f.Modifier == hotKey.BaseModifier);
                }
            );

            if (ChordHotKeyContainer.Contains(hotKey) || LocalExits)
            {
                if (!this.SuppressException)
                    throw new HotKeyAlreadyRegisteredException("HotKey already registered!", hotKey);

                return false;
            }

            ChordHotKeyContainer.Add(hotKey);
            ++ChordHotKeyCount;
            return true;
        }
        /// <summary>Unregisters a GlobalHotKey.
        /// </summary>
        /// <param name="hotKey">The hotKey to be removed</param>
        /// <returns>True if success, otherwise false</returns>
        public bool RemoveGlobalHotKey(GlobalHotKey hotKey)
        {
            if (GlobalHotKeyContainer.Remove(hotKey) == true)
            {
                --GlobalHotKeyCount;

                if (hotKey.Enabled)
                    UnregisterGlobalHotKey(hotKey.Id);

                hotKey.PropertyChanged -= GlobalHotKeyPropertyChanged;
                return true;
            }
            else { return false; }

        }
        /// <summary>Unregisters a LocalHotKey.
        /// </summary>
        /// <param name="hotKey">The hotKey to be removed</param>
        /// <returns>True if success, otherwise false</returns>
        public bool RemoveLocalHotKey(LocalHotKey hotKey)
        {
            if (LocalHotKeyContainer.Remove(hotKey) == true)
            { --LocalHotKeyCount; return true; }
            else { return false; }
        }
        /// <summary>Unregisters a ChordHotKey.
        /// </summary>
        /// <param name="hotKey">The hotKey to be removed</param>
        /// <returns>True if success, otherwise false</returns>
        public bool RemoveChordHotKey(ChordHotKey hotKey)
        {
            if (ChordHotKeyContainer.Remove(hotKey) == true)
            { --ChordHotKeyCount; return true; }
            else { return false; }
        }
        /// <summary>Removes the hotkey(Local, Chord or Global) with the specified name.
        /// </summary>
        /// <param name="name">The name of the hotkey.</param>
        /// <returns>True if successful and false otherwise.</returns>
        public bool RemoveHotKey(string name)
        {
            LocalHotKey local = System.Linq.Enumerable.Where
                (LocalHotKeyContainer, item => item.Name == name).FirstOrDefault();

            //LocalHotKey local = LocalHotKeyContainer.Find
            //    (
            //    delegate(LocalHotKey l)
            //    {
            //        return (l.Name == name);
            //    }
            //);

            if (local != null) { return RemoveLocalHotKey(local); }

            ChordHotKey chord = ChordHotKeyContainer.Where(item => item.Name == name).FirstOrDefault();

            //ChordHotKey chord = ChordHotKeyContainer.Find
            //    (
            //    delegate(ChordHotKey c)
            //    {
            //        return (c.Name == name);
            //    }
            //);

            if (chord != null) { return RemoveChordHotKey(chord); }

            GlobalHotKey global = GlobalHotKeyContainer.Where(item => item.Name == name).FirstOrDefault();

            //GlobalHotKey global = GlobalHotKeyContainer.Find
            //    (
            //    delegate(GlobalHotKey g)
            //    {
            //        return (g.Name == name);
            //    }
            //);

            if (global != null) { return RemoveGlobalHotKey(global); }

            return false;
        }

        /// <summary>Checks if a HotKey has been registered.
        /// </summary>
        /// <param name="name">The name of the HotKey.</param>
        /// <returns>True if the HotKey has been registered, false otherwise.</returns>
        public bool HotKeyExists(string name)
        {
            LocalHotKey local = LocalHotKeyContainer.Where(item => item.Name == name).FirstOrDefault();

            //LocalHotKey local = LocalHotKeyContainer.Find
            //    (
            //    delegate(LocalHotKey l)
            //    {
            //        return (l.Name == name);
            //    }
            //);

            if (local != null) { return true; }

            ChordHotKey chord = ChordHotKeyContainer.Where(item => item.Name == name).FirstOrDefault();

            //ChordHotKey chord = ChordHotKeyContainer.Find
            //    (
            //    delegate(ChordHotKey c)
            //    {
            //        return (c.Name == name);
            //    }
            //);

            if (chord != null) { return true; }

            GlobalHotKey global = GlobalHotKeyContainer.Where(item => item.Name == name).FirstOrDefault();

            //GlobalHotKey global = GlobalHotKeyContainer.Find
            //    (
            //    delegate(GlobalHotKey g)
            //    {
            //        return (g.Name == name);
            //    }
            //);

            if (global != null) { return true; }

            return false;
        }
        /// <summary>Checks if a ChordHotKey has been registered.
        /// </summary>
        /// <param name="chordhotkey">The ChordHotKey to check.</param>
        /// <returns>True if the ChordHotKey has been registered, false otherwise.</returns>
        public bool HotKeyExists(ChordHotKey chordhotkey)
        {
            return (((from item in ChordHotKeyContainer
                      where item == chordhotkey
                      select item).FirstOrDefault()) != null);

            //return ChordHotKeyContainer.Exists
            //    (
            //    delegate(ChordHotKey c)
            //    {
            //        return (c == chordhotkey);
            //    }
            //);
        }
        /// <summary>Checks if a hotkey has already been registered as a Local or Global HotKey.
        /// </summary>
        /// <param name="shortcut">The hotkey string to check.</param>
        /// <param name="ToCheck">The HotKey type to check.</param>
        /// <returns>True if the HotKey is already registered, false otherwise.</returns>
        public bool HotKeyExists(string shortcut, CheckKey ToCheck)
        {
            Key Key = (Key)HotKeyShared.ParseShortcut(shortcut).GetValue(1);
            ModifierKeys Modifier = (ModifierKeys)HotKeyShared.ParseShortcut(shortcut).GetValue(0);
            switch (ToCheck)
            {
                case CheckKey.GlobalHotKey:
                    return (((from item in GlobalHotKeyContainer
                              where item.Key == Key && item.Modifier == Modifier
                              select item).FirstOrDefault()) != null);

                //return GlobalHotKeyContainer.Exists
                //    (
                //    delegate(GlobalHotKey g)
                //    {
                //        return (g.Key == Key && g.Modifier == Modifier);
                //    }
                //);

                case CheckKey.LocalHotKey:
                    return (((from item in LocalHotKeyContainer //-V3093
                              where item.Key == Key && item.Modifier == Modifier
                              select item).FirstOrDefault()) != null)
                              |
                              (((from items in ChordHotKeyContainer
                                 where items.BaseKey == Key && items.BaseModifier == Modifier
                                 select items).FirstOrDefault()) != null);

                //return (LocalHotKeyContainer.Exists
                //           (
                //           delegate(LocalHotKey l)
                //           {
                //               return (l.Key == Key && l.Modifier == Modifier);
                //           }
                //       )
                //       | //Or.
                //       ChordHotKeyContainer.Exists
                //       (
                //       delegate(ChordHotKey c)
                //       {
                //           return (c.BaseKey == Key && c.BaseModifier == Modifier);
                //       }));

                case CheckKey.Both:
                    return (HotKeyExists(shortcut, CheckKey.GlobalHotKey) ^ HotKeyExists(shortcut, CheckKey.LocalHotKey));
            }
            return false;
        }
        /// <summary>Checks if a hotkey has already been registered as a Local or Global HotKey.
        /// </summary>
        /// <param name="key">The key of the HotKey.</param>
        /// <param name="modifier">The modifier of the HotKey.</param>
        /// <param name="ToCheck">The HotKey type to check.</param>
        /// <returns>True if the HotKey is already registered, false otherwise.</returns>
        public bool HotKeyExists(Key key, ModifierKeys modifier, CheckKey ToCheck)
        {
            return (HotKeyExists(HotKeyShared.CombineShortcut(modifier, key), ToCheck));
        }
        #endregion

        #region Destructor
        private bool disposed;

        private void Dispose(bool disposing)
        {
            if (disposed)
                return;

            try
            {

                try
                {
                    if (disposing)
                    {
                        this.SuppressException = true;
                        hwndSource.RemoveHook(hook);
                    }
                }
                catch { }

                try
                {
                    for (int i = GlobalHotKeyContainer.Count - 1; i >= 0; i--)
                    {
                        try
                        {
                            RemoveGlobalHotKey(GlobalHotKeyContainer[i]);
                        }
                        catch { }
                    }
                }
                catch { }

                try
                {
                    KeyBoardUnHook();
                    LocalHotKeyContainer.Clear();
                    ChordHotKeyContainer.Clear();
                }
                catch { }

            }
            catch { }
            finally
            {
                disposed = true;
            }
        }
        /// <summary>Release all resources used by this class.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~HotKeyManager()
        {
            this.Dispose(false);
        }
        #endregion
    }
    #endregion
}
