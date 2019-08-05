// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System.ComponentModel;

namespace System.Drawing
{
    /// <summary>
    ///    Represents an ordered pair of x and y coordinates that
    ///    define a point in a two-dimensional plane.
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public struct PointD : IEquatable<PointD>
    {
        /// <summary>
        ///    <para>
        ///       Creates a new instance of the <see cref='System.Drawing.PointD'/> class
        ///       with member data left uninitialized.
        ///    </para>
        /// </summary>
        public static readonly PointD Empty = new PointD();
        private double x; // Do not rename (binary serialization) 
        private double y; // Do not rename (binary serialization) 

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.PointD'/> class
        ///       with the specified coordinates.
        ///    </para>
        /// </summary>
        public PointD(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        /// <summary>
        ///    <para>
        ///       Gets a value indicating whether this <see cref='System.Drawing.PointD'/> is empty.
        ///    </para>
        /// </summary>
        [Browsable(false)]
        public bool IsEmpty => x == 0d && y == 0d;

        /// <summary>
        ///    <para>
        ///       Gets the x-coordinate of this <see cref='System.Drawing.PointD'/>.
        ///    </para>
        /// </summary>
        public double X
        {
            get { return x; }
            set { x = value; }
        }

        /// <summary>
        ///    <para>
        ///       Gets the y-coordinate of this <see cref='System.Drawing.PointD'/>.
        ///    </para>
        /// </summary>
        public double Y
        {
            get { return y; }
            set { y = value; }
        }

        /// <summary>
        ///    <para>
        ///       Translates a <see cref='System.Drawing.PointD'/> by a given <see cref='System.Drawing.Size'/> .
        ///    </para>
        /// </summary>
        public static PointD operator +(PointD pt, Size sz) => Add(pt, sz);

        /// <summary>
        ///    <para>
        ///       Translates a <see cref='System.Drawing.PointD'/> by the negative of a given <see cref='System.Drawing.Size'/> .
        ///    </para>
        /// </summary>
        public static PointD operator -(PointD pt, Size sz) => Subtract(pt, sz);

        /// <summary>
        ///    <para>
        ///       Translates a <see cref='System.Drawing.PointD'/> by a given <see cref='System.Drawing.SizeF'/> .
        ///    </para>
        /// </summary>
        public static PointD operator +(PointD pt, SizeF sz) => Add(pt, sz);

        /// <summary>
        ///    <para>
        ///       Translates a <see cref='System.Drawing.PointD'/> by the negative of a given <see cref='System.Drawing.SizeF'/> .
        ///    </para>
        /// </summary>
        public static PointD operator -(PointD pt, SizeF sz) => Subtract(pt, sz);

        /// <summary>
        ///    <para>
        ///       Compares two <see cref='System.Drawing.PointD'/> objects. The result specifies
        ///       whether the values of the <see cref='System.Drawing.PointD.X'/> and <see cref='System.Drawing.PointD.Y'/> properties of the two <see cref='System.Drawing.PointD'/>
        ///       objects are equal.
        ///    </para>
        /// </summary>
        public static bool operator ==(PointD left, PointD right) => left.X == right.X && left.Y == right.Y;

        /// <summary>
        ///    <para>
        ///       Compares two <see cref='System.Drawing.PointD'/> objects. The result specifies whether the values
        ///       of the <see cref='System.Drawing.PointD.X'/> or <see cref='System.Drawing.PointD.Y'/> properties of the two
        ///    <see cref='System.Drawing.PointD'/> 
        ///    objects are unequal.
        /// </para>
        /// </summary>
        public static bool operator !=(PointD left, PointD right) => !(left == right);

        /// <summary>
        ///    <para>
        ///       Translates a <see cref='System.Drawing.PointD'/> by a given <see cref='System.Drawing.Size'/> .
        ///    </para>
        /// </summary>
        public static PointD Add(PointD pt, Size sz) => new PointD(pt.X + sz.Width, pt.Y + sz.Height);

        /// <summary>
        ///    <para>
        ///       Translates a <see cref='System.Drawing.PointD'/> by the negative of a given <see cref='System.Drawing.Size'/> .
        ///    </para>
        /// </summary>
        public static PointD Subtract(PointD pt, Size sz) => new PointD(pt.X - sz.Width, pt.Y - sz.Height);

        /// <summary>
        ///    <para>
        ///       Translates a <see cref='System.Drawing.PointD'/> by a given <see cref='System.Drawing.SizeF'/> .
        ///    </para>
        /// </summary>
        public static PointD Add(PointD pt, SizeF sz) => new PointD(pt.X + sz.Width, pt.Y + sz.Height);

        /// <summary>
        ///    <para>
        ///       Translates a <see cref='System.Drawing.PointD'/> by the negative of a given <see cref='System.Drawing.SizeF'/> .
        ///    </para>
        /// </summary>
        public static PointD Subtract(PointD pt, SizeF sz) => new PointD(pt.X - sz.Width, pt.Y - sz.Height);

        public override bool Equals(object obj) => obj is PointD && Equals((PointD)obj);

        public bool Equals(PointD other) => this == other;

        public override int GetHashCode()
        {
            return unchecked(X.GetHashCode() * 17 + Y.GetHashCode());

            //=> HashHelpers.Combine(X.GetHashCode(), Y.GetHashCode());
        }

        public override string ToString() => "{X=" + x.ToString() + ", Y=" + y.ToString() + "}";
    }
}
