using System;

using NUnit.Framework;

using Translation.Exceptions;
using Translation.Http;
using Translation.Models;

namespace Translation.Tests.Http
{
    [TestFixture]
    public class EngineRateLimitGuardTests
    {
        [Test]
        public void BeforeAnyRefusal_NothingIsHeldBack()
        {
            var guard = new EngineRateLimitGuard(TranslationEngineName.Papago, TimeSpan.FromSeconds(30));

            Assert.DoesNotThrow(() => guard.ThrowIfPaused());
        }

        [Test]
        public void AfterARefusal_TheNextRequestsAreStoppedBeforeTheNetwork()
        {
            var guard = new EngineRateLimitGuard(TranslationEngineName.Papago, TimeSpan.FromSeconds(30));

            var refusal = guard.Refused();

            Assert.That(refusal.EngineName, Is.EqualTo(TranslationEngineName.Papago));
            var held = Assert.Throws<QuotaExceededException>(() => guard.ThrowIfPaused());
            Assert.That(held.EngineName, Is.EqualTo(TranslationEngineName.Papago));
        }

        [Test]
        public void WhenThePauseIsOver_RequestsGoThroughAgain()
        {
            var guard = new EngineRateLimitGuard(TranslationEngineName.Papago, TimeSpan.Zero);

            guard.Refused();

            Assert.DoesNotThrow(() => guard.ThrowIfPaused());
        }
    }
}
