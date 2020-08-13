// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using FFXIVTataruHelper.EventArguments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FFXIVTataruHelper.TataruComponentModel
{
    class PropertyBinder
    {
        public Object Object1
        {
            get { return _Object1; }
        }

        public Object Object2
        {
            get { return _Object2; }
        }

        Object _Object1;
        Object _Object2;

        Type _obj1Type;
        Type _obj2Type;

        private Object lockObj = new object();

        Dictionary<string, BasePropertyCouple> _PropertyCouples;

        public PropertyBinder(INotifyPropertyChangedAsync obj1, INotifyPropertyChangedAsync obj2)
        {
            _PropertyCouples = new Dictionary<string, BasePropertyCouple>();

            _Object1 = obj1;
            _Object2 = obj2;

            ((INotifyPropertyChangedAsync)_Object1).AsyncPropertyChanged += OnObject1PropertyChange;
            ((INotifyPropertyChangedAsync)_Object2).AsyncPropertyChanged += OnObject2PropertyChange;

            _obj1Type = _Object1.GetType();
            _obj2Type = _Object2.GetType();
        }

        public void AddPropertyCouple<T1, T2>(PropertyCouple<T1, T2> couple)
        {
            PropertyCoupleExtened<T1, T2> pr = new PropertyCoupleExtened<T1, T2>(couple, _Object1, _Object2);

            if (pr.IsSameName)
            {
                if (!_PropertyCouples.TryAdd(pr.PropName1, pr))
                {
                    string msg = "Tryed to bind pporperty with alredy binded property. " + Environment.NewLine;
                    msg += Convert.ToString(_Object1) + "; " + Convert.ToString(_Object2) + ";" + Environment.NewLine;
                    msg += pr.Property1Info.ToString() + "; " + pr.Property2Info.ToString() + ";";

                    throw new ArgumentException(msg);
                }
            }
            else
            {
                if (!_PropertyCouples.TryAdd(pr.PropName1, pr))
                {
                    string msg = "Tryed to bind pporperty with alredy binded property. " + Environment.NewLine;
                    msg += Convert.ToString(_Object1) + "; " + Convert.ToString(_Object2) + ";" + Environment.NewLine;
                    msg += pr.Property1Info.ToString() + "; " + pr.Property2Info.ToString() + ";";

                    throw new ArgumentException(msg);
                }
                if (!_PropertyCouples.TryAdd(pr.PropName2, pr))
                {
                    string msg = "Tryed to bind pporperty with alredy binded property. " + Environment.NewLine;
                    msg += Convert.ToString(_Object1) + "; " + Convert.ToString(_Object2) + ";" + Environment.NewLine;
                    msg += pr.Property1Info.ToString() + "; " + pr.Property2Info.ToString() + ";";

                    throw new ArgumentException(msg);
                }
            }
        }

        private async Task OnObject1PropertyChange(AsyncPropertyChangedEventArgs ea)
        {
            await Task.Run(() =>
            {
                SetValues(ea.PropertyName, 1);
            });
        }

        private async Task OnObject2PropertyChange(AsyncPropertyChangedEventArgs ea)
        {
            await Task.Run(() =>
            {
                SetValues(ea.PropertyName, -1);
            });
        }

        private void SetValues(string name, int dir)
        {

            BasePropertyCouple couple = null;

            if (_PropertyCouples.TryGetValue(name, out couple))
            {
                if (dir > 0)
                    SetReflectedValues(couple.Property1Info, couple.Property2Info, _Object1, _Object2, couple, couple.Covertor1Info);
                else
                    SetReflectedValues(couple.Property2Info, couple.Property1Info, _Object2, _Object1, couple, couple.Covertor2Info);
            }
        }

        private void SetReflectedValues(PropertyInfo propertyInfo1, PropertyInfo propertyInfo2, object obj1, object obj2, BasePropertyCouple couple, MethodInfo covertorInfo)
        {
            object obj1Val1 = propertyInfo1.GetValue(obj1);
            object obj2Val1 = propertyInfo2.GetValue(obj2);

            lock (lockObj)
            {
                if (!obj1Val1.Equals(obj2Val1))
                {
                    if (couple.HasConverter)
                    {
                        object[] arguments = new object[] { obj1Val1, obj2Val1 };
                        covertorInfo.Invoke(couple, arguments);

                        propertyInfo2.SetValue(obj2, arguments[1]);
                    }
                    else
                    {
                        var obj1Val1Type = obj1Val1.GetType();
                        propertyInfo2.SetValue(obj2, obj1Val1);

                        /*
                        else
                        {
                            ConstructorInfo ctor = obj1Val1Type.GetConstructor(new[] { obj1Val1Type });
                            if (ctor != null)
                            {
                                object instance = ctor.Invoke(new object[] { obj1Val1 });
                                propertyInfo2.SetValue(obj2, instance);
                            }
                            else
                                propertyInfo2.SetValue(obj2, obj1Val1);

                        }//*/
                    }
                }
            }
        }

        public void Stop()
        {
            ((INotifyPropertyChangedAsync)_Object1).AsyncPropertyChanged -= OnObject1PropertyChange;
            ((INotifyPropertyChangedAsync)_Object1).AsyncPropertyChanged -= OnObject2PropertyChange;
        }

        public void Restart()
        {
            ((INotifyPropertyChangedAsync)_Object1).AsyncPropertyChanged += OnObject1PropertyChange;
            ((INotifyPropertyChangedAsync)_Object1).AsyncPropertyChanged += OnObject2PropertyChange;
        }
    }
}
