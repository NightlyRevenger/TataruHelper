// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Runtime.Serialization;

namespace BondTech.HotKeyManagement.WPF._4
{
    //The Class for GlobalHotkeys. Keys registered globally in Windows.
    #region **GlobalHotKey Class
    /// <summary>Initializes a new instance of this class.
    /// </summary>
    [Serializable]
    public class GlobalHotKey : INotifyPropertyChanged, ISerializable, IEquatable<GlobalHotKey>
    {
        #region **Properties
        private string name; //This will contain a unique name for the GlobalHotKey.
        private Key key; //This will contain the Key to be registered.
        private ModifierKeys modifier; //This will contain the Modifier of the specified key.
        private bool enabled; //This will decide if the GlobalHotkey Event should be raised or not.
        private object tag;
        /// <summary> The id this hotkey is registered with, if it has been registered.
        /// </summary>
        public int Id { get; internal set; }
        /// <summary>A unique name for the GlobalHotKey.
        /// </summary>
        public string Name
        {
            get { return name; }
            private set
            {
                if (name != value)
                    if (HotKeyShared.IsValidHotkeyName(value))
                    {
                        name = value;
                    }
                    else
                    {
                        throw new HotKeyInvalidNameException("the HotKeyname '" + value + "' is invalid");
                    }
            }
        }
        /// <summary>The Key.
        /// </summary>
        public Key Key
        {
            get { return key; }
            set
            {
                if (key != value)
                {
                    key = value;
                    OnPropertyChanged("Key");
                }
            }
        }
        ///<summary> The modifier. Multiple modifiers can be combined with or.
        /// </summary>
        public ModifierKeys Modifier
        {
            get { return modifier; }
            set
            {
                if (modifier != value)
                {
                    modifier = value;
                    OnPropertyChanged("Modifier");
                }
            }
        }
        /// <summary>Determines if the Hotkey is active.
        /// </summary>
        public bool Enabled
        {
            get { return enabled; }
            set
            {
                if (value != enabled)
                {
                    enabled = value;
                    OnPropertyChanged("Enabled");
                }
            }
        }
        /// <summary>Gets or Sets the object that contains data about the control.
        /// </summary>
        public object Tag
        {
            get { return tag; }
            set { tag = value; }
        }
        #endregion

        #region **Event Handlers
        /// <summary>Raised when a property of this Hotkey is changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        /// <summary>Will be raised if this hotkey is pressed (works only if registered in the HotKeyManager.)
        /// </summary>
        public event GlobalHotKeyEventHandler HotKeyPressed = delegate { };
        #endregion

        #region **Constructor
        /// <summary>Creates a GlobalHotKey object. This instance has to be registered in a HotKeyManager.
        /// </summary>
        /// <param name="Name">The unique identifier for this GlobalHotKey.</param>
        /// <param name="key">The key to be registered.</param>
        /// <param name="modifier">The modifier for this key. Multiple modifiers can be combined with or.</param>
        public GlobalHotKey(string name, ModifierKeys modifier, Key key)
            : this(name, modifier, key, true) { }
        /// <summary>Creates a GlobalHotKey object. This instance has to be registered in a HotKeyManager.
        /// </summary>
        /// <param name="Name">The unique identifier for this GlobalHotKey</param>
        /// <param name="key">The key to be registered.</param>
        /// <param name="modifier">The modifier for this key. Multiple modifiers can be combined with or.</param>
        public GlobalHotKey(string name, ModifierKeys modifier, int key)
            : this(name, modifier, key, true) { }
        /// <summary>Creates a GlobalHotKey object. This instance has to be registered in a HotKeyManager.
        /// </summary>
        /// <param name="name">The unique identifier for this GlobalHotKey.</param>
        /// <param name="key">The key to be registered.</param>
        /// <param name="modifier">The modifier for this key. Multiple modifiers can be combined with or.</param>
        /// <param name="enabled">Specifies if event for this GlobalHotKey should be raised.</param>
        public GlobalHotKey(string name, ModifierKeys modifier, Key key, bool enabled)
        {

            this.Name = name;
            this.Key = key;
            this.Modifier = modifier;
            this.Enabled = enabled;
        }
        /// <summary>Creates a GlobalHotKey object. This instance has to be registered in a HotKeyManager.
        /// </summary>
        /// <param name="name">The unique identifier for this GlobalHotKey.</param>
        /// <param name="Key">The key to be registered.</param>
        /// <param name="modifier">The modifier for this key. Multiple modifiers can be combined with or.</param>
        /// <param name="enabled">Specifies if event for this GlobalHotKey should be raised.</param>
        public GlobalHotKey(string name, ModifierKeys modifier, int key, bool enabled)
        {
            this.Name = name;
            this.Key = (Key)Enum.Parse(typeof(Key), key.ToString());
            this.Modifier = modifier;
            this.Enabled = enabled;
        }

        protected GlobalHotKey(SerializationInfo info, StreamingContext context)
        {
            Name = info.GetString("Name");
            Key = (Key)info.GetValue("Key", typeof(Key));
            Modifier = (ModifierKeys)info.GetValue("Modifier", typeof(ModifierKeys));
            Enabled = info.GetBoolean("Enabled");
        }
        #endregion

        #region **Events, Methods and Helpers
        /// <summary>Compares a GlobalHotKey to another.
        /// </summary>
        /// <param name="other">The GlobalHotKey to compare.</param>
        /// <returns>True if the HotKey is equal and false if otherwise.</returns>
        public bool Equals(GlobalHotKey other)
        {
            //We'll be comparing the Key, Modifier and the Name.
            if (Key == other.Key && Modifier == other.Modifier)
                return true;
            if (Name == other.Name)
                return true;

            return false;
        }
        //Override .Equals(object)
        public override bool Equals(object obj)
        {
            GlobalHotKey hotKey = obj as GlobalHotKey;
            if (hotKey != null)
                return Equals(hotKey);
            else
                return false;
        }
        //Override .GetHashCode of this object.
        public override int GetHashCode()
        {
            return (int)Modifier ^ (int)Key;
        }
        //To determine if a property of the GlobalHotkey has changed.
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        //Override the .ToString()        
        public override string ToString()
        {
            return Name;
        }
        /// <summary>Information about this Hotkey.
        /// </summary>
        /// <returns>The information about this, delimited by ';'</returns>
        public string FullInfo()
        {
            return string.Format("{0} ; {1} ; {2}Enabled ; GlobalHotKey", Name, HotKeyShared.CombineShortcut(Modifier, Key), Enabled ? "" : "Not ");
        }
        //Can use (string)GlobalHotKey.
        /// <summary>Converts the GlobalHotKey to a string.
        /// </summary>
        /// <param name="toConvert">The Hotkey to convert.</param>
        /// <returns>The string Name of the GlobalHotKey.</returns>
        public static explicit operator string(GlobalHotKey toConvert)
        {
            return toConvert.Name;
        }
        /// <summary>Converts the GlobalHotKey to a LocalHotKey
        /// </summary>
        /// <param name="toConvert">The GlobalHotKey to convert.</param>
        /// <returns>a LocalHotKey of the GlobalHotKey.</returns>
        public static explicit operator LocalHotKey(GlobalHotKey toConvert)
        {
            return new LocalHotKey(toConvert.Name, toConvert.Modifier, toConvert.Key, RaiseLocalEvent.OnKeyDown, toConvert.Enabled);
        }
        /// <summary>The Event raised the hotkey is pressed.
        /// </summary>
        protected virtual void OnHotKeyPress()
        {
            if (HotKeyPressed != null && Enabled)
                HotKeyPressed(this, new GlobalHotKeyEventArgs(this));
        }
        /// <summary>Raises the GlobalHotKey Pressed event.
        /// </summary>
        internal void RaiseOnHotKeyPressed()
        {
            OnHotKeyPress();
        }
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) //-V3099
        {
            info.AddValue("Name", Name);
            info.AddValue("Key", Key, typeof(System.Windows.Input.Key));
            info.AddValue("Modifiers", Modifier, typeof(ModifierKeys));
            info.AddValue("Enabled", Enabled);
        }
        #endregion
    }
    #endregion

    //The class for hotkeys registered within the application.
    #region **LocalHotKey Class
    /// <summary>Initializes a new instance of this class.
    /// </summary>
    [Serializable]
    public class LocalHotKey : ISerializable, IEquatable<LocalHotKey>, IEquatable<ChordHotKey>
    {
        #region **Properties
        private string name;
        private Key key;
        private RaiseLocalEvent whenToraise;
        private bool enabled;
        private ModifierKeys modifier;
        private object tag;

        /// <summary>The Unique id for this HotKey.
        /// </summary>
        public string Name
        {
            get { return name; }
            private set
            {
                if (name != value)
                    if (HotKeyShared.IsValidHotkeyName(value))
                        name = value;
                    else
                        throw new HotKeyInvalidNameException("the HotKeyname '" + value + "' is invalid");
            }
        }
        /// <summary>Gets or sets the key to register.
        /// </summary>
        public Key Key
        {
            get { return key; }
            set
            {
                if (key != value)
                    key = value;
            }
        }
        /// <summary>Determines if the HotKey is active.
        /// </summary>
        public bool Enabled
        {
            get { return enabled; }
            set
            {
                if (enabled != value)
                    enabled = value;
            }
        }
        /// <summary>Gets or sets the modifiers for this hotKey, multiple modifiers can be combined with "Xor"
        /// </summary>
        public ModifierKeys Modifier
        {
            get { return modifier; }
            set
            {
                if (modifier != value)
                    modifier = value;
            }
        }
        /// <summary>Specifies when the event for this key should be raised.
        /// </summary>
        public RaiseLocalEvent WhenToRaise
        {
            get { return whenToraise; }
            set
            {
                if (whenToraise != value)
                    whenToraise = value;
            }
        }
        /// <summary>Gets or Sets the object that contains data about the Hotkey.
        /// </summary>
        public object Tag
        {
            get { return tag; }
            set { tag = value; }
        }
        #endregion

        #region **Event Handlers
        /// <summary>Will be raised if this hotkey is pressed (works only if registered in the HotKeyManager.)
        /// </summary>
        public event LocalHotKeyEventHandler HotKeyPressed = delegate { };
        #endregion

        #region **Constructors
        /// <summary>Creates a LocalHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this LocalHotKey.</param>
        /// <param name="key">The key to be registered.</param>
        public LocalHotKey(string name, Key key) :
            this(name, ModifierKeys.None, key, RaiseLocalEvent.OnKeyDown, true)
        { }
        /// <summary>Creates a LocalHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this LocalHotKey.</param>
        /// <param name="key">The key to be registered.</param>
        public LocalHotKey(string name, int key) :
            this(name, ModifierKeys.None, key, RaiseLocalEvent.OnKeyDown, true)
        { }
        /// <summary>Creates a LocalHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this LocalHotKey.</param>
        /// <param name="key">The key to be registered.</param>
        /// <param name="modifiers">The modifier for this key, multiple modifiers can be combined with Xor</param>
        public LocalHotKey(string name, ModifierKeys modifiers, Key key) :
            this(name, modifiers, key, RaiseLocalEvent.OnKeyDown, true)
        { }
        /// <summary>Creates a LocalHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this LocalHotKey.</param>
        /// <param name="key">The key to be registered.</param>
        /// <param name="modifiers">The modifier for this key, multiple modifiers can be combined with Xor</param>
        public LocalHotKey(string name, ModifierKeys modifiers, int key) :
            this(name, modifiers, key, RaiseLocalEvent.OnKeyDown, true)
        { }
        /// <summary>Creates a LocalHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this LocalHotKey.</param>
        /// <param name="key">The key to be registered.</param>
        /// <param name="whentoraise">Specifies when the event should be raised.</param>
        public LocalHotKey(string name, Key key, RaiseLocalEvent whentoraise) :
            this(name, ModifierKeys.None, key, whentoraise, true)
        { }
        /// <summary>Creates a LocalHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this LocalHotKey.</param>
        /// <param name="key">The key to be registered.</param>
        /// <param name="whentoraise">Specifies when the event should be raised.</param>
        public LocalHotKey(string name, int key, RaiseLocalEvent whentoraise) :
            this(name, ModifierKeys.None, key, whentoraise, true)
        { }
        /// <summary>Creates a LocalHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this LocalHotKey.</param>
        /// <param name="key">The key to register.</param>
        /// <param name="modifiers">The modifier for this key, multiple modifiers can be combined with Xor</param>
        /// <param name="whentooraise">Specifies when the event should be raised.</param>
        public LocalHotKey(string name, ModifierKeys modifiers, Key key, RaiseLocalEvent whentoraise) :
            this(name, modifiers, key, whentoraise, true)
        { }
        /// <summary>Creates a LocalHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this LocalHotKey.</param>
        /// <param name="key">The key to register.</param>
        /// <param name="modifiers">The modifier for this key, multiple modifiers can be combined with Xor</param>
        /// <param name="whentooraise">Specifies when the event should be raised.</param>
        public LocalHotKey(string name, ModifierKeys modifiers, int key, RaiseLocalEvent whentoraise) :
            this(name, modifiers, key, whentoraise, true)
        { }
        /// <summary>Creates a LocalHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this LocalHotKey</param>
        /// <param name="key">The key to register.</param>
        /// <param name="whentoraise">Specifies when the event should be raised.</param>
        /// <param name="enabled">Specifies if event for this GlobalHotKey should be raised.</param>
        public LocalHotKey(string name, Key key, RaiseLocalEvent whentoraise, bool enabled) :
            this(name, ModifierKeys.None, key, whentoraise, enabled)
        { }
        /// <summary>Creates a LocalHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this LocalHotKey</param>
        /// <param name="key">The key to register.</param>
        /// <param name="whentoraise">Specifies when the event should be raised.</param>
        /// <param name="enabled">Specifies if event for this GlobalHotKey should be raised.</param>
        public LocalHotKey(string name, int key, RaiseLocalEvent whentoraise, bool enabled) :
            this(name, ModifierKeys.None, key, whentoraise, enabled)
        { }
        /// <summary>Creates a LocalHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this LocalHotKey</param>
        /// <param name="key">The key to register.</param>
        /// <param name="modifiers">The modifier for this key, multiple modifiers can be combined with Xor</param>
        /// <param name="whentoraise">Specifies when the event should be raised.</param>
        /// <param name="enabled">Specifies if event for this GlobalHotKey should be raised.</param>
        public LocalHotKey(string name, ModifierKeys modifiers, Key key, RaiseLocalEvent whentoraise, bool enabled)
        {
            //if (modifiers == Win.Modifiers.Win) { throw new InvalidOperationException("Window Key cannot be used as modifier for Local HotKeys"); }
            this.Name = name;
            this.Key = key;
            this.WhenToRaise = whentoraise;
            this.Enabled = enabled;
            this.Modifier = modifiers;
        }
        /// <summary>Creates a LocalHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this LocalHotKey</param>
        /// <param name="key">The key to register.</param>
        /// <param name="modifiers">The modifier for this key, multiple modifiers can be combined with Xor</param>
        /// <param name="whentoraise">Specifies when the event should be raised.</param>
        /// <param name="enabled">Specifies if event for this GlobalHotKey should be raised.</param>
        public LocalHotKey(string name, ModifierKeys modifiers, int key, RaiseLocalEvent whentoraise, bool enabled)
        {
            //if (modifiers == Win.Modifiers.Win) { throw new InvalidOperationException("Window Key cannot be used as modifier for Local HotKeys"); }
            this.Name = name;
            this.Key = (Key)Enum.Parse(typeof(Key), key.ToString());
            this.WhenToRaise = whentoraise;
            this.Enabled = enabled;
            this.Modifier = modifiers;
        }

        protected LocalHotKey(SerializationInfo info, StreamingContext context)
        {
            Name = info.GetString("Name");
            Key = (Key)info.GetValue("Key", typeof(Key));
            WhenToRaise = (RaiseLocalEvent)info.GetValue("WTR", typeof(RaiseLocalEvent));
            Modifier = (ModifierKeys)info.GetValue("Modifiers", typeof(ModifierKeys));
            Enabled = info.GetBoolean("Enabled");
            //SuppressKeyPress = info.GetBoolean("SuppressKeyPress");
        }
        #endregion

        #region **Events, Methods and Helpers
        /// <summary>Compares a LocalHotKey to another.
        /// </summary>
        /// <param name="other">The LocalHotKey to compare.</param>
        /// <returns>True if the HotKey is equal and false if otherwise.</returns>
        public bool Equals(LocalHotKey other)
        {
            //We'll be comparing the Key, Modifier and the Name.
            if (Key == other.Key && Modifier == other.Modifier)
                return true;
            if (Name.ToLower() == other.Name.ToLower())
                return true;

            return false;
        }
        /// <summary>Compares a LocalHotKey to a ChordHotKey.
        /// </summary>
        /// <param name="other">The ChordHotKey to compare.</param>
        /// <returns>True if equal, false otherwise.</returns>
        public bool Equals(ChordHotKey other)
        {
            return (Key == other.BaseKey && Modifier == other.BaseModifier);
        }
        //Override .Equals(object)
        public override bool Equals(object obj)
        {
            LocalHotKey hotKey = obj as LocalHotKey;
            if (hotKey != null)
                return Equals(hotKey);

            ChordHotKey chotKey = obj as ChordHotKey;
            if (chotKey != null)
                return Equals(chotKey);

            return false;
        }
        //Override .GetHashCode of this object.
        public override int GetHashCode()
        {
            return (int)whenToraise ^ (int)key;
        }
        //Override the .ToString()        
        public override string ToString()
        {
            return FullInfo();
        }
        /// <summary>Information about this Hotkey.
        /// </summary>
        /// <returns>The properties of the hotkey.</returns>
        public string FullInfo()
        {
            return string.Format("{0} ; {1} ; {2} ; {3}Enabled ; LocalHotKey", Name, HotKeyShared.CombineShortcut(Modifier, Key), WhenToRaise, Enabled ? "" : "Not ");
        }
        //Can use (string)LocalHotKey.
        /// <summary>Converts the LocalHotKey to a string.
        /// </summary>
        /// <param name="toConvert">The Hotkey to convert.</param>
        /// <returns>The string Name of the LocalHotKey.</returns>
        public static explicit operator string(LocalHotKey toConvert)
        {
            return toConvert.Name;
        }
        /// <summary>Converts a LocalHotKey to a GlobalHotKey.
        /// </summary>
        /// <param name="toConvert">The LocalHotKey to convert.</param>
        /// <returns>an instance of the GlobalHotKey.</returns>
        public static explicit operator GlobalHotKey(LocalHotKey toConvert)
        {
            return new GlobalHotKey(toConvert.Name, toConvert.Modifier, toConvert.Key, toConvert.Enabled);
        }
        /// <summary>The Event raised the hotkey is pressed.
        /// </summary>
        protected virtual void OnHotKeyPress()
        {
            if (HotKeyPressed != null && Enabled)
                HotKeyPressed(this, new LocalHotKeyEventArgs(this));
        }
        /// <summary>Raises the HotKeyPressed event.
        /// </summary>
        internal void RaiseOnHotKeyPressed()
        {
            OnHotKeyPress();
        }
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Name", Name);
            info.AddValue("Key", Key, typeof(Key));
            info.AddValue("Modifier", Modifier, typeof(ModifierKeys));
            info.AddValue("WTR", WhenToRaise, typeof(RaiseLocalEvent));
            info.AddValue("Enabled", Enabled);
            //info.AddValue("SuppressKeyPress", SuppressKeyPress);
        }
        #endregion
    }
    #endregion

    //The class for advanced hotkeys registered within the application.
    #region **Hotkeys of Chord.
    /// <summary>Initializes a new instance of this class.
    /// Register multiple shortcuts like Control + \, Control + N.
    /// </summary>
    [Serializable]
    public class ChordHotKey : ISerializable, IEquatable<ChordHotKey>, IEquatable<LocalHotKey>
    {
        #region **Properties.
        private string name;
        private Key basekey;
        private Key chordkey;
        private ModifierKeys basemodifier;
        private ModifierKeys chordmodifier;
        private bool enabled;
        private object tag;

        /// <summary>The unique id for this key
        /// </summary>
        public string Name
        {
            get { return name; }
            private set
            {
                if (name != value)
                    if (HotKeyShared.IsValidHotkeyName(value))
                        name = value;
                    else
                        throw new HotKeyInvalidNameException("the HotKeyname '" + value + "' is invalid");
            }
        }
        /// <summary>Gets or sets the key to start the chord.
        /// </summary>
        public Key BaseKey
        {
            get { return basekey; }
            set
            {
                if (basekey != value)
                    basekey = value;
            }
        }
        /// <summary>Gets or sets the key of chord. 
        /// </summary>
        public Key ChordKey
        {
            get { return chordkey; }
            set
            {
                if (chordkey != value)
                    chordkey = value;
            }
        }
        /// <summary>Gets or sets the modifier associated with the base key.
        /// </summary>
        public ModifierKeys BaseModifier
        {
            get { return basemodifier; }
            set
            {
                if (value != ModifierKeys.None)
                    basemodifier = value;
                else
                    throw new ArgumentException("Cannot set BaseModifier to None.", "value");
            }
        }
        /// <summary>Gets or sets the modifier associated with the chord key.
        /// </summary>
        public ModifierKeys ChordModifier
        {
            get { return chordmodifier; }
            set
            {
                if (chordmodifier != value)
                    chordmodifier = value;
            }
        }
        /// <summary>Determines if this Hotkey is active.
        /// </summary>
        public bool Enabled
        {
            get { return enabled; }
            set
            {
                if (enabled != value)
                    enabled = value;
            }
        }
        /// <summary>Gets or sets the object that contains data associated with this HotKey.
        /// </summary>
        public Object Tag
        {
            get { return tag; }
            set
            {
                if (tag != value)
                    tag = value;
            }
        }
        #endregion

        #region **Event Handlers.
        /// <summary>Will be raised if this hotkey is pressed.
        /// The event is raised if the basic key and basic modifier and the chord key and modifier is pressed.
        /// Works only if registered in the HotKeyManager.
        /// </summary>
        public event ChordHotKeyEventHandler HotKeyPressed = delegate { };
        #endregion

        #region **Constructors
        /// <summary>Creates a ChordHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this ChordHotKey.</param>
        /// <param name="basekey">The key to start the chord.</param>
        /// <param name="basemodifier">The modifier associated with the base key.</param>
        /// <param name="chordkey">The key of chord.</param>
        /// <param name="chordmodifier">The modifier associated with the Key of chord</param>
        /// <param name="enabled">Specifies if this hotkey is active</param>
        public ChordHotKey(string name, ModifierKeys basemodifier, Key basekey, ModifierKeys chordmodifier, Key chordkey, bool enabled)
        {
            Name = name;
            BaseKey = basekey;
            BaseModifier = basemodifier;
            ChordKey = chordkey;
            ChordModifier = chordmodifier;
            Enabled = enabled;
        }

        /// <summary>Creates a ChordHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this ChordHotKey.</param>
        /// <param name="basekey">The key to start the chord.</param>
        /// <param name="basemodifier">The modifier associated with the base key.</param>
        /// <param name="chordkey">The key of chord.</param>
        /// <param name="chordmodifier">The modifier associated with the Key of chord</param>
        /// <param name="enabled">Specifies if this hotkey is active</param>
        public ChordHotKey(string name, ModifierKeys basemodifier, int basekey, ModifierKeys chordmodifier, int chordkey, bool enabled)
        {
            Name = name;
            BaseKey = (Key)Enum.Parse(typeof(Key), basekey.ToString());
            BaseModifier = basemodifier;
            ChordKey = (Key)Enum.Parse(typeof(Key), chordkey.ToString());
            ChordModifier = chordmodifier;
            Enabled = enabled;
        }

        /// <summary>Creates a ChordHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this ChordHotKey.</param>
        /// <param name="basekey">The key to start the chord.</param>
        /// <param name="basemodifier">The modifier associated with the base key.</param>
        /// <param name="chordkey">The key of chord.</param>
        /// <param name="chordmodifier">The modifier associated with the Key of chord</param>
        public ChordHotKey(string name, ModifierKeys basemodifier, Key basekey, ModifierKeys chordmodifier, Key chordkey) :
            this(name, basemodifier, basekey, chordmodifier, chordkey, true)
        { }

        /// <summary>Creates a ChordHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this ChordHotKey.</param>
        /// <param name="basekey">The key to start the chord.</param>
        /// <param name="basemodifier">The modifier associated with the base key.</param>
        /// <param name="chordkey">The key of chord.</param>
        /// <param name="chordmodifier">The modifier associated with the Key of chord</param>
        public ChordHotKey(string name, ModifierKeys basemodifier, int basekey, ModifierKeys chordmodifier, int chordkey) :
            this(name, basemodifier, basekey, chordmodifier, chordkey, true)
        { }

        /// <summary>Creates a ChordHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this ChordHotKey.</param>
        /// <param name="basekey">The key to start the chord.</param>
        /// <param name="basemodifier">The modifier associated with the base key.</param>
        /// <param name="chordkey">The key of chord.</param>
        /// <param name="chordmodifier">The modifier associated with the Key of chord.</param>
        /// <param name="enabled">Specifies if this hotkey is active.</param>
        public ChordHotKey(string name, ModifierKeys basemodifier, int basekey, ModifierKeys chordmodifier, Key chordkey, bool enabled)
        {
            Name = name;
            BaseKey = (Key)Enum.Parse(typeof(Key), basekey.ToString());
            BaseModifier = basemodifier;
            ChordKey = chordkey;
            ChordModifier = chordmodifier;
            Enabled = enabled;
        }

        /// <summary>Creates a ChordHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this ChordHotKey.</param>
        /// <param name="basekey">The key to start the chord.</param>
        /// <param name="basemodifier">The modifier associated with the base key.</param>
        /// <param name="chordkey">The key of chord.</param>
        /// <param name="chordmodifier">The modifier associated with the Key of chord</param>
        public ChordHotKey(string name, ModifierKeys basemodifier, int basekey, ModifierKeys chordmodifier, Key chordkey) :
            this(name, basemodifier, basekey, chordmodifier, chordkey, true)
        { }

        /// <summary>Creates a ChordHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this ChordHotKey.</param>
        /// <param name="basekey">The key to start the chord.</param>
        /// <param name="basemodifier">The modifier associated with the base key.</param>
        /// <param name="chordkey">The key of chord.</param>
        /// <param name="chordmodifier">The modifier associated with the Key of chord.</param>
        /// <param name="enabled">Specifies if this hotkey is active.</param>
        public ChordHotKey(string name, ModifierKeys basemodifier, Key basekey, ModifierKeys chordmodifier, int chordkey, bool enabled)
        {
            Name = name;
            BaseKey = basekey;
            BaseModifier = basemodifier;
            ChordKey = (Key)Enum.Parse(typeof(Key), chordkey.ToString());
            ChordModifier = chordmodifier;
            Enabled = enabled;
        }

        /// <summary>Creates a ChordHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this ChordHotKey.</param>
        /// <param name="basekey">The key to start the chord.</param>
        /// <param name="basemodifier">The modifier associated with the base key.</param>
        /// <param name="chordkey">The key of chord.</param>
        /// <param name="chordmodifier">The modifier associated with the Key of chord</param>
        public ChordHotKey(string name, ModifierKeys basemodifier, Key basekey, ModifierKeys chordmodifier, int chordkey) :
            this(name, basemodifier, basekey, chordmodifier, chordkey, true)
        { }

        /// <summary>Creates a ChordHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this ChordHotKey.</param>
        /// <param name="basekey">The key to start the chord.</param>
        /// <param name="basemodifier">The modifier associated with the base key.</param>
        /// <param name="ChordHotKey">The LocalHotKey object to extract the chord key and modifier from.</param>
        /// <param name="enabled">Specifies that the hotkey is active,</param>
        public ChordHotKey(string name, ModifierKeys basemodifier, Key basekey, LocalHotKey ChordHotKey, bool enabled)
        {
            Name = name;
            BaseKey = basekey;
            BaseModifier = basemodifier;
            ChordKey = ChordHotKey.Key;
            chordmodifier = ChordHotKey.Modifier;
            Enabled = enabled;
        }

        /// <summary>Creates a ChordHotKey object.
        /// </summary>
        /// <param name="name">The unique identifier for this ChordHotKey.</param>
        /// <param name="basekey">The key to start the chord.</param>
        /// <param name="basemodifier">The modifier associated with the base key.</param>
        /// <param name="ChordHotKey">The LocalHotKey object to extract the chord key and modifier from.</param>
        public ChordHotKey(string name, ModifierKeys basemodifier, Key basekey, LocalHotKey ChordHotKey) :
            this(name, basemodifier, basekey, ChordHotKey, true)
        { }

        protected ChordHotKey(SerializationInfo info, StreamingContext context)
        {
            Name = info.GetString("Name");
            BaseKey = (Key)info.GetValue("BaseKey", typeof(Key));
            BaseModifier = (ModifierKeys)info.GetValue("BaseModifier", typeof(ModifierKeys));
            ChordKey = (Key)info.GetValue("ChordKey", typeof(Key));
            ChordModifier = (ModifierKeys)info.GetValue("ChordModifier", typeof(ModifierKeys));
            Enabled = info.GetBoolean("Enabled");
        }
        #endregion

        #region **Events, Methods and Helpers
        /// <summary>Compares this HotKey to another LocalHotKey.
        /// </summary>
        /// <param name="other">The LocalHotKey to compare.</param>
        /// <returns>True if equal, false otherwise.</returns>
        public bool Equals(LocalHotKey other)
        {
            return (BaseKey == other.Key && BaseModifier == other.Modifier);
        }
        /// <summary>Compares this Hotkey to another ChordHotKey.
        /// </summary>
        /// <param name="other">The ChordHotKey to compare.</param>
        /// <returns>True if equal, false otherwise.</returns>
        public bool Equals(ChordHotKey other)
        {
            if (BaseKey == other.BaseKey && BaseModifier == other.BaseModifier && ChordKey == other.ChordKey && ChordModifier == other.ChordModifier)
                return true;

            if (Name.ToLower() == other.Name.ToLower())
                return true;

            return false;
        }
        /// <summary>Checks if this Hotkey is equal to another ChordHotkey or LocalHotkey.
        /// </summary>
        /// <param name="obj">The Hotkey to compare</param>
        /// <returns>True if equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            LocalHotKey lhotKey = obj as LocalHotKey;
            if (lhotKey != null)
                return Equals(lhotKey);

            ChordHotKey hotkey = obj as ChordHotKey;
            if (hotkey != null)
                return Equals(hotkey);

            return false;
        }
        /// <summary>Serves the hash function for this class.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return (int)BaseKey ^ (int)ChordKey ^ (int)BaseModifier ^ (int)ChordModifier;
        }
        /// <summary>Converts the HotKey to a string.
        /// </summary>
        /// <returns>The FullInfo of the HotKey.</returns>
        public override string ToString()
        {
            return FullInfo();
        }
        /// <summary>Specifies the entire information about this HotKey.
        /// </summary>
        /// <returns>A string representation of the information.</returns>
        public string FullInfo()
        {
            string bhot = "";
            string chot = "";

            bhot = HotKeyShared.CombineShortcut(BaseModifier, BaseKey);
            chot = HotKeyShared.CombineShortcut(ChordModifier, ChordKey);

            return (String.Format("{0} ; {1} ; {2} ; {3}Enabled ; ChordHotKey", Name, bhot, chot, Enabled ? "" : "Not "));
        }
        /// <summary>Specifies the base information of this HotKey.
        /// </summary>
        /// <returns>A string representation of the information.</returns>
        public string BaseInfo()
        {
            return HotKeyShared.CombineShortcut(BaseModifier, BaseKey);
        }
        /// <summary>Specifies the Chord information of this HotKey.
        /// </summary>
        /// <returns>A string representation of the information.</returns>
        public string ChordInfo()
        {
            return HotKeyShared.CombineShortcut(ChordModifier, ChordKey);
        }
        /// <summary>The Event raised when the hotkey is pressed.
        /// </summary>
        protected virtual void OnHotKeyPress()
        {
            if (HotKeyPressed != null && Enabled)
                HotKeyPressed(this, new ChordHotKeyEventArgs(this));
        }
        /// <summary>Raises the HotKeyPressed event.
        /// </summary>
        internal void RaiseOnHotKeyPressed()
        {
            OnHotKeyPress();
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Name", Name);
            info.AddValue("BaseKey", BaseKey, typeof(Key));
            info.AddValue("BaseModifier", BaseModifier, typeof(ModifierKeys));
            info.AddValue("ChordKey", ChordKey, typeof(Key));
            info.AddValue("BaseModifier", ChordModifier, typeof(ModifierKeys));
            info.AddValue("Enabled", Enabled);
        }
        #endregion
    }
    #endregion
}
