using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using FFXIVTataruHelper.FFHandlers;
using FFXIVTataruHelper.Services.GameMemory;
using FFXIVTataruHelper.Services.UI;

namespace FFXIVTataruHelper
{
    /// <summary>
    /// A copy of the game's dialogue box, holding the translation, put over the
    /// real one.
    ///
    /// Drawn rather than the game's own box being written into: nothing here
    /// touches the game, and the copy simply sits on top of it, following where
    /// the client says the box is and going away when it does.
    /// </summary>
    internal sealed class DialogueOverlayWindow : Window
    {
        /// <summary>
        /// How often the box is asked about. The game moves it only when the
        /// player drags it, but it appears and disappears constantly, and a
        /// copy that lingers after a line is dismissed is the thing a reader
        /// would notice first.
        /// </summary>
        private static readonly TimeSpan FollowInterval = TimeSpan.FromMilliseconds(50);

        private readonly IFFMemoryReaderService _memoryReader;
        private readonly Func<IntPtr> _gameWindow;
        private readonly DispatcherTimer _timer;

        private readonly TextBlock _speaker;
        private readonly TextBlock _line;
        private readonly Border _box;
        private readonly Border _plate;
        private readonly ImageBrush _frame;

        /// <summary>The dark ground a subtitle is laid on, which the game gives it none of.</summary>
        private readonly Brush _subtitleGround;

        /// <summary>
        /// Whether the copy is on screen and what it is dressed as. Kept out of
        /// the window's own fields so the deciding can be checked without the
        /// window, the way the placement and the hold are.
        /// </summary>
        private readonly DialogueOverlayPresentation _presentation = new DialogueOverlayPresentation();

        private readonly DialogueOverlayHold _hold = new DialogueOverlayHold();

        /// <summary>
        /// Whether the game's box is opening, sitting still or closing, which
        /// decides whether the copy follows it or comes off.
        /// </summary>
        private readonly DialogueOverlayMotion _motion = new DialogueOverlayMotion();

        private string _speakerText = string.Empty;
        private string _lineText = string.Empty;

        /// <summary>
        /// The line this copy was put out for, reduced to its words - what it
        /// is checked against while the game keeps talking. Empty until the
        /// copy knows what line it is, which is also how it asks to be shown:
        /// a line nobody has named cannot yet be taken for a stale one.
        /// </summary>
        private string _shownLineKey = string.Empty;

        public DialogueOverlayWindow(IFFMemoryReaderService memoryReader, Func<IntPtr> gameWindow)
        {
            _memoryReader = memoryReader;
            _gameWindow = gameWindow;

            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            IsHitTestVisible = false;
            ShowActivated = false;
            Visibility = Visibility.Hidden;

            _speaker = new TextBlock
            {
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            _line = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(0x2A, 0x24, 0x1C)),
                TextWrapping = TextWrapping.Wrap,

                // Said outright: stretched to fill the frame the text came out
                // sitting near the bottom of it, where the game starts it just
                // under the name.
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            // The game's own frame, lifted from it: the shape has notches, a
            // shadow and an embossed rim that a drawn rectangle only ever
            // approximates. Stretched whole rather than cut into corners and
            // edges, because the window is always the same design size and only
            // the interface scale changes it - so the proportions never move.
            _frame = new ImageBrush(
                new BitmapImage(new Uri("pack://application:,,,/Resources/DialogueFrame.png")))
            {
                Stretch = Stretch.Fill
            };

            // What a subtitle is laid on. The game gives its subtitles no
            // ground at all, so the copy has to bring one: without it the
            // translation sits on top of the original and the two read through
            // each other.
            //
            // Dark across the middle and fading out at both ends, rather than a
            // bar with edges. The strip is the full width of the screen and the
            // line is centred in it, so the fade happens well clear of any
            // letters, and what the player sees is a shadow gathering behind
            // the words instead of a black band drawn over the cutscene.
            var subtitleGround = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0)
            };
            subtitleGround.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0x08, 0x08, 0x0A), 0));
            subtitleGround.GradientStops.Add(new GradientStop(Color.FromArgb(0xD0, 0x08, 0x08, 0x0A), 0.10));
            subtitleGround.GradientStops.Add(new GradientStop(Color.FromArgb(0xD0, 0x08, 0x08, 0x0A), 0.90));
            subtitleGround.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0x08, 0x08, 0x0A), 1));
            _subtitleGround = subtitleGround;

            var box = new Border
            {
                Background = _frame,
                Child = _line
            };

            _box = box;

            // The plate sits over the box's top-left corner rather than above
            // it in a row of its own. Given its own row it took height from the
            // box, which then stopped short of the game's frame and left a
            // strip of the original showing along the top - including the name
            // in the language being translated away.
            // The game does not put the name in a box. It lays it on a dark
            // strip that fades away to the right, so the strip ends wherever
            // the name does without ever showing an edge.
            var plateWash = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0)
            };
            plateWash.GradientStops.Add(new GradientStop(Color.FromArgb(0xE6, 0x14, 0x12, 0x10), 0));
            plateWash.GradientStops.Add(new GradientStop(Color.FromArgb(0xE0, 0x14, 0x12, 0x10), 0.62));
            plateWash.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0x14, 0x12, 0x10), 1));

            _plate = new Border
            {
                Background = plateWash,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Child = _speaker
            };

            var layout = new Grid();
            layout.Children.Add(box);
            layout.Children.Add(_plate);

            Content = layout;

            _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = FollowInterval };
            _timer.Tick += (_, __) => Follow();
        }

        /// <summary>
        /// The line to show, as it was put on the chat window, and where it
        /// came from - the line as the game drew it, which the copy is
        /// checked against while the game keeps talking.
        /// </summary>
        public void SetLine(string speaker, string text, string sourceLine)
        {
            _shownLineKey = DialogueOverlayLineCheck.KeyOf(sourceLine);
            var named = (speaker ?? string.Empty).Trim();
            _speakerText = named.TrimEnd(':');
            _lineText = text ?? string.Empty;

            // Taken out wherever it stands rather than only at the front: the
            // marker for a machine translation is put before it, and testing
            // the start left the name both on the plate and in the sentence.
            if (named.Length > 0)
            {
                var at = _lineText.IndexOf(named, StringComparison.Ordinal);
                if (at >= 0)
                {
                    _lineText = (_lineText.Substring(0, at) + _lineText.Substring(at + named.Length)).Trim();
                }
            }

            _speaker.Text = _speakerText;
            _speaker.Visibility = _speakerText.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            _line.Text = _lineText;
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
            Visibility = Visibility.Hidden;
        }

        private void Follow()
        {
            if (!GameWindowLocator.TryLocate(_gameWindow(), out var projection))
            {
                HideCopy("no game window");
                return;
            }

            var bounds = _memoryReader.DialogueBounds;
            var foreground = _memoryReader.IsGameWindowForeground;

            // Away from the game, the copy comes off the screen but keeps what
            // it was showing. Treated as though the line had ended, alt-tabbing
            // away and back left the box blank until somebody said something
            // new - the line was still on screen underneath the whole time.
            if (!foreground)
            {
                HideCopy("hidden: the game is not in front");
                return;
            }

            var surface = _memoryReader.DialogueSurface;

            var placed = DialogueOverlayPlacement.TryPlace(
                true,
                true,
                surface,
                bounds,
                projection,
                _lineText,
                out var rect);

            var now = DateTime.UtcNow;
            if (!_hold.Decide(placed, rect, surface, now, out rect, out var drawnSurface))
            {
                HideCopy(FormattableString.Invariant(
                    $"hidden: foreground={foreground} boundsKnown={bounds.IsKnown} box={bounds.Width}x{bounds.Height} lineChars={_lineText.Length}"));

                // The conversation is over, so the line it ended on is not the
                // line anything says next. Held on to, it was showing through
                // the opening moment of the following conversation, before that
                // one's translation had come back - the last thing an NPC said
                // put in the mouth of the next one.
                Forget();
                return;
            }

            // The translation is a touch behind the game, and in that touch the
            // game moves on: another box, another line. The line read off the
            // screen is the only judge of which conversation the copy is in,
            // so when the game has drawn another one, the copy comes off.
            if (!DialogueOverlayLineCheck.IsCurrent(_shownLineKey, _memoryReader.CurrentDialogueLine))
            {
                HideCopy("stale: the game has moved on to another line");
                return;
            }

            // The game closes its window by shrinking it, and a copy that
            // follows it down is a box that jumps smaller and then vanishes.
            // Growing is the other way about: that is the window opening, and
            // following it covers the original from the first frame rather than
            // leaving it readable for the length of the animation.
            if (!_motion.ShouldDraw(drawnSurface, rect.Width, now))
            {
                HideCopy("the box is closing");
                return;
            }

            Dress(drawnSurface, rect);

            Report(FormattableString.Invariant(
                $"shown on {drawnSurface} at {rect.Left},{rect.Top} {rect.Width}x{rect.Height}"));

            Left = rect.Left;
            Top = rect.Top;
            Width = rect.Width;
            Height = rect.Height;

            if (drawnSurface == DialogueSurface.Subtitle)
            {
                LayOutSubtitle(rect);
            }
            else
            {
                LayOutWindow(rect);
            }
        }

        /// <summary>
        /// Everything is set in the box's own proportions rather than at a
        /// fixed size: the player's interface scale is already in the
        /// rectangle, and text that ignored it would not fit the frame.
        ///
        /// The fractions are the game's own frame measured off a screenshot at
        /// an interface scale of 150%: the name sits at 0.083 across and 0.04
        /// down, the line starts at 0.088 across and 0.225 down.
        /// </summary>
        private void LayOutWindow(Rect rect)
        {
            _line.FontSize = Math.Max(10, rect.Height * 0.098);
            _speaker.FontSize = Math.Max(10, rect.Height * 0.092);

            // The strip is given room past the name for the fade to happen in.
            // Sized to the name alone it ended in a hard edge a few pixels
            // after the last letter - a dark tab, where the game has a wash.
            _plate.Margin = new Thickness(rect.Width * 0.083, rect.Height * 0.035, 0, 0);
            _plate.Padding = new Thickness(rect.Width * 0.012, rect.Height * 0.005, rect.Width * 0.14, 0);

            // Wide enough to bury the game's own name underneath, whatever it
            // says. A strip cut to the translated name left the English one
            // showing past it - two names side by side, which is worse than
            // either alone.
            _plate.MinWidth = rect.Width * 0.30;
            _line.Margin = new Thickness(
                rect.Width * 0.088, rect.Height * 0.225, rect.Width * 0.075, rect.Height * 0.06);
        }

        /// <summary>
        /// A subtitle is a strip the width of the screen, about a hundred tall,
        /// with the line centred in it. The window's proportions are no use
        /// here: a tenth of a hundred-pixel strip is ten-point text, where the
        /// game draws something a good deal larger, and read against the
        /// fractions meant for the dialogue box the copy came out as a caption
        /// nobody could read.
        ///
        /// Measured off the strip's height rather than its width, which is the
        /// whole screen and says nothing about how big the line is drawn.
        /// </summary>
        private void LayOutSubtitle(Rect rect)
        {
            _line.FontSize = Math.Max(12, rect.Height * 0.26);
            _line.Margin = new Thickness(rect.Width * 0.10, 0, rect.Width * 0.10, 0);
        }

        /// <summary>
        /// Dresses the copy for what it is covering, and puts it on screen.
        ///
        /// A cutscene subtitle is not in a window at all - Hydaelyn's lines are
        /// bare text laid over the picture, centred, pale, with a dark edge so
        /// they read against anything. Putting the dialogue frame over one of
        /// those would hang a wooden box in the middle of a cutscene.
        ///
        /// The showing of it is asked of the presentation rather than decided
        /// here: the first line of a conversation used to go up undrawn,
        /// because being dressed for a line and being on screen were one
        /// question, and the first line's answer to it was "already dressed, so
        /// change nothing".
        /// </summary>
        private void Dress(DialogueSurface surface, Rect rect)
        {
            var mustShow = _presentation.Present(surface, out var restyled);

            if (restyled)
            {
                var subtitle = surface == DialogueSurface.Subtitle;

                // The dark ground is the whole reason a subtitle can be covered
                // at all. Left bare, as it was, the copy was pale text laid over
                // the game's own pale text: two lines in two languages in the
                // same place, and neither of them readable.
                _box.Background = subtitle ? _subtitleGround : _frame;
                _plate.Visibility = subtitle ? Visibility.Collapsed : Visibility.Visible;

                _line.TextAlignment = subtitle ? TextAlignment.Center : TextAlignment.Left;
                _line.VerticalAlignment = subtitle ? VerticalAlignment.Center : VerticalAlignment.Top;
                _line.Foreground = subtitle
                    ? Brushes.White
                    : new SolidColorBrush(Color.FromRgb(0x2A, 0x24, 0x1C));

                // The game outlines its subtitles rather than shadowing them, and
                // over a bright sky an unoutlined white line is unreadable.
                _line.Effect = subtitle
                    ? new DropShadowEffect
                    {
                        Color = Colors.Black,
                        BlurRadius = 6,
                        ShadowDepth = 0,
                        Opacity = 1
                    }
                    : null;
            }

            if (mustShow)
            {
                Show();
                Visibility = Visibility.Visible;
                MakeClickThrough();
            }
        }

        /// <summary>
        /// Takes the copy off the screen and says in the raw-dialog log why,
        /// once per change rather than twenty times a second.
        /// </summary>
        private void HideCopy(string reason)
        {
            Report(reason);
            _presentation.Hide();
            Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Drops the line and what was learned about the box it was in. Both
        /// belong to the conversation that has just ended: the next one starts
        /// blank and fills when its own translation arrives, and the width it
        /// opens to is measured again rather than assumed to match.
        /// </summary>
        private void Forget()
        {
            _lineText = string.Empty;
            _speakerText = string.Empty;
            _line.Text = string.Empty;
            _speaker.Text = string.Empty;
            _motion.Forget();
            _shownLineKey = string.Empty;
        }

        private string _lastReport = string.Empty;

        /// <summary>
        /// Says why the copy is or is not on screen, once per change rather
        /// than twenty times a second. Watching it decide is the only way to
        /// tell "nothing to show" from "shown somewhere nobody is looking".
        /// </summary>
        private void Report(string state)
        {
            if (!Logger.RawDialogLogEnabled || string.Equals(state, _lastReport, StringComparison.Ordinal))
            {
                return;
            }

            _lastReport = state;
            Logger.WriteRawDialogLog("DialogueOverlay " + state);
        }

        /// <summary>
        /// Lets the mouse through to the game. Without this the copy swallows
        /// clicks over the dialogue box, which is exactly where the player
        /// clicks to read on.
        /// </summary>
        private void MakeClickThrough()
        {
            var handle = new WindowInteropHelper(this).Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            try
            {
                var style = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
                SetWindowLongPtr(
                    handle,
                    GwlExStyle,
                    new IntPtr(style | WsExTransparent | WsExNoActivate | WsExToolWindow));
            }
            catch (EntryPointNotFoundException)
            {
                // Nothing worth stopping over: the copy still shows, it just
                // takes the clicks meant for the game underneath.
                Report("could not make the copy click-through");
            }
        }

        private const int GwlExStyle = -20;
        private const long WsExTransparent = 0x20;
        private const long WsExNoActivate = 0x08000000;
        private const long WsExToolWindow = 0x80;

        // The W suffix matters: 64-bit user32 exports no bare GetWindowLongPtr,
        // and asking for one throws the first time the copy is shown.
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr value);
    }
}
