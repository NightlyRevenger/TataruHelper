// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using FFXIITataruHelper.EventArguments;
using FFXIITataruHelper.FFHandlers;
using FFXIITataruHelper.Translation;
using FFXIITataruHelper.WinUtils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FFXIITataruHelper
{
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window
    {
        private Window _SettigsWindow;

        private WindowResizer _WindowResizer;

        private MouseHooker _MouseHooker;

        private bool _IsClickThrought;

        private TataruModel _TataruModel;
        private TataruUIModel _TataruUIModel;

        public ChatWindow(Window settigsWindow, TataruModel tataruModel)
        {
            InitializeComponent();

            try
            {
                _TataruModel = tataruModel;
                _TataruUIModel = _TataruModel.TataruUIModel;
                InitTataruModel();

                _WindowResizer = new WindowResizer(this);

                _SettigsWindow = settigsWindow;

                this.ShowInTaskbar = false;

                ChatRtb.AcceptsTab = true;

                ChatRtb.BorderThickness = new Thickness(0);

                ChatRtb.Document.Blocks.Clear();
            }
            catch (Exception e)
            {
                Logger.WriteLog(e);
            }
        }

        void ShowTransaltedText(string translatedMsg, Color color)
        {
            try
            {
                translatedMsg = translatedMsg.Trim(new char[] { ' ' });

                ChatRtb.AppendText(Environment.NewLine);
                ChatRtb.CaretPosition = ChatRtb.CaretPosition.DocumentEnd;

                if (_TataruUIModel.ParagraphSpaceCount > 0)
                {
                    string whiteSpaces = String.Empty;
                    for (int i = 0; i < _TataruUIModel.ParagraphSpaceCount; i++)
                    {
                        whiteSpaces += " ";
                    }
                    ChatRtb.AppendText(whiteSpaces);
                }

                Paragraph p = (ChatRtb.Document.Blocks.LastBlock) as Paragraph;
                p.Margin = new Thickness(0, _TataruUIModel.LineBreakeHeight, 0, 0);

                SolidColorBrush tmpColor = new SolidColorBrush(color);

                int nameInd = 0;
                if ((nameInd = translatedMsg.IndexOf(":")) > 0)
                {
                    string msgText = translatedMsg;
                    string name = String.Empty;
                    string text = String.Empty;

                    name = msgText.Substring(0, nameInd);
                    text = msgText.Substring(nameInd, msgText.Length - nameInd);

                    TextRange tr1 = new TextRange(ChatRtb.Document.ContentEnd, ChatRtb.Document.ContentEnd);
                    tr1.Text = name;
                    tr1.ApplyPropertyValue(TextElement.ForegroundProperty, tmpColor);
                    tr1.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);

                    TextRange tr2 = new TextRange(ChatRtb.Document.ContentEnd, ChatRtb.Document.ContentEnd);
                    tr2.Text = text;
                    tr2.ApplyPropertyValue(TextElement.ForegroundProperty, tmpColor);
                    tr2.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
                }
                else
                {
                    TextRange tr = new TextRange(ChatRtb.Document.ContentEnd, ChatRtb.Document.ContentEnd);
                    tr.Text = translatedMsg;

                    tr.ApplyPropertyValue(TextElement.ForegroundProperty, tmpColor);
                }

                ChatRtb.ScrollToEnd();
            }
            catch (Exception exc)
            {
                Logger.WriteLog(Convert.ToString(exc));
            }
        }

        void ShowErorrText(TranslationArrivedEventArgs ea)
        {
            if (ea.ErrorCode == 1)
            {
                string text = ((string)_SettigsWindow.Resources["TranslationEngineSwitchMsg"]) + " " + Convert.ToString(_TataruUIModel.TranslationEngine);

                ShowTransaltedText(text, ea.Color);
            }
        }

        #region **UserActions.

        public void ClearChat()
        {
            this.UIThread(() =>
            {
                ChatRtb.Document.Blocks.Clear();
            });
        }

        #endregion

        #region **WindowEvents.

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _TataruModel.FFMemoryReader.AddExclusionWindowHandler((new WindowInteropHelper(this).Handle));
            _MouseHooker = null;

            //_MouseHooker = new MouseHooker();
            //_MouseHooker.LowLevelMouseEvent += OnLowLevelMousEvent;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_MouseHooker != null)
                _MouseHooker.UnHook();
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                var loc = _TataruUIModel.ChatWindowRectangle;

                loc.Y = (float)this.Top;
                loc.X = (float)this.Left;

                _TataruUIModel.ChatWindowRectangle = loc;//*/
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {

                var loc = _TataruUIModel.ChatWindowRectangle;

                loc.Width = (float)this.Width;
                loc.Height = (float)this.Height;

                _TataruUIModel.ChatWindowRectangle = loc;//*/
            }

            //SaveWindowPositionAndSize();
        }

        #endregion

        #region **UiEvents.

        private async Task OnChatFontSizeChange(IntegerValueChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                if (ea.NewValue != (int)Math.Round(ChatRtb.FontSize))
                {
                    ChatRtb.FontSize = ea.NewValue;
                }
            });
        }

        private async Task OnBackgroundColorChange(ColorChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                if (((SolidColorBrush)this.Background).Color != ea.NewColor)
                {
                    this.Background = new SolidColorBrush(ea.NewColor);

                    if (_TataruUIModel.IsChatClickThrough)
                        MakeWindowClickThrought();


                    if (((SolidColorBrush)this.Background).Color.A == 0)
                    {
                        try
                        {
                            if (_MouseHooker != null)
                            {
                                _MouseHooker.LowLevelMouseEvent -= OnLowLevelMousEvent;
                                _MouseHooker.Dispose();
                                _MouseHooker = null;
                            }

                            _MouseHooker = new MouseHooker();
                            _MouseHooker.LowLevelMouseEvent += OnLowLevelMousEvent;
                        }
                        catch (Exception e)
                        {
                            Logger.WriteLog(e);
                        }
                    }
                    else
                    {

                        if (_MouseHooker != null)
                        {
                            try
                            {
                                _MouseHooker.LowLevelMouseEvent -= OnLowLevelMousEvent;
                                _MouseHooker.Dispose();
                                _MouseHooker = null;
                            }
                            catch (Exception e)
                            {
                                Logger.WriteLog(e);
                            }
                        }
                    }//*/
                }
            });
        }

        private async Task OnChatWindowRectangleChanged(RectangleDValueChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                System.Drawing.RectangleD winRect = new System.Drawing.RectangleD(this.Left, this.Top, this.Width, this.Height);

                if (ea.NewValue.X < 2 || ea.NewValue.Y < 2 || ea.NewValue.Width < 2 || ea.NewValue.Height < 2)
                {
                    if (((TataruUIModel)ea.Sender).ChatWindowRectangle != winRect)
                        ((TataruUIModel)ea.Sender).ChatWindowRectangle = winRect;
                    return;
                }


                if (ea.NewValue != winRect)
                {
                    var newRect = ea.NewValue;

                    if (newRect.X != winRect.X && newRect.Y != winRect.Y)
                    {
                        this.Left = newRect.X;
                        this.Top = newRect.Y;
                    }

                    if (newRect.Width != winRect.Width && newRect.Height != winRect.Height)
                    {
                        this.Width = newRect.Width;
                        this.Height = newRect.Height;
                    }
                }
                //*/
            });
        }

        private async Task OnChatClickThroughChange(BooleanChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                if (ea.NewValue != _IsClickThrought)
                {
                    _IsClickThrought = ea.NewValue;
                    if (_IsClickThrought)
                        MakeWindowClickThrought();
                    else
                        MakeWindowClickbale();
                }
            });
        }

        private async Task OnChatAlwaysOnTopChange(BooleanChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                if (ea.NewValue != this.Topmost)
                {
                    this.Topmost = ea.NewValue;
                }
            });
        }

        private async Task OnFFWindowStateChange(WindowStateChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                if (ea.NewWindowState != ea.OldWindowState)
                {
                    if (ea.NewWindowState == WindowState.Minimized)
                        this.Hide();
                    else
                        this.Show();
                }
            });
        }

        private async Task OnTranslationArrived(TranslationArrivedEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                if (ea.ErrorCode == 0)
                {
                    ShowTransaltedText(ea.Text, ea.Color);
                }
                else
                {
                    ShowErorrText(ea);
                }
            });
        }

        #endregion

        #region **Initialization.

        void InitTataruModel()
        {
            var UIModel = _TataruModel.TataruUIModel;

            UIModel.ChatFontSizeChanged += OnChatFontSizeChange;

            UIModel.BackgroundColorChanged += OnBackgroundColorChange;

            //UIModel.ParagraphSpaceCountChanged += OnIntervalWidthChange;
            //UIModel.LineBreakeHeightChanged += OnLineBreakHeightChange;

            UIModel.ChatWindowRectangleChanged += OnChatWindowRectangleChanged;

            UIModel.IsChatClickThroughChanged += OnChatClickThroughChange;
            UIModel.IsChatAlwaysOnTopChanged += OnChatAlwaysOnTopChange;

            _TataruModel.FFMemoryReader.FFWindowStateChanged += OnFFWindowStateChange;


            _TataruModel.ChatProcessor.TranslationArrived += OnTranslationArrived;
        }

        #endregion

        #region **WindowResize.

        // for each rectangle, assign the following method to its MouseEnter event.
        private void DisplayResizeCursor(object sender, MouseEventArgs e)
        {
            _WindowResizer.displayResizeCursor(sender);
        }

        private void DisplayDragCursor(object sender, MouseEventArgs e)
        {
            _WindowResizer.DisplayDragCursor(sender);
        }

        // for each rectangle, assign the following method to its MouseLeave event.
        private void ResetCursor(object sender, MouseEventArgs e)
        {
            _WindowResizer.resetCursor();
        }

        // for each rectangle, assign the following method to its PreviewMouseDown event.
        private void Resize(object sender, MouseButtonEventArgs e)
        {
            _WindowResizer.resizeWindow(sender);
        }

        // finally, you may use the following method to enable dragging!
        private void Drag(object sender, MouseButtonEventArgs e)
        {
            _WindowResizer.dragWindow(sender, e);
        }

        #endregion

        #region **System.

        void MakeWindowClickThrought()
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                var style = Win32Interfaces.GetWindowLong(hwnd, Win32Interfaces.GWL_EXSTYLE);
                Win32Interfaces.SetWindowLong(hwnd, Win32Interfaces.GWL_EXSTYLE, style | Win32Interfaces.WS_EX_LAYERED | Win32Interfaces.WS_EX_TRANSPARENT);
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }
        }

        void MakeWindowClickbale()
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                var style = Win32Interfaces.GetWindowLong(hwnd, Win32Interfaces.GWL_EXSTYLE);
                Win32Interfaces.SetWindowLong(hwnd, Win32Interfaces.GWL_EXSTYLE, style ^ Win32Interfaces.WS_EX_LAYERED ^ Win32Interfaces.WS_EX_TRANSPARENT);
            }
            catch (Exception e)
            {
                Logger.WriteLog(Convert.ToString(e));
            }
        }

        async Task OnLowLevelMousEvent(LowLevelMouseEventArgs ea)
        {
            if (ea.MouseMessages == MouseHooker.MouseMessages.WM_MOUSEWHEEL)
            {
                await this.UIThreadAsync(() =>
                {
                    if (!_TataruUIModel.IsChatClickThrough)
                    {
                        var bc = ((SolidColorBrush)this.Background).Color;
                        if (bc.A == 0)
                        {
                            var data = ea.MouseEventFlags.mouseData;
                            uint fxdData = ((data & 0xFFFF0000) >> 16);
                            uint realData = 0;

                            double MouseWheelScrollDelta = System.Windows.Forms.SystemInformation.MouseWheelScrollDelta;

                            string msg = String.Empty;

                            if (IsLowLevelMousOver(ea.MouseEventFlags.pt))
                            {
                                if (fxdData < 32000)
                                {
                                    realData = fxdData;

                                    int res = (int)Math.Round(realData / MouseWheelScrollDelta) * 2;
                                    for (int i = 0; i < res; i++)
                                        ChatRtb.LineUp();
                                }
                                else
                                {
                                    realData = 65536 - fxdData;

                                    int res = (int)Math.Round(realData / MouseWheelScrollDelta) * 2;
                                    for (int i = 0; i < res; i++)
                                        ChatRtb.LineDown();
                                }
                            }
                        }
                    }
                });
            }
        }

        bool IsLowLevelMousOver(MouseHooker.POINT pt)
        {
            var point0 = PointToScreen(new System.Windows.Point(0, 0));

            double right = point0.X + this.Width;
            double bottom = point0.Y + this.Height;

            if (pt.x > point0.X && pt.x < right)
            {
                if (pt.y > point0.Y && pt.y < bottom)
                    return true;
            }

            return false;
        }

        void OnFFXIVWindowEvent(object sender, WindowState e)
        {
            this.UIThread(() =>
            {
                if (e == WindowState.Minimized)
                    this.Hide();
                else
                    this.Show();
            });
        }

        void Settings_Click(object sender, RoutedEventArgs e)
        {

            Helper.Unminimize(_SettigsWindow);

            _SettigsWindow.Visibility = Visibility.Visible;
            _SettigsWindow.Activate();
            _SettigsWindow.Focus();
        }

        void Exit_Click(object sender, RoutedEventArgs e)
        {
            ((MainWindow)_SettigsWindow).ShutDown();
        }

        #endregion
    }
}
