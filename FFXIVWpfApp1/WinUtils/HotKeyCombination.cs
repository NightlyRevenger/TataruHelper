// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FFXIITataruHelper.WinUtils
{
    class HotKeyCombination
    {
        [JsonIgnore]
        public ModifierKeys ModifierKey { get { return _ModifierKey; } }

        [JsonIgnore]
        public Key NormalKey { get { return _NormalKey; } }

        [JsonIgnore]
        public string Name { get { return _Name; } }

        [JsonIgnore]
        public bool IsInitialized
        {
            get
            {
                if (_ModifierKey != ModifierKeys.None && _NormalKey != Key.None)
                    return true;

                return false;
            }
        }

        [JsonProperty]
        private ModifierKeys _ModifierKey;

        [JsonProperty]
        private Key _NormalKey;

        [JsonProperty]
        private string _Name;

        [JsonProperty]
        private List<Key> _Keys;

        public HotKeyCombination()
        {
            _Name = "";

            _Keys = new List<Key>();

            _ModifierKey = ModifierKeys.None;

            _NormalKey = Key.None;
        }

        public HotKeyCombination(string name)
        {
            _Name = name;

            _Keys = new List<Key>();

            _ModifierKey = ModifierKeys.None;

            _NormalKey = Key.None;
        }

        public HotKeyCombination(HotKeyCombination hotKeyCombination)
        {
            _ModifierKey = hotKeyCombination._ModifierKey;
            _NormalKey = hotKeyCombination._NormalKey;

            _Keys = hotKeyCombination._Keys;
        }

        public void AddKey(Key key)
        {
            if (!_Keys.Contains(key))
                _Keys.Add(key);

            SetModiferKey();

            if (!IsModifierKey(key))
            {
                _Keys.RemoveAll(x => !IsModifierKey(x));
                _Keys.Add(key);
                _NormalKey = key;
            }
        }

        public void ClearKeys()
        {
            _ModifierKey = ModifierKeys.None;
            _NormalKey = Key.None;

            _Keys.Clear();
        }

        public void RemoveKey(Key key)
        {
            _Keys.RemoveAll(x => x == key);

            SetModiferKey();

            if (_NormalKey == key)
                _NormalKey = Key.None;
        }

        void SetModiferKey()
        {
            _ModifierKey = ModifierKeys.None;

            for (int i = 0; i < _Keys.Count; i++)
            {
                var _key = _Keys[i];

                if (_key == Key.LeftAlt || _key == Key.RightAlt)
                    _ModifierKey = _ModifierKey | ModifierKeys.Alt;

                if (_key == Key.LeftCtrl || _key == Key.RightCtrl)
                    _ModifierKey = _ModifierKey | ModifierKeys.Control;

                if (_key == Key.LeftShift || _key == Key.RightShift)
                    _ModifierKey = _ModifierKey | ModifierKeys.Shift;

                if (_key == Key.LWin || _key == Key.RWin)
                    _ModifierKey = _ModifierKey | ModifierKeys.Windows;

                if (_key == Key.System)
                    _ModifierKey = _ModifierKey | ModifierKeys.Alt;
            }
        }
        public string toLogString()
        {
            string res = "Empty";
            if (_Keys.Count > 0)
                res = String.Empty;

            for (int i = 0; i < _Keys.Count; i++)
            {
                res += _Keys[i].ToString() + "+";
            }
            res = res.Remove(res.Length - 1, 1);
            res = res.Replace("Left", "");

            return res;
        }

        public static bool IsModifierKey(Key key)
        {
            if (key == Key.LeftAlt || key == Key.RightAlt ||
                 key == Key.LeftShift || key == Key.RightShift ||
                 key == Key.LeftCtrl || key == Key.RightCtrl ||
                 key == Key.LWin || key == Key.RWin || key == Key.System)
            {
                return true;
            }

            return false;
        }
    }
}
