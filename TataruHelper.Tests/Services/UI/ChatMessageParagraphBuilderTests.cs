using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Documents;
using System.Windows.Media;

using FFXIVTataruHelper;
using FFXIVTataruHelper.Compatibility.HotKeys;
using FFXIVTataruHelper.Services.GameMemory;
using FFXIVTataruHelper.Services.HotKeys;
using FFXIVTataruHelper.Services.Logging;
using FFXIVTataruHelper.Services.UI;
using FFXIVTataruHelper.ViewModel;

using NUnit.Framework;

using Translation.Models;

namespace TataruHelper.Tests.Services.UI
{
    /// <summary>
    /// Only the speaker is drawn in bold, and who that is comes from upstream.
    ///
    /// It used to be found again here, by taking whatever stood before the first
    /// colon. Hydaelyn's "Ради всех умоляю Тебя: избавь нас от этой участи!" is a
    /// subtitle with no speaker at all, and half of it was rendered as a name.
    /// The English it was translated from carries a comma, which is why the same
    /// line was judged correctly before it was translated and wrongly after.
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ChatMessageParagraphBuilderTests
    {
        private static readonly Color White = Color.FromArgb(255, 255, 255, 255);

        private static (string Text, bool Bold)[] RunsOf(Paragraph paragraph)
        {
            return paragraph.Inlines.OfType<Run>()
                .Select(run => (run.Text, run.FontWeight.ToOpenTypeWeight() >= 700))
                .ToArray();
        }

        private static ChatMessageParagraphBuilder CreateBuilder(out ChatWindowViewModel viewModel)
        {
            var settings = new ChatWindowViewModelSettings("1", 0);
            var languages = new List<TranslatorLanguage>
            {
                new("Auto", "Auto", "auto"), new("English", "English", "en")
            };
            settings.FromLanguague = languages[0];
            settings.ToLanguague = languages[1];

            var engines = new List<TranslationEngine>
            {
                new(TranslationEngineName.GoogleTranslate, languages, 1.0)
            };

            var chatCodes = new List<ChatMsgType>
            {
                new("0039", MsgType.Translate, "System", White)
            };

            var logger = new NullAppLogger();

            viewModel = new ChatWindowViewModel(
                settings,
                engines,
                null,
                chatCodes,
                new HotKeyManager(null),
                logger,
                new HotKeyBindingService(logger));

            viewModel.SpacingCount = 0;
            viewModel.MessagesInContainer = false;

            // No game to read pictures out of here, which is the ordinary case
            // for a line that carries none.
            return new ChatMessageParagraphBuilder(viewModel, null);
        }

        [Test]
        public void ASubtitleWithAColonInIt_HasNothingInBold()
        {
            var builder = CreateBuilder(out var viewModel);
            try
            {
                var line = "Ради всех умоляю Тебя: избавь нас от этой участи!";

                var runs = RunsOf(builder.BuildMessageParagraph(line, White, string.Empty, default));

                Assert.That(runs.Any(run => run.Bold), Is.False,
                    "a line nobody is speaking should have no name in it");
                Assert.That(string.Concat(runs.Select(run => run.Text)), Is.EqualTo(line));
            }
            finally
            {
                viewModel.Dispose();
            }
        }

        [Test]
        public void ASpokenLine_HasTheSpeakerInBoldAndNothingElse()
        {
            var builder = CreateBuilder(out var viewModel);
            try
            {
                var runs = RunsOf(builder.BuildMessageParagraph(
                    "Naoh Gamduhla: Пусть духи стихий будут благосклонны к тебе.",
                    White, "Naoh Gamduhla:", default));

                Assert.That(runs.Where(run => run.Bold).Select(run => run.Text),
                    Is.EqualTo(new[] { "Naoh Gamduhla:" }));
                Assert.That(runs.Last().Text, Is.EqualTo(" Пусть духи стихий будут благосклонны к тебе."));
            }
            finally
            {
                viewModel.Dispose();
            }
        }

        /// <summary>
        /// A notice about the engine having changed is put in front of the line,
        /// so the name is not at the start - and the notice is not part of it.
        /// </summary>
        [Test]
        public void ANoticeBeforeTheName_IsNotDrawnAsPartOfIt()
        {
            var builder = CreateBuilder(out var viewModel);
            try
            {
                var runs = RunsOf(builder.BuildMessageParagraph(
                    "[Yandex answered instead] Naoh Gamduhla: Пусть духи будут благосклонны.",
                    White, "Naoh Gamduhla:", default));

                Assert.That(runs.Where(run => run.Bold).Select(run => run.Text),
                    Is.EqualTo(new[] { "Naoh Gamduhla:" }));
                Assert.That(runs.First().Text, Is.EqualTo("[Yandex answered instead] "));
            }
            finally
            {
                viewModel.Dispose();
            }
        }

        /// <summary>
        /// The speaker is what upstream said it was. A name that does not appear
        /// in the translated line - the engine dropped or reworded it - leaves
        /// the line alone rather than bolding something arbitrary.
        /// </summary>
        [Test]
        public void ASpeakerThatIsNotInTheLine_LeavesItAlone()
        {
            var builder = CreateBuilder(out var viewModel);
            try
            {
                var runs = RunsOf(builder.BuildMessageParagraph(
                    "Пусть духи стихий будут благосклонны к тебе.", White, "Naoh Gamduhla:", default));

                Assert.That(runs.Any(run => run.Bold), Is.False);
            }
            finally
            {
                viewModel.Dispose();
            }
        }

        private sealed class NullAppLogger : IAppLogger
        {
            public void WriteLog(string input, string memberName = "", int sourceLineNumber = 0)
            {
            }

            public void WriteLog(object input, string memberName = "", int sourceLineNumber = 0)
            {
            }

            public void WriteConsoleLog(string input)
            {
            }

            public void WriteChatLog(string input)
            {
            }
        }

        /// <summary>
        /// The flower between a player's name and the world they are from is a
        /// piece of the name, and it reaches the chat window as a character out
        /// of the private-use area.
        ///
        /// With no game to read the picture out of, the character comes out and
        /// the words close over it - a hole reads better than an empty box, and
        /// it is where this application was before it drew any pictures at all.
        /// </summary>
        [Test]
        public void WithNoPictureToBeHad_TheMarkComesOutAndTheWordsRemain()
        {
            var said = "Cova Rae" + GameIcons.Mark(CrossWorldNames.CrossWorldIcon) + "Louisoix машет рукой.";

            var runs = RunsOf(CreateBuilder(out _).BuildMessageParagraph(said, White, string.Empty, default));

            Assert.That(string.Concat(runs.Select(r => r.Text)),
                Is.EqualTo("Cova RaeLouisoix машет рукой."));
        }
    }
}
