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
    /// <summary>
    ///    <para>
    ///       Stores the location and size of a rectangular region.
    ///    </para>
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public struct RectangleD : IEquatable<RectangleD>
    {
        /// <summary>
        ///    Initializes a new instance of the <see cref='System.Drawing.RectangleD'/>
        ///    class.
        /// </summary>
        public static readonly RectangleD Empty = new RectangleD();

        private double x; // Do not rename (binary serialization) 
        private double y; // Do not rename (binary serialization) 
        private double width; // Do not rename (binary serialization) 
        private double height; // Do not rename (binary serialization) 

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.RectangleD'/>
        ///       class with the specified location and size.
        ///    </para>
        /// </summary>
        public RectangleD(double x, double y, double width, double height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.RectangleD'/>
        ///       class with the specified location
        ///       and size.
        ///    </para>
        /// </summary>
        public RectangleD(PointF location, SizeF size)
        {
            x = location.X;
            y = location.Y;
            width = size.Width;
            height = size.Height;
        }

        /// <summary>
        ///    <para>
        ///       Creates a new <see cref='System.Drawing.RectangleD'/> with
        ///       the specified location and size.
        ///    </para>
        /// </summary>
        public static RectangleD FromLTRB(double left, double top, double right, double bottom) =>
            new RectangleD(left, top, right - left, bottom - top);

        /// <summary>
        ///    <para>
        ///       Gets or sets the coordinates of the upper-left corner of
        ///       the rectangular region represented by this <see cref='System.Drawing.RectangleD'/>.
        ///    </para>
        /// </summary>
        [Browsable(false)]
        public PointD Location
        {
            get { return new PointD(X, Y); }
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets or sets the size of this <see cref='System.Drawing.RectangleD'/>.
        ///    </para>
        /// </summary>
        [Browsable(false)]
        public SizeD Size
        {
            get { return new SizeD(Width, Height); }
            set
            {
                Width = value.Width;
                Height = value.Height;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets or sets the x-coordinate of the
        ///       upper-left corner of the rectangular region defined by this <see cref='System.Drawing.RectangleD'/>.
        ///    </para>
        /// </summary>
        public double X
        {
            get { return x; }
            set { x = value; }
        }

        /// <summary>
        ///    <para>
        ///       Gets or sets the y-coordinate of the
        ///       upper-left corner of the rectangular region defined by this <see cref='System.Drawing.RectangleD'/>.
        ///    </para>
        /// </summary>
        public double Y
        {
            get { return y; }
            set { y = value; }
        }

        /// <summary>
        ///    <para>
        ///       Gets or sets the width of the rectangular
        ///       region defined by this <see cref='System.Drawing.RectangleD'/>.
        ///    </para>
        /// </summary>
        public double Width
        {
            get { return width; }
            set { width = value; }
        }

        /// <summary>
        ///    <para>
        ///       Gets or sets the height of the
        ///       rectangular region defined by this <see cref='System.Drawing.RectangleD'/>.
        ///    </para>
        /// </summary>
        public double Height
        {
            get { return height; }
            set { height = value; }
        }

        /// <summary>
        ///    <para>
        ///       Gets the x-coordinate of the upper-left corner of the
        ///       rectangular region defined by this <see cref='System.Drawing.RectangleD'/> .
        ///    </para>
        /// </summary>
        [Browsable(false)]
        public double Left => X;

        /// <summary>
        ///    <para>
        ///       Gets the y-coordinate of the upper-left corner of the
        ///       rectangular region defined by this <see cref='System.Drawing.RectangleD'/>.
        ///    </para>
        /// </summary>
        [Browsable(false)]
        public double Top => Y;

        /// <summary>
        ///    <para>
        ///       Gets the x-coordinate of the lower-right corner of the
        ///       rectangular region defined by this <see cref='System.Drawing.RectangleD'/>.
        ///    </para>
        /// </summary>
        [Browsable(false)]
        public double Right => X + Width;

        /// <summary>
        ///    <para>
        ///       Gets the y-coordinate of the lower-right corner of the
        ///       rectangular region defined by this <see cref='System.Drawing.RectangleD'/>.
        ///    </para>
        /// </summary>
        [Browsable(false)]
        public double Bottom => Y + Height;

        /// <summary>
        ///    <para>
        ///       Tests whether this <see cref='System.Drawing.RectangleD'/> has a <see cref='System.Drawing.RectangleD.Width'/> or a <see cref='System.Drawing.RectangleD.Height'/> of 0.
        ///    </para>
        /// </summary>
        [Browsable(false)]
        public bool IsEmpty => (Width <= 0) || (Height <= 0);

        /// <summary>
        ///    <para>
        ///       Tests whether <paramref name="obj"/> is a <see cref='System.Drawing.RectangleD'/> with the same location and size of this
        ///    <see cref='System.Drawing.RectangleD'/>.
        ///    </para>
        /// </summary>
        public override bool Equals(object obj) => obj is RectangleD && Equals((RectangleD)obj);

        public bool Equals(RectangleD other) => this == other;

        /// <summary>
        ///    <para>
        ///       Tests whether two <see cref='System.Drawing.RectangleD'/>
        ///       objects have equal location and size.
        ///    </para>
        /// </summary>
        public static bool operator ==(RectangleD left, RectangleD right) =>
            left.X == right.X && left.Y == right.Y && left.Width == right.Width && left.Height == right.Height;

        /// <summary>
        ///    <para>
        ///       Tests whether two <see cref='System.Drawing.RectangleD'/>
        ///       objects differ in location or size.
        ///    </para>
        /// </summary>
        public static bool operator !=(RectangleD left, RectangleD right) => !(left == right);

        /// <summary>
        ///    <para>
        ///       Determines if the specified point is contained within the
        ///       rectangular region defined by this <see cref='System.Drawing.Rectangle'/> .
        ///    </para>
        /// </summary>
        public bool Contains(double x, double y) => X <= x && x < X + Width && Y <= y && y < Y + Height;

        /// <summary>
        ///    <para>
        ///       Determines if the specified point is contained within the
        ///       rectangular region defined by this <see cref='System.Drawing.Rectangle'/> .
        ///    </para>
        /// </summary>
        public bool Contains(PointD pt) => Contains(pt.X, pt.Y);

        /// <summary>
        ///    <para>
        ///       Determines if the rectangular region represented by
        ///    <paramref name="rect"/> is entirely contained within the rectangular region represented by 
        ///       this <see cref='System.Drawing.Rectangle'/> .
        ///    </para>
        /// </summary>
        public bool Contains(RectangleD rect) =>
            (X <= rect.X) && (rect.X + rect.Width <= X + Width) && (Y <= rect.Y) && (rect.Y + rect.Height <= Y + Height);

        /// <summary>
        ///    Gets the hash code for this <see cref='System.Drawing.RectangleD'/>.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int result = 17;
                result = result * 23 + X.GetHashCode();
                result = result * 23 + Y.GetHashCode();
                result = result * 23 + Width.GetHashCode();
                result = result * 23 + Height.GetHashCode();
                return result;
            }

            /* HashHelpers.Combine(
               HashHelpers.Combine(HashHelpers.Combine(X.GetHashCode(), Y.GetHashCode()), Width.GetHashCode()),
               Height.GetHashCode());//*/
        }

        /// <summary>
        ///    <para>
        ///       Inflates this <see cref='System.Drawing.Rectangle'/>
        ///       by the specified amount.
        ///    </para>
        /// </summary>
        public void Inflate(double x, double y)
        {
            X -= x;
            Y -= y;
            Width += 2 * x;
            Height += 2 * y;
        }

        /// <summary>
        ///    Inflates this <see cref='System.Drawing.Rectangle'/> by the specified amount.
        /// </summary>
        public void Inflate(SizeF size) => Inflate(size.Width, size.Height);

        /// <summary>
        ///    <para>
        ///       Creates a <see cref='System.Drawing.Rectangle'/>
        ///       that is inflated by the specified amount.
        ///    </para>
        /// </summary>
        public static RectangleD Inflate(RectangleD rect, double x, double y)
        {
            RectangleD r = rect;
            r.Inflate(x, y);
            return r;
        }

        /// <summary> Creates a Rectangle that represents the intersection between this Rectangle and rect.
        /// </summary>
        public void Intersect(RectangleD rect)
        {
            RectangleD result = Intersect(rect, this);

            X = result.X;
            Y = result.Y;
            Width = result.Width;
            Height = result.Height;
        }

        /// <summary>
        ///    Creates a rectangle that represents the intersection between a and
        ///    b. If there is no intersection, null is returned.
        /// </summary>
        public static RectangleD Intersect(RectangleD a, RectangleD b)
        {
            double x1 = Math.Max(a.X, b.X);
            double x2 = Math.Min(a.X + a.Width, b.X + b.Width);
            double y1 = Math.Max(a.Y, b.Y);
            double y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);

            if (x2 >= x1 && y2 >= y1)
            {
                return new RectangleD(x1, y1, x2 - x1, y2 - y1);
            }

            return Empty;
        }

        /// <summary>
        ///    Determines if this rectangle intersects with rect.
        /// </summary>
        public bool IntersectsWith(RectangleD rect) =>
            (rect.X < X + Width) && (X < rect.X + rect.Width) && (rect.Y < Y + Height) && (Y < rect.Y + rect.Height);

        /// <summary>
        ///    Creates a rectangle that represents the union between a and
        ///    b.
        /// </summary>
        public static RectangleD Union(RectangleD a, RectangleD b)
        {
            double x1 = Math.Min(a.X, b.X);
            double x2 = Math.Max(a.X + a.Width, b.X + b.Width);
            double y1 = Math.Min(a.Y, b.Y);
            double y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);

            return new RectangleD(x1, y1, x2 - x1, y2 - y1);
        }

        /// <summary>
        ///    Adjusts the location of this rectangle by the specified amount.
        /// </summary>
        public void Offset(PointD pos) => Offset(pos.X, pos.Y);

        /// <summary>
        ///    Adjusts the location of this rectangle by the specified amount.
        /// </summary>
        public void Offset(double x, double y)
        {
            X += x;
            Y += y;
        }

        /// <summary>
        ///    Converts the specified <see cref='System.Drawing.Rectangle'/> to a
        /// <see cref='System.Drawing.RectangleD'/>.
        /// </summary>
        public static implicit operator RectangleD(Rectangle r) => new RectangleD(r.X, r.Y, r.Width, r.Height);

        /// <summary>
        ///    Converts the <see cref='System.Drawing.RectangleD.Location'/> and <see cref='System.Drawing.RectangleD.Size'/> of this <see cref='System.Drawing.RectangleD'/> to a
        ///    human-readable string.
        /// </summary>
        public override string ToString() =>
            "{X=" + X.ToString() + ",Y=" + Y.ToString() +
            ",Width=" + Width.ToString() + ",Height=" + Height.ToString() + "}";
    }
}