using System;

namespace FFXIVTataruHelper.WinUtils
{
    /// <summary>
    /// Where a window's edges go when it is moved or resized by following the
    /// cursor by hand - which is how it is done under Wine, where the system's
    /// own loops are not available to the chat window (see WindowResizer).
    ///
    /// Kept apart from anything that draws, so the arithmetic can be checked
    /// against numbers.
    /// </summary>
    public static class HandTrackedBounds
    {
        public readonly struct Bounds : IEquatable<Bounds>
        {
            public Bounds(double left, double top, double width, double height)
            {
                Left = left;
                Top = top;
                Width = width;
                Height = height;
            }

            public double Left { get; }

            public double Top { get; }

            public double Width { get; }

            public double Height { get; }

            public double Right => Left + Width;

            public double Bottom => Top + Height;

            public bool Equals(Bounds other) =>
                Left.Equals(other.Left) && Top.Equals(other.Top) &&
                Width.Equals(other.Width) && Height.Equals(other.Height);

            public override bool Equals(object obj) => obj is Bounds other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(Left, Top, Width, Height);

            public override string ToString() => FormattableString.Invariant($"{Left},{Top} {Width}x{Height}");
        }

        /// <summary>The window carried along by the cursor, the same size.</summary>
        public static Bounds Moved(Bounds start, double dx, double dy) =>
            new Bounds(start.Left + dx, start.Top + dy, start.Width, start.Height);

        /// <summary>
        /// The window with one or two of its edges following the cursor.
        ///
        /// <paramref name="horizontal"/> says which side edge is held: -1 the
        /// left, +1 the right, 0 neither; <paramref name="vertical"/> the same
        /// for the top (-1) and the bottom (+1). The edge that is not held
        /// stays where it was, and a window is never made smaller than the
        /// least it may be - the held edge stops instead, rather than pushing
        /// the far edge away.
        /// </summary>
        public static Bounds Resized(Bounds start, int horizontal, int vertical,
            double dx, double dy, double minWidth, double minHeight)
        {
            double left = start.Left, width = start.Width;
            double top = start.Top, height = start.Height;

            if (horizontal < 0)
            {
                width = Math.Max(minWidth, start.Width - dx);
                left = start.Right - width;
            }
            else if (horizontal > 0)
            {
                width = Math.Max(minWidth, start.Width + dx);
            }

            if (vertical < 0)
            {
                height = Math.Max(minHeight, start.Height - dy);
                top = start.Bottom - height;
            }
            else if (vertical > 0)
            {
                height = Math.Max(minHeight, start.Height + dy);
            }

            return new Bounds(left, top, width, height);
        }
    }
}
