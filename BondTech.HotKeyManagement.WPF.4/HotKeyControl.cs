// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com



/*The Original source code of this control and it's style in Generic.Xaml is by
 * Paul Middlemiss - http://www.silverlightshow.net/items/Create-a-Custom-Control-Inheriting-from-TextBox.aspx
 * who licensed and released this control and it's style under the Creative Commons license
 * visit http://creativecommons.org/licenses/by/3.0/us for more information
 * Feel free to use this control and style with proper attribution.
 * The rest of the code and modification is by Bond - http://www.codeproject.com/Members/bonded, 
 * and well, licensed under CODE PROJECT OPEN LICENSE where you downloaded this project. :)
 *  */
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Windows.Interop;

namespace BondTech.HotKeyManagement.WPF._4
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:BondTech.HotKeyManagement.WPF._4"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:BondTech.HotKeyManagement.WPF._4;assembly=BondTech.HotKeyManagement.WPF._4"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:HotKeyControl/>
    ///
    /// </summary>
    [StyleTypedProperty(Property = "TextRemoverStyle", StyleTargetType = typeof(Button)),
    StyleTypedProperty(Property = "WatermarkStyle", StyleTargetType = typeof(TextBlock)),
    TemplatePart(Name = "TextRemover", Type = typeof(Button)),
    TemplatePart(Name = "Watermark", Type = typeof(TextBlock)),
    TemplateVisualState(Name = "WatermarkVisible", GroupName = "WatermarkStates"),
    TemplateVisualState(Name = "WatermarkHidden", GroupName = "WatermarkStates"),
    TemplateVisualState(Name = "TextRemoverVisible", GroupName = "TextRemoverStates"),
    TemplateVisualState(Name = "TextRemoverHidden", GroupName = "TextRemoverStates")]
    [DefaultProperty("ForceModifiers"), DefaultEvent("HotKeyIsSet")]
    public class HotKeyControl : TextBox
    {
        #region **Properties.
        HwndSource hwndSource;
        HwndSourceHook hook;

        /// <summary>Identifies the Watermark text dependency property
        /// </summary>
        public static readonly DependencyProperty WatermarkProperty = DependencyProperty.Register(
            "Watermark",
            typeof(string),
            typeof(HotKeyControl),
            new PropertyMetadata("Enter HotKey Here"));

        /// <summary>Identifies the Watermark foreground dependency property
        /// </summary>
        public static readonly DependencyProperty WatermarkForegroundProperty = DependencyProperty.Register(
            "WatermarkForeground",
            typeof(Brush),
            typeof(HotKeyControl),
            new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 134, 134, 134))));

        /// <summary>Identifies the Text remover tool tip dependency property
        /// </summary>
        public static readonly DependencyProperty TextRemoverToolTipProperty = DependencyProperty.Register(
            "TextRemoverToolTip",
            typeof(string),
            typeof(HotKeyControl),
            new PropertyMetadata("Reset HotKey"));

        /// <summary>Immediate text update dependency property
        /// </summary>
        public static readonly DependencyProperty IsUpdateImmediateProperty = DependencyProperty.Register(
            "IsUpdateImmediate",
            typeof(bool),
            typeof(HotKeyControl),
            new PropertyMetadata(false));

        /// <summary>Identifies the Watermark style dependency property
        /// </summary>
        public static readonly DependencyProperty WatermarkStyleProperty = DependencyProperty.Register(
            "WatermarkStyle",
            typeof(Style),
            typeof(HotKeyControl),
            new PropertyMetadata(null));

        /// <summary>Identifies the Text remover style dependency property.
        /// </summary>
        public static readonly DependencyProperty TextRemoverStyleProperty = DependencyProperty.Register(
            "TextRemoverStyle",
            typeof(Style),
            typeof(HotKeyControl),
            new PropertyMetadata(null));

        /// <summary>Identifies the HotKey control ForceModifiers dependency property.
        /// </summary>
        public static readonly DependencyProperty ForceModifiersProperty = DependencyProperty.Register(
            "ForceModifiers",
            typeof(Boolean),
            typeof(HotKeyControl),
            new PropertyMetadata(true));


        /// <summary>Gets or sets the text remover tool tip. This is a dependency property.
        /// </summary>
        /// <value>The text remover tool tip.</value>
        [Category("Watermark"), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Description("Gets or sets the text for the watermark"), Browsable(false)]
        public string TextRemoverToolTip
        {
            get { return (string)GetValue(TextRemoverToolTipProperty); }
            set { SetValue(TextRemoverToolTipProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether bindings on the Text property updates
        /// as soon as the text change. This is a dependency property.
        /// </summary>
        /// <value>If true then TextChanges fires whenever the text changes, else only on LostFocus</value>
        [Category("Watermark")]
        [Description("Gets or sets a value indicating whether the binding source is updated immediately as text changes, or on LostFocus")]
        public bool IsUpdateImmediate
        {
            get { return (bool)GetValue(IsUpdateImmediateProperty); }
            set { SetValue(IsUpdateImmediateProperty, value); }
        }

        /// <summary>Gets or sets the text remover style. This is a dependency property.
        /// </summary>
        /// <value>The text remover style.</value>
        [Category("Watermark")]
        [Description("Gets or sets the style for the remove-text button")]
        public Style TextRemoverStyle
        {
            get { return (Style)GetValue(TextRemoverStyleProperty); }
            set { SetValue(TextRemoverStyleProperty, value); }
        }

        /// <summary>Gets or sets the watermark foreground. This is a dependency property.
        /// </summary>
        /// <value>The watermark foreground.</value>
        [Description("Gets or sets the foreground brush for the watermark")]
        public Brush WatermarkForeground
        {
            get { return (Brush)GetValue(WatermarkForegroundProperty); }
            set { SetValue(WatermarkForegroundProperty, value); }
        }

        /// <summary>Gets or sets the watermark. This is a dependency property.
        /// </summary>
        /// <value>The watermark.</value>
        [Description("Gets or sets the watermark")]
        [Category("Watermark"), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Watermark
        {
            get { return (string)GetValue(WatermarkProperty); }
            set { SetValue(WatermarkProperty, value); }
        }

        /// <summary>Gets or sets the watermark style. This is a dependency property.
        /// </summary>
        /// <value>The watermark style.</value>
        [Description("Gets or sets the watermark style")]
        [Category("Watermark")]
        public Style WatermarkStyle
        {
            get { return (Style)GetValue(WatermarkStyleProperty); }
            set { SetValue(WatermarkStyleProperty, value); }
        }

        /// <summary>Gets or sets the text content of the HotKey control.
        /// </summary>
        [Browsable(false), Category("Text"), Description("Gets or sets the text content of the HotKey Control.")]
        public new string Text
        {
            get
            { return base.Text; }
            set
            { base.Text = value; }
        }

        /// <summary>Gets or sets a value specifying that the user should be forced to enter modifiers. This is a dependency property.
        /// </summary>
        [Bindable(true), EditorBrowsable(EditorBrowsableState.Always), Browsable(true)]
        [Description("Gets or sets a value specifying that the user be forced to enter modifiers.")]
        public bool ForceModifiers
        {
            get { return (bool)GetValue(ForceModifiersProperty); }
            set { SetValue(ForceModifiersProperty, value); }
        }

        /// <summary>Returns the key set by the user.
        /// </summary>
        [Browsable(false)]
        public Key UserKey
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(this.Text) && this.Text != Key.None.ToString())
                {
                    return (Key)HotKeyShared.ParseShortcut(this.Text).GetValue(1);
                }
                return Key.None;
            }
        }

        /// <summary>Returns the Modifier set by the user.
        /// </summary>
        [Browsable(false)]
        public ModifierKeys UserModifier
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(this.Text) && this.Text != Key.None.ToString())
                {
                    return (ModifierKeys)HotKeyShared.ParseShortcut(this.Text).GetValue(0);
                }
                return ModifierKeys.None;
            }
        }

        private Button textRemoverButton;
        private bool isFocused;
        #endregion

        #region **Events
        public static readonly RoutedEvent HotKeyIsSetEvent = EventManager.RegisterRoutedEvent(
            "HotKeyIsSet", RoutingStrategy.Bubble, typeof(HotKeyIsSetEventHandler), typeof(HotKeyControl));

        [Category("Behaviour")]
        public event HotKeyIsSetEventHandler HotKeyIsSet
        {
            add { AddHandler(HotKeyIsSetEvent, value); }
            remove { RemoveHandler(HotKeyIsSetEvent, value); }
        }
        #endregion

        #region **Constructor.
        static HotKeyControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HotKeyControl), new FrameworkPropertyMetadata(typeof(HotKeyControl)));
        }

        public HotKeyControl()
        {
            this.GotFocus += this.TextBoxGotFocus;
            this.LostFocus += this.TextBoxLostFocus;
            this.TextChanged += this.TextBoxTextChanged;
            this.PreviewKeyDown += this.TextBoxKeyDown;

            this.hook = new HwndSourceHook(WndProc);
            this.ContextMenu = null; //Disable shortcuts.
            this.IsReadOnly = true;
            this.AllowDrop = false;
        }
        #endregion

        #region **Helpers
        /// <summary>When overridden in a derived class, is invoked whenever application code or internal processes (such as a rebuilding layout pass) call <see cref="M:System.Windows.Controls.Control.ApplyTemplate"/>.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // remove old button handler
            if (null != this.textRemoverButton)
            {
                this.textRemoverButton.Click -= this.TextRemoverClick;
            }

            // add new button handler
            this.textRemoverButton = GetTemplateChild("TextRemover") as Button;
            if (null != this.textRemoverButton)
            {
                this.textRemoverButton.Click += this.TextRemoverClick;
            }

            this.UpdateState();
        }

        private void UpdateState()
        {
            if (string.IsNullOrEmpty(this.Text))
            {
                VisualStateManager.GoToState(this, "TextRemoverHidden", true);
                if (!this.isFocused)
                {
                    VisualStateManager.GoToState(this, "WatermarkVisible", true);
                }
            }
            else
            {
                VisualStateManager.GoToState(this, "TextRemoverVisible", true);
                VisualStateManager.GoToState(this, "WatermarkHidden", false);
            }
        }

        private void TextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            this.UpdateState();

            if (!this.IsUpdateImmediate)
            {
                return;
            }

            BindingExpression binding = this.GetBindingExpression(TextBox.TextProperty);
            if (null != binding)
            {
                binding.UpdateSource();
            }
        }

        private void TextRemoverClick(object sender, RoutedEventArgs e)
        {
            this.Text = string.Empty;
            this.isFocused = false;
            UpdateState();
        }

        private void TextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            this.hwndSource = (HwndSource)HwndSource.FromVisual(this); // new WindowInteropHelper(window).Handle // If the InPtr is needed.
            this.hwndSource.AddHook(hook);

            VisualStateManager.GoToState(this, "WatermarkHidden", false);

            this.isFocused = true;
            this.UpdateState();
        }

        private void TextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            this.hwndSource.RemoveHook(hook);
            this.isFocused = false;
            this.UpdateState();
        }

        private void TextBoxKeyDown(object sender, KeyEventArgs e)
        {
            Microsoft.VisualBasic.Devices.Keyboard UserKeyBoard = new Microsoft.VisualBasic.Devices.Keyboard();
            bool AltPressed = UserKeyBoard.AltKeyDown;
            bool ControlPressed = UserKeyBoard.CtrlKeyDown;
            bool ShiftPressed = UserKeyBoard.ShiftKeyDown;

            ModifierKeys LocalModifier = ModifierKeys.None;
            if (AltPressed) { LocalModifier = ModifierKeys.Alt; }
            if (ControlPressed) { LocalModifier |= ModifierKeys.Control; }
            if (ShiftPressed) { LocalModifier |= ModifierKeys.Shift; }

            switch (e.Key)
            {
                case System.Windows.Input.Key.Back:
                    this.Text = CheckModifier(LocalModifier) ? HotKeyShared.CombineShortcut(LocalModifier, Key.Back) : "";
                    e.Handled = true;
                    break;

                case System.Windows.Input.Key.Space:
                    this.Text = CheckModifier(LocalModifier) ? HotKeyShared.CombineShortcut(LocalModifier, Key.Space) : "";
                    e.Handled = true;
                    break;

                case System.Windows.Input.Key.Delete:
                    this.Text = CheckModifier(LocalModifier) ? HotKeyShared.CombineShortcut(LocalModifier, Key.Delete) : "";
                    e.Handled = true;
                    break;

                case System.Windows.Input.Key.Home:
                    this.Text = CheckModifier(LocalModifier) ? HotKeyShared.CombineShortcut(LocalModifier, Key.Home) : "";
                    e.Handled = true;
                    break;

                case System.Windows.Input.Key.PageUp:
                    this.Text = CheckModifier(LocalModifier) ? HotKeyShared.CombineShortcut(LocalModifier, Key.PageUp) : "";
                    e.Handled = true;
                    break;

                case System.Windows.Input.Key.Next:
                    this.Text = CheckModifier(LocalModifier) ? HotKeyShared.CombineShortcut(LocalModifier, Key.Next) : "";
                    e.Handled = true;
                    break;

                case System.Windows.Input.Key.End:
                    this.Text = CheckModifier(LocalModifier) ? HotKeyShared.CombineShortcut(LocalModifier, Key.End) : "";
                    break;

                case System.Windows.Input.Key.Up:
                    this.Text = CheckModifier(LocalModifier) ? HotKeyShared.CombineShortcut(LocalModifier, Key.Up) : "";
                    break;

                case System.Windows.Input.Key.Down:
                    this.Text = CheckModifier(LocalModifier) ? HotKeyShared.CombineShortcut(LocalModifier, Key.Down) : "";
                    break;

                case System.Windows.Input.Key.Right:
                    this.Text = CheckModifier(LocalModifier) ? HotKeyShared.CombineShortcut(LocalModifier, Key.Right) : "";
                    break;

                case System.Windows.Input.Key.Left:
                    this.Text = CheckModifier(LocalModifier) ? HotKeyShared.CombineShortcut(LocalModifier, Key.Left) : "";
                    break;
            }
        }

        private bool CheckModifier(ModifierKeys modifier)
        {
            if (modifier == ModifierKeys.None && ForceModifiers)
            {
                this.Text = "";
                this.isFocused = false;
                this.UpdateState();
                System.Media.SystemSounds.Asterisk.Play();
                MessageBox.Show("You have to specify a modifier like 'Control', 'Alt' or 'Shift'");
                return false;
            }

            return true;
        }
        #endregion

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            try
            {
                Key KeyPressed = (Key)wParam;

                Microsoft.VisualBasic.Devices.Keyboard UserKeyBoard = new Microsoft.VisualBasic.Devices.Keyboard();
                bool AltPressed = UserKeyBoard.AltKeyDown;
                bool ControlPressed = UserKeyBoard.CtrlKeyDown;
                bool ShiftPressed = UserKeyBoard.ShiftKeyDown;

                ModifierKeys LocalModifier = ModifierKeys.None;
                if (AltPressed) { LocalModifier = ModifierKeys.Alt; }
                if (ControlPressed) { LocalModifier |= ModifierKeys.Control; }
                if (ShiftPressed) { LocalModifier |= ModifierKeys.Shift; }

                switch ((KeyboardMessages)msg) //-V3002
                {
                    case KeyboardMessages.WmSyskeydown:
                    case KeyboardMessages.WmKeydown:
                        switch (KeyPressed)
                        {
                            case Key.Control:
                            case Key.ControlKey:
                            case Key.LControlKey:
                            case Key.RControlKey:
                            case Key.Shift:
                            case Key.ShiftKey:
                            case Key.LShiftKey:
                            case Key.RShiftKey:
                            case Key.Alt:
                            case Key.Menu:
                            case Key.LMenu:
                            case Key.RMenu:
                            case Key.LWin:
                                return IntPtr.Zero;

                            //case Keys.Back:
                            //    this.Text = Keys.None.ToString();
                            //    return IntPtr.Zero;
                        }

                        if (LocalModifier != ModifierKeys.None)
                        {
                            this.Text = HotKeyShared.CombineShortcut(LocalModifier, KeyPressed);
                        }
                        else
                        {
                            if (ForceModifiers)
                            {
                                this.Text = "";
                                this.isFocused = false;
                                this.UpdateState();
                                System.Media.SystemSounds.Asterisk.Play();
                                MessageBox.Show("You have to specify a modifier like 'Control', 'Alt' or 'Shift'");
                            }
                            else
                            { this.Text = KeyPressed.ToString(); }
                        }
                        return IntPtr.Zero; ;

                    case KeyboardMessages.WmSyskeyup:
                    case KeyboardMessages.WmKeyup:
                        if (!String.IsNullOrWhiteSpace(Text.Trim()) || this.Text != Key.None.ToString())
                        {
                            if (HotKeyIsSetEvent != null)
                            {
                                var e = new HotKeyIsSetEventArgs(HotKeyIsSetEvent, UserKey, UserModifier);
                                base.RaiseEvent(e);
                                if (e.Cancel)
                                {
                                    this.Text = "";
                                    isFocused = false;
                                    UpdateState();
                                }
                            }
                        }
                        return IntPtr.Zero;
                }
            }
            catch (OverflowException) { }

            return IntPtr.Zero;
        }
    }
}