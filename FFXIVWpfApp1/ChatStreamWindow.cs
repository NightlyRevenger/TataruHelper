// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using FFXIITataruHelper.EventArguments;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace FFXIITataruHelper
{
    public partial class ChatStreamWindow : ChatWindow
    {
        public ChatStreamWindow(Window settigsWindow, TataruModel tataruModel) : base(settigsWindow, tataruModel)
        {
            this.AllowsTransparency = false;
            this.Topmost = false;
            this.BorderThickness = new Thickness(0);

            this.Title = (string)settigsWindow.Resources["StreamWindowName"];
        }

        #region **UiEvents.

        protected override async Task OnBackgroundColorChange(ColorChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                if (((SolidColorBrush)this.Background).Color != ea.NewColor)
                {
                    var tmpColor = ea.NewColor;
                    tmpColor.A = 255;

                    this.Background = new SolidColorBrush(tmpColor);

                    MakeWindowClickThrought();
                }
            });
        }

        protected override async Task OnChatWindowRectangleChanged(RectangleDValueChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                System.Drawing.RectangleD winRect = new System.Drawing.RectangleD(this.Left, this.Top, this.Width, this.Height);

                if (ea.NewValue != winRect)
                {
                    var newRect = ea.NewValue;

                    this.Left = newRect.X;
                    this.Top = newRect.Y;


                    this.Width = newRect.Width;
                    this.Height = newRect.Height;

                }
            });
        }

        protected override async Task OnChatClickThroughChange(BooleanChangeEventArgs ea)
        {
            await this.UIThreadAsync(() =>
            {
                MakeWindowClickThrought();
            });
        }

        protected override async Task OnChatAlwaysOnTopChange(BooleanChangeEventArgs ea)
        {
        }

        protected override async Task OnFFWindowStateChange(WindowStateChangeEventArgs ea)
        {
        }

        protected override async Task OnAutoHideChange(BooleanChangeEventArgs ea)
        {
        }

        #endregion

        #region **Initialization.

        void InitStreamerWindow()
        {
            ChatRtb.FontSize = _TataruUIModel.ChatFontSize;

            var tmpColor = _TataruUIModel.BackgroundColor;
            tmpColor.A = 255;
            this.Background = new SolidColorBrush(tmpColor);

            this.Left = _TataruUIModel.ChatWindowRectangle.X;
            this.Top = _TataruUIModel.ChatWindowRectangle.Y;

            this.Width = _TataruUIModel.ChatWindowRectangle.Width;
            this.Height = _TataruUIModel.ChatWindowRectangle.Height;

        }

        #endregion

        #region **WindowEvents.

        protected override void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _TataruModel.FFMemoryReader.AddExclusionWindowHandler((new WindowInteropHelper(this).Handle));

            InitStreamerWindow();

            MakeWindowClickThrought();
        }

        protected override void Window_LocationChanged(object sender, EventArgs e)
        {
        }

        protected override void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        protected override void Window_Deactivated(object sender, EventArgs e)
        {
            /*
            this.Show();
            this.Activate();
            Helper.Unminimize(this);
            this.Show();
            this.Activate();//*/
        }

        #endregion

        #region **System.

        protected override void AutoHideStatusCheck()
        {

        }

        #endregion

    }
}
