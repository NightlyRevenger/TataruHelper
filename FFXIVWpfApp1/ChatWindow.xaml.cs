// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



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
        public string FFXIVLanguague
        {
            get { return _AppLogic.FFXIVLanguague; }
            set { _AppLogic.FFXIVLanguague = value; }
        }

        public string TargetLanguage
        {
            get { return _AppLogic.TargetLanguage; }
            set { _AppLogic.TargetLanguage = value; }
        }

        public WebTranslator.TranslationEngine TranslationEngine
        {
            get { return _AppLogic.TranslationEngine; }
            set { _AppLogic.TranslationEngine = value; }
        }

        public ReadOnlyCollection<TranslatorLanguague> CurrentLanguages
        {
            get { return _AppLogic.CurrentLanguages; }
        }

        public int InsertSpaceCount { get; set; }

        public int LineBreakeHight { get; set; }

        public Color FontColor1
        {
            get
            {
                return _FontColor1.Color;
            }
            set
            {
                _FontColor1 = new SolidColorBrush(value);
            }
        }

        public Color FontColor2
        {
            get
            {
                return _FontColor2.Color;
            }
            set
            {
                _FontColor2 = new SolidColorBrush(value);
            }
        }

        public SolidColorBrush FormBackGround
        {
            get
            {
                return (SolidColorBrush)this.Background;
            }

            set
            {
                this.Background = value;
                if (_IsClickThrought)
                    MakeWindowClickThrought();
            }
        }

        public bool IsClickThrought
        {
            get
            {
                return _IsClickThrought;
            }

            set
            {
                _IsClickThrought = value;

                if (_IsClickThrought)
                {
                    MakeWindowClickThrought();
                }
                else
                {
                    MakeWindowClickbale();
                }
            }
        }

        public int ChatFontSize
        {
            get
            {
                return (int)Math.Round(ChatRtb.FontSize);
            }
            set
            {
                ChatRtb.FontSize = value;
            }
        }

        public AppLogic _AppLogic;

        private IntPtr ChatWinHandle;

        private WindowResizer _WindowResizer;

        private MouseHooker _MouseHooker;

        private Window _SettigsWindow;

        private bool _IsClickThrought;

        private SolidColorBrush _FontColor1;
        private SolidColorBrush _FontColor2;

        private int _CurrFontColor = -1;

        private int MaxTranslatedSentences;

        public ChatWindow(Window settigsWindow)
        {
            _WindowResizer = new WindowResizer(this);
            _SettigsWindow = settigsWindow;
            _SettigsWindow.Loaded += OnSettingsWindowLoaded;

            _FontColor1 = new SolidColorBrush(Colors.Black);
            _FontColor2 = new SolidColorBrush(Colors.White);

            LineBreakeHight = 1;
            InsertSpaceCount = 0;

            InitializeComponent();

            this.ShowInTaskbar = false;

            ChatRtb.AcceptsTab = true;

            _AppLogic = new AppLogic();

            TranslationEngine = 0;

            ChatRtb.BorderThickness = new Thickness(0);

            ChatRtb.Document.Blocks.Clear();

            MaxTranslatedSentences = GlobalSettings.MaxTranslatedSentencesCount;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _AppLogic.Start();

            _AppLogic.NewTransaltionEvent += ShowTransaltions;
            _AppLogic.FFXIVWindowEvent += OnFFXIVWindowEvent;
            _AppLogic.FFXIVProcessEventEvent += OnFFXIVProcessEventEvent;

            ChatWinHandle = (new WindowInteropHelper(this).Handle);

            _AppLogic.AddExclusionWindowHandler(ChatWinHandle);

            _MouseHooker = null;

            _MouseHooker = new MouseHooker();
            _MouseHooker.LowLevelMouseEvent += OnLowLevelMousEvent;
        }

        void ShowTransaltions(object sender, AppLogic.NewTranslationEventArgs e)
        {
            this.UIThread(() =>
            {
                for (int i = 0; i < e.Sentence.Count; i++)
                {
                    ShowTransaltedText(e.Sentence[i]);
                }
            });
        }

        void ShowTransaltedText(TranslatedMsg translatedMsg)
        {
            try
            {
                if (translatedMsg.OriginalText.Length >= 1 && translatedMsg.TranslatedText.Length <= 1)
                {
                    translatedMsg.TranslatedText = (string)_SettigsWindow.Resources["TranslationEngineError"];
                }

                translatedMsg.TranslatedText = translatedMsg.TranslatedText.Trim(new char[] { ' ' });

                ChatRtb.AppendText(Environment.NewLine);
                ChatRtb.CaretPosition = ChatRtb.CaretPosition.DocumentEnd;
                if (InsertSpaceCount > 0)
                {
                    string whiteSpaces = String.Empty;
                    for (int i = 0; i < InsertSpaceCount; i++)
                    {
                        whiteSpaces += " ";
                    }
                    ChatRtb.AppendText(whiteSpaces);
                }

                Paragraph p = (ChatRtb.Document.Blocks.LastBlock) as Paragraph;
                p.Margin = new Thickness(0, LineBreakeHight, 0, 0);

                SolidColorBrush tmpColor = Brushes.Red;
                if (_CurrFontColor < 0)
                    tmpColor = _FontColor1;
                else
                    tmpColor = _FontColor2;
                _CurrFontColor = _CurrFontColor * -1;

                int nameInd = 0;
                if ((nameInd = translatedMsg.TranslatedText.IndexOf(":")) > 0)
                {
                    string msgText = translatedMsg.TranslatedText;
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
                    tr.Text = translatedMsg.TranslatedText;

                    tr.ApplyPropertyValue(TextElement.ForegroundProperty, tmpColor);
                }

                ChatRtb.ScrollToEnd();
            }
            catch (Exception exc)
            {
                Logger.WriteLog(Convert.ToString(exc));
            }
        }

        void OnFFXIVProcessEventEvent(object sender, AppLogic.FFProcessEventArgs e)
        {
            this.UIThreadAsync(() =>
            {
                var _win = ((MainWindow)_SettigsWindow);
                if (e.IsRunning)
                    _win.FFStatusText.Content = ((string)_win.Resources["FFStatusTextFound"]) + " " + e.Text;
                else
                    _win.FFStatusText.Content = ((string)_win.Resources["FFStatusText"]);

            });
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

        void OnSettingsWindowLoaded(object sender, RoutedEventArgs e)
        {
            var tmpWin = (FFXIITataruHelper.MainWindow)sender;
            var tmp = new WindowInteropHelper(tmpWin).Handle;

            _AppLogic.AddExclusionWindowHandler(tmp);
        }

        void OnLowLevelMousEvent(object sender, MouseHooker.LowLevelMouseEventArgs e)
        {
            if (e.MouseMessages == MouseHooker.MouseMessages.WM_MOUSEWHEEL)
            {
                this.UIThreadAsync(() =>
                {
                    if (!IsClickThrought)
                    {
                        var bc = this.FormBackGround.Color;
                        if (bc.A == 0)
                        {
                            var data = e.MouseEventFlags.mouseData;
                            uint fxdData = ((data & 0xFFFF0000) >> 16);
                            uint realData = 0;

                            double MouseWheelScrollDelta = System.Windows.Forms.SystemInformation.MouseWheelScrollDelta;

                            string msg = String.Empty;

                            if (IsLowLevelMousOver(e.MouseEventFlags.pt))
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
            _WindowResizer.dragWindow();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _AppLogic.Stop();
            _AppLogic.Dispose();

            if (_MouseHooker != null)
                _MouseHooker.UnHook();
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            SaveWindowPositionAndSize();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SaveWindowPositionAndSize();
        }

        void SaveWindowPositionAndSize()
        {
            if (this.WindowState == WindowState.Normal)
            {
                GlobalSettings.ChatWinTop = this.Top;
                GlobalSettings.ChatWinLeft = this.Left;

                GlobalSettings.ChatWinHeight = this.Height;
                GlobalSettings.ChatWinWidth = this.Width;
            }
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
    }
}
