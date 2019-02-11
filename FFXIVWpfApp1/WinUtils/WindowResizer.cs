// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FFXIITataruHelper.WinUtils
{
    class WindowResizer
    {
        private const int WM_SYSCOMMAND = 0x112;
        private HwndSource hwndSource;
        Window activeWin;

        public WindowResizer(Window activeW)
        {
            try
            {
                activeWin = activeW;

                activeWin.SourceInitialized += new EventHandler(InitializeWindowSource);
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }
        }

        public void resetCursor()
        {
            try
            {
                if (Mouse.LeftButton != MouseButtonState.Pressed)
                {
                    activeWin.Cursor = Cursors.Arrow;
                }
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }
        }

        public void dragWindow(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (activeWin.WindowState == WindowState.Maximized)
                {
                    var point = activeWin.PointToScreen(e.MouseDevice.GetPosition(activeWin));

                    if (point.X <= activeWin.RestoreBounds.Width / 2)
                        activeWin.Left = 0;

                    else if (point.X >= activeWin.RestoreBounds.Width)
                        activeWin.Left = point.X - (activeWin.RestoreBounds.Width - (activeWin.ActualWidth - point.X));

                    else
                        activeWin.Left = point.X - (activeWin.RestoreBounds.Width / 2);

                    activeWin.Top = point.Y - (((FrameworkElement)sender).ActualHeight / 2);
                    activeWin.WindowState = WindowState.Normal;
                }

                activeWin.DragMove();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(Convert.ToString(ex));
            }
        }

        private void InitializeWindowSource(object sender, EventArgs e)
        {
            try
            {
                hwndSource = PresentationSource.FromVisual((Visual)sender) as HwndSource;

                hwndSource.AddHook(new HwndSourceHook(WndProc));
            }
            catch (Exception ex)
            {
                Logger.WriteLog(Convert.ToString(ex));
            }
        }

        IntPtr retInt = IntPtr.Zero;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            //Debug.WriteLine("WndProc messages: " + msg.ToString());
            //
            // Check incoming window system messages
            //
            if (msg == WM_SYSCOMMAND)
            {
                //Debug.WriteLine("WndProc messages: " + msg.ToString());
            }

            return IntPtr.Zero;
        }

        public enum ResizeDirection
        {
            Left = 1,
            Right = 2,
            Top = 3,
            TopLeft = 4,
            TopRight = 5,
            Bottom = 6,
            BottomLeft = 7,
            BottomRight = 8,
        }

        private void ResizeWindow(ResizeDirection direction)
        {
            try
            {
                Win32Interfaces.SendMessage(hwndSource.Handle, WM_SYSCOMMAND, (IntPtr)(61440 + direction), IntPtr.Zero);
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }
        }

        public void resizeWindow(object sender)
        {
            try
            {
                Rectangle clickedRectangle = sender as Rectangle;

                switch (clickedRectangle.Name)
                {
                    case "top":
                        activeWin.Cursor = Cursors.SizeNS;
                        ResizeWindow(ResizeDirection.Top);
                        break;
                    case "bottom":
                        activeWin.Cursor = Cursors.SizeNS;
                        ResizeWindow(ResizeDirection.Bottom);
                        break;
                    case "left":
                        activeWin.Cursor = Cursors.SizeWE;
                        ResizeWindow(ResizeDirection.Left);
                        break;
                    case "right":
                        activeWin.Cursor = Cursors.SizeWE;
                        ResizeWindow(ResizeDirection.Right);
                        break;
                    case "topLeft":
                        activeWin.Cursor = Cursors.SizeNWSE;
                        ResizeWindow(ResizeDirection.TopLeft);
                        break;
                    case "topRight":
                        activeWin.Cursor = Cursors.SizeNESW;
                        ResizeWindow(ResizeDirection.TopRight);
                        break;
                    case "bottomLeft":
                        activeWin.Cursor = Cursors.SizeNESW;
                        ResizeWindow(ResizeDirection.BottomLeft);
                        break;
                    case "bottomRight":
                        activeWin.Cursor = Cursors.SizeNWSE;
                        ResizeWindow(ResizeDirection.BottomRight);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }
        }

        public void displayResizeCursor(object sender)
        {
            try
            {
                Rectangle clickedRectangle = sender as Rectangle;

                switch (clickedRectangle.Name)
                {
                    case "top":
                        activeWin.Cursor = Cursors.SizeNS;
                        break;
                    case "bottom":
                        activeWin.Cursor = Cursors.SizeNS;
                        break;
                    case "left":
                        activeWin.Cursor = Cursors.SizeWE;
                        break;
                    case "right":
                        activeWin.Cursor = Cursors.SizeWE;
                        break;
                    case "topLeft":
                        activeWin.Cursor = Cursors.SizeNWSE;
                        break;
                    case "topRight":
                        activeWin.Cursor = Cursors.SizeNESW;
                        break;
                    case "bottomLeft":
                        activeWin.Cursor = Cursors.SizeNESW;
                        break;
                    case "bottomRight":
                        activeWin.Cursor = Cursors.SizeNWSE;
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }
        }

        public void DisplayDragCursor(object sender)
        {
            try
            {
                activeWin.Cursor = Cursors.Hand;
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }
        }

    }
}
