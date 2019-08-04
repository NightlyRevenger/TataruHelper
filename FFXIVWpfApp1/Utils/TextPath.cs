using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FFXIITataruHelper.Utils
{
    /// <summary>
    /// This class generates a Geometry from a block of text in a specific font, weight, etc.
    /// and renders it to WPF as a shape.
    /// </summary>
    public class TextPath : Shape
    {
        private Geometry _textGeometry;

        #region Dependency Properties
        public static readonly DependencyProperty FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner(typeof(TextPath),
                                                     new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily,
                                                             FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.Inherits,
                                                             OnPropertyChanged));
        [Bindable(true), Category("Appearance")]
        [Localizability(LocalizationCategory.Font)]
        [TypeConverter(typeof(FontFamilyConverter))]
        public FontFamily FontFamily { get { return (FontFamily)GetValue(FontFamilyProperty); } set { SetValue(FontFamilyProperty, value); } }

        public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(typeof(TextPath),
                                                        new FrameworkPropertyMetadata(SystemFonts.MessageFontSize,
                                                             FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
                                                             OnPropertyChanged));
        [Bindable(true), Category("Appearance")]
        [TypeConverter(typeof(FontSizeConverter))]
        [Localizability(LocalizationCategory.None)]
        public double FontSize { get { return (double)GetValue(FontSizeProperty); } set { SetValue(FontSizeProperty, value); } }

        public static readonly DependencyProperty FontStretchProperty = TextElement.FontStretchProperty.AddOwner(typeof(TextPath),
                                                     new FrameworkPropertyMetadata(TextElement.FontStretchProperty.DefaultMetadata.DefaultValue,
                                                             FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.Inherits,
                                                             OnPropertyChanged));
        [Bindable(true), Category("Appearance")]
        [TypeConverter(typeof(FontStretchConverter))]
        public FontStretch FontStretch { get { return (FontStretch)GetValue(FontStretchProperty); } set { SetValue(FontStretchProperty, value); } }

        public static readonly DependencyProperty FontStyleProperty = TextElement.FontStyleProperty.AddOwner(typeof(TextPath),
                                                     new FrameworkPropertyMetadata(SystemFonts.MessageFontStyle,
                                                             FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.Inherits,
                                                             OnPropertyChanged));
        [Bindable(true), Category("Appearance")]
        [TypeConverter(typeof(FontStyleConverter))]
        public FontStyle FontStyle { get { return (FontStyle)GetValue(FontStyleProperty); } set { SetValue(FontStyleProperty, value); } }

        public static readonly DependencyProperty FontWeightProperty = TextElement.FontWeightProperty.AddOwner(typeof(TextPath),
                                                     new FrameworkPropertyMetadata(SystemFonts.MessageFontWeight,
                                                             FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.Inherits,
                                                             OnPropertyChanged));
        [Bindable(true), Category("Appearance")]
        [TypeConverter(typeof(FontWeightConverter))]
        public FontWeight FontWeight { get { return (FontWeight)GetValue(FontWeightProperty); } set { SetValue(FontWeightProperty, value); } }

        public static readonly DependencyProperty OriginPointProperty =
                                        DependencyProperty.Register("Origin", typeof(Point), typeof(TextPath),
                                                new FrameworkPropertyMetadata(new Point(0, 0),
                                                        FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
                                                             OnPropertyChanged));
        [Bindable(true), Category("Appearance")]
        [TypeConverter(typeof(PointConverter))]
        public Point Origin { get { return (Point)GetValue(OriginPointProperty); } set { SetValue(OriginPointProperty, value); } }

        public static readonly DependencyProperty TextProperty =
                                        DependencyProperty.Register("Text", typeof(string), typeof(TextPath),
                                                new FrameworkPropertyMetadata(string.Empty,
                                                        FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
                                                             OnPropertyChanged));
        [Bindable(true), Category("Appearance")]
        public string Text { get { return (string)GetValue(TextProperty); } set { SetValue(TextProperty, value); } }
        #endregion

        protected override Geometry DefiningGeometry => _textGeometry ?? Geometry.Empty;

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((TextPath)d).CreateTextGeometry();

        private void CreateTextGeometry()
        {
            var formattedText = new FormattedText(Text, Thread.CurrentThread.CurrentUICulture, FlowDirection.LeftToRight,
                                    new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize, Brushes.Black);
            _textGeometry = formattedText.BuildGeometry(Origin);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (_textGeometry == null) CreateTextGeometry();
            if (_textGeometry.Bounds == Rect.Empty)
                return new Size(0, 0);
            // return the desired size
            return new Size(Math.Min(availableSize.Width, _textGeometry.Bounds.Width), Math.Min(availableSize.Height, _textGeometry.Bounds.Height));
        }
    }
}
