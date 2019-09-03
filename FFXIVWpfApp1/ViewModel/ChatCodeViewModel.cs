// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace FFXIVTataruHelper.ViewModel
{
    public class ChatCodeViewModel : INotifyPropertyChanged, IEquatable<ChatCodeViewModel>
    {
        [JsonProperty]
        string _Code;

        [JsonProperty]
        string _Name;

        [JsonProperty]
        Color _Color;

        [JsonProperty]
        bool _IsChecked;

        [JsonIgnore]
        public string Code
        {
            get { return _Code; }
            private set
            {
                _Code = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public string Name
        {
            get { return _Name; }
            set
            {
                var val = value.Replace("Ck", "");
                _Name = "Ck" + val;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public Color Color
        {
            get { return _Color; }
            set
            {
                if (_Color == value) return;

                _Color = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public bool IsChecked
        {
            get { return _IsChecked; }
            set
            {
                if (_IsChecked == value) return;

                _IsChecked = value;
                OnPropertyChanged();
            }
        }

        public ChatCodeViewModel()
        {
            Code = string.Empty;
            Name = string.Empty;
            Color = Color.FromArgb(255, 255, 255, 255);
            IsChecked = false;
        }

        public ChatCodeViewModel(string code, string name, Color color, bool isChecked)
        {
            Code = code;
            Name = name;
            Color = color;
            IsChecked = isChecked;
        }

        public ChatCodeViewModel(ChatCodeViewModel chatCodeViewModel)
        {
            Code = chatCodeViewModel._Code;
            Name = chatCodeViewModel._Name;
            Color = chatCodeViewModel._Color;
            IsChecked = chatCodeViewModel._IsChecked;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            var localPropertyChanged = PropertyChanged;
            if (localPropertyChanged != null)
                localPropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ChatCodeViewModel);
        }

        public bool Equals(ChatCodeViewModel code)
        {
            // If parameter is null, return false.
            if (Object.ReferenceEquals(code, null))
                return false;

            // Optimization for a common success case.
            if (Object.ReferenceEquals(this, code))

                // If run-time types are not exactly the same, return false.
                if (this.GetType() != code.GetType())
                    return false;

            return this._Code == code._Code;
        }

        public static bool operator ==(ChatCodeViewModel left, ChatCodeViewModel right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (ReferenceEquals(left, null))
                return false;

            if (ReferenceEquals(right, null))
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(ChatCodeViewModel left, ChatCodeViewModel right) => !(left == right);

        public override int GetHashCode()
        {
            return _Code.GetHashCode();
        }
    }
}
