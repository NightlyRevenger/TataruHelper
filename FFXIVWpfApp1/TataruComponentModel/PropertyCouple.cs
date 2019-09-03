// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVTataruHelper.TataruComponentModel
{
    public abstract class BasePropertyCouple
    {
        public virtual string PropName1 { get; set; }

        public virtual string PropName2 { get; set; }

        public virtual string CombinedName { get; set; }

        public virtual bool IsSameName { get { return PropName1 == PropName2; } }

        public virtual PropertyInfo Property1Info { get; set; }
        public virtual PropertyInfo Property2Info { get; set; }

        public virtual MethodInfo Covertor1Info { get; set; }
        public virtual MethodInfo Covertor2Info { get; set; }

        public virtual bool HasConverter { get; set; }
    }

    class PropertyCouple<T1, T2> : BasePropertyCouple, IEquatable<PropertyCouple<T1, T2>>
    {
        public override string PropName1
        {
            get { return _PropName1; }
        }

        public override string PropName2
        {
            get { return _PropName2; }
        }

        public override string CombinedName
        {
            get { return _PropName1 + PropName2; }
        }

        public override bool HasConverter
        {
            get
            {
                return !(_PropertyConverter1 == null || _PropertyConverter2 == null);
            }
        }


        public override MethodInfo Covertor1Info
        {
            get { return _Covertor1Info; }
        }
        public override MethodInfo Covertor2Info
        {
            get { return _Covertor2Info; }
        }

        protected string _PropName1;
        protected string _PropName2;

        protected MethodInfo _Covertor1Info;
        protected MethodInfo _Covertor2Info;

        public delegate void PropertyConverter1(ref T1 o1, ref T2 o2);
        public delegate void PropertyConverter2(ref T2 o2, ref T1 o1);

        public delegate T2 PropertyConverter3(T1 o1);
        public delegate T1 PropertyConverter4(T2 o2);

        protected PropertyConverter1 _PropertyConverter1;
        protected PropertyConverter2 _PropertyConverter2;

        protected PropertyConverter3 _PropertyConverter3;
        protected PropertyConverter4 _PropertyConverter4;

        public PropertyCouple(string name1, string name2)
        {
            _PropName1 = name1;
            _PropName2 = name2;
        }

        public PropertyCouple(string name1, string name2, PropertyConverter1 propertyConverter1, PropertyConverter2 propertyConverter2)
        {
            _PropName1 = name1;
            _PropName2 = name2;

            _PropertyConverter1 = propertyConverter1;
            _PropertyConverter2 = propertyConverter2;

            if (this.HasConverter)
            {
                _Covertor1Info = this.GetType().GetMethod("Convert", new Type[] { typeof(T1).MakeByRefType(), typeof(T2).MakeByRefType() });
                _Covertor2Info = this.GetType().GetMethod("Convert", new Type[] { typeof(T2).MakeByRefType(), typeof(T1).MakeByRefType() });
            }
        }

        public PropertyCouple(PropertyCouple<T1, T2> couple)
        {
            this._PropName1 = couple._PropName1;
            this._PropName2 = couple._PropName2;

            this._PropertyConverter1 = couple._PropertyConverter1;
            this._PropertyConverter2 = couple._PropertyConverter2;

            this._Covertor1Info = couple._Covertor1Info;
            this._Covertor2Info = couple._Covertor2Info;
        }

        public virtual void Convert(ref T1 obj1, ref T2 obj2)
        {
            if (_PropertyConverter1 != null)
                _PropertyConverter1(ref obj1, ref obj2);
        }

        public virtual void Convert(ref T2 obj1, ref T1 obj2)
        {

            if (_PropertyConverter2 != null)
                _PropertyConverter2(ref obj1, ref obj2);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as PropertyCouple<T1, T2>);
        }

        public virtual bool Equals(PropertyCouple<T1, T2> couple)
        {
            if (Object.ReferenceEquals(couple, null))
                return false;

            if (Object.ReferenceEquals(this, couple))
                return true;

            if (this.GetType() != couple.GetType())
                return false;

            return this._PropName1 == couple._PropName2;
        }

        public static bool operator ==(PropertyCouple<T1, T2> left, PropertyCouple<T1, T2> right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (ReferenceEquals(left, null))
                return false;

            if (ReferenceEquals(right, null))
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(PropertyCouple<T1, T2> left, PropertyCouple<T1, T2> right) => !(left == right);

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = hash * 23 + _PropName1.GetHashCode();
                hash = hash * 23 + _PropName2.GetHashCode();
                return hash;
            }
        }
    }

    class PropertyCoupleExtened<T1, T2> : PropertyCouple<T1, T2>
    {
        public override PropertyInfo Property1Info
        {
            get
            {
                return _Property1Info;
            }
        }
        public override PropertyInfo Property2Info
        {
            get
            {

                return _Property2Info;
            }
        }

        PropertyInfo _Property1Info;
        PropertyInfo _Property2Info;

        public PropertyCoupleExtened(PropertyCouple<T1, T2> couple, object obj1, object obj2) : base(couple)
        {
            var type1 = obj1.GetType();
            var type2 = obj2.GetType();

            _Property1Info = type1.GetProperty(PropName1);
            _Property2Info = type2.GetProperty(PropName2);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as PropertyCoupleExtened<T1, T2>);
        }

        public virtual bool Equals(PropertyCoupleExtened<T1, T2> couple)
        {
            if (Object.ReferenceEquals(couple, null))
                return false;

            if (Object.ReferenceEquals(this, couple))
                return true;

            if (this.GetType() != couple.GetType())
                return false;

            return this.Property1Info == couple.Property2Info;
        }

        public static bool operator ==(PropertyCoupleExtened<T1, T2> left, PropertyCoupleExtened<T1, T2> right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (ReferenceEquals(left, null))
                return false;

            if (ReferenceEquals(right, null))
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(PropertyCoupleExtened<T1, T2> left, PropertyCoupleExtened<T1, T2> right) => !(left == right);

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                hash = hash * 23 + Property1Info.GetHashCode();
                hash = hash * 23 + Property2Info.GetHashCode();
                return hash;
            }
        }//*/

    }
}
