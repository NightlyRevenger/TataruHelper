// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com




using System.Runtime.InteropServices;

namespace Hardcodet.Wpf.TaskbarNotification.Interop
{
    /// <summary>
    /// Win API struct providing coordinates for a single point.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
        /// <summary>
        /// X coordinate.
        /// </summary>
        public int X;
        /// <summary>
        /// Y coordinate.
        /// </summary>
        public int Y;
    }
}