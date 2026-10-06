using FFXIVTataruHelper.Services.Logging;
using FFXIVTataruHelper.Utils;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace FFXIVTataruHelper.WinUtils
{
    class WindowResizer
    {
        private const int WM_SYSCOMMAND = 0x112;
        private HwndSource hwndSource;
        Window activeWin;
        private readonly IAppLogger _logger;

        public WindowResizer(Window activeW, IAppLogger logger)
        {
            try
            {
                activeWin = activeW;
                _logger = logger;

                activeWin.SourceInitialized += new EventHandler(InitializeWindowSource);

                if (WineEnvironment.IsRunning)
                {
                    _trackTimer = new DispatcherTimer(DispatcherPriority.Render, activeWin.Dispatcher)
                    {
                        Interval = TimeSpan.FromMilliseconds(15)
                    };
                    _trackTimer.Tick += (_, _) => TrackByHand();
                }
            }
            catch (Exception e)
            {
                _logger.WriteLog(Convert.ToString(e));
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
                _logger.WriteLog(Convert.ToString(e));
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

                if (WineEnvironment.IsRunning)
                {
                    StartTrackingByHand(null);
                    return;
                }

                activeWin.DragMove();
            }
            catch (Exception ex)
            {
                _logger.WriteLog(Convert.ToString(ex));
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
                _logger.WriteLog(Convert.ToString(ex));
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

        // Moving and resizing by hand, for Wine.
        //
        // On Windows both are the system's own loops, entered with
        // WM_SYSCOMMAND. Under Wine the chat window is kept out of the Linux
        // desktop's window manager so it can stay above the game, and that
        // costs it both loops: resizing wants the frame Wine reads as "managed
        // window", and a window that is activated - which the system move loop
        // may do - is handed to the manager for good and sinks under the game.
        // So the mouse is followed here instead.
        //
        // Not by its events. Under Wine a window gets no mouse events from
        // beyond its own edges - they go to whatever is under the cursor,
        // which here is the game - so a window being made larger lost the
        // cursor the moment it ran ahead of the edge, and grew by a few pixels
        // a pull. Tried on 2026-10-06; the player called it stiff. The cursor
        // and the button are asked for instead, a frame at a time, for as long
        // as the button is down. Wine knows both for the whole screen, since
        // the game's input and this window's go through the same wineserver.

        private DispatcherTimer _trackTimer;
        private ResizeDirection? _trackedEdge;
        private Point _trackStart;
        private HandTrackedBounds.Bounds _trackBounds;

        private const double SmallestSide = 60;

        private void StartTrackingByHand(ResizeDirection? edge)
        {
            if (_trackTimer == null)
            {
                return;
            }

            _trackedEdge = edge;
            _trackStart = CursorInDips();
            _trackBounds = new HandTrackedBounds.Bounds(activeWin.Left, activeWin.Top, activeWin.ActualWidth, activeWin.ActualHeight);
            _trackTimer.Start();
        }

        private void StopTrackingByHand()
        {
            _trackTimer.Stop();
            _trackedEdge = null;
            resetCursor();
        }

        private void TrackByHand()
        {
            if ((GetAsyncKeyState(VK_LBUTTON) & 0x8000) == 0)
            {
                StopTrackingByHand();
                return;
            }

            var now = CursorInDips();
            var dx = now.X - _trackStart.X;
            var dy = now.Y - _trackStart.Y;

            var bounds = _trackedEdge == null
                ? HandTrackedBounds.Moved(_trackBounds, dx, dy)
                : HandTrackedBounds.Resized(_trackBounds,
                    HorizontalSideOf(_trackedEdge.Value), VerticalSideOf(_trackedEdge.Value), dx, dy,
                    Math.Max(activeWin.MinWidth, SmallestSide), Math.Max(activeWin.MinHeight, SmallestSide));

            activeWin.Left = bounds.Left;
            activeWin.Top = bounds.Top;

            if (_trackedEdge != null)
            {
                activeWin.Width = bounds.Width;
                activeWin.Height = bounds.Height;
            }
        }

        private static int HorizontalSideOf(ResizeDirection edge)
        {
            switch (edge)
            {
                case ResizeDirection.Left:
                case ResizeDirection.TopLeft:
                case ResizeDirection.BottomLeft:
                    return -1;
                case ResizeDirection.Right:
                case ResizeDirection.TopRight:
                case ResizeDirection.BottomRight:
                    return 1;
                default:
                    return 0;
            }
        }

        private static int VerticalSideOf(ResizeDirection edge)
        {
            switch (edge)
            {
                case ResizeDirection.Top:
                case ResizeDirection.TopLeft:
                case ResizeDirection.TopRight:
                    return -1;
                case ResizeDirection.Bottom:
                case ResizeDirection.BottomLeft:
                case ResizeDirection.BottomRight:
                    return 1;
                default:
                    return 0;
            }
        }

        private const int VK_LBUTTON = 0x01;

        [StructLayout(LayoutKind.Sequential)]
        private struct CursorPoint
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out CursorPoint point);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int virtualKey);

        /// <summary>
        /// Where the cursor is on the screen, in the units Left and Top are
        /// in - device pixels turned back into the window's own.
        /// </summary>
        private Point CursorInDips()
        {
            GetCursorPos(out var cursor);
            var onScreen = new Point(cursor.X, cursor.Y);
            var fromDevice = hwndSource?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
            return fromDevice.Transform(onScreen);
        }

        private void ResizeWindow(ResizeDirection direction)
        {
            try
            {
                if (WineEnvironment.IsRunning)
                {
                    StartTrackingByHand(direction);
                    return;
                }

                Win32Interfaces.SendMessage(hwndSource.Handle, WM_SYSCOMMAND, (IntPtr)(61440 + direction), IntPtr.Zero);
            }
            catch (Exception e)
            {
                _logger.WriteLog(Convert.ToString(e));
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
                _logger.WriteLog(Convert.ToString(e));
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
                _logger.WriteLog(Convert.ToString(e));
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
                _logger.WriteLog(Convert.ToString(e));
            }
        }

    }
}
