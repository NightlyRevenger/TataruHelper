// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FFXIITataruHelper.ViewModel
{
    public class ChatCodeViewModel : INotifyPropertyChanged
    {
        string _Code;
        string _Name;
        string _VisibleName;
        Color _Color;

        bool _IsChecked;

        public string Code
        {
            get { return _Code; }
            private set
            {
                _Code = value;
                OnPropertyChanged("Code");
            }
        }

        public string Name
        {
            get { return _Name; }
            set
            {
                _Name = "Ck" + value;
                OnPropertyChanged("Name");
            }
        }

        public string VisibleName
        {
            get { return _VisibleName; }
            set
            {
                _Name = value;
                OnPropertyChanged("VisibleName");
            }
        }

        public Color Color
        {
            get { return _Color; }
            set
            {
                _Color = value;
                OnPropertyChanged("Color");
            }
        }

        public bool IsChecked
        {
            get { return _IsChecked; }
            set
            {
                _IsChecked = value;
                OnPropertyChanged("IsChecked");
            }
        }

        public ChatCodeViewModel(string code, string name, Color color, bool isChecked)
        {
            Code = code;
            Name = name;
            Color = color;
            IsChecked = isChecked;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            var localPropertyChanged = PropertyChanged;
            if (localPropertyChanged != null)
                localPropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}
