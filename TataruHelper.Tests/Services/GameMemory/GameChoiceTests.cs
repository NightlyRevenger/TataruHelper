using FFXIVTataruHelper.Services.GameMemory;

using NUnit.Framework;

namespace TataruHelper.Tests.Services.GameMemory
{
    /// <summary>
    /// The question a cutscene asks and the answers it offers, on their way to
    /// a translator and back.
    ///
    /// Read off a live client on 8 September, from the strip at the foot of the
    /// screen: "What will you say?", and two answers.
    /// </summary>
    [TestFixture]
    public class GameChoiceTests
    {
        private static GameChoice Asked() => new GameChoice(
            "What will you say?",
            AddonBounds.From(688, 808, 172, 32, 1f),
            new[] { "I thought you were always watching?", "Miss that part, did you?" },
            new[] { AddonBounds.From(688, 858, 512, 56, 1f), AddonBounds.From(688, 914, 512, 56, 1f) });

        [Test]
        public void TheWholeQuestionGoesToTheTranslatorAtOnce()
        {
            Assert.That(Asked().AsBlock(), Is.EqualTo(
                "What will you say?\n1. I thought you were always watching?\n2. Miss that part, did you?"));
        }

        /// <summary>
        /// What Yandex handed back for the mentor lines kept the numbering and
        /// the line breaks, which is what this leans on.
        /// </summary>
        [Test]
        public void ATranslatedBlockComesBackApart()
        {
            const string translated =
                "Что ты скажешь?\n1. Я думала, ты всегда смотришь?\n2. Пропустил эту часть, да?";

            Assert.That(GameChoice.TryReadBlock(translated, 2, out var question, out var answers), Is.True);
            Assert.That(question, Is.EqualTo("Что ты скажешь?"));
            Assert.That(answers[0], Is.EqualTo("Я думала, ты всегда смотришь?"));
            Assert.That(answers[1], Is.EqualTo("Пропустил эту часть, да?"));
        }

        /// <summary>
        /// An engine may hand the answers back in another order. The number
        /// says which is which, not the order they arrive in.
        /// </summary>
        [Test]
        public void TheNumberSaysWhichAnswerItIs()
        {
            Assert.That(GameChoice.TryReadBlock("Вопрос\n2. Второй\n1. Первый", 2, out _, out var answers), Is.True);
            Assert.That(answers[0], Is.EqualTo("Первый"));
            Assert.That(answers[1], Is.EqualTo("Второй"));
        }

        [TestCase("1) Первый\n2) Второй")]
        [TestCase("1 Первый\n2 Второй")]
        [TestCase("1.  Первый\n2.  Второй")]
        public void EnginesAreNotFussyAboutHowTheyNumber(string block)
        {
            Assert.That(GameChoice.TryReadBlock(block, 2, out _, out var answers), Is.True);
            Assert.That(answers[0], Is.EqualTo("Первый"));
        }

        /// <summary>
        /// Everything before the first number is the question, however many
        /// lines an engine has spread it over.
        /// </summary>
        [Test]
        public void WhateverComesBeforeTheFirstNumberIsTheQuestion()
        {
            GameChoice.TryReadBlock("Что ты\nскажешь?\n1. Первый\n2. Второй", 2, out var question, out _);

            Assert.That(question, Is.EqualTo("Что ты скажешь?"));
        }

        /// <summary>
        /// When the numbers do not add up, nothing comes back. An answer shown
        /// against the wrong one of the game's own rows is worse than no
        /// translation, because the player would click it.
        /// </summary>
        [TestCase("Вопрос\n1. Только один")]
        [TestCase("Вопрос без ответов")]
        [TestCase("Вопрос\n1. Первый\n1. Первый снова")]
        [TestCase("")]
        [TestCase(null)]
        public void WhenTheNumbersDoNotAddUp_NothingComesBack(string block)
        {
            Assert.That(GameChoice.TryReadBlock(block, 2, out _, out _), Is.False);
        }

        [Test]
        public void ALineThatIsOnlyANumber_IsNotAnAnswer()
        {
            Assert.That(GameChoice.TryReadBlock("Вопрос\n1.\n2. Второй", 2, out _, out _), Is.False);
        }

        /// <summary>
        /// Replayed from a cutscene on 9 September. The player picked an answer
        /// and the chat window showed it five times over, numbered: the game
        /// keeps spare rows in its list and they carry a copy of a neighbour's
        /// words for a sweep at a time. A block built from that is a different
        /// block, which is also what made the copy blink - the translation in
        /// hand stopped being a translation of it.
        /// </summary>
        [Test]
        public void TheSameAnswerFiveTimesIsStillOneAnswer()
        {
            const string first = "I thought you were always watching?";
            var spare = AddonBounds.From(688, 858, 512, 56, 1f);

            var asked = new GameChoice(
                "What will you say?",
                AddonBounds.From(688, 808, 172, 32, 1f),
                new[] { first, first, first, first, "Miss that part, did you?" },
                new[] { spare, spare, spare, spare, AddonBounds.From(688, 914, 512, 56, 1f) });

            // The reader takes the copies out before this; what is checked here
            // is what the block looks like when it has not, so the shape of the
            // fault is written down where it can be recognised again.
            Assert.That(asked.AsBlock().Split((char)10).Length, Is.EqualTo(6));
        }

        [Test]
        public void NothingBeingAsked_IsNotAChoice()
        {
            Assert.That(GameChoice.None.IsBeingAsked, Is.False);
            Assert.That(Asked().IsBeingAsked, Is.True);
        }
    }
}
