using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WpfXamlExtensions
{
    /// <summary>
    /// Interaction logic for CircularProgressBar.xaml
    /// </summary>
    public partial class CircularProgressBar
    {
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(int), typeof(CircularProgressBar), new UIPropertyMetadata(1));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(int), typeof(CircularProgressBar), new UIPropertyMetadata(1));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(CircularProgressBar), new UIPropertyMetadata(100));

        #region Fields

        private readonly DispatcherTimer _animationTimer;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CircularProgressBar"/> class.
        /// </summary>
        public CircularProgressBar()
        {
            InitializeComponent();

            IsVisibleChanged += OnVisibleChanged;

            _animationTimer = new DispatcherTimer(DispatcherPriority.ContextIdle, Dispatcher)
            {
                Interval = new TimeSpan(0, 0, 0, 0, 75)
            };
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the minimum.
        /// </summary>
        /// <value>The minimum.</value>
        public int Minimum
        {
            get { return (int)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        /// <summary>
        /// Gets or sets the maximum.
        /// </summary>
        /// <value>The maximum.</value>
        public int Maximum
        {
            get { return (int)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        #endregion

        /// <summary>
        /// Sets the position.
        /// </summary>
        /// <param name="ellipse">The ellipse.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="posOffSet">The pos off set.</param>
        /// <param name="step">The step to change.</param>
        private static void SetPosition(DependencyObject ellipse, double offset, double posOffSet, double step)
        {
            ellipse.SetValue(Canvas.LeftProperty, 50 + (Math.Sin(offset + (posOffSet * step)) * 50));
            ellipse.SetValue(Canvas.TopProperty, 50 + (Math.Cos(offset + (posOffSet * step)) * 50));
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        private void Start()
        {
            _animationTimer.Tick += OnAnimationTick;
            _animationTimer.Start();
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        private void Stop()
        {
            _animationTimer.Stop();
            _animationTimer.Tick -= OnAnimationTick;
        }

        /// <summary>
        /// Handles the animation tick.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void OnAnimationTick(object sender, EventArgs e)
        {
            _spinnerRotate.Angle = (_spinnerRotate.Angle + 36) % 360;
        }

        /// <summary>
        /// Handles the loaded.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void OnCanvasLoaded(object sender, RoutedEventArgs e)
        {
            const double offset = Math.PI;
            const double step = Math.PI * 2 / 10.0;

            SetPosition(_circle0, offset, 0.0, step);
            SetPosition(_circle1, offset, 1.0, step);
            SetPosition(_circle2, offset, 2.0, step);
            SetPosition(_circle3, offset, 3.0, step);
            SetPosition(_circle4, offset, 4.0, step);
            SetPosition(_circle5, offset, 5.0, step);
            SetPosition(_circle6, offset, 6.0, step);
            SetPosition(_circle7, offset, 7.0, step);
            SetPosition(_circle8, offset, 8.0, step);
        }

        /// <summary>
        /// Handles the unloaded.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void OnCanvasUnloaded(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        /// <summary>
        /// Handles the visible changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var isVisible = (bool)e.NewValue;

            if (isVisible)
            {
                Start();
            }
            else
            {
                Stop();
            }
        }
    }

}
