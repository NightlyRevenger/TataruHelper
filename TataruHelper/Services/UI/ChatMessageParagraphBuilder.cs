using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

using FFXIVTataruHelper.ViewModel;

using FFXIVTataruHelper.Services.GameMemory;

namespace FFXIVTataruHelper.Services.UI
{
    public sealed class ChatMessageParagraphBuilder
    {
        private readonly ChatWindowViewModel _viewModel;

        public ChatMessageParagraphBuilder(ChatWindowViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public Paragraph BuildMessageParagraph(
            string translatedMsg, Color color, string speaker, DateTime timeStamp)
        {
            // The game's icons are carried through the application as
            // characters out of the private-use area, and they are for the copy
            // drawn over the game's own dialogue box, which can draw pictures.
            // Here they would be empty boxes, so here they come out.
            translatedMsg = GameIcons.Strip(translatedMsg);

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

            if (!string.IsNullOrEmpty(leadingSpaces))
            {
                paragraph.Inlines.Add(CreateRun(leadingSpaces, color, FontWeights.Normal));
            }

            if (!string.IsNullOrEmpty(prefix))
            {
                paragraph.Inlines.Add(CreateRun(prefix, color, FontWeights.Normal));
            }

            if (!string.IsNullOrEmpty(name))
            {
                paragraph.Inlines.Add(CreateRun(name, color, FontWeights.Bold));
            }

            paragraph.Inlines.Add(CreateRun(text, color, FontWeights.Normal));
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

            if (!string.IsNullOrEmpty(leadingSpaces))
            {
                messageText.Inlines.Add(new Run(leadingSpaces));
            }

            if (!string.IsNullOrEmpty(prefix))
            {
                messageText.Inlines.Add(new Run(prefix));
            }

            if (!string.IsNullOrEmpty(name))
            {
                messageText.Inlines.Add(new Run(name) { FontWeight = FontWeights.Bold });
            }

            messageText.Inlines.Add(new Run(text));

            var messageBorder = new Border { CornerRadius = new CornerRadius(6), Tag = color, Child = messageText };
            ApplyMessageContainerVisual(messageBorder);

            var paragraph = new Paragraph
            {
                Margin = new Thickness(0, _viewModel.LineBreakHeight, 0, 0), TextAlignment = TextAlignment.Left
            };

            paragraph.Inlines.Add(new InlineUIContainer(messageBorder));
            return paragraph;
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