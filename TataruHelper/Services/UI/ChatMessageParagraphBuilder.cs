using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

using FFXIVTataruHelper.Services.GameMemory;
using FFXIVTataruHelper.ViewModel;

namespace FFXIVTataruHelper.Services.UI
{
    public sealed class ChatMessageParagraphBuilder
    {
        private readonly ChatWindowViewModel _viewModel;

        /// <summary>
        /// The little pictures the game puts in a line, read out of the
        /// player's own game. Only one of them reaches the chat window: the
        /// flower the game draws between a player's name and the world they
        /// are from, which is a piece of the name and not decoration.
        /// </summary>
        private readonly GameIconReader _icons;

        public ChatMessageParagraphBuilder(ChatWindowViewModel viewModel, GameIconReader icons)
        {
            _viewModel = viewModel;
            _icons = icons;
        }

        public Paragraph BuildMessageParagraph(
            string translatedMsg, Color color, string speaker, DateTime timeStamp)
        {
            string leadingSpaces = _viewModel.SpacingCount > 0
                ? new string(' ', _viewModel.SpacingCount)
                : string.Empty;

            string prefix = string.Empty;
            string name = null;
            string text = translatedMsg;

            // Who is speaking was settled upstream, where the line still read as
            // the game wrote it. Looking for it again here - by taking whatever
            // stands before the first colon - put half a sentence in bold:
            // Hydaelyn's "Ради всех умоляю Тебя: избавь нас от этой участи!" has
            // no speaker at all, and no reading of the Russian can tell that
            // colon from the one after a name. The English it was translated
            // from has a comma, which is why the same line passes upstream and
            // failed here.
            //
            // Found rather than assumed to be at the front: a notice about the
            // engine having changed is put before the line.
            var nameStart = FindSpeaker(translatedMsg, speaker);
            if (nameStart >= 0)
            {
                prefix = translatedMsg.Substring(0, nameStart);
                name = translatedMsg.Substring(nameStart, speaker.Length);
                text = translatedMsg.Substring(nameStart + speaker.Length);
            }

            if (timeStamp != default(DateTime))
            {
                var stamp = timeStamp.ToString("HH:mm") + " ";

                if (prefix.Length > 0)
                {
                    prefix = stamp + prefix;
                }
                else if (!string.IsNullOrEmpty(name))
                {
                    name = stamp + name;
                }
                else
                {
                    text = stamp + text;
                }
            }

            if (_viewModel.MessagesInContainer)
            {
                return BuildContainedMessageParagraph(leadingSpaces, prefix, name, text, color);
            }

            return BuildPlainMessageParagraph(leadingSpaces, prefix, name, text, color);
        }

        /// <summary>
        /// Where the speaker stands in the line, or -1 when nobody is speaking.
        ///
        /// The name is not always at the front - a notice about the engine
        /// having changed is put before it - so it is searched for. Only the
        /// name itself is bold; whatever precedes it is left as it reads.
        /// </summary>
        private static int FindSpeaker(string translatedMsg, string speaker)
        {
            if (string.IsNullOrEmpty(speaker) || string.IsNullOrEmpty(translatedMsg))
            {
                return -1;
            }

            return translatedMsg.IndexOf(speaker, StringComparison.Ordinal);
        }

        public void ApplyMessageContainerVisual(Border border)
        {
            if (border == null)
            {
                return;
            }

            var baseColor = border.Tag is Color color ? color : Colors.White;
            var backgroundAlpha = (byte)Math.Clamp(_viewModel.MessageContainerAlpha, 0, 255);
            var borderAlpha = (byte)Math.Clamp(_viewModel.MessageContainerBorderAlpha, 0, 255);

            border.Padding = new Thickness(_viewModel.MessageContainerPadding);
            border.Background = new SolidColorBrush(
                Color.FromArgb(backgroundAlpha, baseColor.R, baseColor.G, baseColor.B));
            border.BorderThickness = new Thickness(_viewModel.MessageContainerBorderThickness);
            border.BorderBrush = new SolidColorBrush(
                Color.FromArgb(borderAlpha, baseColor.R, baseColor.G, baseColor.B));
        }

        private Paragraph BuildPlainMessageParagraph(
            string leadingSpaces, string prefix, string name, string text, Color color)
        {
            var paragraph = new Paragraph
            {
                Margin = new Thickness(0, _viewModel.LineBreakHeight, 0, 0), TextAlignment = TextAlignment.Left
            };

            AddWords(paragraph.Inlines, leadingSpaces, words => CreateRun(words, color, FontWeights.Normal));
            AddWords(paragraph.Inlines, prefix, words => CreateRun(words, color, FontWeights.Normal));
            AddWords(paragraph.Inlines, name, words => CreateRun(words, color, FontWeights.Bold));
            AddWords(paragraph.Inlines, text, words => CreateRun(words, color, FontWeights.Normal));
            return paragraph;
        }

        private Paragraph BuildContainedMessageParagraph(
            string leadingSpaces, string prefix, string name, string text, Color color)
        {
            var messageText = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontFamily = _viewModel.ChatFont,
                FontSize = _viewModel.ChatFontSize,
                Foreground = new SolidColorBrush(color)
            };

            AddWords(messageText.Inlines, leadingSpaces, words => new Run(words));
            AddWords(messageText.Inlines, prefix, words => new Run(words));
            AddWords(messageText.Inlines, name, words => new Run(words) { FontWeight = FontWeights.Bold });
            AddWords(messageText.Inlines, text, words => new Run(words));

            var messageBorder = new Border { CornerRadius = new CornerRadius(6), Tag = color, Child = messageText };
            ApplyMessageContainerVisual(messageBorder);

            var paragraph = new Paragraph
            {
                Margin = new Thickness(0, _viewModel.LineBreakHeight, 0, 0), TextAlignment = TextAlignment.Left
            };

            paragraph.Inlines.Add(new InlineUIContainer(messageBorder));
            return paragraph;
        }

        /// <summary>
        /// Puts a piece of a line into the paragraph, drawing the game's own
        /// pictures where the line carries them rather than leaving a hole.
        ///
        /// Only the flower between a name and a world turns up here, and it is
        /// part of a name: without it "Cova Rae" and "Louisoix" run together
        /// into one word that is neither. A picture that cannot be had is left
        /// out, and the line reads on as it did before any were drawn.
        /// </summary>
        private void AddWords(InlineCollection into, string words, Func<string, Inline> asText)
        {
            if (string.IsNullOrEmpty(words))
            {
                return;
            }

            var from = 0;

            for (var at = 0; at < words.Length; at++)
            {
                if (!GameIcons.IsMark(words[at]))
                {
                    continue;
                }

                var picture = _icons?.Icon(GameIcons.IdOf(words[at]));
                if (picture == null)
                {
                    continue;
                }

                if (at > from)
                {
                    into.Add(asText(GameIcons.Strip(words.Substring(from, at - from))));
                }

                into.Add(Draw(picture));
                from = at + 1;
            }

            if (from < words.Length)
            {
                into.Add(asText(GameIcons.Strip(words.Substring(from))));
            }
        }

        /// <summary>
        /// One of the game's pictures set among the words, sized off the text
        /// rather than drawn at the twenty pixels it is stored at.
        /// </summary>
        private InlineUIContainer Draw(ImageSource picture)
        {
            var height = Math.Max(8, _viewModel.ChatFontSize * 1.05);

            return new InlineUIContainer(new Image
            {
                Source = picture,
                Height = height,
                Width = height * (picture.Width / Math.Max(picture.Height, 1)),
                Stretch = Stretch.Uniform,
                Margin = new Thickness(1, 0, 1, -height * 0.15)
            })
            {
                BaselineAlignment = BaselineAlignment.Baseline
            };
        }

        private Run CreateRun(string text, Color color, FontWeight fontWeight)
        {
            return new Run(text)
            {
                Foreground = new SolidColorBrush(color),
                FontWeight = fontWeight,
                FontFamily = _viewModel.ChatFont,
                FontSize = _viewModel.ChatFontSize
            };
        }
    }
}