// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Drawing
{
    /**
     * Represents a dimension in 2D coordinate space
     */
    /// <summary>
    ///    <para>
    ///       Represents the size of a rectangular region
    ///       with an ordered pair of width and height.
    ///    </para>
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public struct SizeD : IEquatable<SizeD>
    {
        /// <summary>
        ///    Initializes a new instance of the <see cref='System.Drawing.SizeD'/> class.
        /// </summary>
        public static readonly SizeD Empty = new SizeD();
        private double width; // Do not rename (binary serialization) 
        private double height; // Do not rename (binary serialization) 

        /**
         * Create a new SizeD object from another size object
         */
        /// <summary>
        ///    Initializes a new instance of the <see cref='System.Drawing.SizeD'/> class
        ///    from the specified existing <see cref='System.Drawing.SizeD'/>.
        /// </summary>
        public SizeD(SizeD size)
        {
            width = size.width;
            height = size.height;
        }

        /**
         * Create a new SizeD object from a point
         */
        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.SizeD'/> class from
        ///       the specified <see cref='System.Drawing.PointD'/>.
        ///    </para>
        /// </summary>
        public SizeD(PointD pt)
        {
            width = pt.X;
            height = pt.Y;
        }

        /**
         * Create a new SizeD object of the specified dimension
         */
        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.SizeD'/> class from
        ///       the specified dimensions.
        ///    </para>
        /// </summary>
        public SizeD(double width, double height)
        {
            this.width = width;
            this.height = height;
        }

        /// <summary>
        ///    <para>
        ///       Performs vector addition of two <see cref='System.Drawing.SizeD'/> objects.
        ///    </para>
        /// </summary>
        public static SizeD operator +(SizeD sz1, SizeD sz2) => Add(sz1, sz2);

        /// <summary>
        ///    <para>
        ///       Contracts a <see cref='System.Drawing.SizeD'/> by another <see cref='System.Drawing.SizeD'/>
        ///    </para>
        /// </summary>        
        public static SizeD operator -(SizeD sz1, SizeD sz2) => Subtract(sz1, sz2);

        /// <summary>
        /// Multiplies <see cref="SizeD"/> by a <see cref="double"/> producing <see cref="SizeD"/>.
        /// </summary>
        /// <param name="left">Multiplier of type <see cref="double"/>.</param>
        /// <param name="right">Multiplicand of type <see cref="SizeD"/>.</param>
        /// <returns>Product of type <see cref="SizeD"/>.</returns>
        public static SizeD operator *(double left, SizeD right) => Multiply(right, left);

        /// <summary>
        /// Multiplies <see cref="SizeD"/> by a <see cref="double"/> producing <see cref="SizeD"/>.
        /// </summary>
        /// <param name="left">Multiplicand of type <see cref="SizeD"/>.</param>
        /// <param name="right">Multiplier of type <see cref="double"/>.</param>
        /// <returns>Product of type <see cref="SizeD"/>.</returns>
        public static SizeD operator *(SizeD left, double right) => Multiply(left, right);

        /// <summary>
        /// Divides <see cref="SizeD"/> by a <see cref="double"/> producing <see cref="SizeD"/>.
        /// </summary>
        /// <param name="left">Dividend of type <see cref="SizeD"/>.</param>
        /// <param name="right">Divisor of type <see cref="int"/>.</param>
        /// <returns>Result of type <see cref="SizeD"/>.</returns>
        public static SizeD operator /(SizeD left, double right)
            => new SizeD(left.width / right, left.height / right);

        /// <summary>
        ///    Tests whether two <see cref='System.Drawing.SizeD'/> objects
        ///    are identical.
        /// </summary>
        public static bool operator ==(SizeD sz1, SizeD sz2) => sz1.Width == sz2.Width && sz1.Height == sz2.Height;

        /// <summary>
        ///    <para>
        ///       Tests whether two <see cref='System.Drawing.SizeD'/> objects are different.
        ///    </para>
        /// </summary>
        public static bool operator !=(SizeD sz1, SizeD sz2) => !(sz1 == sz2);

        /// <summary>
        ///    <para>
        ///       Converts the specified <see cref='System.Drawing.SizeD'/> to a
        ///    <see cref='System.Drawing.PointD'/>.
        ///    </para>
        /// </summary>
        public static explicit operator PointD(SizeD size) => new PointD(size.Width, size.Height);

        /// <summary>
        ///    <para>
        ///       Tests whether this <see cref='System.Drawing.SizeD'/> has zero
        ///       width and height.
        ///    </para>
        /// </summary>
        [Browsable(false)]
        public bool IsEmpty => width == 0 && height == 0;

        /**
         * Horizontal dimension
         */

        /// <summary>
        ///    <para>
        ///       Represents the horizontal component of this
        ///    <see cref='System.Drawing.SizeD'/>.
        ///    </para>
        /// </summary>
        public double Width
        {
            get { return width; }
            set { width = value; }
        }

        /**
         * Vertical dimension
         */

        /// <summary>
        ///    <para>
        ///       Represents the vertical component of this
        ///    <see cref='System.Drawing.SizeD'/>.
        ///    </para>
        /// </summary>
        public double Height
        {
            get { return height; }
            set { height = value; }
        }

        /// <summary>
        ///    <para>
        ///       Performs vector addition of two <see cref='System.Drawing.SizeD'/> objects.
        ///    </para>
        /// </summary>
        public static SizeD Add(SizeD sz1, SizeD sz2) => new SizeD(sz1.Width + sz2.Width, sz1.Height + sz2.Height);

        /// <summary>
        ///    <para>
        ///       Contracts a <see cref='System.Drawing.SizeD'/> by another <see cref='System.Drawing.SizeD'/>
        ///       .
        ///    </para>
        /// </summary>        
        public static SizeD Subtract(SizeD sz1, SizeD sz2) => new SizeD(sz1.Width - sz2.Width, sz1.Height - sz2.Height);

        /// <summary>
        ///    <para>
        ///       Tests to see whether the specified object is a
        ///    <see cref='System.Drawing.SizeD'/> 
        ///    with the same dimensions as this <see cref='System.Drawing.SizeD'/>.
        /// </para>
        /// </summary>
        public override bool Equals(object obj) => obj is SizeD && Equals((SizeD)obj);

        public bool Equals(SizeD other) => this == other;

        public override int GetHashCode()
        {
            return unchecked(Width.GetHashCode() * 17 + Height.GetHashCode());
            //=> HashHelpers.Combine(Width.GetHashCode(), Height.GetHashCode());
        }

        public PointD ToPointD() => (PointD)this;

        public Size ToSize() => Size.Truncate((SizeF)this);

        /// <summary>
        ///    <para>
        ///       Creates a human-readable string that represents this
        ///    <see cref='System.Drawing.SizeD'/>.
        ///    </para>
        /// </summary>
        public override string ToString() => "{Width=" + width.ToString() + ", Height=" + height.ToString() + "}";

        /// <summary>
        /// Multiplies <see cref="SizeD"/> by a <see cref="double"/> producing <see cref="SizeD"/>.
        /// </summary>
        /// <param name="size">Multiplicand of type <see cref="SizeD"/>.</param>
        /// <param name="multiplier">Multiplier of type <see cref="double"/>.</param>
        /// <returns>Product of type SizeD.</returns>
        private static SizeD Multiply(SizeD size, double multiplier) =>
            new SizeD(size.width * multiplier, size.height * multiplier);

        public static explicit operator SizeF(SizeD size)
        {
            var tmp = new SizeF((float)size.Width, (float)size.Height);

            return tmp;
        }
    }
}
