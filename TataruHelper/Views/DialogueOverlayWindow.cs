using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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

        /// <summary>
        /// The little pictures a line can have in the middle of it, read out of
        /// the player's own game. This is the only place in the application
        /// that draws them: everywhere else they are taken out of the text.
        /// </summary>
        private readonly GameIconReader _icons;
        private readonly DispatcherTimer _timer;

        private readonly TextBlock _speaker;
        private readonly TextBlock _line;
        private readonly Border _box;
        private readonly Border _plate;
        private readonly Canvas _choices;

        /// <summary>
        /// The answers as they are on screen now, in the order the game lists
        /// them, so the one under the cursor can be marked.
        /// </summary>
        private readonly List<TextBlock> _answers = new List<TextBlock>();

        private string _shownChoice = string.Empty;

        private int _markedAnswer = -1;
        private readonly ImageBrush _frame;

        /// <summary>The dark ground a subtitle is laid on, which the game gives it none of.</summary>
        private readonly Brush _subtitleGround;

        /// <summary>And the one a cutscene's question is laid on, which buries the game's own.</summary>
        private readonly Brush _choiceGround;

        /// <summary>
        /// The dark panel the game lays a notice on, in place of the wooden
        /// frame. Drawn to measurements taken off a running client rather than
        /// cut out of it: the game's panel is translucent, and a translucent
        /// copy would let the original read through it - so this one is a
        /// solid shape in the panel's own outline and colour.
        /// </summary>
        private readonly ImageBrush _noticeFrame;

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

        /// <summary>
        /// The words currently in front of the reader. Kept because the line is
        /// no longer one piece of text that can be compared against the block
        /// showing it - it may be text, a picture and more text.
        /// </summary>
        private string _shownWords;

        /// <summary>
        /// The line with the name still in it.
        ///
        /// Whoever is speaking is taken out of the line and put on the plate,
        /// which is right for a dialogue box and wrong everywhere the plate is
        /// not drawn - and what stands before a colon is not always a name. The
        /// game's notice about mentors begins "Mentor symbols are as follows:",
        /// which was read as a speaker, put on a plate nobody draws, and lost:
        /// the copy showed the three lines under it and not the line that says
        /// what they are.
        /// </summary>
        private string _wholeText = string.Empty;

        public DialogueOverlayWindow(IFFMemoryReaderService memoryReader, Func<IntPtr> gameWindow,
            GameIconReader icons)
        {
            _memoryReader = memoryReader;
            _gameWindow = gameWindow;
            _icons = icons;

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
            //
            // All but opaque where the words are. The copy sits on top of the
            // game's own line, and anything the ground lets through is that
            // line reading back out from under the translation.
            var subtitleGround = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0)
            };
            subtitleGround.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0x08, 0x08, 0x0A), 0));
            subtitleGround.GradientStops.Add(new GradientStop(Color.FromArgb(0xF4, 0x08, 0x08, 0x0A), 0.10));
            subtitleGround.GradientStops.Add(new GradientStop(Color.FromArgb(0xF4, 0x08, 0x08, 0x0A), 0.90));
            subtitleGround.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0x08, 0x08, 0x0A), 1));
            _subtitleGround = subtitleGround;

            // What a cutscene's question is laid on. The game gives it a dark
            // ground of its own, so this one has only to bury it - and at
            // anything less than opaque the English read faintly through the
            // Russian, which is how the question first came out.
            var choiceGround = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 0)
            };
            choiceGround.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0x06, 0x06, 0x08), 0));
            choiceGround.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x06, 0x06, 0x08), 0.06));
            choiceGround.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x06, 0x06, 0x08), 0.94));
            choiceGround.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0x06, 0x06, 0x08), 1));
            _choiceGround = choiceGround;

            _noticeFrame = new ImageBrush(
                new BitmapImage(new Uri("pack://application:,,,/Resources/NoticeFrame.png")))
            {
                Stretch = Stretch.Fill
            };

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

            // Where a question and its answers are laid out, each at the very
            // place the game draws its own. Kept apart from the box because
            // they are not one line but several, and each has to be found
            // again by the cursor.
            _choices = new Canvas { Visibility = Visibility.Collapsed };

            var layout = new Grid();
            layout.Children.Add(box);
            layout.Children.Add(_plate);
            layout.Children.Add(_choices);

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
            _wholeText = _lineText;

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

        }

        /// <summary>
        /// Puts a name and a line in front of the reader, if they are not
        /// already there. Asked on every sweep, so it has to be cheap when
        /// nothing has changed: setting the same text again re-measures and
        /// re-wraps it twenty times a second.
        /// </summary>
        private void ShowWords(string speaker, string line)
        {
            if (!string.Equals(_speaker.Text, speaker, StringComparison.Ordinal))
            {
                _speaker.Text = speaker;
                _speaker.Visibility = speaker.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            if (string.Equals(_shownWords, line, StringComparison.Ordinal))
            {
                return;
            }

            _shownWords = line;
            _line.Inlines.Clear();

            var from = 0;
            for (var at = 0; at < line.Length; at++)
            {
                if (!GameIcons.IsMark(line[at]))
                {
                    continue;
                }

                var picture = _icons?.Icon(GameIcons.IdOf(line[at]));
                if (picture == null)
                {
                    // No picture to be had. The mark still goes: a line reads
                    // better with a gap where a crown belongs than with an
                    // empty box drawn in its place.
                    continue;
                }

                if (at > from)
                {
                    _line.Inlines.Add(new Run(GameIcons.Strip(line.Substring(from, at - from))));
                }

                _line.Inlines.Add(Draw(picture));
                from = at + 1;
            }

            if (from < line.Length)
            {
                _line.Inlines.Add(new Run(GameIcons.Strip(line.Substring(from))));
            }
        }

        /// <summary>
        /// An icon set among the words.
        ///
        /// Sized off the text rather than drawn at the size it is stored -
        /// twenty pixels, which is right at one interface scale and wrong at
        /// every other - and dropped a little below the line, where the game
        /// sits it.
        /// </summary>
        private InlineUIContainer Draw(ImageSource picture)
        {
            var height = _line.FontSize * 1.15;
            return new InlineUIContainer(new Image
            {
                Source = picture,
                Height = height,
                Width = height * (picture.Width / Math.Max(picture.Height, 1)),
                Stretch = Stretch.Uniform,
                Margin = new Thickness(0, 0, 0, -height * 0.18)
            })
            {
                BaselineAlignment = BaselineAlignment.Baseline
            };
        }

        /// <summary>
        /// The words of a line without the name in front of them.
        ///
        /// Taken off the front by length rather than found by looking for a
        /// colon: the line is the name, a colon and the words, and plenty of
        /// lines have a colon of their own - "I'll say that again: your cares
        /// and your troubles" - which a search would cut at instead.
        /// </summary>
        internal static string WordsOf(string line, string speaker)
        {
            line = line ?? string.Empty;
            speaker = speaker ?? string.Empty;

            if (speaker.Length == 0 || line.Length <= speaker.Length + 1 ||
                !line.StartsWith(speaker, StringComparison.Ordinal) ||
                line[speaker.Length] != ':')
            {
                return line;
            }

            return line.Substring(speaker.Length + 1);
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

            // What the copy is to say. The translation, when the one that has
            // come back is for the line the game is drawing now - and the
            // game's own words until then.
            //
            // Waiting instead meant the original stayed readable underneath
            // for as long as the translator took, which on a line the index
            // does not have is around half a second. Showing the game's words
            // in the copy means the box goes up at once and the words change
            // in place when the translation lands; and if it never lands, the
            // reader is left with the line rather than with an empty box.
            _icons?.Follow(_memoryReader.GameExecutablePath);

            var asked = _memoryReader.CurrentChoice;
            var gameLine = _memoryReader.CurrentDialogueLine;
            var translated = gameLine.Length == 0 ||
                             DialogueOverlayLineCheck.IsCurrent(_shownLineKey, gameLine);

            var shownSpeaker = translated ? _speakerText : _memoryReader.CurrentDialogueSpeaker;
            var shownLine = translated ? _lineText : WordsOf(gameLine, _memoryReader.CurrentDialogueSpeaker);
            var whole = translated ? _wholeText : gameLine;

            var placed = DialogueOverlayPlacement.TryPlace(
                true,
                true,
                surface,
                bounds,
                projection,

                // The whole line, name and all: whether there is anything to
                // show does not depend on which part of it goes where, and a
                // notice whose every word was read as a name would otherwise
                // count as nothing to show.
                whole,
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

            // Only the dialogue box has a plate to put a name on. Without one
            // the name belongs back in the line it came out of, or it is simply
            // gone from the copy.
            if (drawnSurface != DialogueSurface.Window)
            {
                shownSpeaker = string.Empty;
                shownLine = whole;
            }

            if (drawnSurface == DialogueSurface.Choice)
            {
                // Whether the translation in hand is of this question, which
                // is not the same question as whether it is of the line the
                // Talk window is showing. Asked of the line the copy was put
                // out for and the question on screen, and of nothing else: a
                // conversation that leaves its last line up behind the
                // question would otherwise make the copy flicker on and off,
                // once for every sweep the line was there.
                var haveThisQuestion = string.Equals(
                    _shownLineKey,
                    DialogueOverlayLineCheck.KeyOf(asked.AsBlock()),
                    StringComparison.Ordinal);

                // A question the player has to answer is not shown in the
                // game's own words while a translation is on its way: that
                // would cover the game's mark of which answer the cursor is on
                // and put nothing at all in its place.
                if (!haveThisQuestion || !LayOutChoice(asked, _wholeText, rect, projection))
                {
                    HideCopy("a question, with no translation of it yet");
                    return;
                }

                // The one line the box would otherwise still be showing is
                // whatever was said before the question was put.
                ShowWords(string.Empty, string.Empty);
            }
            else
            {
                ForgetTheChoice();
                ShowWords(shownSpeaker, shownLine);
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
                // A notice is laid out like a dialogue box and not like
                // something of its own. Measured off the game: its text starts
                // at 0.094 across and 0.248 down, which is where a spoken line
                // starts too. The game draws both with the same node, and the
                // missing name above it moves nothing.
                LayOutWindow(rect);
            }
        }

        /// <summary>
        /// Lays a translated question and its answers over the game's own, each
        /// at the very place the game draws it.
        ///
        /// At the game's own places rather than in a list of this copy's own
        /// making, because the player is about to click one of them: an answer
        /// drawn anywhere but on the row it belongs to is an answer they would
        /// pick by mistake.
        ///
        /// False when the translation cannot be taken apart into as many
        /// answers as the game is offering. Then nothing is drawn and the
        /// question stays readable in the game's own words, which is the right
        /// way to be wrong here.
        /// </summary>
        private bool LayOutChoice(GameChoice asked, string translated, Rect rect, GameWindowProjection projection)
        {
            if (!asked.IsBeingAsked ||
                asked.AnswerBounds.Count != asked.Answers.Count ||
                !GameChoice.TryReadBlock(translated, asked.Answers.Count, out var question, out var answers))
            {
                return false;
            }

            var settled = string.Join("\n", answers) + "\n" + question + "\n" + rect;
            if (!string.Equals(_shownChoice, settled, StringComparison.Ordinal))
            {
                _choices.Children.Clear();
                _answers.Clear();
                _markedAnswer = -1;

                var size = Math.Max(12, rect.Height * 0.115);

                if (question.Length > 0 && Place(asked.QuestionBounds, rect, projection, out var where))
                {
                    _choices.Children.Add(Row(question, RoomToTheRight(where, rect), size, QuestionInk, false));
                }

                for (var i = 0; i < answers.Length; i++)
                {
                    if (!Place(asked.AnswerBounds[i], rect, projection, out var row))
                    {
                        ForgetTheChoice();
                        return false;
                    }

                    var drawn = Row(answers[i], RoomToTheRight(row, rect), size, AnswerInk, true);
                    _choices.Children.Add(drawn);
                    _answers.Add(drawn);
                }

                _shownChoice = settled;
            }

            _choices.Visibility = Visibility.Visible;
            MarkTheAnswerUnderTheCursor(asked, projection);
            return true;
        }

        /// <summary>
        /// The same row, given the rest of the strip to be long in.
        ///
        /// The game sizes each of its rows to the English in it, and the
        /// Russian is longer - "What will you say?" came out as "Что ты
        /// ска...", cut off inside a strip with nine hundred empty pixels to
        /// its right. Where a row starts is what matters, because that is what
        /// lines it up with the answer beneath it; where it ends is only the
        /// English having been shorter.
        /// </summary>
        private static Rect RoomToTheRight(Rect row, Rect strip)
        {
            var room = Math.Max(row.Width, strip.Width - row.Left - strip.Width * 0.03);
            return new Rect(row.Left, row.Top, room, row.Height);
        }

        private void ForgetTheChoice()
        {
            if (_shownChoice.Length == 0 && _choices.Visibility == Visibility.Collapsed)
            {
                return;
            }

            _choices.Visibility = Visibility.Collapsed;
            _choices.Children.Clear();
            _answers.Clear();
            _shownChoice = string.Empty;
            _markedAnswer = -1;
        }

        /// <summary>
        /// Where one of the game's own rows lands inside the copy, which is
        /// placed over the whole strip.
        /// </summary>
        private static bool Place(AddonBounds bounds, Rect strip, GameWindowProjection projection, out Rect placed)
        {
            placed = Rect.Empty;
            if (!projection.TryProject(bounds, out var onScreen))
            {
                return false;
            }

            placed = new Rect(
                onScreen.Left - strip.Left, onScreen.Top - strip.Top, onScreen.Width, onScreen.Height);
            return true;
        }

        private static TextBlock Row(string words, Rect where, double fontSize, Brush ink, bool isAnswer)
        {
            var block = new TextBlock
            {
                Text = words,
                FontSize = fontSize,
                Foreground = ink,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Width = Math.Max(where.Width, 1),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black, BlurRadius = 5, ShadowDepth = 0, Opacity = 1
                }
            };

            if (isAnswer)
            {
                block.FontWeight = FontWeights.SemiBold;
            }

            Canvas.SetLeft(block, where.Left);
            Canvas.SetTop(block, where.Top + Math.Max(0, (where.Height - fontSize * 1.35) / 2));
            return block;
        }

        /// <summary>
        /// Marks the answer the cursor is over, the way the game marks its own.
        ///
        /// Worked out from where the cursor is rather than read out of the
        /// game: the rows are known, the cursor is known, and the game marks
        /// whatever is under it - so the same answer comes out without reading
        /// anything more. Somebody choosing with a controller moves the game's
        /// mark without moving the cursor, and this does not follow that.
        /// </summary>
        private void MarkTheAnswerUnderTheCursor(GameChoice asked, GameWindowProjection projection)
        {
            var under = -1;
            if (TryFindTheCursor(out var cursor))
            {
                for (var i = 0; i < _answers.Count && i < asked.AnswerBounds.Count; i++)
                {
                    if (projection.TryProject(asked.AnswerBounds[i], out var row) &&
                        cursor.X >= row.Left && cursor.X <= row.Right &&
                        cursor.Y >= row.Top && cursor.Y <= row.Bottom)
                    {
                        under = i;
                        break;
                    }
                }
            }

            if (under == _markedAnswer)
            {
                return;
            }

            _markedAnswer = under;
            for (var i = 0; i < _answers.Count; i++)
            {
                _answers[i].Foreground = i == under ? MarkedInk : AnswerInk;
            }
        }

        /// <summary>
        /// Where the cursor is, in the units the copy is placed in. The system
        /// answers in real pixels and everything here counts in units that are
        /// pixels only at 100%, which is the same division the rest of the
        /// placing makes.
        /// </summary>
        private static bool TryFindTheCursor(out Point position)
        {
            position = default;
            if (!GetCursorPos(out var point))
            {
                return false;
            }

            var source = PresentationSource.FromVisual(Application.Current?.MainWindow);
            var scale = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            if (!(scale > 0))
            {
                scale = 1.0;
            }

            position = new Point(point.X / scale, point.Y / scale);
            return true;
        }

        private static readonly Brush QuestionInk = new SolidColorBrush(Color.FromRgb(0xC9, 0xC2, 0xB0));

        private static readonly Brush AnswerInk = Brushes.White;

        /// <summary>What the game turns an answer when the cursor is on it.</summary>
        private static readonly Brush MarkedInk = new SolidColorBrush(Color.FromRgb(0xFF, 0xE0, 0x7A));

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out NativePoint point);

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
                var notice = surface == DialogueSurface.Notice;


                // The dark ground is the whole reason a subtitle can be covered
                // at all. Left bare, as it was, the copy was pale text laid over
                // the game's own pale text: two lines in two languages in the
                // same place, and neither of them readable.
                var choice = surface == DialogueSurface.Choice;

                _box.Background = choice ? _choiceGround
                    : subtitle ? _subtitleGround
                    : notice ? _noticeFrame
                    : _frame;

                // Nobody is speaking a notice, and a cutscene subtitle names
                // nobody either.
                _plate.Visibility = subtitle || notice || choice ? Visibility.Collapsed : Visibility.Visible;

                _line.TextAlignment = subtitle ? TextAlignment.Center : TextAlignment.Left;
                _line.VerticalAlignment = subtitle ? VerticalAlignment.Center : VerticalAlignment.Top;

                // Dark ink on the wooden frame, pale on anything dark.
                _line.Foreground = subtitle || notice
                    ? Brushes.White
                    : new SolidColorBrush(Color.FromRgb(0x2A, 0x24, 0x1C));

                // The game outlines its subtitles rather than shadowing them, and
                // over a bright sky an unoutlined white line is unreadable. A
                // notice has its own ground and needs none of that.
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
            _line.Inlines.Clear();
            _shownWords = null;
            _wholeText = string.Empty;
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
